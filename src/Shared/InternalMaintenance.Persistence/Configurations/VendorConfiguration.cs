using InternalMaintenance.Vendors.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InternalMaintenance.Persistence.Configurations.Vendors;

// Vendors sở hữu các trường liên hệ và unique name; giới hạn độ dài giúp dữ liệu phù hợp schema SQL Server.
public sealed class VendorConfiguration : IEntityTypeConfiguration<Vendor>
{
    public void Configure(EntityTypeBuilder<Vendor> builder)
    {
        builder.Property(vendor => vendor.Name).IsRequired().HasMaxLength(255);
        builder.Property(vendor => vendor.ContactPerson).HasMaxLength(255);
        builder.Property(vendor => vendor.Phone).HasMaxLength(50);
        builder.Property(vendor => vendor.Email).HasMaxLength(255);
        builder.Property(vendor => vendor.Address).HasMaxLength(500);
        builder.HasIndex(vendor => vendor.Name).IsUnique();
    }
}
