using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HubspotConnector.CrossCuttingConcerns;
using iSpectAPI.Core.Application.DataAccess.Clients.CouchbaseClients;
using iSpectAPI.Core.Database.ActorModel.Actors;
using iSpectAPI.Core.Database.ActorModel.Records;
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

        public async Task<IEnumerable<ContactHubSpotEntity>> GetAllContacts()
        {
            var contacts = new List<ContactHubSpotEntity>();
            var result = new ContactListHubSpotEntity<ContactHubSpotEntity>
            {
                MoreResultsAvailable = true
            };
            
            while (result.MoreResultsAvailable)
            {
                var continuationOffset = result.ContinuationOffset;
                result = await _contactClient.ListAsync<ContactListHubSpotEntity<ContactHubSpotEntity>>(new ContactListRequestOptions
                {
                    ContactOffset = continuationOffset,
                    NumberOfContactsToReturn = 1000,
                    PropertiesToInclude = new List<string>
                    {
                        "email", "firstName", "lastname", "company", "website", "phone", "address", "city", "state", "zip"
                    }
                });
               contacts.AddRange(result.Contacts);
            }

            return contacts;
        }

        public async Task<ContactHubSpotEntity> CreateContact(IsActor actor)
        {
            var email = await actor.GetLatestEmail(nameof(IsActor.EmailAddressIds), _db);
            IsCompany company = actor as IsCompany;
            IsAddress address = null;
            var companyName = "";
            var website = "";
            var firstName = "";
            var lastName = "";
            var phone = "";
            var addressLine1 = "";
            var zipCode = "";
            var city = "";
            if (actor is IsPerson person)
            {
                address = await person.GetLatestAddress(_db);
                company = await person.GetLatestCompany(_db);
           
                firstName = person.FirstName;
                lastName = person.LastName;
                phone = (await person.GetLatestPhoneNo(_db))?.Number;
                website = (await person.GetLatestWebAddress(_db))?.Address;
            }

            if (company != null)
            {
                companyName = company.Name;
                address = await company.GetLatestAddress(_db);
                website = (await company.GetLatestWebAddress(_db))?.Address;
            }

            if (address != null)
            {
                addressLine1 = address.AddressLine1;
                zipCode = address.ZipCode;
                city = address.City;
            }

            if (email.IsNullOrEmpty())
            {
                _logger.LogError($"{GetType().Name} COULD NOT CREATE CONTACT FOR actor. {actor.Id} emailAddressIds: {actor.EmailAddressIds} NO EMAIL FOUND");                return null;
                return null;
            }
            try
            {
                return await _contactClient.CreateAsync<ContactHubSpotEntity>(new ContactHubSpotEntity
                {
                    Email = email,
                    FirstName = firstName,
                    Lastname = lastName,
                    Company = companyName,
                    Address = addressLine1,
                    City = city,
                    ZipCode = zipCode,
                    Website = website,
                    Phone = phone,
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