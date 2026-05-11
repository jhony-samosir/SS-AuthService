using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SS.AuthService.Application.Common.Interfaces;
using SS.AuthService.Application.Interfaces;
using SS.AuthService.Infrastructure.Authentication;
using SS.AuthService.Infrastructure.BackgroundServices;
using SS.AuthService.Infrastructure.Diagnostics;
using SS.AuthService.Infrastructure.Persistence.Context;
using SS.AuthService.Infrastructure.Persistence.Interceptors;
using SS.AuthService.Infrastructure.Repositories;
using SS.AuthService.Infrastructure.Security;
using SS.AuthService.Infrastructure.Services;

namespace SS.AuthService.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Current User Service
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUserService, CurrentUserService>();

        // Interceptors
        services.AddScoped<AuditInterceptor>();

        // Persistence
        services.AddDbContext<AppDbContext>((sp, options) =>
        {
            var auditInterceptor = sp.GetRequiredService<AuditInterceptor>();
            
            options.UseNpgsql(configuration.GetConnectionString("SSAuthDB"))
                   .AddInterceptors(auditInterceptor);
        });

        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<ILoginAttemptRepository, LoginAttemptRepository>();
        services.AddScoped<IAuthSessionRepository, AuthSessionRepository>();
        services.AddScoped<IEmailVerificationRepository, EmailVerificationRepository>();
        services.AddScoped<IRoleMenuRepository, RoleMenuRepository>();
        services.AddScoped<IMenuRepository, MenuRepository>();
        services.AddScoped<IRoleRepository, RoleRepository>();
        services.AddScoped<IPasswordResetRepository, PasswordResetRepository>();
        services.AddScoped<IPasswordHistoryRepository, PasswordHistoryRepository>();

        // Authentication & Security
        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.Configure<EmailOptions>(configuration.GetSection(EmailOptions.SectionName));

        services.AddScoped<IPasswordHasher, Argon2PasswordHasher>();
        services.AddSingleton<IRsaKeyProvider, RsaKeyProvider>();
        services.AddScoped<IJwtProvider, JwtProvider>();
        services.AddScoped<ITokenHasher, TokenHasher>();
        services.AddScoped<IEmailService, GmailEmailService>();
        services.AddScoped<IDataEncryptionService, DataEncryptionService>();

        // Email Background Processing
        services.AddSingleton<IEmailQueue, EmailQueue>();
        services.AddHostedService<EmailBackgroundWorker>();
        services.AddHostedService<SecurityDataRetentionWorker>();

        // Caching & MFA
        services.AddDistributedMemoryCache();
        services.AddScoped<ICacheService, CacheService>();
        services.AddScoped<IMfaService, MfaService>();

        // 🛡️ Security: Rate Limiting & Health Checks
        services.AddSecurityRateLimiting();
        services.AddSecurityHealthChecks(configuration);
        
        return services;
    }
}
