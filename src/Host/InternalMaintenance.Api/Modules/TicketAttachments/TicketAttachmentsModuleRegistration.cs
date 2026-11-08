using InternalMaintenance.Api.Services;
using InternalMaintenance.TicketAttachments.Contracts;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace InternalMaintenance.Api.Modules.TicketAttachments;

/// <summary>
/// Host đăng ký adapter actor vì chỉ Host biết cách đọc claims HTTP;
/// dịch vụ storage, persistence và worker được module Infrastructure tự đăng ký.
/// </summary>
public static class TicketAttachmentsModuleRegistration
{
    public static IServiceCollection AddTicketAttachmentActorContext(this IServiceCollection services)
    {
        // API cung cấp actor qua contract trung lập để module không phụ thuộc CurrentUserService/HttpContext.
        services.AddScoped<ITicketAttachmentActorContext>(provider =>
            provider.GetRequiredService<CurrentUserService>());
        return services;
    }
}
