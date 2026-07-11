using InternalMaintenance.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace InternalMaintenance.Api.Extensions;

public static class ApplicationBuilderExtensions
{
    private const string FrontendCorsPolicyName = "FrontendCors";

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
            await dbContext.Database.MigrateAsync();
            await SeedData.InitializeAsync(dbContext);
        }
        catch (Exception ex)
        {
            app.Logger.LogError(ex,
                "Database migration/seed failed during startup. The API will keep running, but DB-backed endpoints may fail.");
        }

        app.UseCors(FrontendCorsPolicyName);
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapControllers();

        app.MapGet("/", () => Results.Ok(new
        {
            service = "Internal Maintenance API",
            status = "running",
            environment = app.Environment.EnvironmentName
        }));

        return app;
    }
}
