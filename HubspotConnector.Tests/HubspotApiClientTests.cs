using System;
using System.Threading.Tasks;
using NUnit.Framework;
using Skarp.HubSpotClient.Contact;
using Skarp.HubSpotClient.Contact.Dto;
using Skarp.HubSpotClient.Deal;
using Skarp.HubSpotClient.Deal.Dto;

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
            var contact = await client.GetByEmailAsync<ContactHubSpotEntity>("mikael.palm@einarmattsson.se");
            Assert.IsNotNull(contact);
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