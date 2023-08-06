using System.Threading.Tasks;
using HubspotConnector.Application.DataAccess.Repositories;
using iSpectAPI.Core.Database.Legacy;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace HubspotConnector.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HubspotHooksController : ControllerBase
{
    private readonly ILogger<HubspotHooksController> _logger;
    private readonly IHubspotContactRepository _hubspotContactRepository;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public HubspotHooksController(
        ILogger<HubspotHooksController> logger, 
        IHubspotContactRepository hubspotContactRepository,
        IHttpContextAccessor httpContextAccessor)

    {
        _logger = logger;
        _hubspotContactRepository = hubspotContactRepository;
        _httpContextAccessor = httpContextAccessor;
    }

    
}