using System.Runtime.Serialization;
using Skarp.HubSpotClient.Contact.Dto;

namespace HubspotConnector.Integration.Application.Dtos
{
    [DataContract]
    public class HsContact : ContactHubSpotEntity
    {
        [DataMember(Name = "last_login")]
        public string LastLogin { get; set; }
    }
}