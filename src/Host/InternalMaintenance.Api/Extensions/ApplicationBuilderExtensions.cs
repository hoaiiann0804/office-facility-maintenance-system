using InternalMaintenance.Api.Data;
using InternalMaintenance.Api.Modules.Dashboard;
using InternalMaintenance.Assets.Infrastructure.DependencyInjection;
using InternalMaintenance.Identity.Infrastructure.DependencyInjection;
using InternalMaintenance.Maintenance.Infrastructure.DependencyInjection;
using InternalMaintenance.Organization.Infrastructure.DependencyInjection;
using InternalMaintenance.Persistence;
using InternalMaintenance.Reporting.Infrastructure.DependencyInjection;
using InternalMaintenance.TicketAttachments.Infrastructure.DependencyInjection;
using InternalMaintenance.Vendors.Infrastructure.DependencyInjection;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace InternalMaintenance.Api.Extensions;

public static class ApplicationBuilderExtensions
{
    public static async Task<WebApplication> UseApplicationPipelineAsync(
        this WebApplication app)
    {
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        try
        {
            using var scope = app.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            
            // Ap dung tat ca cac Migration chua thuc thi vao Database va dong bo schema
            await dbContext.Database.MigrateAsync();

            // Seed du lieu mac dinh (Roles, Departments, Equipment, Test Accounts, Vendors)
            await SeedData.InitializeAsync(dbContext);
            app.Logger.LogInformation("Database migrations and seed data initialized successfully.");
        }
        catch (Exception ex)
        {
            app.Logger.LogError(ex,
                "Database initialization/seed failed during startup.");
        }

        app.UseCors(ServiceCollectionExtensions.FrontendCorsPolicyName);
        app.UseAuthentication();
        app.UseAuthorization();

        // [FULLSTACKHERO MINIMAL API ENDPOINT PIPELINE]
        // Thay vì dùng app.MapControllers() quét các Controller cồng kềnh ở Host,
        // Host giờ đây chỉ đóng vai trò Composition Root gọi hàm đăng ký Minimal API Endpoints
        // của từng Module. Mỗi Endpoint nằm ngay trong thư mục Features/{FeatureName}/ của Module đó.
        app.MapVendorEndpoints();
        app.MapOrganizationEndpoints();
        app.MapEquipmentEndpoints();
        app.MapIdentityEndpoints();
        app.MapTicketAttachmentEndpoints(); 
        app.MapReportingEndpoints();
        app.MapMaintenanceEndpoints();
        app.MapDashboardEndpoints();

        // Health Check Endpoint (Giám sát trạng thái API và kết nối Database)
        app.MapHealthChecks("/health", new HealthCheckOptions
        {
            ResponseWriter = async (context, report) =>
            {
                context.Response.ContentType = "application/json";
                var payload = new
                {
                    status = report.Status.ToString(),
                    timestamp = DateTime.UtcNow,
                    durationMs = Math.Round(report.TotalDuration.TotalMilliseconds, 2),
                    components = report.Entries.Select(e => new
                    {
                        name = e.Key,
                        status = e.Value.Status.ToString(),
                        durationMs = Math.Round(e.Value.Duration.TotalMilliseconds, 2),
                        description = e.Value.Description
                    })
                };
                await context.Response.WriteAsJsonAsync(payload);
            }
        });

        app.MapGet("/", () => Results.Ok(new
        {
            service = "Internal Maintenance API",
            status = "running",
            environment = app.Environment.EnvironmentName
        }));

        return app;
    }
}
