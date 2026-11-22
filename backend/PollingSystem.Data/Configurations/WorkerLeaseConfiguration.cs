using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PollingSystem.Data.Entities;

namespace PollingSystem.Data.Configurations;

public sealed class WorkerLeaseConfiguration : IEntityTypeConfiguration<WorkerLease>
{
    public void Configure(EntityTypeBuilder<WorkerLease> builder)
    {
        builder.ToTable("worker_leases");
        builder.HasKey(item => item.Name).HasName("pk_worker_leases");
        builder.Property(item => item.Name)
            .HasColumnName("name")
            .HasMaxLength(100)
            .HasColumnType("character varying(100)");
        builder.Property(item => item.OwnerId)
            .HasColumnName("owner_id")
            .HasColumnType("uuid")
            .IsRequired();
        builder.Property(item => item.ExpiresAt)
            .HasColumnName("expires_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();
    }
}
