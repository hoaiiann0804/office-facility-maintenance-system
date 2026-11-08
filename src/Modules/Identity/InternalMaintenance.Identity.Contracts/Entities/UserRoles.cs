namespace InternalMaintenance.Identity.Contracts;

// Tên role là contract dùng chung giữa Identity và các module áp dụng quyền nghiệp vụ.
public static class UserRoles
{
    public const string Admin = "Admin";
    public const string Manager = "Manager";
    public const string Staff = "Staff";
    public const string Technician = "Technician";
}
