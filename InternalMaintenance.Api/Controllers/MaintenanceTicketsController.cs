using InternalMaintenance.Api.Data;
using InternalMaintenance.Api.DTOs;
using InternalMaintenance.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InternalMaintenance.Api.Controllers;

[ApiController]
[Route("api/tickets")]
public class MaintenanceTicketsController: ControllerBase
{
    private readonly AppDbContext _context;
    public MaintenanceTicketsController (AppDbContext context)
    {
        _context = context;
    }

    private string GenerateTicketCode()
    {
        var now = DateTime.UtcNow;

        // Thêm randomPart để giảm nguy cơ trùng mã nếu nhiều người tạo ticket cùng lúc.
        // KHông dùng CountAsync vì 2 request đồng thời có thể cùng count một số thứ tự 
        var randomPart = Guid.NewGuid()
        .ToString("N")
        .Substring(0,4)
        .ToUpper();

        return $"TICKET-{now:yyyyMMdd-HHmmss}-{randomPart}";
    }
    
    [HttpGet]
    public async Task<ActionResult<List<MaintenanceTicket>>> GetMaintenanceTickets()
    {
        var tickets = await _context.MaintenanceTickets.Select(
            ticket => new MaintenanceTicketResponse
            {
                Id= ticket.Id,
                TicketCode = ticket.TicketCode,
                Title = ticket.Title,
                Description = ticket.Description,
                EquipmentId = ticket.EquipmentId,
                EquipmentName = ticket.Equipment!.Name,
                CreatedByUserId = ticket.CreatedByUserId,
                CreatedByUserName = ticket.CreatedByUser!.FullName, 
                // (co the dung cach  CreatedByUserName = ticket.CreatedByUser!= null? ticket.CreatedByUser.FullName: string.Empty )
                AssignedTechnicianId = ticket.AssignedTechnicianId,
                AssignedTechnicianName = ticket.AssignedTechnician != null ? ticket.AssignedTechnician.FullName : null,
                Priority = ticket.Priority,
                Status = ticket.Status,
                ResolutionNote = ticket.ResolutionNote,
                CreatedAt = ticket.CreatedAt,
                ResolvedAt = ticket.ResolvedAt,
                ClosedAt = ticket.ClosedAt,
            }
        ).FirstOrDefaultAsync();
        return Ok(tickets);
    }

     [HttpGet("{id:int}")]
    public async Task<ActionResult<List<MaintenanceTicket>>> GetGetMaintenanceTicketById (int id)
    {
        var ticket = await _context.MaintenanceTickets.Where(
            ticket => ticket.Id == id
        ).Select(
            ticket => new MaintenanceTicketResponse
            {
                Id= ticket.Id,
                TicketCode = ticket.TicketCode,
                Title = ticket.Title,
                Description = ticket.Description,
                EquipmentId = ticket.EquipmentId,
                EquipmentName = ticket.Equipment!.Name,
                CreatedByUserId = ticket.CreatedByUserId,
                CreatedByUserName = ticket.CreatedByUser!.FullName, 
                // (co the dung cach  CreatedByUserName = ticket.CreatedByUser!= null? ticket.CreatedByUser.FullName: string.Empty )
                AssignedTechnicianId = ticket.AssignedTechnicianId,
                AssignedTechnicianName = ticket.AssignedTechnician != null ? ticket.AssignedTechnician.FullName : null,
                Priority = ticket.Priority,
                Status = ticket.Status,
                ResolutionNote = ticket.ResolutionNote,
                CreatedAt = ticket.CreatedAt,
                ResolvedAt = ticket.ResolvedAt,
                ClosedAt = ticket.ClosedAt,          
            }
        ).FirstOrDefaultAsync();

        if(ticket is null)
        {
            return NotFound(
                new
                {
                    message ="Equipment not found"
                }
            );
        }
        return Ok(ticket);
    }

    [HttpPost]
    public async Task<ActionResult<MaintenanceTicketResponse>> CreateMaintenanceTicket(CreateTicketRequest request){
        var title = request.Title.Trim();
        var description = request.Description.Trim();

        var equipmentExists  = await _context.Equipment
        .AnyAsync(e => e.Id == request.EquipmentId);

        if(!equipmentExists)
        {
         return BadRequest(
            new
            {
                 message = "Equipment does not exist"
            }
         );
        }

        
        // Không cho tạo nhiều ticket đang mở cùng một thiết bị
        // Nếu Equipment này đã có ticket Pending/Assgined/InProgress
        // Nghĩa là sự cố cũ chưa xử lý xong, nên chặn để tránh tạo ticket trùng

        var openStatus = new[] { "Pending", "Assigned", "InProgress" };

        var hasOpenTicket = await _context.MaintenanceTickets
        .AnyAsync(ticket => ticket.EquipmentId == request.EquipmentId 
        && openStatus.Contains(ticket.Status));

        if (hasOpenTicket)
        {
            return BadRequest(
                new
                {
                    message="This equipment already has an open maintenance ticket"
                }
            );
        }   
        // kiểm tra User tạo tickets cho có tồn tại ? 
        var createUserExists = await _context.Users
        .AnyAsync(u=>u.Id == request.CreatedByUserId);

        if (!createUserExists)
        {
            return BadRequest(
                new
                {
                    message="Created user does not exist"
                }
            );
        }

        // kiểm tra Priority có hợp lệ 

        var priority = string.IsNullOrWhiteSpace(request.Priority)
        ? "Medium":request.Priority.Trim();

        var allowedPriorities  = new[] {"Low", "Medium", "High", "Urgent"};

        if (!allowedPriorities.Contains(priority))
        {
            return BadRequest (
                new
                {
                    message="Invalid ticket priority"
                }
            );
        }

        var ticket = new MaintenanceTicket
        {
            TicketCode = GenerateTicketCode(),
            Title = title,
            Description = description,
            EquipmentId = request.EquipmentId,
            Priority = priority,
            Status = "Pending",
            CreatedByUserId = request.CreatedByUserId,
            AssignedTechnicianId = null,
            CreatedAt = DateTime.UtcNow,
        };

        _context.MaintenanceTickets.Add(ticket);
        await _context.SaveChangesAsync();

        var response = await _context.MaintenanceTickets
        .Where(t => t.Id == ticket.Id)
        .Select(
            t => new MaintenanceTicketResponse
            {
                Id=t.Id,
                TicketCode=t.TicketCode,
                Title = t.Title,
                Description = t.Description,
                EquipmentId = t.EquipmentId,
                EquipmentCode = t.Equipment!.Code,
                EquipmentName = t.Equipment!.Name,
                CreatedByUserId = t.CreatedByUserId,
                CreatedByUserName= t.CreatedByUser!.FullName,
                AssignedTechnicianId= t.AssignedTechnicianId,
                AssignedTechnicianName = t.AssignedTechnician != null ? t.AssignedTechnician.FullName : null,
                Priority = t.Priority,
                Status = t.Status,
                ResolutionNote = t.ResolutionNote,
                CreatedAt = t.CreatedAt,
                ResolvedAt = t.ResolvedAt,
                ClosedAt = t.ClosedAt
            }
        ).FirstOrDefaultAsync();
        
        return CreatedAtAction(
            nameof(GetGetMaintenanceTicketById),
            new{id=ticket.Id},
            response
        );

    }
}
