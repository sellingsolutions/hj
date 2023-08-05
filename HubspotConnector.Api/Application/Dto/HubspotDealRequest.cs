using System;
using iSpectAPI.Core.Database.ActorModel.Actors;
using iSpectAPI.Core.Database.Legacy;

namespace HubspotConnector.Application.Dto
{
    public class HubspotDealRequest
    {
        public string Name { get; set; }
        
        public string DealType { get; set; }
        
        public string Pipeline { get; set; }
        public string DealStage { get; set; }
        
        public IsActor DealOwner { get; set; } 
        public Party CustomerParty { get; set; }
        public IsActor Customer { get; set; }
        public string CustomerEmail { get; set; }
        public Project Project { get; set; }
        public Inspection Inspection { get; set; }
        public string Url => $"https://app.ispect.se/organization/{Project.ClientId}/projects/{Project.Id}/inspections/{Inspection.Id}/verdicts";
        public DateTime CloseDate { get; set; }
        public decimal DealValue { get; set; }
        
        public long NoOfApartments { get; set; }
        public long NoOfApartmentBuildings { get; set; }
    }
}