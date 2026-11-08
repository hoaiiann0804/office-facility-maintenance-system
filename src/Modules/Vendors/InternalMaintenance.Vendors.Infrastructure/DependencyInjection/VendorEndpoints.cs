using InternalMaintenance.Vendors.Infrastructure.Features.CreateVendor;
using InternalMaintenance.Vendors.Infrastructure.Features.GetVendorById;
using InternalMaintenance.Vendors.Infrastructure.Features.GetVendors;
using InternalMaintenance.Vendors.Infrastructure.Features.ToggleVendorActive;
using InternalMaintenance.Vendors.Infrastructure.Features.UpdateVendor;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace InternalMaintenance.Vendors.Infrastructure.DependencyInjection;

/// <summary>
/// [MODULE ENDPOINT REGISTRATION - VENDORS]
/// Gom toàn bộ 5 Minimal API Endpoints của các Vertical Slices trong module Vendors
/// vào chung một Route Group `/api/vendors` theo chuẩn FullStackHero.
/// </summary>
public static class VendorEndpoints
{
    public static IEndpointRouteBuilder MapVendorEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("api/vendors")
            .WithTags("Vendors")
            .RequireAuthorization(new AuthorizeAttribute { Roles = "Admin,Manager" });

        group.MapGetVendorsEndpoint();
        group.MapGetVendorByIdEndpoint();
        group.MapCreateVendorEndpoint();
        group.MapUpdateVendorEndpoint();
        group.MapToggleVendorActiveEndpoint();

        return app;
    }
}

