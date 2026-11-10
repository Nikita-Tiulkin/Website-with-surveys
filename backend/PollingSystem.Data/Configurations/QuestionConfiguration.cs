using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using PollingSystem.Data.Enums;
using PollingSystem.Data.Entities;

namespace PollingSystem.Data.Configurations;

public sealed class QuestionConfiguration : IEntityTypeConfiguration<Question>
{
    public void Configure(EntityTypeBuilder<Question> builder)
    {
        builder.ToTable("questions", table =>
        {
            table.HasCheckConstraint("ck_questions_position", "position >= 0");
            table.HasCheckConstraint("ck_questions_type", "type IN ('single_choice', 'multiple_choice', 'text')");
        });
        builder.HasKey(x => x.Id).HasName("pk_questions");
        builder.Property(x => x.Id).HasColumnName("id").HasColumnType("uuid").IsRequired();
        builder.Property(x => x.SurveyId).HasColumnName("survey_id").HasColumnType("uuid").IsRequired();
        builder.Property(x => x.Title).HasColumnName("title").HasMaxLength(500).IsRequired();
        builder.Property(x => x.Type).HasColumnName("type").HasMaxLength(24).IsRequired();
        builder.Property(x => x.IsRequired).HasColumnName("is_required").HasColumnType("boolean").IsRequired();
        builder.Property(x => x.Position).HasColumnName("position").HasColumnType("integer").IsRequired();

        builder.Property(x => x.Type).HasConversion(new ValueConverter<QuestionType, string>(
            value => ToDatabase(value), value => FromDatabase(value)));
        builder.Property(x => x.IsRequired).HasDefaultValue(true).HasSentinel(true);
        builder.HasIndex(x => new { x.SurveyId, x.Position }).IsUnique()
            .HasDatabaseName("ux_questions_survey_position");
        builder.HasOne(x => x.Survey).WithMany(x => x.Questions).HasForeignKey(x => x.SurveyId)
            .OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_questions_survey");
    }

    private static string ToDatabase(QuestionType value) => value switch
    {
        QuestionType.SingleChoice => "single_choice",
        QuestionType.MultipleChoice => "multiple_choice",
        QuestionType.Text => "text",
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown question type.")
    };

    private static QuestionType FromDatabase(string value) => value switch
    {
        "single_choice" => QuestionType.SingleChoice,
        "multiple_choice" => QuestionType.MultipleChoice,
        "text" => QuestionType.Text,
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown question type.")
    };
}
