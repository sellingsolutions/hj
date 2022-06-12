using System.Linq;
using System.Threading.Tasks;
using HubspotConnector.CrossCuttingConcerns;
using iSpectAPI.Core.Application.DataAccess.Clients.CouchbaseClients;
using iSpectAPI.Core.Database.ActorModel.Actors;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Skarp.HubSpotClient;
using Skarp.HubSpotClient.Associations;
using Skarp.HubSpotClient.Company;
using Skarp.HubSpotClient.Company.Dto;
using Skarp.HubSpotClient.Company.Interfaces;
using Skarp.HubSpotClient.Contact;
using Skarp.HubSpotClient.Contact.Dto;
using Skarp.HubSpotClient.Contact.Interfaces;
using Skarp.HubSpotClient.Core;
using WeihanLi.Extensions;

namespace HubspotConnector.Application.DataAccess.Repositories
{
    
    public class HubspotContactRepository : IHubspotContactRepository
    {
        private readonly ICBSClient _db;
        private readonly ILogger<IHubspotContactRepository> _logger;
        private readonly HsAppSettings _appSettings;
        private readonly IHubSpotContactClient _contactClient;
        private readonly IHubSpotAssociationsClient _associationsClient;
        private readonly IHubSpotCompanyClient _companyClient;

        public HubspotContactRepository(
            IOptions<HsAppSettings> appSettings,
            ICBSClient db,
            ILogger<IHubspotContactRepository> logger)
        {
            _db = db;
            _logger = logger;
            _appSettings = appSettings.Value;
            _contactClient = new HubSpotContactClient(_appSettings.ApiKey);
            _associationsClient = new HubSpotAssociationsClient(_appSettings.ApiKey);
            _companyClient = new HubSpotCompanyClient(_appSettings.ApiKey);
        }

        public async Task<ContactHubSpotEntity> CreateContact(string email)
        {
            if (email.IsNullOrEmpty())
                return null;
            try
            {
                return await _contactClient.CreateAsync<ContactHubSpotEntity>(new ContactHubSpotEntity
                {
                    Email = email
                });
            }
            catch (HubSpotException e)
            {
                _logger.LogError($"{GetType().Name} COULD NOT CREATE CONTACT FOR {email} {e}");
                return null;
            }
        }
        public async Task<ContactHubSpotEntity> GetContactByEmail(string email)
        {
            if (email.IsNullOrEmpty())
                return null;
            
            var contact = await _contactClient.GetByEmailAsync<ContactHubSpotEntity>(email, 
                new ContactGetRequestOptions
            {
                IncludeListMemberships = true,
                IncludeHistory = true
            });

            return contact;
        }

        public async Task<CompanyHubSpotEntity> CreateCompany(string name)
        {
            return await _companyClient.CreateAsync<CompanyHubSpotEntity>(new CompanyHubSpotEntity
            {
                Name = name
            });
        }
        public async Task<CompanyHubSpotEntity> CreateCompany(IsCompany company)
        {
            var domain = await company.GetLatestWebAddress(_db);
            
            return await _companyClient.CreateAsync<CompanyHubSpotEntity>(new CompanyHubSpotEntity
            {
                Name = company.Name,
                Domain = domain?.Address,
                Website = domain?.Address
            });
        }
        public async Task<CompanyHubSpotEntity> GetContactCompany(long contactId)
        {
            if (contactId == 0)
                return null;
            
            var associations = await _associationsClient.GetListByIdAsync(
                contactId,
                HubSpotAssociationDefinitions.ContactToCompany,
                new AssociationListRequestOptions
                {
                    NumberOfAssociationsToReturn = 1
                });
            
            if (associations?.Results?.IsNullOrEmpty() ?? true)
                return null;
            
            var company = await _companyClient.GetByIdAsync<CompanyHubSpotEntity>(associations.Results.FirstOrDefault());
            return company;
        }
    }
}