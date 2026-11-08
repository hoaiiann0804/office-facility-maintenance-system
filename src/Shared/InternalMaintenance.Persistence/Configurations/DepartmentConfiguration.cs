using InternalMaintenance.Organization.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InternalMaintenance.Persistence.Configurations.Organization;

// Organization sở hữu Department; unique name được bảo vệ ở SQL để tránh race khi tạo phòng ban đồng thời.
public sealed class DepartmentConfiguration : IEntityTypeConfiguration<Department>
{
    public void Configure(EntityTypeBuilder<Department> builder) =>
        builder.HasIndex(department => department.Name).IsUnique();
}
