using System.ComponentModel.DataAnnotations;
using InternalMaintenance.Api.Common.Pagination;
namespace InternalMaintenance.Api.Modules.Tickets.Contracts;

public class TicketQuery : PaginationQuery
{
    public string? Status { get; set; }

    public string? Priority { get; set; }

    public int? EquipmentId { get; set; }

}
