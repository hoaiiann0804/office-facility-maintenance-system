using InternalMaintenance.Assets.Contracts;
using InternalMaintenance.Assets.Infrastructure.Features.CreateEquipment;
using InternalMaintenance.Assets.Infrastructure.Features.DeleteEquipment;
using InternalMaintenance.Assets.Infrastructure.Features.GetEquipmentById;
using InternalMaintenance.Assets.Infrastructure.Features.GetEquipmentPage;
using InternalMaintenance.Assets.Infrastructure.Features.UpdateEquipment;
using Microsoft.Extensions.DependencyInjection;

namespace InternalMaintenance.Assets.Infrastructure.DependencyInjection;

/// <summary>
/// [ĐĂNG KÝ DI CHO VERTICAL SLICES CỦA MODULE EQUIPMENT]
/// Sử dụng `ActivatorUtilities.CreateInstance` cho Thin Façade `EquipmentCatalog` để DI Container
/// ưu tiên constructor gắn `[ActivatorUtilitiesConstructor]` mà không báo lỗi Ambiguous Constructors
/// khi bật `ValidateOnBuild = true`.
/// </summary>
public static class EquipmentModuleRegistration
{
    public static IServiceCollection AddEquipmentModule(this IServiceCollection services)
    {
        // 5 Vertical Slice Handlers của module Equipment
        services.AddScoped<GetEquipmentPageHandler>();
        services.AddScoped<GetEquipmentByIdHandler>();
        services.AddScoped<CreateEquipmentHandler>();
        services.AddScoped<UpdateEquipmentHandler>();
        services.AddScoped<DeleteEquipmentHandler>();

        // Public Ports & Thin Façade
        services.AddScoped<IEquipmentCatalog>(sp =>
            ActivatorUtilities.CreateInstance<EquipmentCatalog>(sp));
        services.AddScoped<IEquipmentDashboardReader, EquipmentDashboardReader>();
        services.AddScoped<IEquipmentMaintenanceGateway, EquipmentMaintenanceGateway>();
        services.AddScoped<IEquipmentTicketContextReader, EquipmentTicketContextReader>();
        return services;
    }
}
