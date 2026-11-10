using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PollingSystem.Data.Entities;

namespace PollingSystem.Data.Configurations;

public sealed class QuestionOptionConfiguration : IEntityTypeConfiguration<QuestionOption>
{
    public void Configure(EntityTypeBuilder<QuestionOption> builder)
    {
        builder.ToTable("question_options", table =>
        {
            table.HasCheckConstraint("ck_question_options_position", "position >= 0");
        });
        builder.HasKey(x => x.Id).HasName("pk_question_options");
        builder.Property(x => x.Id).HasColumnName("id").HasColumnType("uuid").IsRequired();
        builder.Property(x => x.QuestionId).HasColumnName("question_id").HasColumnType("uuid").IsRequired();
        builder.Property(x => x.Text).HasColumnName("text").HasMaxLength(500).IsRequired();
        builder.Property(x => x.Position).HasColumnName("position").HasColumnType("integer").IsRequired();

        builder.HasIndex(x => new { x.QuestionId, x.Position }).IsUnique()
            .HasDatabaseName("ux_question_options_question_position");
        builder.HasOne(x => x.Question).WithMany(x => x.Options).HasForeignKey(x => x.QuestionId)
            .OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_question_options_question");
    }
}
