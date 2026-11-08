namespace InternalMaintenance.Identity.Application;

/// <summary>
/// Quy tắc mật khẩu do module Identity sở hữu để mọi use case áp dụng thống nhất.
/// [Lean Modular Monolith Refactoring]:
/// Di chuyển từ InternalMaintenance.Identity.Application sang InternalMaintenance.Identity.Contracts.
/// </summary>
public static class IdentityPasswordPolicy
{
    public const int MinimumLength = 8;

    // DataAnnotations bảo vệ request đi qua HTTP; kiểm tra ở Application còn bảo vệ
    // các lời gọi nội bộ và job có thể gọi service mà không qua MVC model validation.
    public static bool MeetsMinimumLength(string? password) =>
        password is { Length: >= MinimumLength };
}
