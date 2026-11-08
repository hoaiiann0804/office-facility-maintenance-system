using InternalMaintenance.Assets.Contracts;
using InternalMaintenance.Maintenance.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace InternalMaintenance.Assets.Infrastructure.Features.DeleteEquipment;

/// <summary>
/// [MINIMAL API ENDPOINT - XÓA THIẾT BỊ]
///
/// - Luồng đi (Request Flow):
///   HTTP DELETE /api/equipment/{id:int}
///     -> DeleteEquipmentEndpoint.MapDeleteEquipmentEndpoint(...)
///     -> Hỏi IMaintenanceTicketLookup.HasAnyTicketForEquipmentAsync(...) qua hợp đồng công khai
///     -> IEquipmentCatalog.DeleteAsync(...) (điều phối xuống DeleteEquipmentHandler)
///     -> Trả về HTTP 204 NoContent hoặc mã lỗi từ ServiceResult (404 / 409 Conflict).
/// </summary>
public static class DeleteEquipmentEndpoint
{
    public static RouteHandlerBuilder MapDeleteEquipmentEndpoint(this IEndpointRouteBuilder group)
    {
        return group.MapDelete("{id:int}", async (
            int id,
            IEquipmentCatalog catalog,
            IMaintenanceTicketLookup maintenanceTicketLookup,
            CancellationToken cancellationToken) =>
        {
            var hasTicket = await maintenanceTicketLookup.HasAnyTicketForEquipmentAsync(id, cancellationToken);
            var result = await catalog.DeleteAsync(id, hasTicket, cancellationToken);
            if (!result.IsSuccess)
                return Results.Json(new { message = result.Message }, statusCode: result.StatusCode);

            return Results.NoContent();
        })
        .RequireAuthorization(new AuthorizeAttribute { Roles = "Admin" })
        .WithName("DeleteEquipment")
        .Produces(StatusCodes.Status204NoContent);
    }
}

