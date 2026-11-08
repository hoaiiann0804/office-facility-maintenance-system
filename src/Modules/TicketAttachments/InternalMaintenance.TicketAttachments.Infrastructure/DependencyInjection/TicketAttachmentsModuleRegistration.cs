using InternalMaintenance.TicketAttachments.Contracts;
using InternalMaintenance.TicketAttachments.Infrastructure.Features.ConfirmAttachment;
using InternalMaintenance.TicketAttachments.Infrastructure.Features.CreatePresign;
using InternalMaintenance.TicketAttachments.Infrastructure.Features.DeleteAttachment;
using InternalMaintenance.TicketAttachments.Infrastructure.Features.GetDownloadUrl;
using InternalMaintenance.TicketAttachments.Infrastructure.Features.GetTicketAttachments;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace InternalMaintenance.TicketAttachments.Infrastructure.DependencyInjection;

/// <summary>
/// [ĐĂNG KÝ DI CHO VERTICAL SLICES CỦA MODULE TICKET ATTACHMENTS]
/// </summary>
public static class TicketAttachmentsModuleRegistration
{
    public static IServiceCollection AddTicketAttachmentsModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<R2AttachmentStorageOptions>(configuration.GetSection("R2"));

        services.AddScoped<CreatePresignAttachmentHandler>();
        services.AddScoped<ConfirmAttachmentHandler>();
        services.AddScoped<GetTicketAttachmentsHandler>();
        services.AddScoped<GetAttachmentDownloadUrlHandler>();
        services.AddScoped<DeleteAttachmentHandler>();

        services.AddScoped<ITicketAttachmentsService>(sp =>
            ActivatorUtilities.CreateInstance<TicketAttachmentsService>(sp));
        services.AddScoped<IAttachmentStorageService, R2AttachmentStorageService>();
        services.AddScoped<AttachmentStorageCleanupProcessor>();
        services.AddHostedService<AttachmentStorageCleanupWorker>();
        return services;
    }
}
