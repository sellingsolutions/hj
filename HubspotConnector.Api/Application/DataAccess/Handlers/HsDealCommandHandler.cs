using System;
using System.Linq;
using System.Threading.Tasks;
using HubspotConnector.Application.DataAccess.Services;
using HubspotConnector.Application.Dto;
using HubspotConnector.CrossCuttingConcerns;
using iSpectAPI.Core.Application.DataAccess.Clients.CouchbaseClients;
using iSpectAPI.Core.Application.DataAccess.Repositories;
using iSpectAPI.Core.Application.DataAccess.Repositories.Interfaces;
using iSpectAPI.Core.Application.Extensions;
using iSpectAPI.Core.Database.ActorModel.Actors;
using iSpectAPI.Core.Database.ActorModel.Actors.Groups;
using iSpectAPI.Core.Database.ActorModel.Commerce.Orders;
using iSpectAPI.Core.Database.HubspotConnector.Commands;
using iSpectAPI.Core.Database.Legacy;
using iSpectAPI.Core.Database.QueueItems.Notifications;
using iSpectAPI.Core.Database.QueueItems.Notifications.WarrantyReminders;
using Microsoft.Extensions.Options;
using Skarp.HubSpotClient;
using WeihanLi.Common.Event;

namespace HubspotConnector.Application.DataAccess.Handlers
{
    public class HsDealCommandHandler: EventHandlerBase<HsDealCommand>
    {
        private readonly ICBSClient _db;
        private readonly HsAppSettings _appSettings;
        private readonly IHubspotDealService _hubspotDealService;
        private readonly IGroupActorRepository _groupActorRepository;
        private readonly IActorRepository _actorRepository;
        private readonly IPartyRepository _partyRepository;
        public HsDealCommandHandler(
            ICBSClient db, 
            IOptions<HsAppSettings> appSettings,
            IHubspotDealService hubspotDealService,
            IGroupActorRepository groupActorRepository,
            IPartyRepository partyRepository)
        {
            _appSettings = appSettings.Value;
            _db = db;
            _hubspotDealService = hubspotDealService;
            _groupActorRepository = groupActorRepository;
            _partyRepository = partyRepository;
        }

        public override async Task Handle(HsDealCommand @event)
        {
            var notification = await _db.Get<IsNotification>(@event.ParentId);
            var order = await _db.Get<IsOrderHead>(@event.ParentId);
            var projectId = notification?.SourceDocumentId;

            if (notification == null)
                projectId = order.ProjectId;
            
            var project = await _db.Get<Project>(projectId);

            var client = await _db.Get<Client>(project.ClientId);
            var company = await _db.Get<IsCompany>(client.SubjectId);
            var salesGroupId = IsSalesGroup.CreateDocumentId(company.Id, IsDefaultGroupEnum.Sales.ToString());

            var sales = await _db.Get<IsSalesGroup>(salesGroupId);
            var salesReps = await sales.GetMembers(_db);
            var defaultRepPerson = salesReps.FirstOrDefault();

            if (@event is HsCreateDealCommand createDealCommand)
            {
                try
                {
                    if (order != null)
                    {
                        await HandleOrderDeal(order, project);
                    }
                    else if (notification is IsWarrantyReminderNotification warrantyReminder)
                    {
                        await HandleWarrantyReminderDeal(warrantyReminder, project, defaultRepPerson);
                    }
                }
                catch (HubSpotException hubSpotException)
                {
                    // Mark as processed if invalid domain, no use in retrying
                    if (hubSpotException.RawJsonResponse.Contains("INVALID_DOMAIN"))
                    {
                        return;
                    }

                    throw new HubSpotException(hubSpotException.Message, hubSpotException.InnerException);
                }
                
            }
        }

        async Task HandleOrderDeal(IsOrderHead order, Project project)
        {
            var dealOwner = await _db.Get<IsPerson>(order.OwnerId);
            var customer = await _db.Get<IsActor>(order.BuyerId);
            var customerParties = await _partyRepository.GetPartiesWithActorIds(new [] { order.BuyerId }, order.ProjectId);
            var customerPartyEmail = await customer.GetLatestEmail(nameof(IsActor.EmailAddressIds), _db);
            var inspections = await project.GetChildren<Inspection>(_db);
            var request = new HubspotDealRequest
            {
                Name = $"Order #{order.ReferenceId}",
                DealType = "existingbusiness",
                Pipeline = _appSettings.DefaultPipeline,
                DealStage = _appSettings.BookingsDealStage,
                DealOwner = dealOwner,
                CustomerParty = customerParties.FirstOrDefault(),
                Customer = customer,
                Project = project,
                Inspection = inspections.FirstOrDefault(),
                CloseDate = order.CreatedAt,
                CustomerEmail = customerPartyEmail,
                DealValue = order.Price
            };

            await _hubspotDealService.CreateDeal(request);
        }
        async Task HandleWarrantyReminderDeal(
            IsWarrantyReminderNotification warrantyReminder, 
            Project project, 
            IsPerson defaultRepPerson)
        {
            var isGb = warrantyReminder is IsGarantibesiktningReminderQueueItem;
            var name = isGb ? "GB" : "SÄB";
            
            var inspection = await _db.Get<Inspection>(warrantyReminder.InspectionId);
            var warrantyExpiresAt = isGb ? inspection.EndsAt.AddYears(2) : inspection.EndsAt.AddYears(5);
            var followUpDeadline = warrantyExpiresAt.AddMonths(-3);
            if (followUpDeadline < DateTime.UtcNow)
                followUpDeadline = DateTime.UtcNow;
            
            var customerParty = await _db.Get<Party>(warrantyReminder.PartyId);
            var customerPartyMembers = await customerParty.GetMembers(_db);
            var defaultPartyMember = customerPartyMembers.FirstOrDefault();
            var defaultPartyMemberEmail = "";
            if (defaultPartyMember != null)
                defaultPartyMemberEmail = await defaultPartyMember.GetLatestEmail(nameof(IsActor.EmailAddressIds), _db);
            
            var customer = await _db.Get<IsActor>(customerParty.SubjectId);
            if (customer == null)
            {
                var userContext = await inspection.GetUserContext(_db);
                var legacyContact = await _db.Get<Contact>(customerParty.CompanyContactId ?? customerParty.ContactId);
                if (customerParty.CompanyContactId.IsNotNullOrEmpty())
                {
                    customer = await IsCompany.CreateFromContact(legacyContact, userContext, _db);
                }
                if (customerParty.ContactId.IsNotNullOrEmpty())
                {
                    customer = await _db.Insert(typeof(IsPerson).CreateDocumentId(), new IsPerson
                    {
                        Channels = userContext != null ? new[] { userContext.ClientChannel } : Array.Empty<string>(),
                        FirstName = legacyContact.FirstName,
                        LastName = legacyContact.LastName,
                        TaxId = legacyContact.TaxId,
                        BillingAddressIds = legacyContact.BillingAddressIds,
                        AddressIds = legacyContact.AddressIds,
                        PhoneNumberIds = legacyContact.PhoneNumberIds,
                        EmailAddressIds = legacyContact.EmailAddressIds,
                        WebAddressIds = legacyContact.WebAddressIds,
                        ContactId = legacyContact.Id
                    });
                }
            }

            var propertyEntities = await _db.Get<PropertyEntity>(warrantyReminder.PropertyEntityIds);
            var noOfPropertyEntities = propertyEntities?.Count() ?? 0;
            var noOfApartmentBuildings = 0;
            var noOfApartments = 0;
            if (propertyEntities.IsNotNullOrEmpty())
            {
                noOfApartmentBuildings = propertyEntities.Count(o => o.PropertyType == "Flerfamiljshus");
                noOfApartments = propertyEntities.Count(o => o.PropertyType == "Lägenhet");
            }

            var dealValue = noOfApartments * 1900;
            if (noOfApartments == 0)
                dealValue = noOfPropertyEntities * 1500;

            var request = new HubspotDealRequest
            {
                Name = name,
                DealType = "existingbusiness",
                Pipeline = _appSettings.DefaultPipeline,
                DealStage = _appSettings.DefaultDealStage,
                DealOwner = defaultRepPerson,
                CustomerParty = customerParty,
                Customer = customer,
                Project = project,
                Inspection = inspection,
                CloseDate = followUpDeadline,
                CustomerEmail = defaultPartyMemberEmail,
                DealValue = dealValue,
                NoOfApartmentBuildings = noOfApartmentBuildings,
                NoOfApartments = noOfApartments
            };

            await _hubspotDealService.CreateDeal(request);
        }
    }
}