using InternalMaintenance.Maintenance.Contracts;

namespace InternalMaintenance.Maintenance.Application;

public enum TicketUpdateError
{
    None,
    NotFound,
    Forbidden,
    InvalidRequest
}

public sealed record TicketUpdateOutcome(
    MaintenanceTicketResponse? Response,
    TicketUpdateError Error,
    string? Message)
{
    public bool IsSuccess => Error == TicketUpdateError.None;

    public static TicketUpdateOutcome Success(MaintenanceTicketResponse response) =>
        new(response, TicketUpdateError.None, null);

    public static TicketUpdateOutcome Fail(TicketUpdateError error, string message) =>
        new(null, error, message);
}
