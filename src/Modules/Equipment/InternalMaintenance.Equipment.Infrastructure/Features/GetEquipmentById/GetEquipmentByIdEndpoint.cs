using System.Security.Claims;
using InternalMaintenance.Assets.Contracts;
using InternalMaintenance.BuildingBlocks.Security;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace InternalMaintenance.Assets.Infrastructure.Features.GetEquipmentById;

/// <summary>
/// [MINIMAL API ENDPOINT - LẤY CHI TIẾT THIẾT BỊ THEO ID]
///
/// - Luồng đi (Request Flow):
///   HTTP GET /api/equipment/{id:int}
///     -> GetEquipmentByIdEndpoint.MapGetEquipmentByIdEndpoint(...)
///     -> IEquipmentCatalog.GetByIdAsync(...) (điều phối xuống GetEquipmentByIdHandler)
///     -> Kiểm tra quyền xem theo phòng ban sở hữu / phòng ban bảo trì
///     -> Trả về HTTP 200 OK, 403 Forbid hoặc 404 NotFound.
/// </summary>
public static class GetEquipmentByIdEndpoint
{
    public static RouteHandlerBuilder MapGetEquipmentByIdEndpoint(this IEndpointRouteBuilder group)
    {
        return group.MapGet("{id:int}", async (
            int id,
            ClaimsPrincipal user,
            IEquipmentCatalog catalog,
            CancellationToken cancellationToken) =>
        {
            var equipment = await catalog.GetByIdAsync(id, cancellationToken);
            if (equipment is null)
                return Results.NotFound(new { message = "Equipment not found" });

            var role = user.GetRole();
            var departmentId = user.GetDepartmentId();
            if (role != "Admin" &&
                equipment.DepartmentId != departmentId &&
                equipment.MaintenanceDepartmentId != departmentId)
                return Results.Forbid();

            return Results.Ok(equipment);
        })
        .RequireAuthorization()
        .WithName("GetEquipmentById")
        .Produces<EquipmentResponse>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status403Forbidden)
        .Produces(StatusCodes.Status404NotFound);
    }
}

