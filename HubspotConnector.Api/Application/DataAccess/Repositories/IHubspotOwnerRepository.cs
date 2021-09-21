using System.Threading.Tasks;
using Skarp.HubSpotClient.Owner.Dto;

namespace HubspotConnector.Application.DataAccess.Repositories
{
    public interface IHubspotOwnerRepository
    {
        Task<OwnerHubSpotEntity> GetOwnerByEmail(string email);
    }
}