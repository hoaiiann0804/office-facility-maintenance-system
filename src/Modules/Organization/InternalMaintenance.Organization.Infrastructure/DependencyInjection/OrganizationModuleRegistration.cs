using InternalMaintenance.Organization.Contracts;
using InternalMaintenance.Organization.Infrastructure.Features.CreateDepartment;
using InternalMaintenance.Organization.Infrastructure.Features.DeleteDepartment;
using InternalMaintenance.Organization.Infrastructure.Features.GetDepartmentById;
using InternalMaintenance.Organization.Infrastructure.Features.GetDepartmentsPage;
using InternalMaintenance.Organization.Infrastructure.Features.UpdateDepartment;
using Microsoft.Extensions.DependencyInjection;

namespace InternalMaintenance.Organization.Infrastructure.DependencyInjection;

/// <summary>
/// [ĐĂNG KÝ DI CHO VERTICAL SLICES CỦA MODULE ORGANIZATION]
/// </summary>
public static class OrganizationModuleRegistration
{
    public static IServiceCollection AddOrganizationModule(this IServiceCollection services)
    {
        services.AddScoped<GetDepartmentsPageHandler>();
        services.AddScoped<GetDepartmentByIdHandler>();
        services.AddScoped<CreateDepartmentHandler>();
        services.AddScoped<UpdateDepartmentHandler>();
        services.AddScoped<DeleteDepartmentHandler>();

        services.AddScoped<IDepartmentCatalog>(sp =>
            ActivatorUtilities.CreateInstance<EfDepartmentCatalog>(sp));
        services.AddScoped<IOrganizationDirectory, OrganizationDirectory>();
        return services;
    }
}
