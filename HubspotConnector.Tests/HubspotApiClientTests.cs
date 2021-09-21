using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using NUnit.Framework;
using Skarp.HubSpotClient.Associations;
using Skarp.HubSpotClient.Associations.Dto;
using Skarp.HubSpotClient.Company;
using Skarp.HubSpotClient.Company.Dto;
using Skarp.HubSpotClient.Contact;
using Skarp.HubSpotClient.Contact.Dto;
using Skarp.HubSpotClient.Core;
using Skarp.HubSpotClient.Deal;
using Skarp.HubSpotClient.Deal.Dto;
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
            var client = new HubSpotContactClient("ec4e818f-8961-4d91-96df-ac72de5a1cdc");

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
            var contactClient = new HubSpotContactClient("ec4e818f-8961-4d91-96df-ac72de5a1cdc");

            var contact = await contactClient.GetByEmailAsync<ContactHubSpotEntity>("lovisa.hallman@spg.se", new ContactGetRequestOptions
            {
                IncludeListMemberships = true,
                IncludeHistory = true
            });
            
            var client = new HubSpotAssociationsClient("ec4e818f-8961-4d91-96df-ac72de5a1cdc");
            var associations = await client.GetListByIdAsync(
                contact.Id ?? 0,
                HubSpotAssociationDefinitions.ContactToCompany,
                new AssociationListRequestOptions
                {
                    NumberOfAssociationsToReturn = 1
                });
            
            var companyClient = new HubSpotCompanyClient("ec4e818f-8961-4d91-96df-ac72de5a1cdc");
            var company = await companyClient.GetByIdAsync<CompanyHubSpotEntity>(associations.Results.FirstOrDefault());
            
            Assert.IsNotNull(company);
        }
        
        [Test]
        public async Task GetOwner()
        {
            var client = new HubSpotOwnerClient("ec4e818f-8961-4d91-96df-ac72de5a1cdc");
            var ownerList = await client.ListAsync<OwnerHubSpotEntity>(new OwnerListRequestOptions
            {
                Email = "yones@besiktningsman.se"
            });
            
            Assert.IsNotNull(ownerList);
        }
        
        [Test]
        public async Task GetCompany()
        {
            var client = new HubSpotCompanyClient("ec4e818f-8961-4d91-96df-ac72de5a1cdc");
            var company = await client.GetByIdAsync<CompanyHubSpotEntity>(3285005450);
            
            var duplicate = await client.CreateAsync<CompanyHubSpotEntity>(company);

            Assert.IsNotNull(company);
        }
        
        [Test]
        public async Task GetDeal()
        {
            var client = new HubSpotDealClient("ec4e818f-8961-4d91-96df-ac72de5a1cdc");
            var deal = await client.GetByIdAsync<DealHubSpotEntity>(5364203815);
            Assert.IsNotNull(deal);
        }
    }
}