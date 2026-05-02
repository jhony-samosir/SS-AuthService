using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SS.AuthService.Application.Interfaces;
using SS.AuthService.Infrastructure.Authentication;
using SS.AuthService.Infrastructure.Persistence.Context;
using SS.AuthService.Infrastructure.Repositories;
using SS.AuthService.Infrastructure.Services;

namespace SS.AuthService.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Persistence
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("SSAuthDB")));

        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IEmailVerificationRepository, EmailVerificationRepository>();

        // Authentication & Security
        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.AddScoped<IPasswordHasher, Argon2PasswordHasher>();
        services.AddScoped<IJwtProvider, JwtProvider>();
        services.AddScoped<ITokenHasher, TokenHasher>();
        services.AddScoped<IEmailService, SimulatedEmailService>();
        
        return services;
    }
}
