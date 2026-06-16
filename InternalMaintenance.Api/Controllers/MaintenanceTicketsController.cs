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
                EquipmentCode= ticket.Equipment!.Code,
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
        ).ToListAsync();
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
                EquipmentCode = ticket.Equipment!.Code,
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
    [HttpPatch("{id:int}/assign")]
    public async Task<ActionResult<MaintenanceTicketResponse>> AssignTicket(int id, AssignTicketRequest request)
    {
        var ticket = await _context.MaintenanceTickets
        .FirstOrDefaultAsync(t => t.Id==id);

        if (ticket is null)
        {
           return NotFound(
            new
            {
                message="Ticket Not Found"
            }
           );
        }

        // Chỉ cho assign ticket đang Pending hoặc Assigned
        //Pending = assign lần đầu
        //Assigned= đổi technician nếu cần

        var assignableStatuses  = new[] {"Pending", "Assigned"};
        if (!assignableStatuses.Contains(ticket.Status))
        {
            return BadRequest(
                  new
                  {
                    message="Only pending or assigned tickets can be assigned "
                  }
            );
          
        }

        
        //Không nên assign ticket cho Staff/Admin/Manager
        //Ticket bảo trì phải được giao cho technician xử lý
        var technician = await _context.Users.Include(user=> user.Role).
        FirstOrDefaultAsync(u=>u.Id == request.AssignedTechnicianId);
        
        if(technician is null)
        {
            return BadRequest(
                new
                {
                    message="Technician does not exists"
                }
            );
        }

        if(technician.Role == null || technician.Role.Name != "Technician")
        {
            return BadRequest(
                new
                {
                    message="Assigned user must be a technician"
                }
            );
        }
        var oldStatus = ticket.Status;
        // Update ticket hiện tại
        ticket.AssignedTechnicianId = request.AssignedTechnicianId;
        ticket.Status="Assigned";

        //Ghi lịch sử assign
        var history = new TicketStatusHistory
        {
            MaintenanceTicketId = ticket.Id,
            OldStatus = oldStatus,
            NewStatus="Assigned",

            //Chưa có Auth thì tạm hard code
            //Sau này lấy từ currentUser 
            ChangedByUserId=1,
            ChangedAt = DateTime.UtcNow,
            Note= request.Note
        };

        _context.TicketStatusHistories.Add(history);

        // Lưu UPDATE ticket + insert history xuống DATABASE
        await _context.SaveChangesAsync();

        var response = await _context.MaintenanceTickets
            .Where(t => t.Id == id)
            .Select( t=> new MaintenanceTicketResponse
            {
                Id = t.Id,
                TicketCode = t.TicketCode,
                Title = t.Title,
                Description = t.Description,

                EquipmentId = t.EquipmentId,
                EquipmentCode = t.Equipment!.Code,
                EquipmentName = t.Equipment.Name,

                CreatedByUserId = t.CreatedByUserId,
                CreatedByUserName = t.CreatedByUser!.FullName,

                AssignedTechnicianId = t.AssignedTechnicianId,
                AssignedTechnicianName = t.AssignedTechnician != null
                    ? t.AssignedTechnician.FullName
                    : null,

                Priority = t.Priority,
                Status = t.Status,
                ResolutionNote = t.ResolutionNote,
                CreatedAt = t.CreatedAt,
                ResolvedAt = t.ResolvedAt,
                ClosedAt = t.ClosedAt

            }).FirstAsync();
        return Ok (response);
    }

    [HttpPatch("{id:int}/status")]
    public async Task<ActionResult<MaintenanceTicketResponse>> UpdateTicketStatus(int id, UpdateTicketStatusRequest request)
    {
        var ticket = await _context.MaintenanceTickets
        .FirstOrDefaultAsync( t => t.Id==id);

        if(ticket is null)
        {
            return NotFound(
                new
                {
                    message= "Ticket not found"
                });
        }

        var newStatus = request.Status.Trim();

        //Chỉ cho phép những status có trong workflow xử lý
        // KHông cho client gửi status tùy ý
        var allowedStatuses = new[] { "InProgress", "Resolved", "Closed" };

        if (!allowedStatuses.Contains(newStatus))
        {
            return BadRequest(
                new
                {
                    message="Invalid ticket status"
                }
            );
        }

        //Ticket đã "Closed" nghĩa là quy trình đã kết thúc hoàn toàn.
        //Không cho đổi trạng thái nữa để tránh làm sai lịch sử

        if(ticket.Status == "Closed")
        {
            return BadRequest(
                new
                {
                    message ="Closed ticket cannot be updated"
                }
            );
        }
        
        //Kiểm tra chuyển trạng thái có đúng workflow không 
        // Chỉ cho đi từng bước , không cho nhảy cóc

        var isValidTransition = 
            (ticket.Status =="Assigned" && newStatus=="InProgress") ||
            (ticket.Status=="InProgress"&& newStatus=="Resolved") ||
            (ticket.Status=="Resolved" && newStatus=="Closed");

        if(!isValidTransition)
        {
            return BadRequest(
                new
                {
                    message = $"Cannot change ticket from {ticket.Status} to {newStatus}"
                }
            );
        }
        // Muốn chuyển sang InProgress thì ticket phải được assign cho technician trước.
        // Nếu chưa có AssignedTechnicianId mà cho xử lý thì không biết ai đang phụ trách

        if(newStatus=="InProgress" && ticket.AssignedTechnicianId is null)
        {
            return BadRequest(
                new
                {
                    message="Ticket must be assigned before moving to InProgress"
                }
            );
        }

        // Khi technician báo xử lý xong, bắt buộc phải có kết quả xử lý xong.
        if(newStatus=="Resolved" && string.IsNullOrWhiteSpace(request.ResolutionNote))
        {
            return BadRequest(
                new
                {
                    message="Resolution note is required when resolving a ticket"
                }
            );
        }
        
        var oldStatus = ticket.Status;

        //Update trạng thái hiện tại của ticket
        ticket.Status = newStatus;

        if (newStatus == "Resolved")
        {
            ticket.ResolutionNote = request.ResolutionNote?.Trim();
            ticket.ResolvedAt = DateTime.UtcNow;
        }

        if(newStatus == "Closed")
        {
            ticket.ClosedAt = DateTime.UtcNow;
        }

        //Ghi lại lịch sử đổi trạng thái
        // MaintenanceTicket lưu trạng thái hiện tại
        // TicketStatusHistory lưu timeline từng lần thay đổi 
        var history = new TicketStatusHistory
        {
            MaintenanceTicketId = ticket.Id,
            OldStatus = oldStatus,
            NewStatus = newStatus,
            ChangedByUserId = 1,
            ChangedAt = DateTime.UtcNow,
            Note = request.Note?.Trim()
        };
        
        _context.TicketStatusHistories.Add(history);
        await _context.SaveChangesAsync();

        var response = await _context.MaintenanceTickets
        .Where(t => t.Id==id)
        .Select(
            t=> new MaintenanceTicketResponse
            {
            Id = t.Id,
            TicketCode = t.TicketCode,
            Title = t.Title,
            Description = t.Description,

            EquipmentId = t.EquipmentId,
            EquipmentCode = t.Equipment!.Code,
            EquipmentName = t.Equipment.Name,

            CreatedByUserId = t.CreatedByUserId,
            CreatedByUserName = t.CreatedByUser!.FullName,

            AssignedTechnicianId = t.AssignedTechnicianId,
            AssignedTechnicianName = t.AssignedTechnician != null
                ? t.AssignedTechnician.FullName
                : null,

            Priority = t.Priority,
            Status = t.Status,
            ResolutionNote = t.ResolutionNote,
            CreatedAt = t.CreatedAt,
            ResolvedAt = t.ResolvedAt,
            ClosedAt = t.ClosedAt
            }
        ).FirstAsync();

        return Ok(response);
    }

    [HttpGet("{id:int}/history")]
    public async Task<ActionResult<TicketStatusHistoryResponse>> GetTicketHistory (int id)
    {
        var ticketExists  = await _context.MaintenanceTickets.AnyAsync(t=>t.Id == id);
        if(!ticketExists)
        {
            return BadRequest (
                new
                {
                    message="Ticket not found"
                }
            );
        }
        // Lấy danh sách lịch sử trạng thái của ticket.
        // Sắp xếp theo ChagedAt tăng dần để nhìn được timeline từ cũ đến mới
        var histories = await _context.TicketStatusHistories
        .Where(history => history.MaintenanceTicketId == id)
        .OrderBy(history => history.ChangedAt)
        .Select( history => new TicketStatusHistoryResponse
        {
           Id = history.Id,
           MaintenanceTicketId = history.MaintenanceTicketId,
           OldStatus = history.OldStatus,
           NewStatus = history.NewStatus,
           ChangedByUserId = history.ChangedByUserId,
           ChangedByUserName = history.ChangedByUser!.FullName,
           ChangedAt = history.ChangedAt,
           Note = history.Note
        }).ToListAsync();

        return Ok(histories);
    }
}
