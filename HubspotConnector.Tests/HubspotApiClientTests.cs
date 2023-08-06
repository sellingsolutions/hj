using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using NUnit.Framework;
using Skarp.HubSpotClient.Associations;
using Skarp.HubSpotClient.Associations.Dto;
using Skarp.HubSpotClient.Common.Dto.Properties;
using Skarp.HubSpotClient.Company;
using Skarp.HubSpotClient.Company.Dto;
using Skarp.HubSpotClient.Contact;
using Skarp.HubSpotClient.Contact.Dto;
using Skarp.HubSpotClient.Core;
using Skarp.HubSpotClient.Deal;
using Skarp.HubSpotClient.Deal.Dto;
using Skarp.HubSpotClient.ListOfContacts;
using Skarp.HubSpotClient.Owner;
using Skarp.HubSpotClient.Owner.Dto;

namespace HubspotConnector.Tests
{
    public class HubspotApiClientTests
    {
        [SetUp]
        public void Setup()
        {
        }

        
        [Test]
        public async Task GetContact()
        {
            var client = new HubSpotContactClient("pat-na1-7514089e-8af4-46fd-a7fe-2ee37751c10d");

            var contact = await client.GetByEmailAsync<ContactHubSpotEntity>("lovisa.hallman@spg.se", new ContactGetRequestOptions
            {
                IncludeListMemberships = true,
                IncludeHistory = true
            });
            Assert.IsNotNull(contact);
        }
        
        [Test]
        public async Task GetAssociations()
        {
            var contactClient = new HubSpotContactClient("pat-na1-7514089e-8af4-46fd-a7fe-2ee37751c10d");

            var contact = await contactClient.GetByEmailAsync<ContactHubSpotEntity>("lovisa.hallman@spg.se", new ContactGetRequestOptions
            {
                IncludeListMemberships = true,
                IncludeHistory = true
            });
            
            var client = new HubSpotAssociationsClient("pat-na1-7514089e-8af4-46fd-a7fe-2ee37751c10d");
            var associations = await client.GetListByIdAsync(
                contact.Id ?? 0,
                HubSpotAssociationDefinitions.ContactToCompany,
                new AssociationListRequestOptions
                {
                    NumberOfAssociationsToReturn = 1
                });
            
            var companyClient = new HubSpotCompanyClient("pat-na1-7514089e-8af4-46fd-a7fe-2ee37751c10d");
            var company = await companyClient.GetByIdAsync<CompanyHubSpotEntity>(associations.Results.FirstOrDefault());
            
            Assert.IsNotNull(company);
        }
        
        [Test]
        public async Task GetOwner()
        {
            var client = new HubSpotOwnerClient("pat-na1-7514089e-8af4-46fd-a7fe-2ee37751c10d");
            var ownerList = await client.ListAsync<OwnerHubSpotEntity>(new OwnerListRequestOptions
            {
                Email = "joakim.carpfelt@besiktningsman.se"
            });
            
            Assert.IsNotNull(ownerList);
        }
        
        [Test]
        public async Task GetCompany()
        {
            var client = new HubSpotCompanyClient("pat-na1-7514089e-8af4-46fd-a7fe-2ee37751c10d");
            var company = await client.GetByIdAsync<CompanyHubSpotEntity>(3285005450);
            
            var duplicate = await client.CreateAsync<CompanyHubSpotEntity>(company);

            Assert.IsNotNull(company);
        }
        
        [Test]
        public async Task GetDeal()
        {
            var client = new HubSpotDealClient("pat-na1-7514089e-8af4-46fd-a7fe-2ee37751c10d");
            
            var deal = await client.GetByIdAsync<DealHubSpotEntity>(14003985166);
            var contactsClient = new HubSpotContactClient("pat-na1-7514089e-8af4-46fd-a7fe-2ee37751c10d");
            var result = new ContactListHubSpotEntity<ContactHubSpotEntity>
            {
                MoreResultsAvailable = true
            };
            
            while (result.MoreResultsAvailable)
            {
                var continuationOffset = result.ContinuationOffset;
                result = await contactsClient.ListAsync<ContactListHubSpotEntity<ContactHubSpotEntity>>(new ContactListRequestOptions
                {
                    ContactOffset = continuationOffset,
                    NumberOfContactsToReturn = 10,
                    PropertiesToInclude = new List<string>
                    {
                        "email", "firstName", "lastname", "company", "website", "phone", "address", "city", "state", "zip"
                    }
                });
                Console.WriteLine(result);
            }
            
            Assert.IsNotNull(deal);
        }

    }
}