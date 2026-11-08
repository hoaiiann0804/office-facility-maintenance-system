using InternalMaintenance.Api.Modules.Tickets;
using AssetsEquipment = InternalMaintenance.Assets.Domain.Equipment;
using InternalMaintenance.Maintenance.Domain;
using InternalMaintenance.Maintenance.Application;
using InternalMaintenance.Api.Modules.Equipment;
using InternalMaintenance.Assets.Infrastructure;
using InternalMaintenance.Organization.Infrastructure;
using InternalMaintenance.Vendors.Infrastructure;
using InternalMaintenance.TicketAttachments.Infrastructure;
using InternalMaintenance.Maintenance.Infrastructure;
using InternalMaintenance.Api.Modules.Maintenance;
using InternalMaintenance.Assets.Contracts;
using InternalMaintenance.Api.Modules.Dashboard;
using InternalMaintenance.Identity.Infrastructure;
using InternalMaintenance.Identity.Infrastructure.DependencyInjection;
using InternalMaintenance.Api.Modules.Reports;
using InternalMaintenance.Api.Extensions;
using InternalMaintenance.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NetArchTest.Rules;
using IdentityUser = InternalMaintenance.Identity.Domain.User;
using OrganizationDepartment = InternalMaintenance.Organization.Domain.Department;

namespace Architecture.Tests;

public class NamespaceArchitectureTests
{
    [Fact]
    public void Ticket_attachment_domain_does_not_reference_the_api_host_or_ef()
    {
        // Entity attachment Ä‘Ă£ sang assembly riĂªng nhÆ°ng dá»¯ liá»‡u váº«n do AppDbContext cá»§a host lÆ°u.
        // RĂ o cháº¯n nĂ y trĂ¡nh Domain má»›i vĂ´ tĂ¬nh kĂ©o HTTP host hoáº·c EF vĂ o khi module phĂ¡t triá»ƒn tiáº¿p.
        var assembly = typeof(InternalMaintenance.TicketAttachments.Domain.TicketAttachment).Assembly;
        var references = assembly.GetReferencedAssemblies().Select(reference => reference.Name).ToArray();

        Assert.DoesNotContain("InternalMaintenance.Api", references);
        Assert.DoesNotContain("Microsoft.EntityFrameworkCore", references);
        Assert.DoesNotContain("InternalMaintenance.Maintenance.Domain", references);
    }

    [Fact]
    public void Ticket_attachment_contracts_do_not_reference_the_api_host_or_web_framework()
    {
        // DTO vĂ  service port lĂ  giao kĂ¨o giá»¯a host vá»›i module; giá»¯ chĂºng thuáº§n giĂºp Ä‘á»•i endpoint,
        // storage hoáº·c EF mĂ  khĂ´ng buá»™c assembly contract phá»¥ thuá»™c ASP.NET hay API implementation.
        var assembly = typeof(InternalMaintenance.TicketAttachments.Contracts.ITicketAttachmentsService).Assembly;
        var references = assembly.GetReferencedAssemblies().Select(reference => reference.Name).ToArray();

        Assert.DoesNotContain("InternalMaintenance.Api", references);
        Assert.DoesNotContain("Microsoft.AspNetCore.Http", references);
        Assert.DoesNotContain("Microsoft.EntityFrameworkCore", references);
    }

    [Fact]
    public void Shared_persistence_does_not_reference_the_api_host()
    {
        // DbContext dĂ¹ng chung náº±m á»Ÿ persistence Ä‘á»ƒ cĂ¡c module Infrastructure cĂ³ thá»ƒ tham chiáº¿u xuá»‘ng,
        // cĂ²n host chá»‰ ghĂ©p persistence vá»›i endpoint; dependency ngÆ°á»£c sáº½ táº¡o vĂ²ng khi tĂ¡ch adapter.
        var assembly = typeof(InternalMaintenance.Persistence.AppDbContext).Assembly;
        var references = assembly.GetReferencedAssemblies().Select(reference => reference.Name).ToArray();

        Assert.Equal("InternalMaintenance.Persistence", assembly.GetName().Name);
        Assert.Equal("InternalMaintenance.Persistence", typeof(InternalMaintenance.Persistence.AppDbContext).Namespace);
        Assert.DoesNotContain("InternalMaintenance.Api", references);
    }

    [Fact]
    public void Module_domain_assemblies_do_not_reference_the_host_or_data_frameworks()
    {
        // Domain chỉ mô tả entity và quy tắc nghiệp vụ; giữ nó độc lập giúp host/EF thay đổi
        // mà không buộc các module nghiệp vụ phải đổi theo implementation bên ngoài.
        var assemblies = new[]
        {
            typeof(IdentityUser).Assembly,
            typeof(MaintenanceTicket).Assembly,
            typeof(InternalMaintenance.TicketAttachments.Domain.TicketAttachment).Assembly
        };

        foreach (var assembly in assemblies)
        {
            var references = assembly.GetReferencedAssemblies().Select(reference => reference.Name).ToArray();
            Assert.DoesNotContain("InternalMaintenance.Api", references);
            Assert.DoesNotContain("Microsoft.EntityFrameworkCore", references);
            Assert.DoesNotContain("Microsoft.AspNetCore", references);
        }
    }

    [Fact]
    public void Identity_infrastructure_depends_on_contracts_and_persistence_but_not_the_api_host()
    {
        // Directory EF Ä‘Ă£ chuyá»ƒn khá»i host. Náº¿u nĂ³ tham chiáº¿u API, host sáº½ pháº£i tham chiáº¿u ngÆ°á»£c
        // vĂ o chĂ­nh nĂ³ Ä‘á»ƒ ghĂ©p module, lĂ m máº¥t ranh giá»›i project vá»«a thiáº¿t láº­p.
        var assembly = typeof(InternalMaintenance.Identity.Infrastructure.IdentityUserDirectory).Assembly;
        var references = assembly.GetReferencedAssemblies().Select(reference => reference.Name).ToArray();

        Assert.Equal("InternalMaintenance.Identity.Infrastructure", assembly.GetName().Name);
        Assert.DoesNotContain("InternalMaintenance.Api", references);
        Assert.Contains("InternalMaintenance.Persistence", references);
        Assert.Contains("InternalMaintenance.Identity.Contracts", references);
    }

    [Fact]
    public void Equipment_infrastructure_depends_on_module_contracts_and_persistence_but_not_the_api_host()
    {
        // EF adapter thuá»™c Equipment nĂªn chá»‰ Ä‘Æ°á»£c phá»¥ thuá»™c vĂ o contract, entity sá»Ÿ há»¯u vĂ  persistence chung.
        // Cháº·n tham chiáº¿u ngÆ°á»£c tá»›i API host Ä‘á»ƒ sau nĂ y host cĂ³ thá»ƒ chá»‰ lĂ m composition root cho cĂ¡c module.
        var assembly = typeof(EquipmentCatalog).Assembly;
        var references = assembly.GetReferencedAssemblies().Select(reference => reference.Name).ToArray();

        Assert.Equal("InternalMaintenance.Equipment.Infrastructure", assembly.GetName().Name);
        Assert.DoesNotContain("InternalMaintenance.Api", references);
        Assert.Contains("InternalMaintenance.Equipment.Contracts", references);
        Assert.Contains("InternalMaintenance.Persistence", references);
    }

    [Fact]
    public void Equipment_contracts_do_not_reference_api_host_ef_or_persistence()
    {
        // Controller, Dashboard vĂ  module gá»i trao Ä‘á»•i qua DTO/port thuáº§n; chĂºng khĂ´ng Ä‘Æ°á»£c nháº­n EF hoáº·c DbContext.
        var assembly = typeof(InternalMaintenance.Assets.Contracts.IEquipmentCatalog).Assembly;
        var references = assembly.GetReferencedAssemblies().Select(reference => reference.Name).ToArray();

        Assert.Equal("InternalMaintenance.Equipment.Contracts", assembly.GetName().Name);
        Assert.DoesNotContain("InternalMaintenance.Api", references);
        Assert.DoesNotContain("Microsoft.EntityFrameworkCore", references);
        Assert.DoesNotContain("InternalMaintenance.Persistence", references);
    }

    [Fact]
    public void Organization_contracts_do_not_reference_the_api_host_or_persistence()
    {
        // Identity, Equipment vĂ  Dashboard trao Ä‘á»•i dá»¯ liá»‡u phĂ²ng ban qua contract nĂ y;
        // contract khĂ´ng Ä‘Æ°á»£c kĂ©o API host hoáº·c EF vĂ o chuá»—i dependency cá»§a cĂ¡c module gá»i.
        var assembly = typeof(InternalMaintenance.Organization.Contracts.IOrganizationDirectory).Assembly;
        var references = assembly.GetReferencedAssemblies().Select(reference => reference.Name).ToArray();

        Assert.Equal("InternalMaintenance.Organization.Contracts", assembly.GetName().Name);
        Assert.DoesNotContain("InternalMaintenance.Api", references);
        Assert.DoesNotContain("Microsoft.EntityFrameworkCore", references);
        Assert.DoesNotContain("InternalMaintenance.Persistence", references);
    }

    [Fact]
    public void Organization_infrastructure_depends_on_contracts_and_persistence_but_not_the_api_host()
    {
        // Organization sá»Ÿ há»¯u adapter truy váº¥n Department; cĂ¡c module khĂ¡c chá»‰ tháº¥y contract projection.
        // Náº¿u adapter tham chiáº¿u ngÆ°á»£c API host, host sáº½ khĂ´ng thá»ƒ láº¯p module theo dependency má»™t chiá»u.
        var assembly = typeof(OrganizationDirectory).Assembly;
        var references = assembly.GetReferencedAssemblies().Select(reference => reference.Name).ToArray();

        Assert.Equal("InternalMaintenance.Organization.Infrastructure", assembly.GetName().Name);
        Assert.DoesNotContain("InternalMaintenance.Api", references);
        Assert.Contains("InternalMaintenance.Organization.Contracts", references);
        Assert.Contains("InternalMaintenance.Persistence", references);
    }

    [Fact]
    public void Vendors_infrastructure_depends_on_its_contract_and_persistence_but_not_the_api_host()
    {
        // Vendors chá»‰ cung cáº¥p catalog vĂ  directory; adapter EF pháº£i á»Ÿ project riĂªng Ä‘á»ƒ module khĂ¡c gá»i qua contract.
        // Giá»¯ API host khá»i assembly implementation giĂºp thay Ä‘á»•i cĂ¡ch Ä‘á»c/ghi vendor khĂ´ng lan sang caller.
        var assembly = typeof(VendorDirectory).Assembly;
        var references = assembly.GetReferencedAssemblies().Select(reference => reference.Name).ToArray();

        Assert.Equal("InternalMaintenance.Vendors.Infrastructure", assembly.GetName().Name);
        Assert.DoesNotContain("InternalMaintenance.Api", references);
        Assert.Contains("InternalMaintenance.Vendors.Contracts", references);
        Assert.Contains("InternalMaintenance.Persistence", references);
    }

    [Fact]
    public void Maintenance_infrastructure_depends_on_application_contracts_and_persistence_but_not_the_api_host()
    {
        // EF stores, readers vĂ  projection lĂ  implementation ná»™i bá»™ cá»§a Maintenance, khĂ´ng thuá»™c API host.
        // DĂ¹ng AppDbContext chung qua Persistence váº«n giá»¯ transaction nguyĂªn váº¹n trong giai Ä‘oáº¡n tĂ¡ch module.
        var assembly = typeof(EfTicketCreationStore).Assembly;
        var references = assembly.GetReferencedAssemblies().Select(reference => reference.Name).ToArray();

        Assert.Equal("InternalMaintenance.Maintenance.Infrastructure", assembly.GetName().Name);
        Assert.DoesNotContain("InternalMaintenance.Api", references);
        Assert.Contains("InternalMaintenance.Maintenance.Application", references);
        Assert.Contains("InternalMaintenance.Persistence", references);
    }

    [Fact]
    public void Ticket_attachments_infrastructure_uses_contracts_and_persistence_without_api_host_dependency()
    {
        // Upload/R2/cleanup lĂ  implementation cá»§a Attachments. API chá»‰ cung cáº¥p actor qua contract,
        // nhá» Ä‘Ă³ module khĂ´ng pháº£i tham chiáº¿u ngÆ°á»£c CurrentUserService hoáº·c namespace cá»§a host.
        var assembly = typeof(TicketAttachmentsService).Assembly;
        var references = assembly.GetReferencedAssemblies().Select(reference => reference.Name).ToArray();

        Assert.Equal("InternalMaintenance.TicketAttachments.Infrastructure", assembly.GetName().Name);
        Assert.DoesNotContain("InternalMaintenance.Api", references);
        Assert.Contains("InternalMaintenance.TicketAttachments.Contracts", references);
        Assert.Contains("InternalMaintenance.Persistence", references);
    }

    [Fact]
    public void Module_infrastructure_assemblies_do_not_depend_on_the_api_host()
    {
        // Host được phép ghép các module, nhưng Infrastructure không được gọi ngược vào host để lấy service hay cấu hình.
        // Danh sách tập trung này giúp module mới cũng phải giữ cùng chiều phụ thuộc khi thêm project vào solution.
        var assemblies = new[]
        {
            typeof(EquipmentCatalog).Assembly,
            typeof(InternalMaintenance.Identity.Infrastructure.IdentityUserDirectory).Assembly,
            typeof(InternalMaintenance.Organization.Infrastructure.OrganizationDirectory).Assembly,
            typeof(VendorDirectory).Assembly,
            typeof(TicketAttachmentsService).Assembly,
            typeof(EfTicketCreationStore).Assembly,
            typeof(InternalMaintenance.Reporting.Infrastructure.SlaComplianceReportService).Assembly
        };

        foreach (var assembly in assemblies)
        {
            var references = assembly.GetReferencedAssemblies().Select(reference => reference.Name).ToArray();
            Assert.DoesNotContain("InternalMaintenance.Api", references);
        }
    }

    [Theory]
    [InlineData(null, false)]
    [InlineData("1234567", false)]
    [InlineData("12345678", true)]
    [InlineData(" 123456", false)]
    public void Identity_password_policy_requires_eight_characters_without_normalizing_input(
        string? password,
        bool expected)
    {
        // Policy pháº£i khá»›p vá»›i MinLength trĂªn DTO: Ä‘áº¿m Ä‘Ăºng kĂ½ tá»± nháº­n Ä‘Æ°á»£c vĂ  khĂ´ng tá»± trim,
        // trĂ¡nh viá»‡c API vĂ  cĂ¡c lá»i gá»i ná»™i bá»™ diá»…n giáº£i cĂ¹ng má»™t máº­t kháº©u theo hai cĂ¡ch khĂ¡c nhau.
        Assert.Equal(expected, InternalMaintenance.Identity.Application.IdentityPasswordPolicy
            .MeetsMinimumLength(password));
    }

    [Fact]
    public void Maintenance_application_and_public_contracts_do_not_reference_the_api_host_or_ef()
    {
        // DĂ¹ cĂ¡c namespace váº«n Ä‘Æ°á»£c giá»¯ nguyĂªn Ä‘á»ƒ trĂ¡nh breaking change diá»‡n rá»™ng,
        // assembly boundary pháº£i báº£o Ä‘áº£m use case/contracts khĂ´ng cáº§n tham chiáº¿u API host hay EF.
        var assemblies = new[]
        {
            typeof(ChangeTicketStatusHandler).Assembly,
            typeof(InternalMaintenance.Maintenance.Contracts.MaintenanceTicketResponse).Assembly,
            typeof(InternalMaintenance.Assets.Contracts.IEquipmentTicketContextReader).Assembly,
            typeof(InternalMaintenance.Identity.Contracts.IIdentityUserDirectory).Assembly
        };

        foreach (var assembly in assemblies)
        {
            var references = assembly.GetReferencedAssemblies().Select(reference => reference.Name).ToArray();
            Assert.DoesNotContain("InternalMaintenance.Api", references);
            Assert.DoesNotContain("Microsoft.EntityFrameworkCore", references);
        }
    }

    [Fact]
    public void Identity_application_and_contracts_do_not_reference_the_api_host_or_ef()
    {
        // CĂ¡c DTO/service port cá»§a Identity Ä‘Ă£ rá»i API host. Giá»¯ dependency má»™t chiá»u giĂºp host
        // ghĂ©p adapter EF mĂ  khĂ´ng khiáº¿n module Application/Contracts quay láº¡i phá»¥ thuá»™c host.
        var assemblies = new[]
        {
            typeof(InternalMaintenance.Identity.Application.IAuthService).Assembly,
            typeof(InternalMaintenance.Identity.Contracts.IIdentityUserDirectory).Assembly
        };

        foreach (var assembly in assemblies)
        {
            var references = assembly.GetReferencedAssemblies().Select(reference => reference.Name).ToArray();
            Assert.DoesNotContain("InternalMaintenance.Api", references);
            Assert.DoesNotContain("Microsoft.EntityFrameworkCore", references);
        }
    }

    [Fact]
    public void Reports_controller_does_not_depend_on_database_or_module_entities()
    {
        // Controller chá»‰ giá»¯ trĂ¡ch nhiá»‡m HTTP. Quy táº¯c nĂ y ngÄƒn query EF vĂ  entity cá»§a module
        // quay láº¡i controller khi bá»• sung thĂªm má»™t loáº¡i bĂ¡o cĂ¡o má»›i.
        var result = Types.InAssembly(typeof(ReportsController).Assembly)
            .That().HaveName("ReportsController")
            .ShouldNot().HaveDependencyOnAny(
                "InternalMaintenance.Api.Data",
                "InternalMaintenance.Api.Models",
                "InternalMaintenance.Maintenance.Domain",
                "InternalMaintenance.Assets.Domain",
                "InternalMaintenance.Api.Modules.Vendors",
                "Microsoft.EntityFrameworkCore")
            .GetResult();

        Assert.True(result.IsSuccessful,
            string.Join(Environment.NewLine, result.FailingTypeNames ?? []));
    }

    [Fact]
    public void Ticket_and_equipment_persistence_adapters_are_scoped_for_one_request_unit_of_work()
    {
        // Status change có th? s?a Ticket, Equipment, VendorLog và History. Dùng composition root th?t c?a host
        // đ? phát hi?n n?u context ho?c m?t adapter b? đ?i lifetime, làm m?t unit of work c?a request.
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = "Server=localhost;Database=ArchitectureOnly;TrustServerCertificate=True",
                ["Jwt:Key"] = "architecture-test-signing-key-with-at-least-32-characters",
                ["Jwt:Issuer"] = "architecture-tests",
                ["Jwt:Audience"] = "architecture-tests"
            })
            .Build();
        var services = new ServiceCollection();
        services.AddApplicationServices(configuration);

        Assert.Equal(ServiceLifetime.Scoped,
            services.Single(descriptor => descriptor.ServiceType == typeof(AppDbContext)).Lifetime);
        Assert.Equal(ServiceLifetime.Scoped,
            services.Single(descriptor => descriptor.ServiceType == typeof(IEquipmentDashboardReader)).Lifetime);
        Assert.Equal(ServiceLifetime.Scoped,
            services.Single(descriptor => descriptor.ServiceType == typeof(IEquipmentMaintenanceGateway)).Lifetime);
        Assert.Equal(ServiceLifetime.Scoped,
            services.Single(descriptor => descriptor.ServiceType == typeof(ITicketStatusChangeStore)).Lifetime);
        // Hai adapter phải nhận DbContext từ DI, không tự tạo context riêng làm vỡ transaction của use case.
        Assert.Contains(typeof(AppDbContext), typeof(EquipmentMaintenanceGateway).GetConstructors()
            .Single().GetParameters().Select(parameter => parameter.ParameterType));
        Assert.Contains(typeof(AppDbContext), typeof(EfTicketStatusChangeStore).GetConstructors()
            .Single().GetParameters().Select(parameter => parameter.ParameterType));

        // Không m? k?t n?i SQL; ch? ki?m tra scope c?p m?t context dùng chung cho adapter và không tái s? d?ng
        // context gi?a request, nh? đó thay đ?i tracked c?a Equipment đư?c SaveChanges cùng Ticket và audit.
        using var provider = services.BuildServiceProvider();
        using var firstScope = provider.CreateScope();
        var firstContext = firstScope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.Same(firstContext, firstScope.ServiceProvider.GetRequiredService<AppDbContext>());

        using var secondScope = provider.CreateScope();
        var secondContext = secondScope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.NotSame(firstContext, secondContext);
    }

    [Fact]
    public void Identity_controllers_use_module_services_instead_of_database_adapters()
    {
        // Auth/Users lĂ  ranh giá»›i HTTP. Náº¿u controller nháº­n AppDbContext hoáº·c implementation EF,
        // thay Ä‘á»•i schema vĂ  cĂ¡ch lÆ°u Identity sáº½ lan tháº³ng lĂªn API thay vĂ¬ Ä‘i qua service cá»§a module.
        var assembly = typeof(InternalMaintenance.Api.Modules.Auth.AuthController).Assembly;
        var forbiddenDependencies = new[]
        {
            "InternalMaintenance.Api.Data",
            "InternalMaintenance.Identity.Infrastructure",
            "Microsoft.EntityFrameworkCore"
        };

        foreach (var controllerName in new[] { "AuthController", "UsersController" })
        {
            var result = Types.InAssembly(assembly)
                .That().HaveName(controllerName)
                .ShouldNot().HaveDependencyOnAny(forbiddenDependencies)
                .GetResult();

            Assert.True(result.IsSuccessful,
                $"{controllerName} references a persistence implementation: " +
                string.Join(Environment.NewLine, result.FailingTypeNames ?? []));
        }
    }

    [Fact]
    public void Identity_module_registers_its_public_services_as_scoped()
    {
        // Directory, authentication vĂ  user administration dĂ¹ng AppDbContext scoped.
        // CĂ¹ng lifetime giĂºp má»™t request giá»¯ Ä‘Ăºng má»™t unit of work thay vĂ¬ táº¡o context rá»i ráº¡c.
        var services = new Microsoft.Extensions.DependencyInjection.ServiceCollection();
        services.AddIdentityModule();

        var expectedScopedServices = new[]
        {
            typeof(InternalMaintenance.Identity.Application.IAuthService),
            typeof(InternalMaintenance.Identity.Application.IUserAdministrationService),
            typeof(InternalMaintenance.Identity.Contracts.IIdentityUserDirectory),
            typeof(InternalMaintenance.Identity.Contracts.ITechnicianDirectory)
        };

        foreach (var serviceType in expectedScopedServices)
        {
            Assert.Equal(
                Microsoft.Extensions.DependencyInjection.ServiceLifetime.Scoped,
                services.Single(descriptor => descriptor.ServiceType == serviceType).Lifetime);
        }
    }

    [Fact]
    public void Ticket_workflow_rules_do_not_depend_on_web_or_persistence_frameworks()
    {
        // Giai Ä‘oáº¡n nĂ y Ä‘ang phĂ¢n lá»›p trong má»™t assembly. Luáº­t nhá» nĂ y báº£o vá»‡ pháº§n
        // workflow thuáº§n khá»i bá»‹ kĂ©o ngÆ°á»£c vĂ o ASP.NET Core/EF khi tiáº¿p tá»¥c refactor.
        var result = Types.InAssembly(typeof(TicketWorkflowRules).Assembly)
            .That().ResideInNamespace("InternalMaintenance.Maintenance.Domain")
            .ShouldNot().HaveDependencyOnAny(
                "Microsoft.EntityFrameworkCore",
                "Microsoft.AspNetCore",
                "InternalMaintenance.Api.Data",
                "InternalMaintenance.Api.Models",
                "InternalMaintenance.Api.Modules.Tickets")
            .GetResult();

        Assert.True(result.IsSuccessful,
            string.Join(Environment.NewLine, result.FailingTypeNames ?? []));
    }

    [Fact]
    public void Maintenance_application_does_not_depend_on_web_or_ef_implementations()
    {
        // The use case may depend on domain rules and ports, while HTTP and EF remain
        // at the outer edges. This catches accidental movement of controller/SQL code inward.
        var result = Types.InAssembly(typeof(ChangeTicketStatusHandler).Assembly)
            .That().ResideInNamespace("InternalMaintenance.Maintenance.Application")
            .ShouldNot().HaveDependencyOnAny(
                "Microsoft.EntityFrameworkCore",
                "Microsoft.AspNetCore",
                "InternalMaintenance.Api.Data")
            .GetResult();

        Assert.True(result.IsSuccessful,
            string.Join(Environment.NewLine, result.FailingTypeNames ?? []));
    }

    [Fact]
    public void Maintenance_ticket_controller_only_depends_on_application_and_read_contracts()
    {
        // CĂ¡c endpoint ticket Ä‘Ă£ chuyá»ƒn truy váº¥n/ghi dá»¯ liá»‡u sang handler vĂ  reader.
        // Rule nĂ y ngÄƒn controller nháº­n láº¡i AppDbContext hoáº·c báº¯t Ä‘áº§u dĂ¹ng EF trá»±c tiáº¿p khi thĂªm API má»›i.
        var result = Types.InAssembly(typeof(MaintenanceTicketsController).Assembly)
            .That().HaveName("MaintenanceTicketsController")
            .ShouldNot().HaveDependencyOnAny(
                "InternalMaintenance.Api.Data",
                "Microsoft.EntityFrameworkCore")
            .GetResult();

        Assert.True(result.IsSuccessful,
            string.Join(Environment.NewLine, result.FailingTypeNames ?? []));
    }

    [Fact]
    public void Dashboard_composes_module_contracts_without_querying_module_entities()
    {
        // Dashboard chá»‰ Ä‘iá»u phá»‘i káº¿t quáº£ tá»•ng há»£p; tá»«ng module tá»± truy váº¥n báº£ng mĂ¬nh sá»Ÿ há»¯u.
        // Äiá»u nĂ y giĂºp Ä‘á»•i schema/persistence cá»§a Assets, Identity hoáº·c Maintenance khĂ´ng lan vĂ o controller.
        var result = Types.InAssembly(typeof(DashboardController).Assembly)
            .That().HaveName("DashboardController")
            .ShouldNot().HaveDependencyOnAny(
                "InternalMaintenance.Api.Data",
                "InternalMaintenance.Api.Models",
                "InternalMaintenance.Assets.Domain",
                "InternalMaintenance.Maintenance.Domain",
                "InternalMaintenance.Identity.Infrastructure",
                "InternalMaintenance.Assets.Infrastructure",
                "InternalMaintenance.Maintenance.Infrastructure",
                "InternalMaintenance.Organization.Infrastructure")
            .GetResult();

        Assert.True(result.IsSuccessful,
            string.Join(Environment.NewLine, result.FailingTypeNames ?? []));
    }

    [Fact]
    public void Maintenance_application_does_not_depend_on_other_module_implementations()
    {
        // Maintenance cĂ³ thá»ƒ gá»i contract cĂ´ng khai cá»§a Equipment/Identity. Cháº·n tham chiáº¿u
        // Ä‘áº¿n Infrastructure Ä‘á»ƒ thay Ä‘á»•i cĂ¡ch lÆ°u hoáº·c truy váº¥n cá»§a module kia khĂ´ng lan sang use case.
        var result = Types.InAssembly(typeof(ChangeTicketStatusHandler).Assembly)
            .That().ResideInNamespace("InternalMaintenance.Maintenance.Application")
            .ShouldNot().HaveDependencyOnAny(
                "InternalMaintenance.Assets.Infrastructure",
                "InternalMaintenance.Identity.Infrastructure")
            .GetResult();

        Assert.True(result.IsSuccessful,
            string.Join(Environment.NewLine, result.FailingTypeNames ?? []));
    }

    [Fact]
    public void Equipment_controller_uses_maintenance_contract_instead_of_maintenance_domain_entities()
    {
        // Assets cáº§n há»i Maintenance vá» ticket Ä‘ang chiáº¿m thiáº¿t bá»‹, nhÆ°ng khĂ´ng nĂªn tá»± Ä‘á»c
        // tráº¡ng thĂ¡i/domain entity cá»§a Maintenance. Contract giá»¯ chiá»u phá»¥ thuá»™c rĂµ rĂ ng.
        var result = Types.InAssembly(typeof(EquipmentController).Assembly)
            .That().HaveName("EquipmentController")
            .ShouldNot().HaveDependencyOnAny(
                "InternalMaintenance.Maintenance.Domain",
                "InternalMaintenance.Maintenance.Infrastructure")
            .GetResult();

        Assert.True(result.IsSuccessful,
            string.Join(Environment.NewLine, result.FailingTypeNames ?? []));
    }

    [Fact]
    public void Ticket_module_does_not_depend_on_other_module_implementations()
    {
        // Tickets Ä‘Æ°á»£c phĂ©p gá»i Identity.Contracts, nhÆ°ng náº¿u gá»i tháº³ng Infrastructure
        // thĂ¬ cĂ¡ch lÆ°u/query Identity sáº½ rĂ² sang module khĂ¡c vĂ  khĂ³ thay Ä‘á»•i Ä‘á»™c láº­p.
        var result = Types.InAssembly(typeof(MaintenanceTicketsController).Assembly)
            .That().ResideInNamespace("InternalMaintenance.Api.Modules.Tickets")
            .ShouldNot().HaveDependencyOnAny(
                "InternalMaintenance.Identity.Infrastructure",
                "InternalMaintenance.Assets.Infrastructure")
            .GetResult();

        Assert.True(result.IsSuccessful,
            string.Join(Environment.NewLine, result.FailingTypeNames ?? []));
    }

    [Fact]
    public void Ticket_attachments_module_uses_other_modules_through_contracts()
    {
        // Attachments cĂ³ thá»ƒ gá»i contract, nhÆ°ng truy váº¥n/báº¥t biáº¿n cá»§a Identity vĂ  Equipment
        // pháº£i tiáº¿p tá»¥c náº±m trong module sá»Ÿ há»¯u thay vĂ¬ bá»‹ gá»i trá»±c tiáº¿p tá»« tĂ­nh nÄƒng Ä‘Ă­nh kĂ¨m.
        var result = Types.InAssembly(typeof(MaintenanceTicketsController).Assembly)
            .That().ResideInNamespace("InternalMaintenance.Api.Modules.TicketAttachments")
            .ShouldNot().HaveDependencyOnAny(
                "InternalMaintenance.Identity.Infrastructure",
                "InternalMaintenance.Assets.Infrastructure")
            .GetResult();

        Assert.True(result.IsSuccessful,
            string.Join(Environment.NewLine, result.FailingTypeNames ?? []));
    }

    [Fact]
    public void Ticket_attachments_service_uses_maintenance_contract_instead_of_maintenance_entity()
    {
        // Attachments cáº§n mĂ£, tráº¡ng thĂ¡i vĂ  ID ticket; Maintenance.Contracts cung cáº¥p projection Ä‘Ă³.
        // Quy táº¯c ngÄƒn service quay láº¡i táº£i MaintenanceTicket trá»±c tiáº¿p tá»« DbContext.
        var result = Types.InAssembly(typeof(MaintenanceTicketsController).Assembly)
            .That().HaveName("TicketAttachmentsService")
            .ShouldNot().HaveDependencyOnAny(
                "InternalMaintenance.Maintenance.Domain",
                "InternalMaintenance.Maintenance.Infrastructure")
            .GetResult();

        Assert.True(result.IsSuccessful,
            string.Join(Environment.NewLine, result.FailingTypeNames ?? []));
    }

    [Fact]
    public void Assets_domain_does_not_depend_on_maintenance_domain()
    {
        // Quan há»‡ má»™t-nhiá»u trÆ°á»›c Ä‘Ă¢y buá»™c Equipment giá»¯ ICollection<MaintenanceTicket>.
        // Assets chá»‰ cĂ²n sá»Ÿ há»¯u Equipment; FK váº«n do Maintenance lÆ°u vĂ  SQL Server báº£o vá»‡.
        var result = Types.InAssembly(typeof(AssetsEquipment).Assembly)
            .That().ResideInNamespace("InternalMaintenance.Assets.Domain")
            .ShouldNot().HaveDependencyOn("InternalMaintenance.Maintenance.Domain")
            .GetResult();

        Assert.True(result.IsSuccessful,
            string.Join(Environment.NewLine, result.FailingTypeNames ?? []));
    }

    [Fact]
    public void Identity_user_does_not_depend_on_ticket_entities()
    {
        // User chá»‰ giá»¯ dá»¯ liá»‡u Identity. Ticket/History/Attachment tham chiáº¿u UserId má»™t chiá»u,
        // nĂªn thĂªm loáº¡i ticket má»›i khĂ´ng lĂ m entity User pháº£i sá»­a theo.
        var result = Types.InAssembly(typeof(IdentityUser).Assembly)
            .That().HaveName("User")
            .ShouldNot().HaveDependencyOnAny(
                "InternalMaintenance.Maintenance.Domain",
                "InternalMaintenance.Api.Modules.Tickets",
                "InternalMaintenance.Api.Modules.TicketAttachments")
            .GetResult();

        Assert.True(result.IsSuccessful,
            string.Join(Environment.NewLine, result.FailingTypeNames ?? []));
    }

    [Fact]
    public void Ticket_comment_keeps_identity_reference_as_an_id_and_not_an_entity_navigation()
    {
        // UserId cĂ¹ng FK váº«n báº£o Ä‘áº£m comment trá» tá»›i ngÆ°á»i dĂ¹ng há»£p lá»‡. Bá» navigation giĂºp
        // module Tickets láº¥y tĂªn qua Identity contract thay vĂ¬ kĂ©o entity User vĂ o model cá»§a mĂ¬nh.
        var result = Types.InAssembly(typeof(IdentityUser).Assembly)
            .That().HaveName("TicketComment")
            .ShouldNot().HaveDependencyOn("InternalMaintenance.Identity.Domain.User")
            .GetResult();

        Assert.True(result.IsSuccessful,
            string.Join(Environment.NewLine, result.FailingTypeNames ?? []));
    }

    [Fact]
    public void Ticket_status_history_keeps_actor_reference_as_an_id_and_not_an_identity_entity()
    {
        // Lá»‹ch sá»­ cáº§n ghi ai Ä‘Ă£ Ä‘á»•i tráº¡ng thĂ¡i, khĂ´ng cáº§n náº¯m entity User.
        // ID vĂ  FK giá»¯ nguyĂªn; tĂªn hiá»ƒn thá»‹ do Identity contract cung cáº¥p á»Ÿ táº§ng Ä‘á»c.
        var result = Types.InAssembly(typeof(IdentityUser).Assembly)
            .That().HaveName("TicketStatusHistory")
            .ShouldNot().HaveDependencyOn("InternalMaintenance.Identity.Domain.User")
            .GetResult();

        Assert.True(result.IsSuccessful,
            string.Join(Environment.NewLine, result.FailingTypeNames ?? []));
    }

    [Fact]
    public void Ticket_attachment_keeps_uploader_reference_as_an_id_and_not_an_identity_entity()
    {
        // Attachment chá»‰ lÆ°u cĂ¡c ID tham chiáº¿u; tĂªn uploader vĂ  quyá»n/thĂ´ng tin ticket
        // Ä‘Æ°á»£c láº¥y qua contract. CĂ¡c FK persistence váº«n giá»¯ toĂ n váº¹n dá»¯ liá»‡u trong database chung.
        var result = Types.InAssembly(typeof(IdentityUser).Assembly)
            .That().HaveName("TicketAttachment")
            .ShouldNot().HaveDependencyOnAny(
                "InternalMaintenance.Identity.Domain.User",
                "InternalMaintenance.Maintenance.Domain")
            .GetResult();

        Assert.True(result.IsSuccessful,
            string.Join(Environment.NewLine, result.FailingTypeNames ?? []));
    }

    [Fact]
    public void Maintenance_ticket_does_not_own_attachment_module_entities()
    {
        // Ticket khĂ´ng cáº§n collection Attachment Ä‘á»ƒ thá»±c hiá»‡n use case; module Attachments
        // truy váº¥n Ä‘á»™c láº­p theo MaintenanceTicketId, vĂ¬ váº­y trĂ¡nh dependency ngÆ°á»£c trong Domain.
        var result = Types.InAssembly(typeof(MaintenanceTicket).Assembly)
            .That().HaveName("MaintenanceTicket")
            .ShouldNot().HaveDependencyOn("InternalMaintenance.Api.Modules.TicketAttachments")
            .GetResult();

        Assert.True(result.IsSuccessful,
            string.Join(Environment.NewLine, result.FailingTypeNames ?? []));
    }

    [Fact]
    public void Maintenance_ticket_references_identity_users_by_id()
    {
        // Ticket giá»¯ UserId cho ngÆ°á»i táº¡o/ká»¹ thuáº­t viĂªn; tĂªn vĂ  email Ä‘Æ°á»£c láº¥y qua Identity contract.
        // Quan há»‡ FK váº«n Ä‘Æ°á»£c EF cáº¥u hĂ¬nh á»Ÿ persistence, khĂ´ng cáº§n entity User trong domain.
        var result = Types.InAssembly(typeof(MaintenanceTicket).Assembly)
            .That().HaveName("MaintenanceTicket")
            .ShouldNot().HaveDependencyOn("InternalMaintenance.Identity.Domain.User")
            .GetResult();

        Assert.True(result.IsSuccessful,
            string.Join(Environment.NewLine, result.FailingTypeNames ?? []));
    }

    [Fact]
    public void Maintenance_ticket_references_assets_by_equipment_id()
    {
        // EquipmentId cĂ¹ng FK giá»¯ má»‘i liĂªn há»‡ dá»¯ liá»‡u; thĂ´ng tin thiáº¿t bá»‹ láº¥y qua Assets contract.
        // Maintenance Domain khĂ´ng cáº§n giá»¯ Equipment entity/navigation Ä‘á»ƒ xá»­ lĂ½ ticket.
        var result = Types.InAssembly(typeof(MaintenanceTicket).Assembly)
            .That().HaveName("MaintenanceTicket")
            .ShouldNot().HaveDependencyOn("InternalMaintenance.Assets.Domain.Equipment")
            .GetResult();

        Assert.True(result.IsSuccessful,
            string.Join(Environment.NewLine, result.FailingTypeNames ?? []));
    }

    [Fact]
    public void Assets_equipment_references_organization_by_department_ids()
    {
        // Assets lÆ°u DepartmentId/FK nhÆ°ng khĂ´ng cáº§n Organization entity Ä‘á»ƒ tá»“n táº¡i.
        // TĂªn phĂ²ng ban Ä‘Æ°á»£c ghĂ©p á»Ÿ query/projection cá»§a á»©ng dá»¥ng khi cáº§n hiá»ƒn thá»‹.
        var result = Types.InAssembly(typeof(AssetsEquipment).Assembly)
            .That().HaveName("Equipment")
            .ShouldNot().HaveDependencyOn("InternalMaintenance.Organization.Domain.Department")
            .GetResult();

        Assert.True(result.IsSuccessful,
            string.Join(Environment.NewLine, result.FailingTypeNames ?? []));
    }

    [Fact]
    public void Organization_department_does_not_own_assets_entities()
    {
        // Department khĂ´ng giá»¯ collection Equipment; Assets quáº£n lĂ½ thiáº¿t bá»‹ vĂ  chá»‰ tham chiáº¿u ID.
        var result = Types.InAssembly(typeof(OrganizationDepartment).Assembly)
            .That().HaveName("Department")
            .ShouldNot().HaveDependencyOn("InternalMaintenance.Assets.Domain")
            .GetResult();

        Assert.True(result.IsSuccessful,
            string.Join(Environment.NewLine, result.FailingTypeNames ?? []));
    }

    [Fact]
    public void Ticket_vendor_log_references_vendors_by_id_not_entity_navigation()
    {
        // TicketVendorLog chá»‰ cáº§n lÆ°u VendorId. TĂªn vendor Ä‘Æ°á»£c láº¥y qua IVendorDirectory,
        // nhá» Ä‘Ă³ module Tickets khĂ´ng phá»¥ thuá»™c entity cá»§a Vendors nhÆ°ng FK váº«n do EF báº£o vá»‡.
        var result = Types.InAssembly(typeof(TicketVendorLog).Assembly)
            .That().HaveName("TicketVendorLog")
            .ShouldNot().HaveDependencyOn("InternalMaintenance.Vendors.Domain.Vendor")
            .GetResult();

        Assert.True(result.IsSuccessful,
            string.Join(Environment.NewLine, result.FailingTypeNames ?? []));
    }

    [Fact]
    public void Department_does_not_keep_a_reverse_collection_of_identity_users()
    {
        // PhĂ²ng ban khĂ´ng cáº§n Ä‘iá»u hÆ°á»›ng ngÆ°á»£c Ä‘á»ƒ tá»“n táº¡i hay sá»­a thĂ´ng tin; Identity lÆ°u DepartmentId.
        // Quy táº¯c nĂ y trĂ¡nh Ä‘á»ƒ entity Organization phá»¥ thuá»™c ngÆ°á»£c vĂ o danh sĂ¡ch User.
        var result = Types.InAssembly(typeof(OrganizationDepartment).Assembly)
            .That().HaveName("Department")
            .ShouldNot().HaveDependencyOn("InternalMaintenance.Identity.Domain.User")
            .GetResult();

        Assert.True(result.IsSuccessful,
            string.Join(Environment.NewLine, result.FailingTypeNames ?? []));
    }

    [Fact]
    public void Identity_user_keeps_organization_reference_as_an_id_not_entity_navigation()
    {
        // Identity lÆ°u DepartmentId; tĂªn vĂ  thuá»™c tĂ­nh phĂ²ng ban Ä‘Æ°á»£c truy váº¥n khi cáº§n hiá»ƒn thá»‹/xĂ¡c thá»±c.
        // FK váº«n Ä‘Æ°á»£c cáº¥u hĂ¬nh á»Ÿ persistence nĂªn bá» navigation khĂ´ng lĂ m máº¥t kiá»ƒm tra toĂ n váº¹n dá»¯ liá»‡u.
        var result = Types.InAssembly(typeof(IdentityUser).Assembly)
            .That().HaveName("User")
            .ShouldNot().HaveDependencyOn("InternalMaintenance.Organization.Domain.Department")
            .GetResult();

        Assert.True(result.IsSuccessful,
            string.Join(Environment.NewLine, result.FailingTypeNames ?? []));
    }

    [Fact]
    public void Identity_auth_adapter_uses_organization_contract_instead_of_department_persistence()
    {
        // Auth cáº§n phĂ²ng ban máº·c Ä‘á»‹nh vĂ  thĂ´ng tin phĂ²ng Ä‘á»ƒ táº¡o claims/response.
        // Nhá»¯ng truy váº¥n Ä‘Ă³ pháº£i Ä‘i qua IOrganizationDirectory, khĂ´ng gá»i entity Department
        // hoáº·c adapter EF cá»§a Organization; nhÆ° váº­y schema Organization khĂ´ng rĂ² sang Identity.
        var result = Types.InAssembly(typeof(AuthService).Assembly)
            .That().HaveName("AuthService")
            .ShouldNot().HaveDependencyOnAny(
                "InternalMaintenance.Organization.Domain.Department",
                "InternalMaintenance.Organization.Infrastructure")
            .GetResult();

        Assert.True(result.IsSuccessful,
            string.Join(Environment.NewLine, result.FailingTypeNames ?? []));
    }
}
