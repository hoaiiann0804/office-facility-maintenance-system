using InternalMaintenance.Vendors.Contracts;
using InternalMaintenance.Vendors.Infrastructure.Features.CreateVendor;
using InternalMaintenance.Vendors.Infrastructure.Features.GetVendorById;
using InternalMaintenance.Vendors.Infrastructure.Features.GetVendors;
using InternalMaintenance.Vendors.Infrastructure.Features.ToggleVendorActive;
using InternalMaintenance.Vendors.Infrastructure.Features.UpdateVendor;
using Microsoft.Extensions.DependencyInjection;

namespace InternalMaintenance.Vendors.Infrastructure.DependencyInjection;

/// <summary>
/// [ĐĂNG KÝ DI CHO VERTICAL SLICES CỦA MODULE VENDORS]
/// </summary>
public static class VendorModuleRegistration
{
    public static IServiceCollection AddVendorModule(this IServiceCollection services)
    {
        services.AddScoped<GetVendorsHandler>();
        services.AddScoped<GetVendorByIdHandler>();
        services.AddScoped<CreateVendorHandler>();
        services.AddScoped<UpdateVendorHandler>();
        services.AddScoped<ToggleVendorActiveHandler>();

        services.AddScoped<IVendorCatalog>(sp =>
            ActivatorUtilities.CreateInstance<EfVendorCatalog>(sp));
        services.AddScoped<IVendorDirectory, VendorDirectory>();
        return services;
    }
}
