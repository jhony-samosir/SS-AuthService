using Microsoft.AspNetCore.RateLimiting;
using SS.AuthService.API.Middlewares;
using SS.AuthService.Application;
using SS.AuthService.Infrastructure;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// 1. Configure CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("DefaultPolicy", policy =>
    {
        // Hanya izinkan domain spesifik (ganti dengan URL frontend asli di production)
        policy.WithOrigins("https://localhost:3000") 
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

// 2. Configure Rate Limiting
builder.Services.AddRateLimiter(options =>
{
    // Policy ketat untuk Login/Register (Berbasis IP)
    options.AddPolicy("StrictPolicy", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = 5, // Max 5 request per 15 menit per IP
                Window = TimeSpan.FromMinutes(15),
                QueueLimit = 0
            }));

    // Global policy
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = 100,
                Window = TimeSpan.FromMinutes(1)
            }));

    options.OnRejected = async (context, token) =>
    {
        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        await context.HttpContext.Response.WriteAsJsonAsync(new { message = "Too many requests. Please try again later." }, token);
    };
});

// Add services to the container.
builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddSwaggerGen();

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

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

app.MapControllers();

app.Run();
