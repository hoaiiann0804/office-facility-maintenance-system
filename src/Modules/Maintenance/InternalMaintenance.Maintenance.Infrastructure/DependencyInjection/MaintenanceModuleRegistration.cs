using InternalMaintenance.Maintenance.Application;
using InternalMaintenance.Maintenance.Contracts;
using InternalMaintenance.Maintenance.Contracts.Reporting;
using InternalMaintenance.Maintenance.Infrastructure.Features.Dashboard;
using InternalMaintenance.Maintenance.Infrastructure.Features.Tickets.Common;
using InternalMaintenance.Maintenance.Infrastructure.Features.Tickets.GetTicketById;
using InternalMaintenance.Maintenance.Infrastructure.Features.Tickets.GetTicketComments;
using InternalMaintenance.Maintenance.Infrastructure.Features.Tickets.GetTicketHistory;
using InternalMaintenance.Maintenance.Infrastructure.Features.Tickets.GetTicketsPage;
using InternalMaintenance.Maintenance.Infrastructure.Reporting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace InternalMaintenance.Maintenance.Infrastructure.DependencyInjection;

/// <summary>
/// [ĐĂNG KÝ DI CHO VERTICAL SLICES CỦA MODULE MAINTENANCE (CORE)]
/// </summary>
public static class MaintenanceModuleRegistration
{
    public static IServiceCollection AddMaintenanceModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Write-side Vertical Slices & Stores
        services.AddScoped<ITicketStatusChangeStore, EfTicketStatusChangeStore>();
        services.AddScoped<ChangeTicketStatusHandler>();
        services.AddScoped<ITicketAssignmentStore, EfTicketAssignmentStore>();
        services.AddScoped<AssignMaintenanceTicketHandler>();
        services.AddScoped<ITicketUpdateStore, EfTicketUpdateStore>();
        services.AddScoped<UpdateMaintenanceTicketHandler>();
        services.AddScoped<ITicketCommentStore, EfTicketCommentStore>();
        services.AddScoped<CreateTicketCommentHandler>();
        services.AddScoped<ITicketCreationStore, EfTicketCreationStore>();
        services.AddScoped<CreateMaintenanceTicketHandler>();
        services.AddScoped<ITicketCodeGenerator, TicketCodeGenerator>();

        // Read-side Vertical Slices & Visibility Filter
        services.AddScoped<TicketVisibilityFilter>();
        services.AddScoped<GetTicketsPageHandler>();
        services.AddScoped<GetTicketByIdHandler>();
        services.AddScoped<GetTicketCommentsHandler>();
        services.AddScoped<GetTicketHistoryHandler>();
        services.AddScoped<GetMaintenanceDashboardHandler>();

        // Public Read Ports & Thin Façades
        services.AddScoped<IMaintenanceTicketReader>(sp =>
            ActivatorUtilities.CreateInstance<MaintenanceTicketReader>(sp));
        services.AddScoped<IMaintenanceTicketAccessChecker, MaintenanceTicketAccessChecker>();
        services.AddScoped<IMaintenanceTicketLookup, MaintenanceTicketLookup>();
        services.AddScoped<IMaintenanceDashboardReader>(sp =>
            ActivatorUtilities.CreateInstance<MaintenanceDashboardReader>(sp));

        // Reporting Read Stores owned by Maintenance
        services.AddScoped<ITechnicianPerformanceReadStore, EfTechnicianPerformanceReadStore>();
        services.AddScoped<ISlaComplianceReadStore, EfSlaComplianceReadStore>();
        services.AddScoped<IMaintenanceCostReadStore, EfMaintenanceCostReadStore>();
        services.AddScoped<ISlaOutboxFailureReader, SlaOutboxFailureReader>();
        services.AddScoped<ISlaOutboxRetryService, SlaOutboxRetryService>();

        // Transactional Outbox Email & SLA Monitor Background Worker
        services.Configure<ResendOptions>(configuration.GetSection(ResendOptions.SectionName));
        services.AddHttpClient(ResendEmailService.HttpClientName, client =>
            client.Timeout = TimeSpan.FromSeconds(10));
        services.AddScoped<IEmailService, ResendEmailService>();
        services.AddHostedService<SlaMonitorWorker>();

        return services;
    }
}
