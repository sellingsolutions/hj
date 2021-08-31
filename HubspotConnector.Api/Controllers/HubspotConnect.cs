using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Skarp.HubSpotClient.Contact;
using Skarp.HubSpotClient.Contact.Dto;

namespace HubspotConnector.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class HubspotConnect : Controller
    {

        // GET
        public async Task<IActionResult> Index()
        {
            var currentDatetime = DateTime.SpecifyKind(DateTime.Now.AddDays(-7), DateTimeKind.Utc);
            var since = ((DateTimeOffset)currentDatetime).ToUnixTimeMilliseconds().ToString();

            var client = new HubSpotContactClient("ec4e818f-8961-4d91-96df-ac72de5a1cdc");
            var contact = await client.GetByEmailAsync<ContactHubSpotEntity>("mikael.palm@einarmattsson.se");

            return null;
        }
    }
}