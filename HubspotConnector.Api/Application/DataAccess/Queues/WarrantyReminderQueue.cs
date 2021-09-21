using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Couchbase;
using iSpectAPI.Core.Application.DataAccess.Clients.CouchbaseClients;
using iSpectAPI.Core.Application.DataAccess.Services.Queues;
using iSpectAPI.Core.Application.Extensions;
using iSpectAPI.Core.Application.Extensions.Reflection;
using iSpectAPI.Core.Configuration;
using iSpectAPI.Core.Database.QueueItems;
using iSpectAPI.Core.Database.QueueItems.Notifications;
using iSpectAPI.Core.Database.QueueItems.Notifications.WarrantyReminders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using WeihanLi.Common.Event;
using WeihanLi.Extensions;

namespace HubspotConnector.Application.DataAccess.Queues
{
    public class WarrantyReminderQueue : IsQueue<IsWarrantyReminderNotification>, IWarrantyReminderQueue
    {
        private readonly ICBSClient _cbsClient;
        private readonly ILogger<IWarrantyReminderQueue> _logger;

        private readonly IEventHandlerFactory _eventHandlerFactory;

        public WarrantyReminderQueue(
            IHostApplicationLifetime applicationLifetime,
            ICBSClient cbsClient,
            IEventHandlerFactory eventHandlerFactory,
            ILogger<IWarrantyReminderQueue> logger,
            ILogger<IsQueue<IsWarrantyReminderNotification>> parentLogger) :
            base(parentLogger, 
                applicationLifetime, 
                TimeSpan.FromSeconds(5), 
                TimeSpan.FromMinutes(1), 
                CancellationToken.None,
                IsApplicationEnvironment.Production)
        {
            _cbsClient = cbsClient;
            _logger = logger;
            _eventHandlerFactory = eventHandlerFactory;
        }
        
        protected override async Task<IEnumerable<IsWarrantyReminderNotification>> GetQueueItems()
        {
            var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
            var statement = "SELECT meta().id, type, sourceDocumentID, processed " +
                            "FROM ispect " +
                            $"WHERE typeSpecifier LIKE '{typeof(IsWarrantyReminderNotification).ComputeTypeSpecifier()}%' " +
                            $"AND environment = '{env}' " +
                            "AND propertyEntityIDs IS VALUED " +
                            "AND (metadata.hsDealProcessed IS NOT VALUED OR metadata.hsDealProcessed = false) ";
            
            var unprocessed = await _cbsClient.ExecuteStatement<IsWarrantyReminderNotification>(statement);
            _logger.LogInformation($"{GetType().Name} - There are currently {unprocessed.Count} UNPROCESSED {nameof(IsNotification)}");

            return unprocessed;
        }
        
        protected override async Task ProcessQueueItem(IsWarrantyReminderNotification item)
        {
            await Task.Run(async () =>
            {
                try
                {
                    if (item == null)
                    {
                        _logger.LogError($"{GetType().Name} QUEUED ITEM IS NULL");
                        return;
                    }
                    
                    var isProcessed = await _cbsClient.GetKey<IsWarrantyReminderNotification, bool>(item.Id, "metadata.hsDealProcessed");
                    if (isProcessed)
                    {
                        _logger.LogInformation($"{GetType().Name} - Aborting - already processed {item.Id} {item.SourceDocumentId} {item.CreatedAt}");
                        return;
                    }

                    try
                    {
                        var lockDuration = TimeSpan.FromSeconds(30);
                        var (notificationObject, cas) =
                            await _cbsClient.GetAndLock<IsWarrantyReminderNotification>(item.Id, lockDuration);
                        
                        _logger.LogInformation($"{GetType().Name} - LOCK ACQUIRED {item.Id} - Trying to process");

                        await HandleEvent(notificationObject);
                        
                        notificationObject.AddMetadata("hsDealProcessed", true);
                        notificationObject.AddMetadata("hsDealProcessedAt", DateTime.UtcNow.ToEpoch());
                        await _cbsClient.Replace(notificationObject, cas);
                    
                        _logger.LogInformation($"{GetType().Name} - LOCK RELEASED {item.Id} - Finished processing {item.Id} {item.SourceDocumentId}");

                    }
                    catch (TemporaryLockFailureException)
                    {
                        _logger.LogError($"{GetType().Name} ABORTING - {item.Id} IS LOCKED! ");
                    }
                }
                catch (Exception e)
                {
                    _logger.LogCritical($"{GetType().Name} CRITICAL ERROR {e.GetType()} {e.Message} {e.StackTrace} Event: {item.Id}");
                }
            });
        }

        private async Task HandleEvent(IsNotification notificationObj)
        {
            var handlers = _eventHandlerFactory.GetHandlers(notificationObj.GetType());
            if (handlers.Count > 0)
            {
                _logger.LogInformation($"{GetType().Name} Found {handlers.Count} Event Handlers");
                _logger.LogInformation($"{GetType().Name} Trying to find an event handler for {notificationObj.Id}");
                await handlers
                        .Select(h => h.Handle(notificationObj))
                        .WhenAll();
            }
        }
    }
}