using System.Threading.Tasks;
using HubspotConnector.CrossCuttingConcerns;
using iSpectAPI.Core.Application.DataAccess.Clients.CouchbaseClients;
using iSpectAPI.Core.Database.HubspotConnector.Commands;
using iSpectAPI.Core.Database.QueueItems.Notifications;
using iSpectAPI.Core.Database.QueueItems.Notifications.WarrantyReminders;
using Microsoft.Extensions.Options;
using WeihanLi.Common.Event;

namespace HubspotConnector.Application.DataAccess.Handlers
{
    public class WarrantyReminderHandler : EventHandlerBase<IsNotification>
    {
        private readonly ICBSClient _db;
        private readonly HsAppSettings _appSettings;

        public WarrantyReminderHandler(
            ICBSClient db, 
            IOptions<HsAppSettings> appSettings)
        {
            _appSettings = appSettings.Value;
            _db = db;
        }

        public override async Task Handle(IsNotification @event)
        {
            if (@event is IsWarrantyReminderNotification warrantyReminder)
            {
                await HsCommand
                    .CreateHsEvent<HsCreateDealCommand>(warrantyReminder, _db);
            }
        }
    }
}