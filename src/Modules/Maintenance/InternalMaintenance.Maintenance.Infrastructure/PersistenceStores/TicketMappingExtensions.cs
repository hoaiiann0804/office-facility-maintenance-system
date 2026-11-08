using System.Linq.Expressions;
using InternalMaintenance.Assets.Contracts;
using InternalMaintenance.Identity.Contracts;
using InternalMaintenance.Maintenance.Contracts;
using InternalMaintenance.Maintenance.Domain;
using InternalMaintenance.Persistence;
using InternalMaintenance.Vendors.Contracts;
using Microsoft.EntityFrameworkCore;

namespace InternalMaintenance.Maintenance.Infrastructure;

/// <summary>
/// [SHARED CONCERN TRONG NỘI BỘ MODULE MAINTENANCE]
/// Chứa biểu thức ánh xạ SQL (`ResponseProjection`) và các hàm Batch Lookup liên module
/// (`Identity`, `Equipment`, `Vendors`) để lắp ghép `MaintenanceTicketResponse` mà không cần
/// giữ Navigation Property chéo module hay bị lỗi N+1 Query.
/// </summary>
public static class TicketMappingExtensions
{
    /// <summary>
    /// Expression Projection dùng trong IQueryable (EF Core LINQ to Entities)
    /// giúp tối ưu SQL chỉ query các cột cần thiết trên Database.
    /// </summary>
    public static readonly Expression<Func<MaintenanceTicket, MaintenanceTicketResponse>> ResponseProjection =
        ticket => new MaintenanceTicketResponse
        {
            Id = ticket.Id,
            TicketCode = ticket.TicketCode,
            Title = ticket.Title,
            Description = ticket.Description,
            EquipmentId = ticket.EquipmentId,
            EquipmentCode = string.Empty,
            EquipmentName = string.Empty,
            EquipmentDepartmentId = 0,
            EquipmentMaintenanceDepartmentId = null,
            CreatedByUserId = ticket.CreatedByUserId,
            CreatedByUserName = string.Empty,
            AssignedTechnicianId = ticket.AssignedTechnicianId,
            AssignedTechnicianName = null,
            SupportTechnicianId = ticket.SupportTechnicianId,
            SupportTechnicianName = null,
            Priority = ticket.Priority,
            Status = ticket.Status,
            ResolutionNote = ticket.ResolutionNote,
            CancellationReason = ticket.CancellationReason,
            CreatedAt = ticket.CreatedAt,
            ResolvedAt = ticket.ResolvedAt,
            ClosedAt = ticket.ClosedAt,
            DueAt = ticket.DueAt,
            SlaStatus = ticket.SlaStatus,
            SlaPausedAt = ticket.SlaPausedAt,
            TotalSlaPausedMinutes = ticket.TotalSlaPausedMinutes,
            VendorLogs = ticket.VendorLogs.Select(vl => new TicketVendorLogResponse
            {
                Id = vl.Id,
                MaintenanceTicketId = vl.MaintenanceTicketId,
                VendorId = vl.VendorId,
                VendorName = string.Empty,
                DispatchedAt = vl.DispatchedAt,
                EstimatedReturnDate = vl.EstimatedReturnDate,
                ActualReturnDate = vl.ActualReturnDate,
                PausedMinutes = vl.PausedMinutes,
                RepairCost = vl.RepairCost,
                Note = vl.Note,
                CreatedAt = vl.CreatedAt
            }).ToList()
        };

    /// <summary>
    /// Hàm dùng chung cho các Write Stores (`EfTicketCreationStore`, `EfTicketStatusChangeStore`,
    /// `EfTicketAssignmentStore`, `EfTicketUpdateStore`) để nạp lại `MaintenanceTicketResponse` đầy đủ
    /// sau khi ghi DB, loại bỏ việc lặp lại 4 lần cùng một đoạn code ở 4 Store.
    /// </summary>
    public static async Task<MaintenanceTicketResponse?> LoadResponseByIdAsync(
        AppDbContext db,
        int ticketId,
        IIdentityUserDirectory identityUserDirectory,
        IEquipmentTicketContextReader equipmentReader,
        IVendorDirectory vendorDirectory,
        CancellationToken cancellationToken = default)
    {
        var response = await db.MaintenanceTickets
            .Where(ticket => ticket.Id == ticketId)
            .Select(ResponseProjection)
            .FirstOrDefaultAsync(cancellationToken);

        if (response is not null)
        {
            await PopulateAllExternalDetailsAsync(
                [response],
                identityUserDirectory,
                equipmentReader,
                vendorDirectory,
                cancellationToken);
        }

        return response;
    }

    /// <summary>
    /// Điền đồng bộ toàn bộ thông tin liên module (Tên User, Thông tin Thiết bị, Tên Vendor) theo lô.
    /// </summary>
    public static async Task PopulateAllExternalDetailsAsync(
        IReadOnlyCollection<MaintenanceTicketResponse> responses,
        IIdentityUserDirectory identityUserDirectory,
        IEquipmentTicketContextReader equipmentReader,
        IVendorDirectory vendorDirectory,
        CancellationToken cancellationToken = default)
    {
        if (responses.Count == 0)
            return;

        await PopulateUserNamesAsync(responses, identityUserDirectory, cancellationToken);
        await PopulateEquipmentDetailsAsync(responses, equipmentReader, cancellationToken);
        await PopulateVendorNamesAsync(responses, vendorDirectory, cancellationToken);
    }

    public static async Task PopulateUserNamesAsync(
        IReadOnlyCollection<MaintenanceTicketResponse> responses,
        IIdentityUserDirectory identityUserDirectory,
        CancellationToken cancellationToken = default)
    {
        var userIds = responses
            .SelectMany(response => new int?[]
            {
                response.CreatedByUserId,
                response.AssignedTechnicianId,
                response.SupportTechnicianId
            })
            .Where(userId => userId.HasValue)
            .Select(userId => userId!.Value)
            .Distinct()
            .ToArray();

        var names = await identityUserDirectory.FindDisplayNamesAsync(userIds, cancellationToken);
        foreach (var response in responses)
        {
            response.CreatedByUserName = names.GetValueOrDefault(response.CreatedByUserId, string.Empty);
            response.AssignedTechnicianName = response.AssignedTechnicianId is int assignedId
                ? names.GetValueOrDefault(assignedId)
                : null;
            response.SupportTechnicianName = response.SupportTechnicianId is int supportId
                ? names.GetValueOrDefault(supportId)
                : null;
        }
    }

    public static async Task PopulateEquipmentDetailsAsync(
        IReadOnlyCollection<MaintenanceTicketResponse> responses,
        IEquipmentTicketContextReader equipmentReader,
        CancellationToken cancellationToken = default)
    {
        var equipmentById = await equipmentReader.GetManyAsync(
            responses.Select(response => response.EquipmentId).Distinct().ToArray(),
            cancellationToken);

        foreach (var response in responses)
        {
            if (!equipmentById.TryGetValue(response.EquipmentId, out var equipment))
                continue;

            response.EquipmentCode = equipment.Code;
            response.EquipmentName = equipment.Name;
            response.EquipmentDepartmentId = equipment.DepartmentId;
            response.EquipmentMaintenanceDepartmentId = equipment.MaintenanceDepartmentId;
        }
    }

    public static async Task PopulateVendorNamesAsync(
        IReadOnlyCollection<MaintenanceTicketResponse> responses,
        IVendorDirectory vendorDirectory,
        CancellationToken cancellationToken = default)
    {
        var vendorIds = responses
            .SelectMany(response => response.VendorLogs)
            .Select(log => log.VendorId)
            .Distinct()
            .ToArray();
        var names = await vendorDirectory.FindNamesAsync(vendorIds, cancellationToken);

        foreach (var vendorLog in responses.SelectMany(response => response.VendorLogs))
            vendorLog.VendorName = names.GetValueOrDefault(vendorLog.VendorId, string.Empty);
    }
}
