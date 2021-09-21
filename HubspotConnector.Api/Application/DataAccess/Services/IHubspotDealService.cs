using System.Threading.Tasks;
using HubspotConnector.Application.Dto;
using iSpectAPI.Core.Database.HubspotConnector.Deals;

namespace HubspotConnector.Application.DataAccess.Services
{
    public interface IHubspotDealService
    {
        Task<HsDeal> CreateDeal(HubspotDealRequest request);
    }
}