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
using iSpectAPI.Core.Database.HubspotConnector.Commands;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using WeihanLi.Common.Event;
using WeihanLi.Extensions;

namespace HubspotConnector.Application.DataAccess.Queues
{
    public class HsCommandQueue: IsQueue<HsCommand>, IHsCommandQueue
    {
        private readonly ICBSClient _cbsClient;
        private readonly ILogger<IHsCommandQueue> _logger;

        private readonly IEventHandlerFactory _eventHandlerFactory;

        public HsCommandQueue(
            IHostApplicationLifetime applicationLifetime,
            ICBSClient cbsClient,
            IEventHandlerFactory eventHandlerFactory,
            ILogger<IHsCommandQueue> logger,
            ILogger<IsQueue<HsCommand>> parentLogger) :
            base(parentLogger, 
                applicationLifetime, 
                TimeSpan.FromSeconds(5), 
                TimeSpan.FromMinutes(1), 
                CancellationToken.None,
                IsApplicationEnvironment.Any)
        {
            _cbsClient = cbsClient;
            _logger = logger;
            _eventHandlerFactory = eventHandlerFactory;
        }
        
        protected override async Task<IEnumerable<HsCommand>> GetQueueItems()
        {
            var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
            var statement = "SELECT meta().id, type, sourceDocumentID, processed " +
                            "FROM ispect " +
                            $"WHERE typeSpecifier LIKE '{typeof(HsCommand).ComputeTypeSpecifier()}%' " +
                            $"AND environment = '{env}' " +
                            "AND (processed IS NOT VALUED OR processed = false) " +
                            "ORDER BY createdAt DESC";
            
            var unprocessed = await _cbsClient.ExecuteStatement<HsCommand>(statement);
            _logger.LogInformation($"{GetType().Name} - There are currently {unprocessed.Count} UNPROCESSED {nameof(HsCommand)}");

            return unprocessed;
        }
        
        protected override async Task ProcessQueueItem(HsCommand item)
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
                    
                    var isProcessed = await _cbsClient.GetKey<HsCommand, bool>(item.Id, "processed");
                    if (isProcessed)
                    {
                        _logger.LogInformation($"{GetType().Name} - Aborting - already processed {item.Id} {item.SourceDocumentId} {item.CreatedAt}");
                        return;
                    }

                    try
                    {
                        var lockDuration = TimeSpan.FromSeconds(30);
                        var (commandObj, cas) =
                            await _cbsClient.GetAndLock<HsCommand>(item.Id, lockDuration);
                        
                        _logger.LogInformation($"{GetType().Name} - LOCK ACQUIRED {item.Id} - Trying to process");

                        await HandleEvent(commandObj);
                        
                        commandObj.Processed = true;
                        commandObj.ProcessedAt = DateTime.UtcNow.ToEpoch();
                        await _cbsClient.Replace(commandObj, cas);
                    
                        _logger.LogInformation($"{GetType().Name} - LOCK RELEASED {item.Id} - Finished processing {item.Id} {item.SourceDocumentId}");

                    }
                    catch (Exception e)
                    {
                        _logger.LogError($"{GetType().Name} ABORTING - {item.Id} IS LOCKED! {e}");
                    }
                }
                catch (Exception e)
                {
                    _logger.LogCritical($"{GetType().Name} CRITICAL ERROR {e.GetType()} {e.Message} {e.StackTrace} Event: {item.Id}");
                }
            });
        }

        private async Task HandleEvent(HsCommand commandObj)
        {
            var handlers = _eventHandlerFactory.GetHandlers(commandObj.GetType());
            if (handlers.Count > 0)
            {
                _logger.LogInformation($"{GetType().Name} Found {handlers.Count} Event Handlers");
                _logger.LogInformation($"{GetType().Name} Trying to find an event handler for {commandObj.Id}");
                await handlers
                        .Select(h => h.Handle(commandObj))
                        .WhenAll();
            }
        }
    }
}