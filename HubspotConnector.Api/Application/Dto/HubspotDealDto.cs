
using System.Runtime.Serialization;
using Skarp.HubSpotClient.Deal.Dto;

namespace HubspotConnector.Application.Dto
{
    [DataContract]
    public class HubspotDealDto : DealHubSpotEntity
    {
        [DataMember(Name="ispect_url")]
        public string Url {get; set;}
        
        [DataMember(Name="antal_lagenheter")]
        public long NoOfApartments { get; set; }
        
        [DataMember(Name="antal_lagenheter_flerbostadshus")]
        public long NoOfApartmentBuildings { get; set; }
    }
}