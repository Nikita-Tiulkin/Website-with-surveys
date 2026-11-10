using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PollingSystem.Data.Entities;

namespace PollingSystem.Data.Configurations;

public sealed class AnswerConfiguration : IEntityTypeConfiguration<Answer>
{
    public void Configure(EntityTypeBuilder<Answer> builder)
    {
        builder.ToTable("answers", table =>
        {
            table.HasCheckConstraint("ck_answers_text_value", "text_value IS NULL OR btrim(text_value) <> ''");
        });
        builder.HasKey(x => x.Id).HasName("pk_answers");
        builder.Property(x => x.Id).HasColumnName("id").HasColumnType("uuid").IsRequired();
        builder.Property(x => x.ResponseId).HasColumnName("response_id").HasColumnType("uuid").IsRequired();
        builder.Property(x => x.QuestionId).HasColumnName("question_id").HasColumnType("uuid").IsRequired();
        builder.Property(x => x.TextValue).HasColumnName("text_value").HasColumnType("text");

        builder.HasIndex(x => new { x.ResponseId, x.QuestionId }).IsUnique()
            .HasDatabaseName("ux_answers_response_question");
        builder.HasIndex(x => x.QuestionId).HasDatabaseName("ix_answers_question_id");
        builder.HasOne(x => x.Response).WithMany(x => x.Answers)
            .HasForeignKey(x => x.ResponseId)
            .OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_answers_response");
        builder.HasOne(x => x.Question).WithMany(x => x.Answers)
            .HasForeignKey(x => x.QuestionId)
            .OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_answers_question");
    }
}
