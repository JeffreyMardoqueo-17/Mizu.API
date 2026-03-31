using Microsoft.AspNetCore.Mvc;
using Muzu.Api.Core.Interfaces;
using System.Threading.Tasks;

namespace Muzu.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ConfigController : ControllerBase
    {
        private readonly ITenantConfigRepository _configRepo;
        public ConfigController(ITenantConfigRepository configRepo)
        {
            _configRepo = configRepo;
        }

        [HttpGet("{tenantId}")]
        public async Task<IActionResult> GetConfig([FromRoute] Guid tenantId)
        {
            var config = await _configRepo.ObtenerPorTenantIdAsync(tenantId);
            if (config == null) return NotFound();
            return Ok(config);
        }
    }
}
