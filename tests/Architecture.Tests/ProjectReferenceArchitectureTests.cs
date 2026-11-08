using System.Xml.Linq;

namespace Architecture.Tests;

public sealed class ProjectReferenceArchitectureTests
{
    private static readonly string[] DomainProjects =
    [
        "src/Modules/Maintenance/InternalMaintenance.Maintenance.Domain/InternalMaintenance.Maintenance.Domain.csproj"
    ];

    [Theory]
    [MemberData(nameof(DomainProjectPaths))]
    public void Domain_projects_have_no_project_or_framework_dependencies(string projectPath)
    {
        var project = LoadProject(projectPath);

        // Domain thuần không tham chiếu EF, ASP.NET hay module khác; persistence cấu hình entity từ bên ngoài.
        Assert.Empty(project.Descendants("ProjectReference"));
        Assert.Empty(project.Descendants("PackageReference"));
        Assert.Empty(project.Descendants("FrameworkReference"));
    }

    public static IEnumerable<object[]> DomainProjectPaths() =>
        DomainProjects.Select(path => new object[] { path });

    [Theory]
    [InlineData("src/Modules/Maintenance/InternalMaintenance.Maintenance.Application/InternalMaintenance.Maintenance.Application.csproj")]
    public void Application_projects_do_not_reference_API_persistence_or_infrastructure(string projectPath)
    {
        var references = GetProjectReferenceNames(LoadProject(projectPath));

        // Application chỉ gọi Domain và public contracts; EF adapter/host được nối ở composition root.
        Assert.DoesNotContain("InternalMaintenance.Api", references);
        Assert.DoesNotContain("InternalMaintenance.Persistence", references);
        Assert.DoesNotContain(references, name => name.EndsWith(".Infrastructure", StringComparison.Ordinal));
        Assert.DoesNotContain(references, name => name.EndsWith(".Domain", StringComparison.Ordinal) &&
                                                   name != "InternalMaintenance.Maintenance.Domain");
    }

    [Theory]
    [InlineData("src/Modules/Maintenance/InternalMaintenance.Maintenance.Infrastructure/InternalMaintenance.Maintenance.Infrastructure.csproj")]
    [InlineData("src/Modules/Identity/InternalMaintenance.Identity.Infrastructure/InternalMaintenance.Identity.Infrastructure.csproj")]
    [InlineData("src/Modules/Equipment/InternalMaintenance.Equipment.Infrastructure/InternalMaintenance.Equipment.Infrastructure.csproj")]
    [InlineData("src/Modules/Organization/InternalMaintenance.Organization.Infrastructure/InternalMaintenance.Organization.Infrastructure.csproj")]
    [InlineData("src/Modules/Reporting/InternalMaintenance.Reporting.Infrastructure/InternalMaintenance.Reporting.Infrastructure.csproj")]
    [InlineData("src/Modules/TicketAttachments/InternalMaintenance.TicketAttachments.Infrastructure/InternalMaintenance.TicketAttachments.Infrastructure.csproj")]
    [InlineData("src/Modules/Vendors/InternalMaintenance.Vendors.Infrastructure/InternalMaintenance.Vendors.Infrastructure.csproj")]
    public void Infrastructure_projects_do_not_reference_the_API_host(string projectPath)
    {
        var references = GetProjectReferenceNames(LoadProject(projectPath));

        // Adapter có thể dùng Persistence và contract liên module, nhưng không được phụ thuộc ngược vào HTTP host.
        Assert.DoesNotContain("InternalMaintenance.Api", references);
    }

    [Theory]
    [InlineData("src/Modules/Maintenance/InternalMaintenance.Maintenance.Infrastructure/InternalMaintenance.Maintenance.Infrastructure.csproj", "InternalMaintenance.Maintenance.Domain")]
    [InlineData("src/Modules/Identity/InternalMaintenance.Identity.Infrastructure/InternalMaintenance.Identity.Infrastructure.csproj", null)]
    [InlineData("src/Modules/Equipment/InternalMaintenance.Equipment.Infrastructure/InternalMaintenance.Equipment.Infrastructure.csproj", null)]
    [InlineData("src/Modules/Organization/InternalMaintenance.Organization.Infrastructure/InternalMaintenance.Organization.Infrastructure.csproj", null)]
    [InlineData("src/Modules/Reporting/InternalMaintenance.Reporting.Infrastructure/InternalMaintenance.Reporting.Infrastructure.csproj", null)]
    [InlineData("src/Modules/TicketAttachments/InternalMaintenance.TicketAttachments.Infrastructure/InternalMaintenance.TicketAttachments.Infrastructure.csproj", null)]
    [InlineData("src/Modules/Vendors/InternalMaintenance.Vendors.Infrastructure/InternalMaintenance.Vendors.Infrastructure.csproj", null)]
    public void Infrastructure_projects_do_not_reference_unowned_domain_projects(
        string projectPath,
        string? allowedDomainProject)
    {
        var references = GetProjectReferenceNames(LoadProject(projectPath));
        var domainReferences = references.Where(name => name.EndsWith(".Domain", StringComparison.Ordinal));

        // Reporting chỉ nhận read contract; các adapter khác chỉ tham chiếu Domain mà chúng lưu trữ.
        if (allowedDomainProject is null)
            Assert.Empty(domainReferences);
        else
        {
            Assert.All(domainReferences, name => Assert.Equal(allowedDomainProject, name));
            Assert.Contains(allowedDomainProject, domainReferences);
        }

        // Module không được gọi thẳng implementation của module khác; giao tiếp đi qua Contracts.
        var currentInfrastructure = Path.GetFileNameWithoutExtension(projectPath);
        Assert.DoesNotContain(references, name =>
            name.EndsWith(".Infrastructure", StringComparison.Ordinal) &&
            !name.Equals(currentInfrastructure, StringComparison.Ordinal));
    }

    private static XDocument LoadProject(string relativePath)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "InternalMaintenanceManagement.slnx")))
            directory = directory.Parent;

        Assert.NotNull(directory);
        return XDocument.Load(Path.Combine(directory!.FullName, relativePath));
    }

    private static string[] GetProjectReferenceNames(XDocument project) => project
        .Descendants("ProjectReference")
        .Select(reference => Path.GetFileNameWithoutExtension((string?)reference.Attribute("Include") ?? string.Empty)!)
        .ToArray();
}
