using InternalMaintenance.Assets.Contracts;
using InternalMaintenance.Maintenance.Application;
using InternalMaintenance.Maintenance.Domain;
using InternalMaintenance.Maintenance.Contracts;

namespace Maintenance.Domain.Tests;

public class CreateMaintenanceTicketHandlerTests
{
    [Fact]
    public async Task Creation_uses_medium_priority_by_default_and_the_authenticated_requester()
    {
        var store = new FakeStore();
        var handler = new CreateMaintenanceTicketHandler(
            new FakeEquipmentReader(new EquipmentTicketContext("Active", 3, 5)),
            store,
            new FakeTicketCodeGenerator());

        var result = await handler.Handle(
            new CreateMaintenanceTicketCommand("  Broken printer  ", "  Paper feed is jammed  ", 8, " ", 42),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(TicketPriorities.Medium, store.AddedTicket!.Priority);
        Assert.Equal("Broken printer", store.AddedTicket.Title);
        Assert.Equal("Paper feed is jammed", store.AddedTicket.Description);
        Assert.Equal(42, store.AddedTicket.CreatedByUserId);
        Assert.Equal("MT-NEW", store.AddedTicket.TicketCode);
        Assert.Equal(77, result.TicketId);
        Assert.Equal(1, store.SaveCount);
    }

    [Fact]
    public async Task Equipment_with_existing_occupying_ticket_is_rejected_before_insert()
    {
        var store = new FakeStore { HasOccupyingTicket = true };
        var handler = new CreateMaintenanceTicketHandler(
            new FakeEquipmentReader(new EquipmentTicketContext("Active", 3, 5)),
            store,
            new FakeTicketCodeGenerator());

        var result = await handler.Handle(
            new CreateMaintenanceTicketCommand("Printer failure", "Paper feed stopped working", 8, null, 42),
            CancellationToken.None);

        Assert.Equal(CreateMaintenanceTicketError.DuplicateActiveTicket, result.Error);
        Assert.Null(store.AddedTicket);
        Assert.Equal(0, store.SaveCount);
    }

    [Fact]
    public async Task Database_unique_index_race_is_returned_as_conflict()
    {
        var store = new FakeStore { InsertResult = false };
        var handler = new CreateMaintenanceTicketHandler(
            new FakeEquipmentReader(new EquipmentTicketContext("Active", 3, 5)),
            store,
            new FakeTicketCodeGenerator());

        var result = await handler.Handle(
            new CreateMaintenanceTicketCommand("Printer failure", "Paper feed stopped working", 8, null, 42),
            CancellationToken.None);

        Assert.Equal(CreateMaintenanceTicketError.DuplicateRace, result.Error);
    }

    private sealed class FakeEquipmentReader(EquipmentTicketContext? info) : IEquipmentTicketContextReader
    {
        public Task<EquipmentTicketContext?> GetAsync(int equipmentId, CancellationToken cancellationToken = default) =>
            Task.FromResult(info);

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

    private sealed class FakeStore : ITicketCreationStore
    {
        public bool HasOccupyingTicket { get; init; }
        public bool InsertResult { get; init; } = true;
        public int SaveCount { get; private set; }
        public MaintenanceTicket? AddedTicket { get; private set; }

        public Task<bool> HasEquipmentOccupyingTicketAsync(int equipmentId, CancellationToken cancellationToken) =>
            Task.FromResult(HasOccupyingTicket);

        public Task<bool> TryAddAndSaveAsync(MaintenanceTicket ticket, CancellationToken cancellationToken)
        {
            AddedTicket = ticket;
            ticket.Id = 77; // Giả lập SQL Server sinh khóa chính sau khi insert thành công.
            SaveCount++;
            return Task.FromResult(InsertResult);
        }

        public Task<MaintenanceTicketResponse?> GetResponseAsync(int ticketId, CancellationToken cancellationToken) =>
            Task.FromResult<MaintenanceTicketResponse?>(new MaintenanceTicketResponse { Id = ticketId });
    }

    private sealed class FakeTicketCodeGenerator : ITicketCodeGenerator
    {
        public string GenerateTicketCode() => "MT-NEW";
    }
}
