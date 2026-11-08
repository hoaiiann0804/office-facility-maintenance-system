using InternalMaintenance.Assets.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace InternalMaintenance.Assets.Infrastructure.Features.CreateEquipment;

/// <summary>
/// [MINIMAL API ENDPOINT - TẠO MỚI THIẾT BỊ]
///
/// - Luồng đi (Request Flow):
///   HTTP POST /api/equipment
///     -> CreateEquipmentEndpoint.MapCreateEquipmentEndpoint(...)
///     -> IEquipmentCatalog.CreateAsync(...) (điều phối xuống CreateEquipmentHandler)
///     -> Trả về HTTP 201 CreatedAtRoute("GetEquipmentById") hoặc mã lỗi từ ServiceResult.
/// </summary>
public static class CreateEquipmentEndpoint
{
    public static RouteHandlerBuilder MapCreateEquipmentEndpoint(this IEndpointRouteBuilder group)
    {
        return group.MapPost("", async (
            [FromBody] CreateEquipmentRequest request,
            IEquipmentCatalog catalog,
            CancellationToken cancellationToken) =>
        {
            var result = await catalog.CreateAsync(request, cancellationToken);
            if (!result.IsSuccess)
                return Results.Json(new { message = result.Message }, statusCode: result.StatusCode);

            return Results.CreatedAtRoute("GetEquipmentById", new { id = result.Data!.Id }, result.Data);
        })
        .RequireAuthorization(new AuthorizeAttribute { Roles = "Admin" })
        .WithName("CreateEquipment")
        .Produces<EquipmentResponse>(StatusCodes.Status201Created);
    }
}

