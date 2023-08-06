using System.Collections.Generic;
using System.Threading.Tasks;
using iSpectAPI.Core.Database.ActorModel.Actors;
using Skarp.HubSpotClient.Company.Dto;
using Skarp.HubSpotClient.Contact.Dto;

namespace HubspotConnector.Application.DataAccess.Repositories
{
    public interface IHubspotContactRepository
    {
        Task<IEnumerable<ContactHubSpotEntity>> GetAllContacts();
        Task<ContactHubSpotEntity> CreateContact(IsActor actor);
        Task<ContactHubSpotEntity> GetContactByEmail(string email);

        Task<CompanyHubSpotEntity> CreateCompany(string name);
        Task<CompanyHubSpotEntity> CreateCompany(IsCompany company);
        Task<CompanyHubSpotEntity> GetContactCompany(long contactId);
    }
}