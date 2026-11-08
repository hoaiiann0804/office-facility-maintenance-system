using System.Text;
using InternalMaintenance.Persistence;
using InternalMaintenance.Api.Modules.Auth;
using InternalMaintenance.Api.Modules.TicketAttachments;
using InternalMaintenance.TicketAttachments.Infrastructure.DependencyInjection;
using InternalMaintenance.Assets.Infrastructure.DependencyInjection;
using InternalMaintenance.Identity.Infrastructure.DependencyInjection;
using InternalMaintenance.Maintenance.Infrastructure.DependencyInjection;
using InternalMaintenance.Reporting.Infrastructure.DependencyInjection;
using InternalMaintenance.Organization.Infrastructure.DependencyInjection;
using InternalMaintenance.Vendors.Infrastructure.DependencyInjection;
using InternalMaintenance.Api.Modules.Reports;
using InternalMaintenance.Api.Services;
using InternalMaintenance.Api.Services.Implementation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace InternalMaintenance.Api.Extensions;

public static class ServiceCollectionExtensions
{
    public const string FrontendCorsPolicyName = "FrontendCors";

    public static IServiceCollection AddApplicationServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "Missing connection string: ConnectionStrings:DefaultConnection (or environment variable ConnectionStrings__DefaultConnection)");
        }

        // AddDbContext mặc định tạo context Scoped. Store của Maintenance và gateway của Equipment
        // vì thế nhận cùng một instance trong request; use case đổi trạng thái chỉ SaveChanges một lần
        // để Ticket, Equipment, vendor log và history commit/rollback cùng nhau trong SQL Server.
        services.AddDbContext<AppDbContext>(options =>
            // Migration vẫn được quản lý trong API host; DbContext/mapping chuyển sang Persistence.
            // Ghi rõ assembly để EF runtime và dotnet ef tiếp tục tìm đúng lịch sử migration hiện có.
            options.UseSqlServer(connectionString, sql =>
                sql.MigrationsAssembly(typeof(ServiceCollectionExtensions).Assembly.GetName().Name)));

        // Health Checks: Giám sát trạng thái API và kết nối SQL Server
        services.AddHealthChecks()
            .AddCheck<DatabaseHealthCheck>("database", tags: ["ready"]);

        var allowedOrigins = configuration
            .GetSection("Cors:AllowedOrigins")
            .Get<string[]>() ?? [];

        // Hỗ trợ linh hoạt tất cả các tên miền Vercel (Production & Preview) và Localhost
        services.AddCors(options =>
        {
            options.AddPolicy(FrontendCorsPolicyName, policy =>
            {
                policy.SetIsOriginAllowed(origin =>
                {
                    if (string.IsNullOrWhiteSpace(origin)) return false;
                    try
                    {
                        var uri = new Uri(origin);
                        var host = uri.Host;
                        return host.Equals("localhost", StringComparison.OrdinalIgnoreCase) ||
                               host.EndsWith(".vercel.app", StringComparison.OrdinalIgnoreCase) ||
                               allowedOrigins.Contains(origin, StringComparer.OrdinalIgnoreCase);
                    }
                    catch
                    {
                        return false;
                    }
                })
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
        services.ConfigureHttpJsonOptions(options =>
        {
            options.SerializerOptions.Converters.Add(new InternalMaintenance.Api.Common.UtcDateTimeJsonConverter());
        });
        services.AddControllers().AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.Converters.Add(new InternalMaintenance.Api.Common.UtcDateTimeJsonConverter());
        });
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen();
        
        services.AddHttpClient();
        services.AddHttpContextAccessor();

        services.AddTicketAttachmentsModule(configuration);
        services.AddTicketAttachmentActorContext();
        // Host biết cách lắp module, nhưng không phải biết từng implementation bên trong module.
        services.AddMaintenanceModule(configuration);
        services.AddEquipmentModule();
        services.AddIdentityModule();
        services.AddOrganizationModule();
        services.AddVendorModule();
        services.AddReportingModule();
        services.AddReportsModule();
        // CurrentUserService đọc claims HTTP và được nhiều module dùng; đây là adapter theo request
        // của Host, còn JWT token generator được đăng ký trong IdentityModuleRegistration.
        services.AddScoped<CurrentUserService>();

        return services;
    }

    private static string GetRequiredConfigurationValue(
        IConfiguration configuration,
        string key)
    {
        var value = configuration[key];
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException($"{key} is missing from configuration/environment variables");
        }

        return value;
    }
}
