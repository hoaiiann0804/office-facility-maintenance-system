using InternalMaintenance.Assets.Domain;
using InternalMaintenance.Assets.Contracts;
using InternalMaintenance.Identity.Contracts;
using InternalMaintenance.Maintenance.Application;
using InternalMaintenance.Maintenance.Domain;
using InternalMaintenance.Maintenance.Contracts;

namespace Maintenance.Domain.Tests;

public class AssignMaintenanceTicketHandlerTests
{
    [Fact]
    public async Task Manager_outside_maintenance_department_cannot_assign_ticket()
    {
        var ticket = CreatePendingTicket();
        var store = new FakeStore(ticket);
        var directory = new FakeTechnicianDirectory();
        var handler = new AssignMaintenanceTicketHandler(store, directory, new FakeEquipmentReader(5));

        var result = await handler.Handle(Command(UserRoles.Manager, managerDepartmentId: 99), CancellationToken.None);

        Assert.Equal(TicketAssignmentError.Forbidden, result.Error);
        Assert.Equal(TicketStatuses.Pending, ticket.Status);
        Assert.Equal(0, store.SaveCount);
        Assert.Null(store.History);
        Assert.Equal(0, directory.LookupCount);
    }

    [Fact]
    public async Task Admin_can_assign_active_technicians_in_equipment_maintenance_department()
    {
        var ticket = CreatePendingTicket();
        var store = new FakeStore(ticket);
        var directory = new FakeTechnicianDirectory(
            new TechnicianRecord(20, UserRoles.Technician, 5, true),
            new TechnicianRecord(21, UserRoles.Technician, 5, true));
        var handler = new AssignMaintenanceTicketHandler(store, directory, new FakeEquipmentReader(5));

        var result = await handler.Handle(
            Command(UserRoles.Admin, supportTechnicianId: 21), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(TicketStatuses.Assigned, ticket.Status);
        Assert.Equal(20, ticket.AssignedTechnicianId);
        Assert.Equal(21, ticket.SupportTechnicianId);
        Assert.Equal(TicketStatuses.Pending, store.History?.OldStatus);
        Assert.Equal(TicketStatuses.Assigned, store.History?.NewStatus);
        Assert.Equal(1, store.SaveCount);
    }

    [Fact]
    public async Task Support_technician_must_be_different_from_primary_technician()
    {
        var ticket = CreatePendingTicket();
        var store = new FakeStore(ticket);
        var directory = new FakeTechnicianDirectory(
            new TechnicianRecord(20, UserRoles.Technician, 5, true));
        var handler = new AssignMaintenanceTicketHandler(store, directory, new FakeEquipmentReader(5));

        var result = await handler.Handle(
            Command(UserRoles.Admin, supportTechnicianId: 20), CancellationToken.None);

        Assert.Equal(TicketAssignmentError.InvalidRequest, result.Error);
        Assert.Equal(TicketStatuses.Pending, ticket.Status);
        Assert.Equal(0, store.SaveCount);
    }

    private static MaintenanceTicket CreatePendingTicket()
    {
        var ticket = MaintenanceTicket.Create("MT-ASSIGN", "Printer failure", "Paper feed issue",
            5, 10, TicketPriorities.Medium, DateTime.UtcNow);
        ticket.Id = 1;
        return ticket;
    }

    private static AssignMaintenanceTicketCommand Command(
        string role,
        int? managerDepartmentId = null,
        int? supportTechnicianId = null) => new(
        TicketId: 1,
        AssignedTechnicianId: 20,
        SupportTechnicianId: supportTechnicianId,
        Note: "Assigned by test",
        ActorUserId: 1,
        ActorRole: role,
        ActorDepartmentId: managerDepartmentId);

    private sealed class FakeStore(MaintenanceTicket ticket) : ITicketAssignmentStore
    {
        public int SaveCount { get; private set; }
        public TicketStatusHistory? History { get; private set; }

        public Task<MaintenanceTicket?> GetForAssignmentAsync(int ticketId, CancellationToken cancellationToken) =>
            Task.FromResult<MaintenanceTicket?>(ticketId == ticket.Id ? ticket : null);

        public void AddStatusHistory(TicketStatusHistory history) => History = history;

        public Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            SaveCount++;
            return Task.CompletedTask;
        }

        public Task<MaintenanceTicketResponse?> GetResponseAsync(int ticketId, CancellationToken cancellationToken) =>
            Task.FromResult<MaintenanceTicketResponse?>(new MaintenanceTicketResponse { Id = ticketId });
    }

    private sealed class FakeTechnicianDirectory(params TechnicianRecord[] records) : ITechnicianDirectory
    {
        public int LookupCount { get; private set; }

        public Task<TechnicianRecord?> FindAsync(int userId, CancellationToken cancellationToken = default)
        {
            LookupCount++;
            return Task.FromResult(records.SingleOrDefault(record => record.Id == userId));
        }
    }

    private sealed class FakeEquipmentReader(int? maintenanceDepartmentId) : IEquipmentTicketContextReader
    {
        public Task<EquipmentTicketContext?> GetAsync(int equipmentId, CancellationToken cancellationToken = default) =>
            Task.FromResult<EquipmentTicketContext?>(
                equipmentId == 5
                    ? new EquipmentTicketContext("Active", 3, maintenanceDepartmentId)
                    : null);

        public Task<IReadOnlyDictionary<int, EquipmentTicketContext>> GetManyAsync(
            IReadOnlyCollection<int> equipmentIds,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyDictionary<int, EquipmentTicketContext>>(
                new Dictionary<int, EquipmentTicketContext>());

        public Task<IReadOnlyCollection<int>> FindIdsInDepartmentAsync(
            int? departmentId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyCollection<int>>(Array.Empty<int>());

    }
}
