using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using WebAnalytics.Infrastructure.Data;
using WebAnalytics.Infrastructure.IServices;
using WebAnalytics.Infrastructure.MessageBroker;
using WebAnalytics.Infrastructure.Services;

namespace WebAnalytics.Infrastructure;

public static class InfrastructureServiceExtensions
{
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDatabase(configuration);
        services.AddAuthentication(configuration);
        services.AddApplicationServices();
        services.AddMessageBroker(configuration);

        return services;
    }

    private static IServiceCollection AddDatabase(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

        return services;
    }

    private static IServiceCollection AddAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        var jwtSettings = configuration.GetSection("JWT");
        var secret = jwtSettings["Secret"] ?? throw new InvalidOperationException("JWT Secret not configured");

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtSettings["Issuer"],
                    ValidAudience = jwtSettings["Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret))
                };

                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();
                        if (!string.IsNullOrEmpty(authHeader) &&
                            !authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                        {
                            context.Token = authHeader;
                        }
                        return Task.CompletedTask;
                    }
                };
            });

        return services;
    }

    private static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        // Domain Services
        services.AddScoped<IDataIngestionService, DataIngestionService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IReportService, ReportService>();
        services.AddScoped<IAnalyticsService, AnalyticsService>();

        return services;
    }

    private static IServiceCollection AddMessageBroker(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<RabbitMQService>();
        // Add other message broker configurations

        return services;
    }
}