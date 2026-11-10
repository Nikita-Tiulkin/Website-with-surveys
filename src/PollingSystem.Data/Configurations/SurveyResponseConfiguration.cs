using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PollingSystem.Data.Entities;

namespace PollingSystem.Data.Configurations;

public sealed class SurveyResponseConfiguration : IEntityTypeConfiguration<SurveyResponse>
{
    public void Configure(EntityTypeBuilder<SurveyResponse> builder)
    {
        builder.ToTable("survey_responses");
        builder.HasKey(x => x.Id).HasName("pk_survey_responses");
        builder.Property(x => x.Id).HasColumnName("id").HasColumnType("uuid").IsRequired();
        builder.Property(x => x.SurveyId).HasColumnName("survey_id").HasColumnType("uuid").IsRequired();
        builder.Property(x => x.UserId).HasColumnName("user_id").HasColumnType("uuid").IsRequired();
        builder.Property(x => x.SubmittedAt).HasColumnName("submitted_at").HasColumnType("timestamp with time zone").IsRequired().HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.HasIndex(x => new { x.SurveyId, x.UserId }).IsUnique()
            .HasDatabaseName("ux_survey_responses_survey_user");
        builder.HasIndex(x => new { x.UserId, x.SubmittedAt }).IsDescending(false, true)
            .HasDatabaseName("ix_survey_responses_user_submitted_at");
        builder.HasOne(x => x.Survey).WithMany(x => x.Responses).HasForeignKey(x => x.SurveyId)
            .OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_survey_responses_survey");
    }
}
