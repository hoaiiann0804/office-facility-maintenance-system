namespace InternalMaintenance.Maintenance.Application;

/// <summary>Actor lấy từ claims; client chỉ cung cấp nội dung và ticket ID trên route.</summary>
public sealed record CreateTicketCommentCommand(
    int TicketId,
    int ActorUserId,
    string ActorRole,
    int? ActorDepartmentId,
    string Content);
