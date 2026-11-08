using InternalMaintenance.Persistence;
using InternalMaintenance.Vendors.Contracts;
using Microsoft.EntityFrameworkCore;

namespace InternalMaintenance.Vendors.Infrastructure;

/// <summary>Vendors chỉ trả projection tên; Tickets không cần nhận entity Vendor.</summary>
public sealed class VendorDirectory(AppDbContext db) : IVendorDirectory
{
    public Task<bool> IsActiveAsync(int vendorId, CancellationToken cancellationToken = default) =>
        db.Vendors.AsNoTracking().AnyAsync(
            vendor => vendor.Id == vendorId && vendor.IsActive,
            cancellationToken);

    public async Task<IReadOnlyDictionary<int, string>> FindNamesAsync(
        IReadOnlyCollection<int> vendorIds,
        CancellationToken cancellationToken = default)
    {
        if (vendorIds.Count == 0)
            return new Dictionary<int, string>();

        // Gom các VendorId của timeline thành một truy vấn, tránh navigation chéo và N+1 query.
        return await db.Vendors
            .AsNoTracking()
            .Where(vendor => vendorIds.Contains(vendor.Id))
            .ToDictionaryAsync(vendor => vendor.Id, vendor => vendor.Name, cancellationToken);
    }
}
