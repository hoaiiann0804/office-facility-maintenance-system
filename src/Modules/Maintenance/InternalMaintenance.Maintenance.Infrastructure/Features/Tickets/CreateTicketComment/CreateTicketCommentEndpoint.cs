using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using InternalMaintenance.BuildingBlocks.Security;
using InternalMaintenance.Maintenance.Application;
using InternalMaintenance.Maintenance.Contracts;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace InternalMaintenance.Maintenance.Infrastructure.Features.Tickets.CreateTicketComment;

public sealed class CreateTicketCommentEndpointRequest
{
    [Required(ErrorMessage = "Content must not be empty or whitespace only.")]
    [StringLength(1000)]
    public string Content { get; set; } = string.Empty;
}

/// <summary>
/// [MINIMAL API ENDPOINT - THÊM BÌNH LUẬN VÀO PHIẾU BẢO TRÌ]
///
/// - Luồng đi (Request Flow):
///   HTTP POST /api/tickets/{id:int}/comments
///     -> CreateTicketCommentEndpoint.MapCreateTicketCommentEndpoint(...)
///     -> CreateTicketCommentHandler.Handle(...)
///     -> Trả về HTTP 201 CreatedAtRoute("GetCommentById") hoặc 404/403/401/400.
/// </summary>
public static class CreateTicketCommentEndpoint
{
    public static RouteHandlerBuilder MapCreateTicketCommentEndpoint(this IEndpointRouteBuilder group)
    {
        return group.MapPost("{id:int}/comments", async (
            int id,
            [FromBody] CreateTicketCommentEndpointRequest request,
            ClaimsPrincipal user,
            CreateTicketCommentHandler handler,
            CancellationToken cancellationToken) =>
        {
            var outcome = await handler.Handle(
                new CreateTicketCommentCommand(
                    id,
                    user.GetRequiredUserId(),
                    user.GetRole(),
                    user.GetDepartmentId(),
                    request.Content),
                cancellationToken);

            if (outcome.IsSuccess)
                return Results.CreatedAtRoute("GetCommentById", new { id }, outcome.Response);

            return outcome.Error switch
            {
                TicketCommentError.NotFound => Results.NotFound(new { message = outcome.Message }),
                TicketCommentError.Forbidden => Results.Forbid(),
                TicketCommentError.UserNotFound => Results.Json(
                    new { message = outcome.Message },
                    statusCode: StatusCodes.Status401Unauthorized),
                _ => Results.BadRequest(new { message = outcome.Message })
            };
        })
        .RequireAuthorization()
        .WithName("CreateComment")
        .Produces<TicketCommentResponse>(StatusCodes.Status201Created);
    }
}

