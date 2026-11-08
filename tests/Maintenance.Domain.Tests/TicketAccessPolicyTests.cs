using System.Security.Claims;
using InternalMaintenance.Assets.Contracts;
using InternalMaintenance.Identity.Contracts;
using InternalMaintenance.Maintenance.Domain;
using InternalMaintenance.Api.Modules.Tickets;
using InternalMaintenance.Api.Modules.Tickets.Contracts;
using InternalMaintenance.Api.Services;
using Microsoft.AspNetCore.Http;

namespace Maintenance.Domain.Tests;

public class TicketAccessPolicyTests
{
    [Fact]
    public async Task Manager_can_access_ticket_in_equipment_maintenance_department_without_loading_identity()
    {
        var ticket = MaintenanceTicket.Create(
            "MT-ACCESS", "Printer repair", "Paper jam", 42, 10, TicketPriorities.Medium, DateTime.UtcNow);
        var equipmentReader = new FakeEquipmentReader(new EquipmentTicketContext("Active", 3, 7));
        var identityDirectory = new FakeIdentityDirectory();
        var currentUser = CreateCurrentUser(UserRoles.Manager, userId: 99, departmentId: 7);

        var allowed = await TicketAccessPolicy.CanAccessAsync(
            ToSubject(ticket), currentUser, equipmentReader, identityDirectory, CancellationToken.None);

        Assert.True(allowed);
        Assert.Equal(1, equipmentReader.LookupCount);
        Assert.Equal(0, identityDirectory.DepartmentLookupCount);
    }

    [Fact]
    public async Task Manager_can_access_ticket_created_by_user_in_managers_department()
    {
        var ticket = MaintenanceTicket.Create(
            "MT-ACCESS", "Printer repair", "Paper jam", 42, 10, TicketPriorities.Medium, DateTime.UtcNow);
        var equipmentReader = new FakeEquipmentReader(new EquipmentTicketContext("Active", 3, 5));
        var identityDirectory = new FakeIdentityDirectory(requesterDepartmentId: 7);
        var currentUser = CreateCurrentUser(UserRoles.Manager, userId: 99, departmentId: 7);

        var allowed = await TicketAccessPolicy.CanAccessAsync(
            ToSubject(ticket), currentUser, equipmentReader, identityDirectory, CancellationToken.None);

        Assert.True(allowed);
        Assert.Equal(1, identityDirectory.DepartmentLookupCount);
    }

    [Fact]
    public async Task Manager_list_scope_uses_equipment_and_requester_ids_from_module_contracts()
    {
        var currentUser = CreateCurrentUser(UserRoles.Manager, userId: 99, departmentId: 7);
        var equipmentReader = new FakeEquipmentReader(
            context: null,
            equipmentIdsInDepartment: [42]);
        var identityDirectory = new FakeIdentityDirectory(userIdsInDepartment: [10]);
        var tickets = new[]
        {
            MaintenanceTicket.Create("MT-IN-EQUIPMENT", "A", "A", 42, 300, TicketPriorities.Medium, DateTime.UtcNow),
            MaintenanceTicket.Create("MT-IN-REQUESTER-DEPT", "B", "B", 80, 10, TicketPriorities.Medium, DateTime.UtcNow),
            MaintenanceTicket.Create("MT-OUTSIDE", "C", "C", 80, 20, TicketPriorities.Medium, DateTime.UtcNow)
        };

        var scopedQuery = await TicketAccessPolicy.ApplyAsync(
            tickets.AsQueryable(),
            currentUser,
            identityDirectory,
            equipmentReader,
            CancellationToken.None);

        Assert.Equal(
            new[] { "MT-IN-EQUIPMENT", "MT-IN-REQUESTER-DEPT" },
            scopedQuery.Select(ticket => ticket.TicketCode).OrderBy(code => code).ToArray());
    }

    private static CurrentUserService CreateCurrentUser(string role, int userId, int departmentId)
    {
        var context = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(
            [
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim(ClaimTypes.Role, role),
                new Claim("departmentId", departmentId.ToString())
            ], "test"))
        };
        return new CurrentUserService(new HttpContextAccessor { HttpContext = context });
    }

    private static TicketAccessSubject ToSubject(MaintenanceTicket ticket) => new(
        ticket.EquipmentId,
        ticket.CreatedByUserId,
        ticket.AssignedTechnicianId,
        ticket.SupportTechnicianId);

    private sealed class FakeEquipmentReader(
        EquipmentTicketContext? context,
        IReadOnlyCollection<int>? equipmentIdsInDepartment = null) : IEquipmentTicketContextReader
    {
        private readonly IReadOnlyCollection<int> _equipmentIdsInDepartment = equipmentIdsInDepartment ?? [];
        public int LookupCount { get; private set; }

        public Task<EquipmentTicketContext?> GetAsync(int equipmentId, CancellationToken cancellationToken = default)
        {
            LookupCount++;
            return Task.FromResult(context);
        }

        public Task<IReadOnlyDictionary<int, EquipmentTicketContext>> GetManyAsync(
            IReadOnlyCollection<int> equipmentIds,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyDictionary<int, EquipmentTicketContext>>(
                new Dictionary<int, EquipmentTicketContext>());

        public Task<IReadOnlyCollection<int>> FindIdsInDepartmentAsync(
            int? departmentId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(_equipmentIdsInDepartment);

    }

    private sealed class FakeIdentityDirectory(
        int? requesterDepartmentId = null,
        IReadOnlyCollection<int>? userIdsInDepartment = null) : IIdentityUserDirectory
    {
        private readonly IReadOnlyCollection<int> _userIdsInDepartment = userIdsInDepartment ?? [];
        public int DepartmentLookupCount { get; private set; }

        public Task<string?> FindDisplayNameAsync(int userId, CancellationToken cancellationToken = default) =>
            Task.FromResult<string?>(null);

        public Task<string?> FindEmailAsync(int userId, CancellationToken cancellationToken = default) =>
            Task.FromResult<string?>(null);

        public Task<int?> FindDepartmentIdAsync(int userId, CancellationToken cancellationToken = default)
        {
            DepartmentLookupCount++;
            return Task.FromResult(requesterDepartmentId);
        }

        public Task<IReadOnlyCollection<int>> FindUserIdsByDepartmentAsync(
            int? departmentId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(_userIdsInDepartment);

        public Task<IReadOnlyDictionary<int, string>> FindDisplayNamesAsync(
            IReadOnlyCollection<int> userIds,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyDictionary<int, string>>(new Dictionary<int, string>());

        public Task<int> CountUsersInRoleAsync(string roleName, CancellationToken cancellationToken = default) =>
            Task.FromResult(0);
    }
}
