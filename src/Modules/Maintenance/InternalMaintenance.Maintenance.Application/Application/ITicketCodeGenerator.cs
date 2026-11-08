namespace InternalMaintenance.Maintenance.Application;

/// <summary>Application port for generating the public maintenance-ticket identifier.</summary>
public interface ITicketCodeGenerator
{
    string GenerateTicketCode();
}
