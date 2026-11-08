using InternalMaintenance.Assets.Contracts;
using InternalMaintenance.Assets.Infrastructure.Features.Common;
using InternalMaintenance.Organization.Contracts;
using InternalMaintenance.Persistence;
using Microsoft.EntityFrameworkCore;

namespace InternalMaintenance.Assets.Infrastructure.Features.GetEquipmentById;

/// <summary>
/// [VERTICAL SLICE - FEATURE: LẤY CHI TIẾT THIẾT BỊ THEO ID (GET EQUIPMENT BY ID)]
/// 
/// - Vấn đề giải quyết:
///   Tách biệt hoàn toàn luồng truy vấn chi tiết 1 thiết bị khỏi các luồng danh sách và luồng ghi (CRUD).
/// 
/// - Cách giải quyết:
///   1. Truy vấn trực tiếp bằng `AsNoTracking()` kèm `ProjectToResponse` để chỉ lấy các cột cần thiết từ DB.
///   2. Bổ sung tên phòng ban sở hữu và phòng ban bảo trì qua `IOrganizationDirectory`.
///   3. Có thể tái sử dụng trực tiếp sau khi `CreateEquipmentHandler` hoặc `UpdateEquipmentHandler` lưu thành công
///      để trả về DTO đầy đủ nhất cho Client.
/// </summary>
public sealed class GetEquipmentByIdHandler(
    AppDbContext db,
    IOrganizationDirectory organizationDirectory)
{
    public async Task<EquipmentResponse?> HandleAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        var response = await EquipmentProjection
            .ProjectToResponse(db.Equipment.AsNoTracking().Where(equipment => equipment.Id == id))
            .FirstOrDefaultAsync(cancellationToken);

        if (response is not null)
            await EquipmentProjection.PopulateDepartmentNamesAsync(organizationDirectory, [response], cancellationToken);

        return response;
    }
}

