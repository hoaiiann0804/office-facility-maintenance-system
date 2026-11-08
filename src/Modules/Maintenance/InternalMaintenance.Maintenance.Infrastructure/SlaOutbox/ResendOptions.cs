namespace InternalMaintenance.Maintenance.Infrastructure;

public class ResendOptions
{
    public const string SectionName = "Resend";

    public string? ApiKey { get; set; }
    public string FromEmail { get; set; } = "no-reply@mail.shopmini.io.vn";
    public string FromName { get; set; } = "Maintenance System";
    public string? FallbackAdminEmail { get; set; } = "admin@shopmini.io.vn";
    public string? FallbackManagerEmail { get; set; } = "manager@shopmini.io.vn";
    public string? FallbackTechnicianEmail { get; set; } = "technician@shopmini.io.vn";
}
