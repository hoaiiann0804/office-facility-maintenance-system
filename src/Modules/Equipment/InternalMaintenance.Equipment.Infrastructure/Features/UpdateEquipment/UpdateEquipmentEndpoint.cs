using InternalMaintenance.Assets.Contracts;
using InternalMaintenance.Maintenance.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace InternalMaintenance.Assets.Infrastructure.Features.UpdateEquipment;

/// <summary>
/// [MINIMAL API ENDPOINT - CẬP NHẬT THIẾT BỊ]
///
/// - Luồng đi (Request Flow):
///   HTTP PUT /api/equipment/{id:int}
///     -> UpdateEquipmentEndpoint.MapUpdateEquipmentEndpoint(...)
///     -> Hỏi IMaintenanceTicketLookup.HasEquipmentOccupyingTicketAsync(...) qua hợp đồng công khai
///     -> IEquipmentCatalog.UpdateAsync(...) (điều phối xuống UpdateEquipmentHandler)
///     -> Trả về HTTP 200 OK hoặc mã lỗi từ ServiceResult.
/// </summary>
public static class UpdateEquipmentEndpoint
{
    public static RouteHandlerBuilder MapUpdateEquipmentEndpoint(this IEndpointRouteBuilder group)
    {
        return group.MapPut("{id:int}", async (
            int id,
            [FromBody] UpdateEquipmentRequest request,
            IEquipmentCatalog catalog,
            IMaintenanceTicketLookup maintenanceTicketLookup,
            CancellationToken cancellationToken) =>
        {
            var hasOpenTicket = await maintenanceTicketLookup
                .HasEquipmentOccupyingTicketAsync(id, cancellationToken);
            var result = await catalog.UpdateAsync(id, request, hasOpenTicket, cancellationToken);
            if (!result.IsSuccess)
                return Results.Json(new { message = result.Message }, statusCode: result.StatusCode);

            return Results.Ok(result.Data);
        })
        .RequireAuthorization(new AuthorizeAttribute { Roles = "Admin" })
        .WithName("UpdateEquipment")
        .Produces<EquipmentResponse>(StatusCodes.Status200OK);
    }
}

