using InternalMaintenance.Assets.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InternalMaintenance.Persistence.Configurations.Assets;

// Assets.Domain giữ entity thuần; unique code được định nghĩa ở persistence để cơ sở dữ liệu chặn trùng thiết bị.
public sealed class EquipmentConfiguration : IEntityTypeConfiguration<Equipment>
{
    public void Configure(EntityTypeBuilder<Equipment> builder) =>
        builder.HasIndex(item => item.Code).IsUnique();
}
