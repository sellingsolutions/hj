using System;
using HubspotConnector.Application.DataAccess.Handlers;
using HubspotConnector.Application.DataAccess.Queues;
using HubspotConnector.Application.DataAccess.Repositories;
using HubspotConnector.Application.DataAccess.Services;
using HubspotConnector.CrossCuttingConcerns;
using iSpectAPI.Core.Application.DataAccess.Clients.CouchbaseClients;
using iSpectAPI.Core.Application.DataAccess.Repositories;
using iSpectAPI.Core.Application.DataAccess.Repositories.Interfaces;
using iSpectAPI.Core.Database.HubspotConnector.Commands;
using iSpectAPI.Core.Database.QueueItems.Notifications.WarrantyReminders;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using WeihanLi.Common.Event;

namespace HubspotConnector
{
    public class Startup
    {
        private IConfiguration Configuration { get; set; }
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers();
            
            services.AddOptions();
            var appSettingsSection = Configuration.GetSection("AppSettings");
            services.Configure<HsAppSettings>(appSettingsSection);
            
            services.AddSingleton<IIndexRepository, IndexRepository>();
            services.AddSingleton<IBucketService, BucketService>();
            services.AddSingleton<ICBSClient, CBSClient>();
            services.AddSingleton<ICBSViewClient, CBSViewClient>();
            services.AddSingleton<IRepository, Repository>();
            services.AddSingleton<ILoggerFactory, LoggerFactory>();
            services.AddSingleton(typeof(ILogger<>), typeof(Logger<>));
            
            // Services
            services.AddSingleton<IHubspotDealService, HubspotDealService>();
            
            // Repositories
            services.AddSingleton<IGroupActorRepository, GroupActorRepository>();
            services.AddSingleton<IHubspotContactRepository, HubspotContactRepository>();
            services.AddSingleton<IHubspotOwnerRepository, HubspotOwnerRepository>();
            
            // Queues
            services.AddScoped<IWarrantyReminderQueue, WarrantyReminderQueue>();
            services.AddScoped<IHsCommandQueue, HsCommandQueue>();

            // Handlers
            services
                .AddEvents()
                .AddEventHandler<IsSarskildBesiktningReminderQueueItem, WarrantyReminderHandler>()
                .AddEventHandler<IsGarantibesiktningReminderQueueItem, WarrantyReminderHandler>()
                .AddEventHandler<HsCreateDealCommand, HsDealCommandHandler>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(
            IApplicationBuilder app, 
            IWebHostEnvironment env, 
            IServiceProvider serviceProvider)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            
            /******************** START BACKGROUND QUEUES *************************/
            serviceProvider.GetRequiredService<IWarrantyReminderQueue>();
            serviceProvider.GetRequiredService<IHsCommandQueue>();
            /**********************************************************************/
            
            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints => { endpoints.MapControllers(); });
            
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: false, reloadOnChange: true)
                .AddEnvironmentVariables();

            Configuration = builder.Build();
        }
    }
}