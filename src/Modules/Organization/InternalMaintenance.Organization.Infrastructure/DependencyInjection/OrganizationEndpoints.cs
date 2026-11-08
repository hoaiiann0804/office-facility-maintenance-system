using InternalMaintenance.Organization.Infrastructure.Features.CreateDepartment;
using InternalMaintenance.Organization.Infrastructure.Features.DeleteDepartment;
using InternalMaintenance.Organization.Infrastructure.Features.GetDepartmentById;
using InternalMaintenance.Organization.Infrastructure.Features.GetDepartmentsPage;
using InternalMaintenance.Organization.Infrastructure.Features.UpdateDepartment;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace InternalMaintenance.Organization.Infrastructure.DependencyInjection;

/// <summary>
/// [MODULE ENDPOINT REGISTRATION - ORGANIZATION]
/// Gom toàn bộ 5 Minimal API Endpoints của các Vertical Slices trong module Organization
/// vào chung một Route Group `/api/departments` theo chuẩn FullStackHero.
/// </summary>
public static class OrganizationEndpoints
{
    public static IEndpointRouteBuilder MapOrganizationEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("api/departments")
            .WithTags("Departments");

        group.MapGetDepartmentsPageEndpoint();
        group.MapGetDepartmentByIdEndpoint();
        group.MapCreateDepartmentEndpoint();
        group.MapUpdateDepartmentEndpoint();
        group.MapDeleteDepartmentEndpoint();

        return app;
    }
}

