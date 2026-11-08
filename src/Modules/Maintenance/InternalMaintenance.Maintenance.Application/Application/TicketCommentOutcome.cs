using InternalMaintenance.Maintenance.Contracts;

namespace InternalMaintenance.Maintenance.Application;

public enum TicketCommentError
{
    None,
    NotFound,
    Forbidden,
    InvalidState,
    UserNotFound
}

public sealed record TicketCommentOutcome(
    TicketCommentResponse? Response,
    TicketCommentError Error,
    string? Message)
{
    public bool IsSuccess => Error == TicketCommentError.None;

    public static TicketCommentOutcome Success(TicketCommentResponse response) =>
        new(response, TicketCommentError.None, null);

    public static TicketCommentOutcome Fail(TicketCommentError error, string message) =>
        new(null, error, message);
}
