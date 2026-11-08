using InternalMaintenance.Maintenance.Contracts;

namespace InternalMaintenance.Maintenance.Application;

public enum CreateMaintenanceTicketError
{
    None,
    EquipmentNotFound,
    InvalidRequest,
    DuplicateActiveTicket,
    DuplicateRace
}

public sealed record CreateMaintenanceTicketOutcome(
    int? TicketId,
    MaintenanceTicketResponse? Response,
    CreateMaintenanceTicketError Error,
    string? Message)
{
    public bool IsSuccess => Error == CreateMaintenanceTicketError.None;

    public static CreateMaintenanceTicketOutcome Success(int id, MaintenanceTicketResponse response) =>
        new(id, response, CreateMaintenanceTicketError.None, null);

    public static CreateMaintenanceTicketOutcome Fail(CreateMaintenanceTicketError error, string message) =>
        new(null, null, error, message);
}
