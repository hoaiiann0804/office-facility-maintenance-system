using System.Linq.Expressions;
using InternalMaintenance.Identity.Contracts;
using InternalMaintenance.Identity.Domain;
using InternalMaintenance.Organization.Contracts;

namespace InternalMaintenance.Identity.Infrastructure.Features.Users.Common;

/// <summary>
/// [SHARED CONCERN TRONG NỘI BỘ MODULE IDENTITY - USERS]
/// Chứa biểu thức ánh xạ SQL (Projection) từ thực thể User sang UserResponse và các hàm hỗ trợ
/// tra cứu tên phòng ban liên module qua IOrganizationDirectory (Batch Lookup chống N+1 Query).
/// </summary>
internal static class UserProjection
{
    public static Expression<Func<User, UserResponse>> ToResponseProjection() => user => new UserResponse
    {
        Id = user.Id,
        FullName = user.FullName,
        Email = user.Email,
        RoleId = user.RoleId,
        RoleName = user.Role!.Name,
        DepartmentId = user.DepartmentId,
        IsActive = user.IsActive,
        MustChangePassword = user.MustChangePassword,
        LastLoginAt = user.LastLoginAt,
        CreatedAt = user.CreatedAt,
        UpdatedAt = user.UpdatedAt
    };

    /// <summary>
    /// Điền tên phòng ban hàng loạt (Batch Lookup) qua IOrganizationDirectory để tránh lỗi N+1 Query
    /// và không cần giữ navigation property sang bảng Department của module Organization.
    /// </summary>
    public static async Task PopulateDepartmentNamesAsync(
        IOrganizationDirectory organizationDirectory,
        IReadOnlyCollection<UserResponse> responses,
        CancellationToken cancellationToken)
    {
        var departmentIds = responses
            .Where(user => user.DepartmentId.HasValue)
            .Select(user => user.DepartmentId!.Value)
            .Distinct()
            .ToArray();

        if (departmentIds.Length == 0)
            return;

        var names = await organizationDirectory.FindNamesAsync(departmentIds, cancellationToken);
        foreach (var response in responses)
        {
            if (response.DepartmentId.HasValue)
                response.DepartmentName = names.GetValueOrDefault(response.DepartmentId.Value);
        }
    }

    /// <summary>
    /// Kiểm tra phòng ban có tồn tại hay không thông qua IOrganizationDirectory.
    /// </summary>
    public static async Task<bool> DepartmentExistsAsync(
        IOrganizationDirectory organizationDirectory,
        int? departmentId,
        CancellationToken cancellationToken)
    {
        if (!departmentId.HasValue)
            return false;

        var names = await organizationDirectory.FindNamesAsync([departmentId.Value], cancellationToken);
        return names.ContainsKey(departmentId.Value);
    }
}

