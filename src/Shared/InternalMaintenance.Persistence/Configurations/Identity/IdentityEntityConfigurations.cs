using InternalMaintenance.Identity.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InternalMaintenance.Persistence.Configurations.Identity;

// Các index và giới hạn cột Identity thuộc persistence; giữ chúng ngoài Domain để Domain không phụ thuộc EF Core.
public sealed class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder) =>
        builder.HasIndex(role => role.Name).IsUnique();
}

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder) =>
        builder.HasIndex(user => user.Email).IsUnique();
}

public sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.HasIndex(token => token.UserId);
        builder.HasIndex(token => token.Token).IsUnique();
        builder.Property(token => token.Token).HasMaxLength(500);
    }
}
