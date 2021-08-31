using System;
using System.Threading.Tasks;
using NUnit.Framework;
using Skarp.HubSpotClient.Contact;
using Skarp.HubSpotClient.Contact.Dto;

namespace HubspotConnector.Tests
{
    public class HubspotApiClientTests
    {
        [SetUp]
        public void Setup()
        {
        }

        
        [Test]
        public async Task ConnectClient()
        {
            var client = new HubSpotContactClient("ec4e818f-8961-4d91-96df-ac72de5a1cdc");
            var contact = await client.GetByEmailAsync<ContactHubSpotEntity>("mikael.palm@einarmattsson.se");
            Console.WriteLine();
        }
    }
}