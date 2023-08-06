using System;
using iSpectAPI.Core.Database.ActorModel;
using WeihanLi.Common.Event;

namespace HubspotConnector.Application.Dto;

public class HubspotSyncEvent<THubspotEntity> : IEventBase, IIsConcept
{
    public string Id { get; set; }
    public string Type { get; }
    public string TypeSpecifier { get; }
    public string DynamicTypeSpecifier { get; set; }
    public DateTime CreatedAt { get; set; }
    
    public string EventId => Id;
    
    public DateTimeOffset EventAt => CreatedAt;
    public THubspotEntity Entity { get; set; }

    public HubspotSyncEvent(string id, DateTime createdAt, THubspotEntity entity)
    {
        Id = id;
        Entity = entity;
        CreatedAt = createdAt;
        Entity = entity;
    }
}