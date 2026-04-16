using Muzu.Api.Core.Interfaces;
using Muzu.Api.Core.Interfaces.Service;
using Muzu.Api.Core.Repositories;
using Muzu.Api.Core.Services;

namespace Muzu.Api.Extensions;

public static class DependencyInjectionExtensions
{
    public static IServiceCollection AddPersistence(this IServiceCollection services)
    {
        services.AddSingleton<IDbConnectionFactory, DbConnectionFactory>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<ITenantRepository, TenantRepository>();
        services.AddScoped<IUsuarioRepository, UsuarioRepository>();
        services.AddScoped<IRolRepository, RolRepository>();
        services.AddScoped<IBoardRepository, BoardRepository>();
        services.AddScoped<IReunionRepository, ReunionRepository>();
        services.AddScoped<ITenantConfigRepository, TenantConfigRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<IMultaRepository, MultaRepository>();
        services.AddScoped<IPartnerDocumentRepository, PartnerDocumentRepository>();
        services.AddScoped<IMedidorRepository, MedidorRepository>();
        services.AddScoped<IBillingRepository, BillingRepository>();
        services.AddScoped<IDashboardRepository, DashboardRepository>();

        return services;
    }

    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddSingleton<IRoleMutationGuard, RoleMutationGuard>();
        services.AddSingleton<IJwtService, JwtService>();
        services.AddScoped<IEmailService, EmailService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IAuthSecurityService, AuthSecurityService>();
        services.AddScoped<ITenantConfigService, TenantConfigService>();
        services.AddScoped<IUsuarioAdministracionService, UsuarioAdministracionService>();
        services.AddScoped<IUsersService, UsersService>();
        services.AddScoped<IBoardsService, BoardsService>();
        services.AddScoped<IReunionService, ReunionService>();
        services.AddScoped<IRoleManagementService, RoleManagementService>();
        services.AddScoped<IPartnerDocumentService, PartnerDocumentService>();
        services.AddScoped<IMedidorService, MedidorService>();
        services.AddScoped<IBillingService, BillingService>();
        services.AddScoped<IDashboardService, DashboardService>();

        return services;
    }
}
