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
            var (usuario, tenant, loginResponse) = await _authService.RegistrarPrimerTenantYUsuarioAsync(request.Tenant, request.Usuario);
            Response.Cookies.Append("muzu_token", loginResponse.AccessToken, new CookieOptions { HttpOnly = true, Secure = true, SameSite = SameSiteMode.Strict });
            Response.Cookies.Append("muzu_refresh_token", loginResponse.RefreshToken, new CookieOptions { HttpOnly = true, Secure = true, SameSite = SameSiteMode.Strict });
            return Ok(new { usuario, tenant });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto loginDto)
        {
            var loginResponse = await _authService.LoginAsync(loginDto);
            if (loginResponse == null) return Unauthorized();
            
            Response.Cookies.Append("muzu_token", loginResponse.AccessToken, new CookieOptions { HttpOnly = true, Secure = true, SameSite = SameSiteMode.Strict });
            Response.Cookies.Append("muzu_refresh_token", loginResponse.RefreshToken, new CookieOptions { HttpOnly = true, Secure = true, SameSite = SameSiteMode.Strict });
            
            return Ok(new { 
                tenantId = loginResponse.TenantId,
                usuarioId = loginResponse.UsuarioId,
                rol = loginResponse.Rol,
                refreshToken = loginResponse.RefreshToken
            });
        }

        [HttpPost("refresh-token")]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequestDto request)
        {
            var loginResponse = await _authService.RefreshTokenAsync(request.RefreshToken);
            if (loginResponse == null) return Unauthorized();
            
            Response.Cookies.Append("muzu_token", loginResponse.AccessToken, new CookieOptions { HttpOnly = true, Secure = true, SameSite = SameSiteMode.Strict });
            Response.Cookies.Append("muzu_refresh_token", loginResponse.RefreshToken, new CookieOptions { HttpOnly = true, Secure = true, SameSite = SameSiteMode.Strict });
            
            return Ok(new { 
                tenantId = loginResponse.TenantId,
                usuarioId = loginResponse.UsuarioId,
                rol = loginResponse.Rol
            });
        }
    }
}
