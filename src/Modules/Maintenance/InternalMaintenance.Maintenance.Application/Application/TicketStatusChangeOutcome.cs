using InternalMaintenance.Maintenance.Contracts;

namespace InternalMaintenance.Maintenance.Application;

public enum TicketStatusChangeError
{
    None,
    NotFound,
    Forbidden,
    InvalidRequest
}

public sealed record TicketStatusChangeOutcome(
    MaintenanceTicketResponse? Response,
    TicketStatusChangeError Error,
    string? Message)
{
    public bool IsSuccess => Error == TicketStatusChangeError.None;

    public static TicketStatusChangeOutcome Success(MaintenanceTicketResponse response) =>
        new(response, TicketStatusChangeError.None, null);

    public static TicketStatusChangeOutcome Fail(TicketStatusChangeError error, string message) =>
        new(null, error, message);
}
