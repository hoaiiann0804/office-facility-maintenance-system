using InternalMaintenance.Maintenance.Contracts;

namespace InternalMaintenance.Maintenance.Application;

public enum TicketAssignmentError
{
    None,
    NotFound,
    Forbidden,
    InvalidRequest
}

public sealed record TicketAssignmentOutcome(
    MaintenanceTicketResponse? Response,
    TicketAssignmentError Error,
    string? Message)
{
    public bool IsSuccess => Error == TicketAssignmentError.None;

    public static TicketAssignmentOutcome Success(MaintenanceTicketResponse response) =>
        new(response, TicketAssignmentError.None, null);

    public static TicketAssignmentOutcome Fail(TicketAssignmentError error, string message) =>
        new(null, error, message);
}
