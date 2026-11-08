using InternalMaintenance.Assets.Infrastructure.Features.CreateEquipment;
using InternalMaintenance.Assets.Infrastructure.Features.DeleteEquipment;
using InternalMaintenance.Assets.Infrastructure.Features.GetEquipmentById;
using InternalMaintenance.Assets.Infrastructure.Features.GetEquipmentPage;
using InternalMaintenance.Assets.Infrastructure.Features.UpdateEquipment;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace InternalMaintenance.Assets.Infrastructure.DependencyInjection;

/// <summary>
/// [MODULE ENDPOINT REGISTRATION - EQUIPMENT]
/// Gom toàn bộ 5 Minimal API Endpoints của các Vertical Slices trong module Equipment
/// vào chung một Route Group `/api/equipment` theo chuẩn FullStackHero.
/// </summary>
public static class EquipmentEndpoints
{
    public static IEndpointRouteBuilder MapEquipmentEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("api/equipment")
            .WithTags("Equipment");

        group.MapGetEquipmentPageEndpoint();
        group.MapGetEquipmentByIdEndpoint();
        group.MapCreateEquipmentEndpoint();
        group.MapUpdateEquipmentEndpoint();
        group.MapDeleteEquipmentEndpoint();

        return app;
    }
}

