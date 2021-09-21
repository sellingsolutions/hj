using System;
using System.Threading.Tasks;
using HubspotConnector.Application.DataAccess.Repositories;
using HubspotConnector.Application.DataAccess.Services;
using HubspotConnector.Application.Dto;
using HubspotConnector.CrossCuttingConcerns;
using iSpectAPI.Core.Application.DataAccess.Clients.CouchbaseClients;
using iSpectAPI.Core.Application.DataAccess.Repositories;
using iSpectAPI.Core.Application.DataAccess.Repositories.Interfaces;
using iSpectAPI.Core.Database.ActorModel.Actors;
using iSpectAPI.Core.Database.Legacy;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using Skarp.HubSpotClient.Associations;
using Skarp.HubSpotClient.Company;
using Skarp.HubSpotClient.Company.Interfaces;
using Skarp.HubSpotClient.Contact;
using Skarp.HubSpotClient.Contact.Interfaces;
using Skarp.HubSpotClient.Deal;
using Skarp.HubSpotClient.Deal.Interfaces;

namespace HubspotConnector.Tests
{
    public class HubspotDealServiceTests
    {
        private IServiceProvider serviceProvider { get; set; }
        
        [SetUp]
        public void Setup()
        {
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Stage");
            
            var services = new ServiceCollection();
            services.AddOptions();

            var config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.Test.json", optional: false)
                .Build();
            
            var appSettingsSection = config.GetSection("AppSettings");
            services.Configure<HsAppSettings>(appSettingsSection);
            
            services.AddLogging();
            services.AddSingleton<IIndexRepository, IndexRepository>();
            services.AddSingleton<IBucketService, BucketService>();
            services.AddSingleton<ICBSClient, CBSClient>();
            services.AddSingleton<ICBSViewClient, CBSViewClient>();
            services.AddSingleton<ILoggerFactory, LoggerFactory>();
            services.AddSingleton(typeof(ILogger<>), typeof(Logger<>));

            services.AddSingleton<IHubspotDealService, HubspotDealService>();
            services.AddSingleton<IHubspotContactRepository, HubspotContactRepository>();
            services.AddSingleton<IHubspotOwnerRepository, HubspotOwnerRepository>();

            serviceProvider = services.BuildServiceProvider();
        }

        [Test]
        public async Task SHOULD_CREATE_DEAL()
        {
            var dealService = serviceProvider.GetRequiredService<IHubspotDealService>();
            var db = serviceProvider.GetRequiredService<ICBSClient>();
            var owner = await db.Get<IsPerson>("isPerson_fbe5d799-ca76-476e-bd8a-12000e89deeb");
            var customer = await db.Get<IsCompany>("isCompany_971b3f03-ef58-4f00-a9f3-e6c08e607ddd");
            var customerParty = await db.Get<Party>("party_1580811081_8faca69a-f8f0-4bac-8d4e-84ca19f94813");
            var project = await db.Get<Project>("project_1580810890_2d5224ea-3cca-4376-9aff-f73bd8c088f5");

            var request = new HubspotDealRequest
            {
                Name = "GB",
                DealOwner = owner,
                CustomerParty = customerParty,
                Customer = customer,
                Project = project,
                CloseDate = DateTime.Today,
                CustomerEmail = "joakim.jakobsson@storstadenbostad.se",
                DealValue = 50000,
                NoOfApartmentBuildings = 3,
                NoOfApartments = 50
            };
            
            var newDeal = await dealService.CreateDeal(request);
            Assert.IsNotNull(newDeal);
        }
    }
}