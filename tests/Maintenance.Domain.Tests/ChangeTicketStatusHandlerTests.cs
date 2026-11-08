using InternalMaintenance.Assets.Domain;
using InternalMaintenance.Assets.Contracts;
using InternalMaintenance.Identity.Contracts;
using InternalMaintenance.Maintenance.Application;
using InternalMaintenance.Maintenance.Domain;
using InternalMaintenance.Api.Modules.Tickets;
using InternalMaintenance.Maintenance.Contracts;
using InternalMaintenance.Vendors.Contracts;

namespace Maintenance.Domain.Tests;

public class ChangeTicketStatusHandlerTests
{
    [Fact]
    public async Task Technician_who_is_not_assigned_cannot_resolve_ticket_or_commit_changes()
    {
        var ticket = CreateInProgressTicket();
        var store = new FakeStore(ticket);
        var gateway = new FakeEquipmentGateway();
        var handler = CreateHandler(store, gateway);

        var result = await handler.Handle(Command(actorUserId: 999, role: UserRoles.Technician), CancellationToken.None);

        Assert.Equal(TicketStatusChangeError.Forbidden, result.Error);
        Assert.Equal(TicketStatuses.InProgress, ticket.Status);
        Assert.Equal(0, store.SaveCount);
        Assert.Null(store.History);
        Assert.Empty(gateway.StatusChanges);
    }

    [Fact]
    public async Task Assigned_technician_resolving_ticket_updates_sla_and_history_in_one_commit()
    {
        var ticket = CreateInProgressTicket();
        var store = new FakeStore(ticket);
        var gateway = new FakeEquipmentGateway();
        var handler = CreateHandler(store, gateway);

        var result = await handler.Handle(Command(actorUserId: 7, role: UserRoles.Technician), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(TicketStatuses.Resolved, ticket.Status);
        Assert.Equal(SlaPolicy.MetSLA, ticket.SlaStatus);
        Assert.Equal(1, store.SaveCount);
        Assert.Equal(TicketStatuses.InProgress, store.History?.OldStatus);
        Assert.Equal(TicketStatuses.Resolved, store.History?.NewStatus);
        Assert.Empty(gateway.StatusChanges);
    }

    [Fact]
    public async Task Returning_from_vendor_resumes_sla_and_updates_equipment_in_the_same_commit()
    {
        var ticket = CreateInProgressTicket();
        var dispatchedAt = DateTime.UtcNow.AddMinutes(-10);
        var dueAtBeforeReturn = ticket.DueAt;
        ticket.SendToVendor(dispatchedAt);

        var store = new FakeStore(ticket)
        {
            OpenVendorLog = new TicketVendorLog
            {
                MaintenanceTicketId = ticket.Id,
                VendorId = 2,
                DispatchedAt = dispatchedAt,
                EstimatedReturnDate = DateTime.UtcNow.Date.AddDays(1)
            }
        };
        var gateway = new FakeEquipmentGateway();
        var handler = CreateHandler(store, gateway);

        var result = await handler.Handle(
            Command(actorUserId: 7, role: UserRoles.Technician, newStatus: TicketStatuses.InProgress),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(TicketStatuses.InProgress, ticket.Status);
        Assert.Null(ticket.SlaPausedAt);
        Assert.True(ticket.TotalSlaPausedMinutes >= 10);
        Assert.Equal(dueAtBeforeReturn!.Value.AddMinutes(ticket.TotalSlaPausedMinutes), ticket.DueAt);
        Assert.Equal(ticket.TotalSlaPausedMinutes, store.OpenVendorLog!.PausedMinutes);
        Assert.Single(gateway.StatusChanges);
        Assert.Equal(EquipmentStatuses.UnderMaintenance, gateway.StatusChanges[0].Status);
        Assert.Equal(1, store.SaveCount);
    }

    [Fact]
    public async Task Manager_in_requester_department_can_close_ticket_via_identity_contract()
    {
        var ticket = CreateInProgressTicket();
        ticket.Resolve("Repair completed", DateTime.UtcNow);
        var store = new FakeStore(ticket);
        var gateway = new FakeEquipmentGateway();
        var handler = CreateHandler(store, gateway, requesterDepartmentId: 7);

        var result = await handler.Handle(
            Command(actorUserId: 99, role: UserRoles.Manager, newStatus: TicketStatuses.Closed, actorDepartmentId: 7),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(TicketStatuses.Closed, ticket.Status);
        Assert.Single(gateway.StatusChanges);
        Assert.Equal(EquipmentStatuses.Active, gateway.StatusChanges[0].Status);
    }

    [Fact]
    public async Task Inactive_vendor_is_rejected_before_ticket_or_vendor_log_is_changed()
    {
        var ticket = CreateInProgressTicket();
        var store = new FakeStore(ticket);
        var handler = CreateHandler(store, new FakeEquipmentGateway(), vendorIsActive: false);
        var command = Command(actorUserId: 7, role: UserRoles.Technician, newStatus: TicketStatuses.WaitingForVendor) with
        {
            VendorId = 42,
            VendorEstimatedReturnDate = DateTime.UtcNow.Date.AddDays(1)
        };

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.Equal(TicketStatusChangeError.InvalidRequest, result.Error);
        Assert.Equal(TicketStatuses.InProgress, ticket.Status);
        Assert.Equal(0, store.SaveCount);
        Assert.Null(store.History);
    }

    private static MaintenanceTicket CreateInProgressTicket()
    {
        var now = DateTime.UtcNow;
        var ticket = MaintenanceTicket.Create("MT-TEST", "Test", "Details", 5, 10,
            TicketPriorities.High, now);
        ticket.Id = 1;
        ticket.Assign(7, null);
        ticket.Start(now.AddMinutes(1));
        return ticket;
    }

    private static ChangeTicketStatusCommand Command(
        int actorUserId,
        string role,
        string newStatus = TicketStatuses.Resolved,
        int? actorDepartmentId = null) => new(
        TicketId: 1,
        NewStatus: newStatus,
        ResolutionNote: "Replaced the faulty part",
        CancellationReason: null,
        Note: "Verified by technician",
        VendorId: null,
        VendorEstimatedReturnDate: null,
        VendorNote: null,
        ActorUserId: actorUserId,
        ActorRole: role,
        ActorDepartmentId: actorDepartmentId);

    private static ChangeTicketStatusHandler CreateHandler(
        FakeStore store,
        FakeEquipmentGateway gateway,
        int? requesterDepartmentId = null,
        bool vendorIsActive = true) =>
        new(store, gateway, new FakeEquipmentReader(), new FakeIdentityUserDirectory(requesterDepartmentId),
            new FakeVendorDirectory(vendorIsActive));

    private sealed class FakeVendorDirectory(bool isActive) : IVendorDirectory
    {
        public Task<bool> IsActiveAsync(int vendorId, CancellationToken cancellationToken = default) =>
            Task.FromResult(isActive);

        public Task<IReadOnlyDictionary<int, string>> FindNamesAsync(
            IReadOnlyCollection<int> vendorIds,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyDictionary<int, string>>(new Dictionary<int, string>());
    }

    private sealed class FakeStore(MaintenanceTicket ticket) : ITicketStatusChangeStore
    {
        public int SaveCount { get; private set; }
        public TicketStatusHistory? History { get; private set; }
        public TicketVendorLog? OpenVendorLog { get; init; }

        public Task<MaintenanceTicket?> GetForStatusChangeAsync(int ticketId, CancellationToken cancellationToken) =>
            Task.FromResult<MaintenanceTicket?>(ticketId == ticket.Id ? ticket : null);

        public Task<TicketVendorLog?> GetOpenVendorLogAsync(int ticketId, CancellationToken cancellationToken) =>
            Task.FromResult(OpenVendorLog);

        public void AddVendorLog(TicketVendorLog vendorLog) => throw new InvalidOperationException("Not expected in this test.");

        public void AddStatusHistory(TicketStatusHistory history) => History = history;

        public Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            SaveCount++;
            return Task.CompletedTask;
        }

        public Task<MaintenanceTicketResponse?> GetResponseAsync(int ticketId, CancellationToken cancellationToken) =>
            Task.FromResult<MaintenanceTicketResponse?>(new MaintenanceTicketResponse { Id = ticketId });
    }

    private sealed class FakeEquipmentGateway : IEquipmentMaintenanceGateway
    {
        public List<(int EquipmentId, string Status)> StatusChanges { get; } = [];

        public Task SetStatusAsync(int equipmentId, string status, CancellationToken cancellationToken = default)
        {
            StatusChanges.Add((equipmentId, status));
            return Task.CompletedTask;
        }
    }

    private sealed class FakeEquipmentReader : IEquipmentTicketContextReader
    {
        public Task<EquipmentTicketContext?> GetAsync(int equipmentId, CancellationToken cancellationToken = default) =>
            Task.FromResult<EquipmentTicketContext?>(new EquipmentTicketContext("UnderMaintenance", 3, 5));

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

    private sealed class FakeIdentityUserDirectory(int? departmentId) : IIdentityUserDirectory
    {
        public Task<string?> FindDisplayNameAsync(int userId, CancellationToken cancellationToken = default) =>
            Task.FromResult<string?>(null);

        public Task<string?> FindEmailAsync(int userId, CancellationToken cancellationToken = default) =>
            Task.FromResult<string?>(null);

        public Task<int?> FindDepartmentIdAsync(int userId, CancellationToken cancellationToken = default) =>
            Task.FromResult(departmentId);

        public Task<IReadOnlyCollection<int>> FindUserIdsByDepartmentAsync(
            int? requestedDepartmentId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyCollection<int>>(Array.Empty<int>());

        public Task<IReadOnlyDictionary<int, string>> FindDisplayNamesAsync(
            IReadOnlyCollection<int> userIds,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyDictionary<int, string>>(new Dictionary<int, string>());

        public Task<int> CountUsersInRoleAsync(string roleName, CancellationToken cancellationToken = default) =>
            Task.FromResult(0);
    }
}
