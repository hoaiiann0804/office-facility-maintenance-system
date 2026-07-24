using InternalMaintenance.Api.Common;
using InternalMaintenance.Api.Common.Pagination;
using InternalMaintenance.Api.Constants;
using InternalMaintenance.Api.Data;
using InternalMaintenance.Api.Modules.Tickets.Contracts;
using InternalMaintenance.Api.Models;
using InternalMaintenance.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using InternalMaintenance.Api.Services.Interface;

namespace InternalMaintenance.Api.Modules.Tickets;

[ApiController]
[Route("api/tickets")]
public class MaintenanceTicketsController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly CurrentUserService _currentUserService;

    private readonly ITicketCodeGenerator _ticketCodeGenerator;

    public MaintenanceTicketsController(
        AppDbContext context,
        CurrentUserService currentUserService,
        ITicketCodeGenerator ticketCodeGenerator

        )
    {
        _context = context;
        _currentUserService = currentUserService;
        _ticketCodeGenerator = ticketCodeGenerator;
    }



    // Áp dụng phân quyền dữ liệu cho Ticket theo role
    // Mỗi role chỉ được truy cập các ticket thuộc phạm vi được phép 

    private IQueryable<MaintenanceTicket> ApplyTicketAccessControl(
        IQueryable<MaintenanceTicket> query
    )
    {
        var role = _currentUserService.Role;
        var userId = _currentUserService.UserId;
        var departmentId = _currentUserService.DepartmentId;

        //Admin xem toàn bộ ticket 
        if (role == UserRoles.Admin)
        {
            return query;
        }
        // Manager chỉ xem ticket thuộc phòng ban mình quản lý 
        if (role == UserRoles.Manager)
        {
            return query.Where(
                ticket => ticket.Equipment!.DepartmentId == departmentId
            );
        }
        // Staff chỉ được xem ticket do chính mình tạo
        if (role == UserRoles.Staff)
        {
            return query.Where(
                ticket => ticket.CreatedByUserId == userId
            );
        }
        // Staff chỉ được xem ticket được giao xử lý 
        if (role == UserRoles.Technician)
        {
            return query.Where(
                ticket => ticket.AssignedTechnicianId == userId
            );
        }
        // Role không hợp lệ
        return query.Where(ticket => false);
    }



    [Authorize]
    [HttpGet]
    public async Task<ActionResult<PagedResponse<MaintenanceTicketResponse>>> GetMaintenanceTickets(
        [FromQuery] TicketQuery query
    )
    {
        var ticketQuery = _context.MaintenanceTickets
       .AsNoTracking()
       .AsQueryable();

        ticketQuery = TicketAccessPolicy.Apply(ticketQuery, _currentUserService);
        var status = query.Status?.Trim();
        if (!string.IsNullOrWhiteSpace(status))
        {
            ticketQuery = ticketQuery.Where(
                ticket => ticket.Status == status
            );
        }

        var priority = query.Priority?.Trim();
        if (!string.IsNullOrWhiteSpace(priority))
        {
            ticketQuery = ticketQuery.Where(
                ticket => ticket.Priority == priority
            );
        }

        if (query.EquipmentId.HasValue)
        {
            ticketQuery = ticketQuery.Where(
                ticket => ticket.EquipmentId == query.EquipmentId.Value
            );
        }

        var totalItems = await ticketQuery.CountAsync();
        ticketQuery = ticketQuery
        .OrderByDescending(ticket => ticket.CreatedAt)
        .ThenBy(ticket => ticket.Id);

        ticketQuery = ticketQuery.ApplyPaging(query);

        var tickets = await ticketQuery.Select(
            ticket => new MaintenanceTicketResponse
            {
                Id = ticket.Id,
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
        ).ToListAsync();
        return Ok(tickets.ToPagedResponse(query, totalItems));
    }

    [Authorize]
    [HttpGet("{id:int}")]
    public async Task<ActionResult<MaintenanceTicketResponse>> GetMaintenanceTicketById(int id)
    {
        // Khởi tạo truy vấn và áp dụng phân quyền theo role 
        var ticketQuery = _context.MaintenanceTickets
        .AsNoTracking()
        .AsQueryable();

        ticketQuery = TicketAccessPolicy.Apply(ticketQuery, _currentUserService);
        var ticket = await ticketQuery.Where(
            ticket => ticket.Id == id
        ).Select(
            ticket => new MaintenanceTicketResponse
            {
                Id = ticket.Id,
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

        if (ticket is null)
        {
            return NotFound(
                new
                {
                    message = "Ticket not found"
                }
            );
        }
        return Ok(ticket);
    }

    [Authorize()]
    [HttpPost]
    public async Task<ActionResult<MaintenanceTicketResponse>> CreateMaintenanceTicket(CreateTicketRequest request)
    {
        var title = request.Title.Trim();
        var description = request.Description.Trim();
        var userId = _currentUserService.UserId;
        var role = _currentUserService.Role;
        var departmentId = _currentUserService.DepartmentId;
        var equipment = await _context.Equipment
        .FirstOrDefaultAsync(e => e.Id == request.EquipmentId);

        if (equipment is null)
        {
            return NotFound(
               new
               {
                   message = "Equipment does not exist"
               }
            );
        }
        // Không cho tạo ticket cho thiết bị đã thanh lý

        if (equipment.Status == EquipmentStatuses.Retired)
        {
            return BadRequest(
                new
                {
                    message = "Cannot create ticket for retired equipment"
                }
            );
        }

        //Nếu đang sửa thì không được tạo ticket mới
        if (equipment.Status == EquipmentStatuses.UnderMaintenance)
        {
            return BadRequest(
                new
                {
                    message = "Equipment is already under maintenance"
                }
            );
        }

        // Không cho tạo nhiều ticket đang mở cùng một thiết bị
        // Nếu Equipment này đã có ticket Pending/Assgined/InProgress
        // Nghĩa là sự cố cũ chưa xử lý xong, nên chặn để tránh tạo ticket trùng

        var hasOpenTicket = await _context.MaintenanceTickets
        .AnyAsync(ticket => ticket.EquipmentId == request.EquipmentId
        && TicketWorkflowRules.OpenStatuses.Contains(ticket.Status));

        if (hasOpenTicket)
        {
            return BadRequest(
                new
                {
                    message = "This equipment already has an open maintenance ticket"
                }
            );
        }

        var priority = string.IsNullOrWhiteSpace(request.Priority)
        ? TicketPriorities.Medium : request.Priority.Trim();

        if (!TicketWorkflowRules.IsAllowedPriority(priority))
        {
            return BadRequest(
                new
                {
                    message = "Invalid ticket priority"
                }
            );
        }



        var ticket = new MaintenanceTicket
        {
            TicketCode = _ticketCodeGenerator.GenerateTicketCode(),
            Title = title,
            Description = description,
            EquipmentId = request.EquipmentId,
            Priority = priority,
            Status = TicketStatuses.Pending,
            CreatedByUserId = userId,
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
                Id = t.Id,
                TicketCode = t.TicketCode,
                Title = t.Title,
                Description = t.Description,
                EquipmentId = t.EquipmentId,
                EquipmentCode = t.Equipment!.Code,
                EquipmentName = t.Equipment!.Name,
                CreatedByUserId = t.CreatedByUserId,
                CreatedByUserName = t.CreatedByUser!.FullName,
                AssignedTechnicianId = t.AssignedTechnicianId,
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
            nameof(GetMaintenanceTicketById),
            new { id = ticket.Id },
            response
        );
    }

    [Authorize]
    [HttpPut("{id:int}")]
    public async Task<ActionResult<MaintenanceTicketResponse>> UpdateTicket(int id, UpdateTicketRequest request)
    {
        var ticket = await _context.MaintenanceTickets
        //Load các thông tin liên quan để trả về đầy đủ thông tin ticket sau khi cập nhật 
        .Include(t => t.Equipment)
        .Include(t => t.CreatedByUser)
        .Include(t => t.AssignedTechnician)
        .FirstOrDefaultAsync(t => t.Id == id);
        if (ticket is null)
        {
            return NotFound(
                new
                {
                    message = "Ticket Not Found"
                }
            );
        }

        var role = _currentUserService.Role;
        var userId = _currentUserService.UserId;
        var department = _currentUserService.DepartmentId;

        // Requester (bất kể role) được sửa ticket của mình chỉ khi Pending
        if (ticket.CreatedByUserId == userId)
        {
            if (ticket.Status != TicketStatuses.Pending)
                return BadRequest(new { message = "Ticket already in processing state" });
        }
        else 
        {
            // Không phải Requester, thì chỉ có Admin hoặc Manager (thuộc phòng ban) mới được sửa
            if (role != UserRoles.Admin && role != UserRoles.Manager)
            {
                return Forbid();
            }

            // Manager chỉ sửa được Ticket của phòng ban mình (Owner) 
            if (role == UserRoles.Manager && ticket.Equipment!.DepartmentId != department)
            {
                return Forbid();
            }
        }

        if (!TicketWorkflowRules.IsAllowedPriority(request.Priority))
        {
            return BadRequest("Invalid priority");
        }

        ticket.Title = request.Title.Trim();
        ticket.Description = request.Description.Trim();
        ticket.Priority = request.Priority.Trim();

        await _context.SaveChangesAsync();

        var response = new MaintenanceTicketResponse
        {
            Id = ticket.Id,
            TicketCode = ticket.TicketCode,
            Title = ticket.Title,
            Description = ticket.Description,
            EquipmentId = ticket.EquipmentId,
            EquipmentCode = ticket.Equipment!.Code,
            EquipmentName = ticket.Equipment!.Name,
            CreatedByUserId = ticket.CreatedByUserId,
            CreatedByUserName = ticket.CreatedByUser!.FullName,
            AssignedTechnicianId = ticket.AssignedTechnicianId,
            AssignedTechnicianName = ticket.AssignedTechnician?.FullName,
            Priority = ticket.Priority,
            Status = ticket.Status,
            ResolutionNote = ticket.ResolutionNote,
            CreatedAt = ticket.CreatedAt,
            ResolvedAt = ticket.ResolvedAt,
            ClosedAt = ticket.ClosedAt
        };

        return Ok(response);
    }

    [Authorize(Roles = $"{UserRoles.Admin},{UserRoles.Manager}")]
    [HttpPatch("{id:int}/assign")]
    public async Task<ActionResult<MaintenanceTicketResponse>> AssignTicket(int id, AssignTicketRequest request)
    {
        var role = _currentUserService.Role;
        var departmentId = _currentUserService.DepartmentId;

        var ticket = await _context.MaintenanceTickets
        .Include(t => t.Equipment)
        .FirstOrDefaultAsync(t => t.Id == id);

        if (ticket is null)
        {
            return NotFound(
             new
             {
                 message = "Ticket Not Found"
             }
            );
        }

        var targetDeptId = ticket.Equipment!.MaintenanceDepartmentId ?? ticket.Equipment.DepartmentId;

        // Admin được assign mọi ticket
        if (role == UserRoles.Manager)
        {
            // Manager chỉ assign ticket thuộc phòng ban chuyên môn bảo trì của mình 
            // Nếu thiết bị chưa cấu hình MaintenanceDepartmentId thì fallback về DepartmentId
            if (targetDeptId != departmentId)
            {
                return Forbid();
            }
        }

        // Chặn phân công công việc khi  trạng thái đã  kết thúc và hủy công việc phân công 
        if (ticket.Status == TicketStatuses.Closed || ticket.Status == TicketStatuses.Cancelled)
        {
            return BadRequest(new
            {
                message = "Cannot assign a closed or cancelled ticket"
            });
        }
        // Chỉ cho assign ticket đang Pending hoặc Assigned
        //Pending = assign lần đầu
        //Assigned= đổi technician nếu cần

        if (!TicketWorkflowRules.IsAssignableStatus(ticket.Status))
        {
            return BadRequest(
                  new
                  {
                      message = "Only pending or assigned tickets can be assigned "
                  }
            );

        }


        var technician = await _context.Users.Include(user => user.Role).
        FirstOrDefaultAsync(u => u.Id == request.AssignedTechnicianId);

        if (technician is null)
        {
            return NotFound(
                new
                {
                    message = "Technician does not exists"
                }
            );
        }

        if (technician.Role == null || technician.Role.Name != UserRoles.Technician)
        {
            return BadRequest(
                new
                {
                    message = "Assigned user must be a technician"
                }
            );
        }

        if (technician.DepartmentId != targetDeptId)
        {
            return BadRequest(
                new
                {
                    message = "Assigned technician must belong to the maintenance department for this equipment"
                }
            );
        }

        if (!technician.IsActive)
        {
            return BadRequest(
                new
                {
                    message = "Technician account is inactive"
                });
        }
        var oldStatus = ticket.Status;
        // Update ticket hiện tại
        // Tránh assign lại đúng technician hiện tại.
        // Nếu không có thay đổi thực sự không cần tạo history mới
        if (ticket.AssignedTechnicianId == request.AssignedTechnicianId)
        {
            return BadRequest(
                new
                {
                    message = "Technician already assigned"
                }
            );
        }
        // Cập nhật technician phụ trách ticker
        ticket.AssignedTechnicianId = request.AssignedTechnicianId;
        ticket.Status = TicketStatuses.Assigned;

        //Ghi lịch sử assign
        var history = new TicketStatusHistory
        {
            MaintenanceTicketId = ticket.Id,
            OldStatus = oldStatus,
            NewStatus = TicketStatuses.Assigned,

            //Chưa có Auth thì tạm hard code
            //Sau này lấy từ currentUser 
            ChangedByUserId = _currentUserService.UserId,
            ChangedAt = DateTime.UtcNow,
            Note = request.Note
        };

        _context.TicketStatusHistories.Add(history);

        // Lưu UPDATE ticket + insert history xuống DATABASE
        await _context.SaveChangesAsync();

        var response = await _context.MaintenanceTickets
            .Where(t => t.Id == id)
            .Select(t => new MaintenanceTicketResponse
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
        return Ok(response);
    }

    [Authorize]
    [HttpPatch("{id:int}/status")]
    public async Task<ActionResult<MaintenanceTicketResponse>> TransitionTicketStatus(int id, ChangeTicketStatusRequest request)
    {
        var role = _currentUserService.Role;
        var userId = _currentUserService.UserId;
        var departmentId = _currentUserService.DepartmentId;

        var ticket = await _context.MaintenanceTickets
        .Include(t => t.Equipment)
        .FirstOrDefaultAsync(t => t.Id == id);

        if (ticket is null)
        {
            return NotFound(
                new
                {
                    message = "Ticket not found"
                });
        }

        // Ticket luôn gắn với 1 thiết bị
        // Trạng thái thiết bị cần được đồng bộ với trạng thái ticket
        var equipment = ticket.Equipment;
        if (equipment is null)
        {
            return NotFound(
                new
                {
                    message = "Equipment not found"
                }
            );
        }
        var newStatus = request.Status.Trim();
        //Chỉ cho phép những status có trong workflow xử lý
        // KHông cho client gửi status tùy ý
        if (!new[]
            {
                TicketStatuses.InProgress,
                TicketStatuses.Resolved,
                TicketStatuses.Closed,
                TicketStatuses.Cancelled
            }
            .Contains(newStatus))
        {
            return BadRequest(
                new
                {
                    message = "Invalid ticket status"
                }
            );
        }

        //Ticket đã "Closed" nghĩa là quy trình đã kết thúc hoàn toàn.
        //Không cho đổi trạng thái nữa để tránh làm sai lịch sử

        if (ticket.Status == TicketStatuses.Closed || ticket.Status == TicketStatuses.Cancelled)
        {
            return BadRequest(
                new
                {
                    message = "Finalized ticket cannot be updated"
                }
            );
        }

        //Kiểm tra chuyển trạng thái có đúng workflow không 
        // Chỉ cho đi từng bước , không cho nhảy cóc

        if (!TicketWorkflowRules.CanTransition(ticket.Status, newStatus))
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

        if (newStatus == TicketStatuses.InProgress && ticket.AssignedTechnicianId is null)
        {
            return BadRequest(
                new
                {
                    message = "Ticket must be assigned before moving to InProgress"
                }
            );
        }

        // Chỉ technician được assign mới làm 
        if (newStatus == TicketStatuses.InProgress ||
        newStatus == TicketStatuses.Resolved)
        {
            if (ticket.AssignedTechnicianId != userId)
            {
                return Forbid();
            }
        }

        // Khi technician báo xử lý xong, bắt buộc phải có kết quả xử lý xong.
        if (newStatus == TicketStatuses.Resolved && string.IsNullOrWhiteSpace(request.ResolutionNote))
        {
            return BadRequest(
                new
                {
                    message = "Resolution note is required when resolving a ticket"
                }
            );
        }

        var oldStatus = ticket.Status;



        //Update trạng thái hiện tại của ticket
        ticket.Status = newStatus;

        if (newStatus == TicketStatuses.InProgress)
        {
            equipment.Status = EquipmentStatuses.UnderMaintenance;
        }

        if (newStatus == TicketStatuses.Resolved)
        {
            ticket.ResolutionNote = request.ResolutionNote?.Trim();
            ticket.ResolvedAt = DateTime.UtcNow;
        }

        if (newStatus == TicketStatuses.Closed)
        {
            ticket.ClosedAt = DateTime.UtcNow;
            equipment.Status = EquipmentStatuses.Active;
        }

        if (newStatus == TicketStatuses.Cancelled)
        {
            equipment.Status = EquipmentStatuses.Active;
        }

        // Requester (người tạo), Admin, hoặc Manager (phòng ban sở hữu thiết bị) được nghiệm thu và đóng ticket
        if (newStatus == TicketStatuses.Closed)
        {
            var canClose = ticket.CreatedByUserId == userId
            || role == UserRoles.Admin
            || (role == UserRoles.Manager && equipment.DepartmentId == departmentId);

            if (!canClose)
            {
                return Forbid();
            }
        }

        // Requester, Admin, hoặc Manager được phép hủy ticket 
        if (newStatus == TicketStatuses.Cancelled)
        {
            var canCancel = ticket.CreatedByUserId == userId
            || role == UserRoles.Admin 
            || (role == UserRoles.Manager && equipment.DepartmentId == departmentId);

            if (!canCancel)
            {
                return Forbid();
            }
        }

        //Ghi lại lịch sử đổi trạng thái
        // MaintenanceTicket lưu trạng thái hiện tại
        // TicketStatusHistory lưu timeline từng lần thay đổi 
        var history = new TicketStatusHistory
        {
            MaintenanceTicketId = ticket.Id,
            OldStatus = oldStatus,
            NewStatus = newStatus,
            ChangedByUserId = _currentUserService.UserId,
            ChangedAt = DateTime.UtcNow,
            Note = request.Note?.Trim()
        };

        _context.TicketStatusHistories.Add(history);
        await _context.SaveChangesAsync();

        var response = await _context.MaintenanceTickets
        .Where(t => t.Id == id)
        .Select(
            t => new MaintenanceTicketResponse
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

    [Authorize]
    [HttpPost("{id:int}/comments")]
    public async Task<ActionResult<TicketCommentResponse>> CreateComment(int id, CreateTicketCommentRequest request)
    {
        var userId = _currentUserService.UserId;
        var role = _currentUserService.Role;
        var departmentId = _currentUserService.DepartmentId;

        var ticket = await _context.MaintenanceTickets
        .Include(t => t.Equipment)
        .FirstOrDefaultAsync(t => t.Id == id);

        if (ticket is null)
        {
            return NotFound(
                new
                {
                    message = "Ticket not found"
                }
            );
        }

        if (ticket.Status == TicketStatuses.Closed || ticket.Status == TicketStatuses.Cancelled)
        {
            return BadRequest(
                new
                {
                    message = "Cannot comment on finalized ticket"
                }
            );
        }

        var canComment =
        //Admin được comment mọi ticket
        role == UserRoles.Admin ||
        //Manager chỉ comment ticket thuộc phòng ban mình quản lý
        (role == UserRoles.Manager && ticket.Equipment!.DepartmentId == departmentId)
        ||
        //Staff chỉ commment do chính mình tạo 
        (role == UserRoles.Staff && ticket.CreatedByUserId == userId) ||
        //Technician chỉ comment ticket được assign xử lý 
        (role == UserRoles.Technician && ticket.AssignedTechnicianId == userId);

        if (!canComment)
        {
            return Forbid();
        }

        // Lấy thông tin người dùng đăng nhập
        // UserId lấy từ JWT token thông qua CurrentService
        // Cần FullName để trả về TicketCommentResponse

        var currentUser = await _context.Users
        // Chỉ đọc dữ liệu, không cập nhật nên dùng AsNoTracking
        .AsNoTracking()
        .Where(user => user.Id == userId)
        // Chỉ lấy những field cần dùng
        // Tránh load toàn bộ thông tin User không cần thiết 
        .Select(user => new
        {
            user.FullName
        }).FirstAsync();

        var comment = new TicketComment
        {
            MaintenanceTicketId = ticket.Id,
            UserId = userId,
            Content = request.Content.Trim(),
            CreatedAt = DateTime.UtcNow
        };
        _context.TicketComments.Add(comment);
        await _context.SaveChangesAsync();

        var response = new TicketCommentResponse
        {
            Id = comment.Id,
            UserId = userId,
            UserName = currentUser.FullName,
            Content = comment.Content,
            CreatedAt = comment.CreatedAt
        };

        return CreatedAtAction(
            nameof(GetCommentById),
            new { id = comment.Id },
            response
        );
    }

    [Authorize]
    [HttpGet("{id:int}/comments")]
    public async Task<ActionResult<List<TicketCommentResponse>>> GetCommentById(int id)
    {
        // Kiểm tra Ticket có tổn tại và người dùng có quyền xem hay không

        var ticketQuery = _context.MaintenanceTickets
        .AsNoTracking()
        .AsQueryable();

        ticketQuery = TicketAccessPolicy.Apply(ticketQuery, _currentUserService);
        var ticketExists = await ticketQuery.AnyAsync(
            ticket => ticket.Id == id
        );
        if (!ticketExists)
        {
            return NotFound(
            new
            {
                message = "Ticket not found"
            });
        }
        var comments = await _context.TicketComments
        .AsNoTracking()
        .Where(comment => comment.MaintenanceTicketId == id)
        .OrderBy(comment => comment.CreatedAt)
        .Select(comment => new TicketCommentResponse
        {
            Id = comment.Id,
            UserId = comment.UserId,
            UserName = comment.User!.FullName,
            Content = comment.Content,
            CreatedAt = comment.CreatedAt
        }).ToListAsync();

        return Ok(comments);
    }
    [Authorize]
    [HttpGet("{id:int}/history")]
    public async Task<ActionResult<List<TicketStatusHistoryResponse>>> GetTicketHistory(int id)
    {
        var ticketQuery = _context.MaintenanceTickets.AsQueryable();
        ticketQuery = TicketAccessPolicy.Apply(ticketQuery, _currentUserService);
        var ticketExists = await ticketQuery.AnyAsync(t => t.Id == id);
        if (!ticketExists)
        {
            return BadRequest(
                new
                {
                    message = "Ticket not found"
                }
            );
        }
        // Lấy danh sách lịch sử trạng thái của ticket.
        // Sắp xếp theo ChagedAt tăng dần để nhìn được timeline từ cũ đến mới
        var histories = await _context.TicketStatusHistories
        .Where(history => history.MaintenanceTicketId == id)
        .OrderBy(history => history.ChangedAt)
        .Select(history => new TicketStatusHistoryResponse
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
