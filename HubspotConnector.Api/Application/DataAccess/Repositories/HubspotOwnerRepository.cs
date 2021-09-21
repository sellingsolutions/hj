using System.Linq;
using System.Threading.Tasks;
using HubspotConnector.CrossCuttingConcerns;
using Microsoft.Extensions.Options;
using Skarp.HubSpotClient.Owner;
using Skarp.HubSpotClient.Owner.Dto;

namespace HubspotConnector.Application.DataAccess.Repositories
{
    public class HubspotOwnerRepository : IHubspotOwnerRepository
    {
        private readonly HsAppSettings _appSettings;
        private readonly HubSpotOwnerClient _client;
        
        public HubspotOwnerRepository(IOptions<HsAppSettings> appSettings)
        {
            _appSettings = appSettings.Value;
            _client = new HubSpotOwnerClient(_appSettings.ApiKey);
        }

        public async Task<OwnerHubSpotEntity> GetOwnerByEmail(string email)
        {
            var ownerList = await _client.ListAsync<OwnerHubSpotEntity>(new OwnerListRequestOptions
            {
                Email = email
            });

            return ownerList?.FirstOrDefault();
        }
    }
}