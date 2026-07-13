using InternalMaintenance.Api.Data;
using InternalMaintenance.Api.Modules.Auth;
using InternalMaintenance.Api.Services;
using InternalMaintenance.Api.Services.Interface;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Text;

namespace InternalMaintenance.Api.Extensions;

public static class ServiceCollectionExtensions
{
    private const string FrontendCorsPolicyName = "FrontendCors";

    public static IServiceCollection AddApplicationServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("Missing connection string: ConnectionStrings__DefaultConnection");
        }

        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(connectionString));

        var allowedOrigins = configuration
            .GetSection("Cors:AllowedOrigins")
            .Get<string[]>() ?? ["http://localhost:5173"];

        services.AddCors(options =>
        {
            options.AddPolicy(FrontendCorsPolicyName, policy =>
            {
                policy.WithOrigins(allowedOrigins)
                      .AllowAnyHeader()
                      .AllowAnyMethod();
            });
        });

        var jwtKey = GetRequiredConfigurationValue(configuration, "Jwt:Key");
        var jwtIssuer = GetRequiredConfigurationValue(configuration, "Jwt:Issuer");
        var jwtAudience = GetRequiredConfigurationValue(configuration, "Jwt:Audience");

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwtIssuer,
                    ValidateAudience = true,
                    ValidAudience = jwtAudience,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(jwtKey)
                    ),
                    ClockSkew = TimeSpan.FromMinutes(1)
                };
            });

        services.AddAuthorization();
        services.AddControllers();
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen();
        services.AddHttpContextAccessor();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<JwtTokenService>();
        services.AddScoped<CurrentUserService>();
        services.AddScoped<ITicketCodeGenerator, TicketCodeGenerator>();

        return services;
    }

    private static string GetRequiredConfigurationValue(
        IConfiguration configuration,
        string key)
    {
        var value = configuration[key];
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException($"{key} is missing");
        }

        return value;
    }
}
