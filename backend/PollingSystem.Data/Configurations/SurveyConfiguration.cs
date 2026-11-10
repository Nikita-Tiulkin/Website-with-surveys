using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using PollingSystem.Data.Enums;
using PollingSystem.Data.Entities;

namespace PollingSystem.Data.Configurations;

public sealed class SurveyConfiguration : IEntityTypeConfiguration<Survey>
{
    public void Configure(EntityTypeBuilder<Survey> builder)
    {
        builder.ToTable("surveys", table =>
        {
            table.HasCheckConstraint("ck_surveys_status", "status IN ('draft', 'active', 'closed')");
            table.HasCheckConstraint("ck_surveys_ends_at", "ends_at IS NULL OR ends_at > created_at");
            table.HasCheckConstraint("ck_surveys_closed_at", "closed_at IS NULL OR closed_at >= created_at");
        });
        builder.HasKey(x => x.Id).HasName("pk_surveys");
        builder.Property(x => x.Id).HasColumnName("id").HasColumnType("uuid").IsRequired();
        builder.Property(x => x.AuthorId).HasColumnName("author_id").HasColumnType("uuid").IsRequired();
        builder.Property(x => x.Title).HasColumnName("title").HasMaxLength(200).IsRequired();
        builder.Property(x => x.Description).HasColumnName("description").HasColumnType("text");
        builder.Property(x => x.Status).HasColumnName("status").HasMaxLength(16).IsRequired();
        builder.Property(x => x.EndsAt).HasColumnName("ends_at").HasColumnType("timestamp with time zone");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp with time zone").IsRequired().HasDefaultValueSql("CURRENT_TIMESTAMP");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamp with time zone").IsRequired().HasDefaultValueSql("CURRENT_TIMESTAMP");
        builder.Property(x => x.ClosedAt).HasColumnName("closed_at").HasColumnType("timestamp with time zone");

        builder.Property(x => x.Status)
            .HasConversion(new ValueConverter<SurveyStatus, string>(
                value => ToDatabase(value),
                value => FromDatabase(value)))
            .HasDefaultValue(SurveyStatus.Draft);
        builder.HasIndex(x => new { x.AuthorId, x.CreatedAt }).IsDescending(false, true)
            .HasDatabaseName("ix_surveys_author_created_at");
        builder.HasIndex(x => new { x.Status, x.CreatedAt }).IsDescending(false, true)
            .HasDatabaseName("ix_surveys_status_created_at");
        builder.HasIndex(x => new { x.Status, x.EndsAt }).HasDatabaseName("ix_surveys_status_ends_at");
    }

    private static string ToDatabase(SurveyStatus status)
    {
        return status switch
        {
            SurveyStatus.Draft => "draft",
            SurveyStatus.Active => "active",
            SurveyStatus.Closed => "closed",
            _ => throw new ArgumentOutOfRangeException(
                nameof(status), status, "Unknown survey status")
        };
    }

    private static SurveyStatus FromDatabase(string value)
    {
        return value switch
        {
            "draft" => SurveyStatus.Draft,
            "active" => SurveyStatus.Active,
            "closed" => SurveyStatus.Closed,
            _ => throw new InvalidOperationException(
                $"Unknown survey status: {value}")
        };
    }
}
