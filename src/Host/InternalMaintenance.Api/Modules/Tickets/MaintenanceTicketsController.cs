using InternalMaintenance.BuildingBlocks.Pagination;
using InternalMaintenance.Api.Modules.Tickets.Contracts;
using InternalMaintenance.Maintenance.Application;
using InternalMaintenance.Maintenance.Contracts;
using InternalMaintenance.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InternalMaintenance.Api.Modules.Tickets;

[ApiController]
[Route("api/tickets")]
public class MaintenanceTicketsController : ControllerBase
{
    private readonly CurrentUserService _currentUserService;
    private readonly IMaintenanceTicketReader _ticketReader;

    private readonly ChangeTicketStatusHandler _changeTicketStatusHandler;
    private readonly CreateMaintenanceTicketHandler _createMaintenanceTicketHandler;
    private readonly AssignMaintenanceTicketHandler _assignMaintenanceTicketHandler;
    private readonly UpdateMaintenanceTicketHandler _updateMaintenanceTicketHandler;
    private readonly CreateTicketCommentHandler _createTicketCommentHandler;

    public MaintenanceTicketsController(
        CurrentUserService currentUserService,
        IMaintenanceTicketReader ticketReader,
        ChangeTicketStatusHandler changeTicketStatusHandler,
        CreateMaintenanceTicketHandler createMaintenanceTicketHandler,
        AssignMaintenanceTicketHandler assignMaintenanceTicketHandler,
        UpdateMaintenanceTicketHandler updateMaintenanceTicketHandler,
        CreateTicketCommentHandler createTicketCommentHandler

        )
    {
        _currentUserService = currentUserService;
        _ticketReader = ticketReader;
        _changeTicketStatusHandler = changeTicketStatusHandler;
        _createMaintenanceTicketHandler = createMaintenanceTicketHandler;
        _assignMaintenanceTicketHandler = assignMaintenanceTicketHandler;
        _updateMaintenanceTicketHandler = updateMaintenanceTicketHandler;
        _createTicketCommentHandler = createTicketCommentHandler;
    }



    [Authorize]
    [HttpGet]
    public async Task<ActionResult<PagedResponse<MaintenanceTicketResponse>>> GetMaintenanceTickets(
        [FromQuery] TicketQuery query,
        CancellationToken cancellationToken
    )
    {
        var page = await _ticketReader.GetPageAsync(query, CurrentActor, cancellationToken);
        return Ok(page);
    }

    [Authorize]
    [HttpGet("{id:int}")]
    public async Task<ActionResult<MaintenanceTicketResponse>> GetMaintenanceTicketById(
        int id,
        CancellationToken cancellationToken)
    {
        var ticket = await _ticketReader.GetByIdAsync(id, CurrentActor, cancellationToken);
        if (ticket is null)
            return NotFound(new { message = "Ticket not found" });
        return Ok(ticket);
    }

    [Authorize()]
    [HttpPost]
    public async Task<ActionResult<MaintenanceTicketResponse>> CreateMaintenanceTicket(CreateTicketRequest request)
    {
        // Chuyển request và identity đã xác thực thành command. Use case xử lý quy tắc thiết bị,
        // ticket trùng và khởi tạo aggregate; controller chỉ chuyển kết quả nghiệp vụ thành HTTP.
        var outcome = await _createMaintenanceTicketHandler.Handle(
            new CreateMaintenanceTicketCommand(
                request.Title,
                request.Description,
                request.EquipmentId,
                request.Priority,
                _currentUserService.UserId),
            HttpContext.RequestAborted);

        if (outcome.IsSuccess)
        {
            return CreatedAtAction(
                nameof(GetMaintenanceTicketById),
                new { id = outcome.TicketId },
                outcome.Response);
        }

        return outcome.Error switch
        {
            CreateMaintenanceTicketError.EquipmentNotFound => NotFound(new { message = outcome.Message }),
            CreateMaintenanceTicketError.DuplicateRace => Conflict(new { message = outcome.Message }),
            _ => BadRequest(new { message = outcome.Message })
        };
    }
    [Authorize]
    [HttpPut("{id:int}")]
    public async Task<ActionResult<MaintenanceTicketResponse>> UpdateTicket(int id, UpdateTicketRequest request)
    {
        var outcome = await _updateMaintenanceTicketHandler.Handle(
            new UpdateMaintenanceTicketCommand(
                id,
                request.Title,
                request.Description,
                request.Priority,
                _currentUserService.UserId,
                _currentUserService.Role,
                _currentUserService.DepartmentId),
            HttpContext.RequestAborted);

        if (outcome.IsSuccess)
            return Ok(outcome.Response);

        return outcome.Error switch
        {
            TicketUpdateError.NotFound => NotFound(new { message = outcome.Message }),
            TicketUpdateError.Forbidden => Forbid(),
            _ => BadRequest(new { message = outcome.Message })
        };
    }

    [Authorize(Roles = $"{UserRoles.Admin},{UserRoles.Manager}")]
    [HttpPatch("{id:int}/assign")]
    public async Task<ActionResult<MaintenanceTicketResponse>> AssignTicket(int id, AssignTicketRequest request)
    {
        // Claims được chuyển thành actor context để use case tự kiểm tra quyền theo department.
        // Controller chỉ điều phối HTTP; query ticket/technician và ghi history nằm ngoài đây.
        var outcome = await _assignMaintenanceTicketHandler.Handle(
            new AssignMaintenanceTicketCommand(
                id,
                request.AssignedTechnicianId,
                request.SupportTechnicianId,
                request.Note,
                _currentUserService.UserId,
                _currentUserService.Role,
                _currentUserService.DepartmentId),
            HttpContext.RequestAborted);

        if (outcome.IsSuccess)
            return Ok(outcome.Response);

        return outcome.Error switch
        {
            TicketAssignmentError.NotFound => NotFound(new { message = outcome.Message }),
            TicketAssignmentError.Forbidden => Forbid(),
            _ => BadRequest(new { message = outcome.Message })
        };
    }
    [Authorize]
    [HttpPatch("{id:int}/status")]
    public async Task<ActionResult<MaintenanceTicketResponse>> TransitionTicketStatus(int id, ChangeTicketStatusRequest request)
    {
        // API chuyển request và identity đã xác thực thành command. Handler điều phối workflow,
        // vì vậy endpoint không tự tải hoặc sửa EF entity.
        var outcome = await _changeTicketStatusHandler.Handle(
            new ChangeTicketStatusCommand(
                id,
                request.Status,
                request.ResolutionNote,
                request.CancellationReason,
                request.Note,
                request.VendorId,
                request.VendorEstimatedReturnDate,
                request.VendorNote,
                _currentUserService.UserId,
                _currentUserService.Role,
                _currentUserService.DepartmentId),
            HttpContext.RequestAborted);

        if (outcome.IsSuccess)
            return Ok(outcome.Response);

        return outcome.Error switch
        {
            TicketStatusChangeError.NotFound => NotFound(new { message = outcome.Message }),
            TicketStatusChangeError.Forbidden => Forbid(),
            _ => BadRequest(new { message = outcome.Message })
        };
    }
    [Authorize]
    [HttpPost("{id:int}/comments")]
    public async Task<ActionResult<TicketCommentResponse>> CreateComment(int id, CreateTicketCommentRequest request)
    {
        // API chỉ chuyển claims + body thành command. Quyền truy cập, trạng thái cho phép comment,
        // truy vấn tên actor và ghi comment được gom trong use case của Maintenance.
        var outcome = await _createTicketCommentHandler.Handle(
            new CreateTicketCommentCommand(
                id,
                _currentUserService.UserId,
                _currentUserService.Role,
                _currentUserService.DepartmentId,
                request.Content),
            HttpContext.RequestAborted);

        if (outcome.IsSuccess)
            return CreatedAtAction(nameof(GetCommentById), new { id }, outcome.Response);

        return outcome.Error switch
        {
            TicketCommentError.NotFound => NotFound(new { message = outcome.Message }),
            TicketCommentError.Forbidden => Forbid(),
            TicketCommentError.UserNotFound => Unauthorized(new { message = outcome.Message }),
            _ => BadRequest(new { message = outcome.Message })
        };
    }

    [Authorize]
    [HttpGet("{id:int}/comments")]
    public async Task<ActionResult<IReadOnlyList<TicketCommentResponse>>> GetCommentById(
        int id,
        CancellationToken cancellationToken)
    {
        var comments = await _ticketReader.GetCommentsAsync(id, CurrentActor, cancellationToken);
        return comments is null
            ? NotFound(new { message = "Ticket not found" })
            : Ok(comments);
    }
    [Authorize]
    [HttpGet("{id:int}/history")]
    public async Task<ActionResult<IReadOnlyList<TicketStatusHistoryResponse>>> GetTicketHistory(
        int id,
        CancellationToken cancellationToken)
    {
        var histories = await _ticketReader.GetHistoryAsync(id, CurrentActor, cancellationToken);
        return histories is null
            ? BadRequest(new { message = "Ticket not found" })
            : Ok(histories);
    }

    private MaintenanceTicketActor CurrentActor => new(
        _currentUserService.UserId,
        _currentUserService.Role,
        _currentUserService.DepartmentId);
}

