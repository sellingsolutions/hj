using System;
using System.Linq;
using System.Threading.Tasks;
using HubspotConnector.Application.DataAccess.Services;
using HubspotConnector.Application.Dto;
using HubspotConnector.CrossCuttingConcerns;
using iSpectAPI.Core.Application.DataAccess.Clients.CouchbaseClients;
using iSpectAPI.Core.Application.DataAccess.Repositories.Interfaces;
using iSpectAPI.Core.Application.Extensions;
using iSpectAPI.Core.Database.ActorModel.Actors;
using iSpectAPI.Core.Database.ActorModel.Actors.Groups;
using iSpectAPI.Core.Database.HubspotConnector.Commands;
using iSpectAPI.Core.Database.Legacy;
using iSpectAPI.Core.Database.QueueItems.Notifications;
using iSpectAPI.Core.Database.QueueItems.Notifications.WarrantyReminders;
using Microsoft.Extensions.Options;
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
        public HsDealCommandHandler(
            ICBSClient db, 
            IOptions<HsAppSettings> appSettings,
            IHubspotDealService hubspotDealService,
            IGroupActorRepository groupActorRepository)
        {
            _appSettings = appSettings.Value;
            _db = db;
            _hubspotDealService = hubspotDealService;
            _groupActorRepository = groupActorRepository;
        }

        public override async Task Handle(HsDealCommand @event)
        {
            var notification = await _db.Get<IsNotification>(@event.ParentId);
            var project = await _db.Get<Project>(@event.SourceDocumentId);

            var client = await _db.Get<Client>(project.ClientId);
            var company = await _db.Get<IsCompany>(client.SubjectId);
            var salesGroupId = IsSalesGroup.CreateDocumentId(company.Id, IsDefaultGroupEnum.Sales.ToString());

            var sales = await _db.Get<IsSalesGroup>(salesGroupId);
            var salesReps = await sales.GetMembers(_db);
            var defaultRepPerson = salesReps.FirstOrDefault();

            if (@event is HsCreateDealCommand createDealCommand)
            {
                if (notification is IsWarrantyReminderNotification warrantyReminder)
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
                        defaultPartyMemberEmail = await defaultPartyMember.GetLatestEmail(_db);
                    
                    var customer = await _db.Get<IsActor>(customerParty.SubjectId);

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
    }
}