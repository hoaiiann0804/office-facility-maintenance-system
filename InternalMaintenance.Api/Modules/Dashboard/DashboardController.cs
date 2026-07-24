using InternalMaintenance.Api.Data;
using InternalMaintenance.Api.Modules.Dashboard.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace InternalMaintenance.Api.Modules.Dashboard;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DashboardController : ControllerBase
{
    private readonly AppDbContext _context;

    public DashboardController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet("summary")]
    public async Task<ActionResult<DashboardSummaryResponse>> GetSummary()
    {
        var totalTickets = await _context.MaintenanceTickets.CountAsync();
        var openTickets = await _context.MaintenanceTickets.CountAsync(t => t.Status != "Closed" && t.Status != "Cancelled" && t.Status != "Resolved");
        var resolvedTickets = await _context.MaintenanceTickets.CountAsync(t => t.Status == "Resolved");
        var closedTickets = await _context.MaintenanceTickets.CountAsync(t => t.Status == "Closed");

        var totalEquipment = await _context.Equipment.CountAsync();
        var activeEquipment = await _context.Equipment.CountAsync(e => e.Status == "Active");
        var underMaintenanceEquipment = await _context.Equipment.CountAsync(e => e.Status == "UnderMaintenance");

        var totalTechnicians = await _context.Users.CountAsync(u => u.Role != null && u.Role.Name == "Technician");
        var totalDepartments = await _context.Departments.CountAsync();

        var response = new DashboardSummaryResponse
        {
            TotalTickets = totalTickets,
            OpenTickets = openTickets,
            ResolvedTickets = resolvedTickets,
            ClosedTickets = closedTickets,
            TotalEquipment = totalEquipment,
            ActiveEquipment = activeEquipment,
            UnderMaintenanceEquipment = underMaintenanceEquipment,
            TotalTechnicians = totalTechnicians,
            TotalDepartments = totalDepartments
        };

        return Ok(response);
    }

    [HttpGet("charts")]
    public async Task<ActionResult<ChartDataResponse>> GetCharts()
    {
        var ticketsByStatus = await _context.MaintenanceTickets
            .GroupBy(t => t.Status)
            .Select(g => new ChartItem { Name = g.Key ?? "Unknown", Value = g.Count() })
            .ToListAsync();

        var ticketsByPriority = await _context.MaintenanceTickets
            .GroupBy(t => t.Priority)
            .Select(g => new ChartItem { Name = g.Key ?? "Unknown", Value = g.Count() })
            .ToListAsync();

        var equipmentByDepartment = await _context.Equipment
            .Select(e => e.Department != null ? e.Department.Name : "No Department")
            .GroupBy(name => name)
            .Select(g => new ChartItem { Name = g.Key, Value = g.Count() })
            .ToListAsync();

        var response = new ChartDataResponse
        {
            TicketsByStatus = ticketsByStatus,
            TicketsByPriority = ticketsByPriority,
            EquipmentByDepartment = equipmentByDepartment
        };

        return Ok(response);
    }
}