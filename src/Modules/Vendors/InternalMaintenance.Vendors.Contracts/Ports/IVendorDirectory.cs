namespace InternalMaintenance.Vendors.Contracts;

/// <summary>Contract read-only để module khác lấy tên Vendor mà không tham chiếu entity Vendors.</summary>
public interface IVendorDirectory
{
    // Maintenance chỉ cần biết ID vendor có thể nhận công việc hay không;
    // trả boolean giúp module gọi không phải phụ thuộc Vendor entity hoặc CRUD response.
    Task<bool> IsActiveAsync(int vendorId, CancellationToken cancellationToken = default);

    Task<IReadOnlyDictionary<int, string>> FindNamesAsync(
        IReadOnlyCollection<int> vendorIds,
        CancellationToken cancellationToken = default);
}
