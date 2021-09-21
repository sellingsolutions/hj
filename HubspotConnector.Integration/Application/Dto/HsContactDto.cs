using System.Runtime.Serialization;
using Skarp.HubSpotClient.Contact.Dto;

namespace HubspotConnector.Integration.Application.Dto
{
    [DataContract]
    public class HsContactDto : ContactHubSpotEntity
    {
        [DataMember(Name = "last_login")]
        public string LastLogin { get; set; }
    }
}