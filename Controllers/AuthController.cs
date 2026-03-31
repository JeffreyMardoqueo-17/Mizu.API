using Microsoft.AspNetCore.Mvc;
using Muzu.Api.Core.DTOs;
using Muzu.Api.Core.Services;

namespace Muzu.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("register-tenant")]
        public async Task<IActionResult> RegisterTenant([FromBody] TenantUsuarioRegistroDto request)
        {
            var (usuario, tenant, token) = await _authService.RegistrarPrimerTenantYUsuarioAsync(request.Tenant, request.Usuario);
            Response.Cookies.Append("muzu_token", token, new CookieOptions { HttpOnly = true, Secure = true, SameSite = SameSiteMode.Strict });
            return Ok(new { usuario, tenant });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto loginDto)
        {
            var result = await _authService.LoginAsync(loginDto);
            if (result == null) return Unauthorized();
            var (token, tenantId) = result.Value;
            Response.Cookies.Append("muzu_token", token, new CookieOptions { HttpOnly = true, Secure = true, SameSite = SameSiteMode.Strict });
            return Ok(new { tenantId });
        }
    }
}
