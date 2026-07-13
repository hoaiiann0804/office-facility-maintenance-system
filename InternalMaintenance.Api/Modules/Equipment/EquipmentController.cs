using InternalMaintenance.Api.Common;
using InternalMaintenance.Api.Constants;
using InternalMaintenance.Api.Models;
using InternalMaintenance.Api.Data;
using InternalMaintenance.Api.Common.Pagination;
using InternalMaintenance.Api.Modules.Equipment.Contracts;
using EquipmentEntity = InternalMaintenance.Api.Models.Equipment;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InternalMaintenance.Api.Modules.Equipment;

[ApiController]
[Route("api/equipment")]

public class EquipmentController : ControllerBase
{
    private readonly AppDbContext _context;
    public EquipmentController(AppDbContext context)
    {
        _context = context;

    }

    [Authorize]
    [HttpGet]
    public async Task<ActionResult<PagedResponse<EquipmentResponse>>> GetEquipment(
        [FromQuery] EquipmentQuery query
    )
    {
        var equipmentQuery = _context.Equipment
        .AsNoTracking()
        .AsQueryable();
        var keyword = query.Keyword?.Trim();
        if (!string.IsNullOrWhiteSpace(keyword))
        {
            equipmentQuery = equipmentQuery.Where(
                equipment => equipment.Name.Contains(keyword)
                || equipment.Code.Contains(keyword)
                || (equipment.Description != null && equipment.Description.Contains(keyword)));
        }
        var status = query.Status?.Trim();
        if (!string.IsNullOrWhiteSpace(status))
        {
            equipmentQuery = equipmentQuery.Where(e =>
                e.Status == status
            );
        }
        if (query.DepartmentId.HasValue)
        {
            equipmentQuery = equipmentQuery.Where(
                equipment => equipment.DepartmentId == query.DepartmentId.Value
            );
        }

        var totalItems = await equipmentQuery.CountAsync();
        equipmentQuery = equipmentQuery
        .OrderByDescending(equipment => equipment.CreatedAt)
        .ThenBy(equipment => equipment.Id);

        equipmentQuery = equipmentQuery.ApplyPaging(query);

        var equipment = await equipmentQuery
            .Select(e => new EquipmentResponse
            {
                Id = e.Id,
                Code = e.Code,
                Name = e.Name,
                DepartmentId = e.DepartmentId,
                // ! : bắt buộc phải có dữ liệu để chạy (sẽ ko null khi chạy)
                DepartmentName = e.Department!.Name,
                Status = e.Status,
                PurchasedDate = e.PurchasedDate,
                Description = e.Description,
                CreatedAt = e.CreatedAt,
                UpdatedAt = e.UpdatedAt,
            }
            ).ToListAsync();
        return Ok(equipment.ToPagedResponse(query, totalItems));
    }

    [Authorize]
    [HttpGet("{id:int}")]
    public async Task<ActionResult<EquipmentResponse>> GetEquipmentById(int id)
    {
        var equipment = await _context.Equipment
        .Where(e => e.Id == id)
        .Select(
            e => new EquipmentResponse
            {
                Id = e.Id,
                Code = e.Code,
                Name = e.Name,
                DepartmentId = e.DepartmentId,
                DepartmentName = e.Department!.Name,
                Status = e.Status,
                PurchasedDate = e.PurchasedDate,
                Description = e.Description,
                CreatedAt = e.CreatedAt,
                UpdatedAt = e.UpdatedAt,
            }
        ).FirstOrDefaultAsync();

        if (equipment is null)
        {
            return NotFound(new
            {
                message = "Equipment not found"
            });
        }
        return Ok(equipment);
    }

    [Authorize(
        Roles =
        $"{UserRoles.Admin}," +
        $"{UserRoles.Manager}"
    )]
    [HttpPost]
    public async Task<ActionResult<EquipmentResponse>> CreateEquipment(CreateEquipmentRequest request)
    {
        var code = request.Code.Trim();
        var name = request.Name.Trim();

        // Kiểm tra phòng ban có tồn tại ? 
        var departmentExists = await _context.Departments.AnyAsync(
            e => e.Id == request.DepartmentId
        );

        if (!departmentExists)
        {
            return NotFound(
                new
                {
                    message = "Department does not exist"
                }
            );
        }

        //Kiểm tra mã code có tồn tại ?  
        // lưu ý : Vì ngoài thực tế , nhiều thiết bị cùng tên, nên ko check Unique cho Name
        //Nhưng mỗi thiết bị phải có mã định danh riêng (Code)
        var codeExists = await _context.Equipment.AnyAsync(
            e => e.Code == code
        );

        if (codeExists)
        {
            return NotFound(
                new
                {
                    message = "Equipment code already exists"
                }
            );
        }

        if (request.PurchasedDate.HasValue)
        {
            var today = DateTime.UtcNow.Date;
            if (request.PurchasedDate.Value.Date > today)
            {
                return BadRequest(
                new
                {
                    message = "Purchased date cannot be in the future"
                }
                );
            }
        }
        var status = string.IsNullOrWhiteSpace(request.Status)
        ? EquipmentStatuses.Active : request.Status.Trim();

        var allowedStatus = new[]
        {
            EquipmentStatuses.Active,
            EquipmentStatuses.Inactive,
        };

        if (!allowedStatus.Contains(status))
        {
            return BadRequest(
                new
                {
                    message = "Invalid equipment status"
                }
            );
        }

        var equipment = new EquipmentEntity
        {
            Code = code,
            Name = name,
            DepartmentId = request.DepartmentId,
            Status = status,
            PurchasedDate = request.PurchasedDate,
            Description = request.Description?.Trim()
        };

        _context.Equipment.Add(equipment);
        await _context.SaveChangesAsync();

        // Lý do dùng WHERE + SELEC: 
        //1. Về Business: Frontend cần hiển thị Tên phòng Ban (DepartName) không phải mỗi ID số.
        //2. Về kỹ thuật: Biến "equipment" trên RAM chỉ có DepartmentId. Ta bắt buộc phải dùng 
        // Where theo đúng Id vừa tạo để EF Core tự động JOIN sang bảng Department để Lấy tên về.
        var response = await _context.Equipment
        .Where(e => e.Id == equipment.Id)
        .Select(e => new EquipmentResponse
        {
            Id = e.Id,
            Code = e.Code,
            Name = e.Name,
            DepartmentId = e.DepartmentId,
            DepartmentName = e.Department!.Name, // Tên phòng ban từ bảng liên kết (Join) dưới database 
            Status = e.Status,
            PurchasedDate = e.PurchasedDate,
            Description = e.Description,
            CreatedAt = e.CreatedAt,
            UpdatedAt = e.UpdatedAt
        }).FirstOrDefaultAsync();

        return CreatedAtAction(
            nameof(GetEquipmentById),
            new { id = equipment.Id },
            response
        );
    }

    [Authorize(
       Roles =
       $"{UserRoles.Admin}," +
       $"{UserRoles.Manager}"
   )]
    [HttpPut("{id:int}")]
    public async Task<ActionResult<EquipmentResponse>> UpdateEquipment(int id, UpdateEquipmentRequest request)
    {

        var equipment = await _context.Equipment.FirstOrDefaultAsync(e => e.Id == id);
        if (equipment is null)
        {
            return NotFound(
                new
                {
                    message = "Equipment Not Found"
                }
            );

        }
        // Chuẩn hóa dữ liểu Text trước khi so sánh và lưu database
        // TRánh trường hợp  " LAP-IT-001 " và "LAP-IT-001" bị xem là khác nhau.  
        var code = request.Code.Trim();
        var name = request.Name.Trim();

        //Mỗi thiết bị bắt buộc phải có phòng ban thật
        //Nếu PartmentId không tồn tại thì ko cho update để tránh dữ liệu mồ coi
        var departmentExists = await _context.Departments
        .AnyAsync(e => e.Id == request.DepartmentId);

        if (!departmentExists)
        {
            return NotFound(
                new
                {
                    message = "Department does not exist"
                }
            );
        }

        // Không updte Code vì code  là mã định duy nhất của thiết bị

        if (equipment.Code != code)
        {
            return BadRequest(
                new
                {
                    message = "Equipment code cannot be changed after creation"

                }
            );
        }

        // Nếu client không gửi status hoặc gửi chuỗi rỗng thì mặc định là Active
        // Điều này giúp thiết bị có trạng thái hợp lệ sau khi update.

        var status = string.IsNullOrWhiteSpace(request.Status) ? EquipmentStatuses.Active : request.Status.Trim();

        // Chỉ cho phép các trạng thái nằm trong quy trình quản lý thiết bị 
        // Tránh việc client gửi status tùy ý như "BrokeABC", "abc", "Done"
        // UnderMantenance KHông cho phép set thủ công vì nó được quyết định bởi Ticket flow
        var allowedStatuses = new[] {
            EquipmentStatuses.Active,
            EquipmentStatuses.Inactive,
            EquipmentStatuses.Retired,
        };

        if (!allowedStatuses.Contains(status))
        {
            return BadRequest(new
            {
                message = "Invalid equipment status"
            });
        }

        // Không cho Retired nếu thiết bị đang có ticket chưa xử lý xong 
        // Vì retired ngưng sử dụng vĩch viễn, không thể tồn tại và đang cố xử lý 
        var hasOpenTicket = await _context.MaintenanceTickets
        .AnyAsync(ticket => ticket.EquipmentId == equipment.Id
        && (
            ticket.Status == TicketStatuses.Pending ||
            ticket.Status == TicketStatuses.Assigned ||
            ticket.Status == TicketStatuses.InProgress
        ));

        if (request.Status == EquipmentStatuses.Retired && hasOpenTicket)
        {
            return BadRequest(
                new
                {
                    message = "Cannot retire equipment while it has open mantenance tickets"
                }
            );
        }


        // PuscharsedDate là ngày mua/ngày đưa vào sử dụng
        // KHông hợp lý nếu ngày mua trong tương lai

        if (request.PurchasedDate.HasValue)
        {
            var today = DateTime.UtcNow.Date;
            if (request.PurchasedDate.Value.Date > today)
            {
                return BadRequest(
                new
                {
                    message = "Purchased date cannot be in the future"
                }
                );
            }
        }

        //  EF Core đang tracking Object equipment lấy từ database
        //  Chỉ cần gán lại field, sau đó SaveChangeAsync là EF tự sinh câu SQL UPDATE
        equipment.Code = code;
        equipment.Name = name;
        equipment.DepartmentId = request.DepartmentId;
        equipment.Status = status;
        equipment.PurchasedDate = request.PurchasedDate;
        equipment.Description = request.Description?.Trim();

        //  Ghi lại thời điểm cập nhật gần nhất sau khi audit/debug
        equipment.UpdatedAt = DateTime.UtcNow;

        //Đây mới là thay đổi được ghi thật xuống SQL Server
        await _context.SaveChangesAsync();

        //Query lại response sau khi update để lấy thêm DepartmentName từ bảng Departments.
        // DepartmentName không phải cột trong Equipment , mà là dữ liệu join từ Department.Name.
        var response = await _context.Equipment
        .Where(e => e.Id == equipment.Id)
        .Select(e => new EquipmentResponse
        {
            Id = e.Id,
            Code = e.Code,
            Name = e.Name,
            DepartmentId = e.DepartmentId,

            // Dấu ! nói với compiler rằng Department chắc chắn có dữ liệu
            // Vì Department không bắt buộc có Department hợp lệ và đã check epartmentExists ở trên.
            DepartmentName = e.Department!.Name,
            Status = e.Status,
            PurchasedDate = e.PurchasedDate,
            Description = e.Description,
            CreatedAt = e.CreatedAt,
            UpdatedAt = e.UpdatedAt

            //FirstOrDefaultAsync = dùng khi cần check null / NotFound.
            // FirstAsync = dùng khi logic đã đảm bảo chắc chắn có dữ liệu.
            // Tìm dữ liệu đầu method: FirstOrDefaultAsync
            // Query lại response sau khi update: FirstAsync
        }).FirstAsync();
        return Ok(response);
    }

    [Authorize(Roles = UserRoles.Admin)]
    [HttpDelete("{id:int}")]
    public async Task<ActionResult<EquipmentEntity>> DeleteEquipment(int id)
    {

        var equipment = await _context.Equipment.FirstOrDefaultAsync(e => e.Id == id);
        if (equipment is null)
        {
            return NotFound(
                new
                {
                    message = "Equipment Not Found"
                }
            );
        }

        //Kiểm tra thiết bị này đã từng phát sinh ticket bảo trì/ sự cố chưa
        var hasTicket = await _context.MaintenanceTickets.AnyAsync(ticket => ticket.EquipmentId == id);
        if (hasTicket)
        {
            return BadRequest(
                new
                {
                    message = "Cannot delete equipment because it related maintenance tickets"
                }
            );
        }

        //Remove chỉ đánh dấu entity sẽ bị xóa trong bộ nhớ EF Core
        // Chưa có câu SQL thật ở bước này
        _context.Equipment.Remove(equipment);

        //SaveChangesAsync mới gửi câu SQL DELETE thật xuống DB
        await _context.SaveChangesAsync();

        return NoContent();
    }
}
