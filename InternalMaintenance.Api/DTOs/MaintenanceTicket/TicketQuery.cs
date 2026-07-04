using System.ComponentModel.DataAnnotations;
using InternalMaintenance.Api.DTOs.Common;
namespace InternalMaintenance.Api.DTOs.MaintenanceTicket;

public class TicketQuery : PaginationQuery
{
    public string? Status { get; set; }

    public string? Priority { get; set; }

    public int? EquipmentId { get; set; }

}