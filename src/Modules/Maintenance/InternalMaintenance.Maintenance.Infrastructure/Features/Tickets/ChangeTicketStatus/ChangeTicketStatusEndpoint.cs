using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using InternalMaintenance.BuildingBlocks.Security;
using InternalMaintenance.Maintenance.Application;
using InternalMaintenance.Maintenance.Contracts;
using InternalMaintenance.Maintenance.Domain;
using InternalMaintenance.Persistence;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace InternalMaintenance.Maintenance.Infrastructure.Features.Tickets.ChangeTicketStatus;

public sealed class ChangeTicketStatusEndpointRequest
{
    [Required(ErrorMessage = "Status is required")]
    [StringLength(30, ErrorMessage = "Status must not exceed 30 characters")]
    public string Status { get; set; } = string.Empty;

    public string? ResolutionNote { get; set; }
    public string? CancellationReason { get; set; }
    public string? Note { get; set; }
    public int? VendorId { get; set; }
    public DateTime? VendorEstimatedReturnDate { get; set; }
    public string? VendorNote { get; set; }
    // [FIX G-01]: Chi phí sửa chữa thực tế của đối tác khi nhận thiết bị về (WaitingForVendor -> InProgress).
    public decimal? VendorRepairCost { get; set; }
}

/// <summary>
/// [MINIMAL API ENDPOINT - CHUYỂN ĐỔI TRẠNG THÁI PHIẾU BẢO TRÌ]
///
/// - Luồng đi (Request Flow):
///   HTTP PATCH /api/tickets/{id:int}/status
///     -> ChangeTicketStatusEndpoint.MapChangeTicketStatusEndpoint(...)
///     -> ChangeTicketStatusHandler.Handle(...)
///     -> Trả về HTTP 200 OK hoặc 404/403/400.
/// </summary>
public static class ChangeTicketStatusEndpoint
{
    public static RouteHandlerBuilder MapChangeTicketStatusEndpoint(this IEndpointRouteBuilder group)
    {
        return group.MapPatch("{id:int}/status", async (
            int id,
            [FromBody] ChangeTicketStatusEndpointRequest request,
            ClaimsPrincipal user,
            ChangeTicketStatusHandler handler,
            AppDbContext db,
            IEmailService emailService,
            CancellationToken cancellationToken) =>
        {
            var outcome = await handler.Handle(
                new ChangeTicketStatusCommand(
                    id,
                    request.Status,
                    request.ResolutionNote,
                    request.CancellationReason,
                    request.Note,
                    request.VendorId,
                    request.VendorEstimatedReturnDate,
                    request.VendorNote,
                    user.GetRequiredUserId(),
                    user.GetRole(),
                    user.GetDepartmentId(),
                    request.VendorRepairCost),
                cancellationToken);

            if (outcome.IsSuccess)
            {
                // [FIX G-06]: Khi ticket vừa chuyển sang trạng thái Resolved, gọi SendSlaResultAsync
                // để gửi email thông báo kết quả xử lý (MetSLA / MissedSLA) cho người tạo ticket.
                // Bọc trong try/catch để nếu môi trường dev chưa cấu hình Resend API key thì thao tác
                // hoàn thành ticket trên giao diện vẫn thành công bình thường.
                if (outcome.Response?.Status == TicketStatuses.Resolved)
                {
                    try
                    {
                        var resolvedTicket = await db.MaintenanceTickets
                            .AsNoTracking()
                            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
                        if (resolvedTicket is not null)
                        {
                            var isMet = resolvedTicket.SlaStatus == SlaPolicy.MetSLA;
                            await emailService.SendSlaResultAsync(resolvedTicket, isMet, cancellationToken);
                        }
                    }
                    catch (Exception)
                    {
                        // Bỏ qua lỗi gửi email kết quả SLA để không làm gián đoạn phản hồi HTTP 200 OK.
                    }
                }

                return Results.Ok(outcome.Response);
            }

            return outcome.Error switch
            {
                TicketStatusChangeError.NotFound => Results.NotFound(new { message = outcome.Message }),
                TicketStatusChangeError.Forbidden => Results.Forbid(),
                _ => Results.BadRequest(new { message = outcome.Message })
            };
        })
        .RequireAuthorization()
        .WithName("TransitionTicketStatus")
        .Produces<MaintenanceTicketResponse>(StatusCodes.Status200OK);
    }
}

