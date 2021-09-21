using System.Threading.Tasks;
using HubspotConnector.Application.DataAccess.Repositories;
using HubspotConnector.Application.Dto;
using HubspotConnector.CrossCuttingConcerns;
using iSpectAPI.Core.Application.DataAccess.Clients.CouchbaseClients;
using iSpectAPI.Core.Application.Extensions;
using iSpectAPI.Core.Application.Extensions.Reflection;
using iSpectAPI.Core.Database.ActorModel.Actors;
using iSpectAPI.Core.Database.HubspotConnector.Associations;
using iSpectAPI.Core.Database.HubspotConnector.Deals;
using Microsoft.Extensions.Options;
using Skarp.HubSpotClient.Deal;
using Skarp.HubSpotClient.Deal.Dto;

namespace HubspotConnector.Application.DataAccess.Services
{
    public class HubspotDealService : IHubspotDealService
    {
        private readonly ICBSClient _db;
        private readonly HsAppSettings _appSettings;
        private readonly IHubspotOwnerRepository _hubspotOwnerRepository;
        private readonly IHubspotContactRepository _hubspotContactRepository;

        private HubSpotDealClient _hubSpotDealClient { get; set; }

        public HubspotDealService(
            IOptions<HsAppSettings> appSettings,
            ICBSClient db,
            IHubspotOwnerRepository hubspotOwnerRepository,
            IHubspotContactRepository hubspotContactRepository)
        {
            _db = db;
            _appSettings = appSettings.Value;
            _hubspotOwnerRepository = hubspotOwnerRepository;
            _hubspotContactRepository = hubspotContactRepository;
            
            _hubSpotDealClient = new HubSpotDealClient(_appSettings.ApiKey);
        }

        public async Task<HsDeal> CreateDeal(HubspotDealRequest request)
        {
            var ownerEmail = await request.DealOwner.GetLatestEmail(_db);
            var hsOwner = await _hubspotOwnerRepository.GetOwnerByEmail(ownerEmail);

            var customerContact = await _hubspotContactRepository.GetContactByEmail(request.CustomerEmail);
            if (customerContact == null && request.CustomerEmail.IsNotNullOrEmpty())
            {
                customerContact = await _hubspotContactRepository.CreateContact(request.CustomerEmail);
            }

            var contactCompany = await _hubspotContactRepository.GetContactCompany(customerContact?.Id ?? 0);
            if (contactCompany == null && request.Customer is IsCompany company)
                contactCompany = await _hubspotContactRepository.CreateCompany(company);
            if (contactCompany == null && request.CustomerParty.Company.IsNotNullOrEmpty())
                contactCompany = await _hubspotContactRepository.CreateCompany(request.CustomerParty.Company);

            var dealDto = new HubspotDealDto
            {
                OwnerId = hsOwner.Id,
                DealType = "existingbusiness",
                Name = $"#{request.Project.ReferenceId ?? ""} - {request.Project.Name} - {request.Name}",
                Amount = request.DealValue,
                Pipeline = _appSettings.DefaultPipeline,
                Stage = _appSettings.DefaultDealStage,
                CloseDate = $"{request.CloseDate.ToEpochMillis()}",
                NoOfApartments = request.NoOfApartments,
                NoOfApartmentBuildings = request.NoOfApartmentBuildings
            };

            if (contactCompany != null)
                dealDto.Associations.AssociatedCompany = new[] { contactCompany.Id ?? 0 };
            if (customerContact != null)
                dealDto.Associations.AssociatedContacts = new[] { customerContact.Id ?? 0 };

            var response = await _hubSpotDealClient.CreateAsync<DealHubSpotEntity>(dealDto);
            var hsId = response.Id ?? 0;

            var deal = await HsDeal.Ensure<HsDeal>(hsId, _db);
            deal.ProjectId = request.Project.Id;
            deal.OwnerId = hsOwner.Id;
            deal.PartyId = request.CustomerParty.Id;

            var dealAssociations = await HsAssociation.Ensure<HsDealAssociations>(hsId, _db);
            if (contactCompany?.Id != null)
                dealAssociations.CompanyIds = new[] { contactCompany.Id ?? 0 };

            if (customerContact?.Id != null)
                dealAssociations.ContactIds = new[] { customerContact.Id ?? 0 };

            dealDto.CopyPropsTo(deal);

            return await _db.Update(deal);
        }
    }
}