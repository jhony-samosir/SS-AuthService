using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using SS.AuthService.API.Middlewares;
using SS.AuthService.Application;
using SS.AuthService.Infrastructure;
using SS.AuthService.Infrastructure.Persistence.Context;
using SS.AuthService.Infrastructure.Authentication;
using SS.AuthService.API.Configurations.Json;
using SS.AuthService.API.Filters;
using System.IdentityModel.Tokens.Jwt;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.DataProtection;
using SS.AuthService.Infrastructure.Diagnostics;
using SS.AuthService.Infrastructure.Security;
using Microsoft.AspNetCore.HttpOverrides;

var builder = WebApplication.CreateBuilder(args);

// Clear default claim mapping to ensure 'sub' is used as-is
JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();

// 1. Configure CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("DefaultPolicy", policy =>
    {
        var securitySettings = builder.Configuration.GetSection("SecuritySettings").Get<SS.AuthService.Application.Common.Settings.SecuritySettings>();
        var origins = securitySettings?.AllowedCorsOrigins ?? Array.Empty<string>();

        if (origins.Length > 0)
        {
            policy.WithOrigins(origins)
                  .AllowAnyMethod()
                  .AllowAnyHeader()
                  .AllowCredentials();
        }
        else
        {
            // Default for local development if not configured
            policy.WithOrigins("https://localhost:5000")
                  .AllowAnyMethod()
                  .AllowAnyHeader()
                  .AllowCredentials();
        }
    });
});

// 2. Rate Limiting is configured in Infrastructure layer via AddSecurityRateLimiting()

// Add services to the container.
builder.Services.AddControllers(options =>
{
    options.Filters.Add<SanitizeInputFilter>();
})
.AddJsonOptions(options =>
{
    options.JsonSerializerOptions.Converters.Add(new SanitizedStringConverter());
});
builder.Services.AddMemoryCache();
builder.Services.Configure<SS.AuthService.Application.Common.Settings.SecuritySettings>(
    builder.Configuration.GetSection(SS.AuthService.Application.Common.Settings.SecuritySettings.SectionName));
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddSwaggerGen();

// 1. Data Protection Hardening (Enterprise Ready)
builder.Services.AddDataProtection()
    .SetApplicationName("SS.AuthService")
    .PersistKeysToDbContext<AppDbContext>();

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// 2. Configure Options with Validation
builder.Services.AddOptions<JwtOptions>()
    .BindConfiguration(JwtOptions.SectionName)
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.ConfigureOptions<ConfigureJwtBearerOptions>();

// 3. Configure Authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
    });

// 4. Configure Authorization (RBAC)
builder.Services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
builder.Services.AddScoped<IAuthorizationHandler, PermissionHandler>();
builder.Services.AddAuthorization();

// 5. Configure Forwarded Headers for YARP/Proxy
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    
    var securitySettings = builder.Configuration.GetSection("SecuritySettings").Get<SS.AuthService.Application.Common.Settings.SecuritySettings>();
    
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();

    if (securitySettings?.TrustedNetworks != null)
    {
        foreach (var network in securitySettings.TrustedNetworks)
        {
            if (System.Net.IPNetwork.TryParse(network, out var ipNetwork))
                options.KnownIPNetworks.Add(ipNetwork);
        }
    }

    if (securitySettings?.TrustedProxies != null)
    {
        foreach (var proxy in securitySettings.TrustedProxies)
        {
            if (System.Net.IPAddress.TryParse(proxy, out var ipAddress))
                options.KnownProxies.Add(ipAddress);
        }
    }
});

var app = builder.Build();

// 3. Configure Security Headers (Helmet-like)
app.UseSecurityHeaders(new HeaderPolicyCollection()
    .AddDefaultSecurityHeaders()
    .AddContentSecurityPolicy(builder =>
    {
        builder.AddDefaultSrc().Self();
        builder.AddConnectSrc().Self();
        builder.AddFontSrc().Self();
        builder.AddFrameAncestors().None();
        builder.AddImgSrc().Self();
        builder.AddScriptSrc().Self();
        builder.AddStyleSrc().Self();
    })
    .AddCustomHeader("X-Permitted-Cross-Domain-Policies", "none")
    .RemoveServerHeader());

app.UseMiddleware<ExceptionMiddleware>();

app.UseForwardedHeaders();

// NOTE: UsePathBase is removed to avoid conflict with [Route("api/[controller]")] 
// which already handles the /api/auth prefix. This ensures /api/auth/login maps 
// correctly to AuthController.Login.

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors("DefaultPolicy");
app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

// 🛡️ Identity Propagation Policy:
// This service only trusts identity information from validated JWT tokens.
// It DOES NOT use 'X-User-*' headers from proxies to prevent spoofing risks.
// Internal services must propagate the original JWT for identity verification.

// 🏥 Health Checks & Security
app.UseMiddleware<InternalHealthCheckMiddleware>();
app.UseSecurityHealthChecks();

app.MapControllers();

app.Run();
