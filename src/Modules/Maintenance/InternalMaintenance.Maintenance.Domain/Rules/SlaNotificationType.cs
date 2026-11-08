namespace InternalMaintenance.Maintenance.Domain;

// Các loại thông báo do đồng hồ SLA phát ra. Giá trị này là quyết định nghiệp vụ;
// worker chỉ chịu trách nhiệm claim nguyên tử và chuyển thông báo tới email service.
public enum SlaNotificationType
{
    NearBreach,
    Breached,
    Escalated
}
