using InternalMaintenance.Vendors.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InternalMaintenance.Api.Modules.Vendors;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin,Manager")]
public sealed class VendorsController(IVendorCatalog vendorCatalog) : ControllerBase
{
    [HttpGet]
    [AllowAnonymous] // Technician cũng cần xem vendor để chọn khi gửi thiết bị ra ngoài.
    public async Task<ActionResult<IReadOnlyList<VendorResponse>>> GetVendors(
        [FromQuery] bool? isActive,
        CancellationToken cancellationToken)
    {
        var vendors = await vendorCatalog.GetAllAsync(isActive, cancellationToken);
        return Ok(vendors);
    }

    [HttpGet("{id:int}")]
    [AllowAnonymous]
    public async Task<ActionResult<VendorResponse>> GetVendorById(int id, CancellationToken cancellationToken)
    {
        var vendor = await vendorCatalog.GetByIdAsync(id, cancellationToken);
        return vendor is null
            ? NotFound(new { message = "Vendor not found" })
            : Ok(vendor);
    }

    [HttpPost]
    public async Task<ActionResult<VendorResponse>> CreateVendor(
        [FromBody] CreateVendorRequest request,
        CancellationToken cancellationToken)
    {
        var result = await vendorCatalog.CreateAsync(request, cancellationToken);
        if (!result.IsSuccess)
            return StatusCode(result.StatusCode, new { message = result.Message });

        return CreatedAtAction(nameof(GetVendorById), new { id = result.Data!.Id }, result.Data);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<VendorResponse>> UpdateVendor(
        int id,
        [FromBody] UpdateVendorRequest request,
        CancellationToken cancellationToken)
    {
        var result = await vendorCatalog.UpdateAsync(id, request, cancellationToken);
        if (!result.IsSuccess)
            return StatusCode(result.StatusCode, new { message = result.Message });
        return Ok(result.Data);
    }

    [HttpPatch("{id:int}/toggle-active")]
    public async Task<ActionResult<VendorResponse>> ToggleVendorActive(int id, CancellationToken cancellationToken)
    {
        var result = await vendorCatalog.ToggleActiveAsync(id, cancellationToken);
        if (!result.IsSuccess)
            return StatusCode(result.StatusCode, new { message = result.Message });
        return Ok(result.Data);
    }
}
