using System.ComponentModel.DataAnnotations;
namespace InternalMaintenance.Api.DTOs.MaintenanceTicket;

public class TicketQuery
{
    public string? Status { get; set; }

    public string? Priority { get; set; }

    public int? EquipmentId { get; set; }

    [Range(1, int.MaxValue)]
    public int Page { get; set; } = 1;

    [Range(1, 100)]
    public int PageSize { get; set; } = 10;
}