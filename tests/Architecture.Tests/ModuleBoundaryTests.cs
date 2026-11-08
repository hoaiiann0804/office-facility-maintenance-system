using InternalMaintenance.Reporting.Contracts;
using InternalMaintenance.Reporting.Infrastructure;
using InternalMaintenance.TicketAttachments.Infrastructure;
using InternalMaintenance.Maintenance.Application;

namespace Architecture.Tests;

public sealed class ModuleBoundaryTests
{
    [Fact]
    public void Maintenance_application_does_not_reference_persistence_or_module_implementations()
    {
        // Use case chỉ điều phối qua Domain và contract; EF store nằm ở Infrastructure để có thể thay adapter
        // mà không đổi quy tắc nghiệp vụ hoặc buộc module gọi trực tiếp vào implementation của module khác.
        var references = typeof(CreateMaintenanceTicketHandler).Assembly.GetReferencedAssemblies()
            .Select(reference => reference.Name)
            .ToArray();

        Assert.DoesNotContain("InternalMaintenance.Api", references);
        Assert.DoesNotContain("InternalMaintenance.Persistence", references);
        Assert.DoesNotContain("InternalMaintenance.Maintenance.Infrastructure", references);
        Assert.DoesNotContain("InternalMaintenance.Equipment.Infrastructure", references);
        Assert.DoesNotContain("InternalMaintenance.Identity.Infrastructure", references);
        Assert.DoesNotContain("InternalMaintenance.Identity.Domain", references);
        Assert.DoesNotContain("InternalMaintenance.Vendors.Domain", references);
        Assert.Contains("InternalMaintenance.Maintenance.Domain", references);
        Assert.Contains("InternalMaintenance.Maintenance.Contracts", references);
        Assert.Contains("InternalMaintenance.Identity.Contracts", references);
        Assert.Contains("InternalMaintenance.Vendors.Contracts", references);
    }

    [Fact]
    public void Maintenance_public_contracts_are_not_owned_by_the_application_project()
    {
        // Equipment và TicketAttachments cần hỏi Maintenance về trạng thái/quyền ticket;
        // contract công khai nằm ở project Contracts để caller không kéo use case nội bộ sang.
        var references = typeof(InternalMaintenance.Maintenance.Contracts.IMaintenanceTicketLookup)
            .Assembly.GetReferencedAssemblies()
            .Select(reference => reference.Name)
            .ToArray();

        Assert.DoesNotContain("InternalMaintenance.Maintenance.Application", references);
        Assert.DoesNotContain("InternalMaintenance.Api", references);
        Assert.DoesNotContain("Microsoft.EntityFrameworkCore", references);
        Assert.Equal(
            typeof(InternalMaintenance.Maintenance.Contracts.IMaintenanceTicketReader).Assembly,
            typeof(InternalMaintenance.Maintenance.Contracts.IMaintenanceDashboardReader).Assembly);
    }

    [Fact]
    public void Ticket_attachments_infrastructure_calls_maintenance_through_public_contracts()
    {
        // Attachments cần ticket projection và access check, nhưng không nên biết Maintenance xử lý use case ra sao.
        var references = typeof(TicketAttachmentsService).Assembly.GetReferencedAssemblies()
            .Select(reference => reference.Name)
            .ToArray();

        Assert.DoesNotContain("InternalMaintenance.Maintenance.Application", references);
        Assert.DoesNotContain("InternalMaintenance.Maintenance.Domain", references);
        Assert.Contains("InternalMaintenance.Maintenance.Contracts", references);
    }

    [Fact]
    public void Reporting_contracts_do_not_reference_the_api_or_persistence()
    {
        // Controller và consumer chỉ cần DTO/port; contract không được kéo API, EF hoặc DbContext vào module.
        var references = typeof(ReportFilterQuery).Assembly.GetReferencedAssemblies()
            .Select(reference => reference.Name)
            .ToArray();

        Assert.DoesNotContain("InternalMaintenance.Api", references);
        Assert.DoesNotContain("InternalMaintenance.Persistence", references);
        Assert.DoesNotContain("Microsoft.EntityFrameworkCore", references);
    }

    [Fact]
    public void Reporting_infrastructure_uses_module_contracts_without_referencing_persistence_or_the_api_host()
    {
        // Reporting gọi read contract của module sở hữu dữ liệu; chỉ host composition root biết adapter EF.
        var references = typeof(SlaComplianceReportService).Assembly.GetReferencedAssemblies()
            .Select(reference => reference.Name)
            .ToArray();

        Assert.Equal("InternalMaintenance.Reporting.Infrastructure", typeof(SlaComplianceReportService).Assembly.GetName().Name);
        Assert.DoesNotContain("InternalMaintenance.Api", references);
        Assert.DoesNotContain("InternalMaintenance.Persistence", references);
        Assert.DoesNotContain("InternalMaintenance.Maintenance.Domain", references);
        Assert.Contains("InternalMaintenance.Reporting.Contracts", references);
        Assert.Contains("InternalMaintenance.Maintenance.Contracts", references);
    }

    [Fact]
    public void Reporting_infrastructure_uses_vendor_contract_instead_of_vendor_entity_project()
    {
        // Báo cáo vẫn đọc dữ liệu tổng hợp từ Persistence, nhưng tên Vendor phải được hỏi qua
        // contract sở hữu bởi Vendors để báo cáo không truy cập trực tiếp entity của module đó.
        var references = typeof(MaintenanceCostReportService).Assembly.GetReferencedAssemblies()
            .Select(reference => reference.Name)
            .ToArray();

        Assert.Contains("InternalMaintenance.Vendors.Contracts", references);
        Assert.DoesNotContain("InternalMaintenance.Vendors.Domain", references);
    }
}
