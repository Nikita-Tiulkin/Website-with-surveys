using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PollingSystem.Data.Entities;

namespace PollingSystem.Data.Configurations;

public sealed class AnswerSelectedOptionConfiguration : IEntityTypeConfiguration<AnswerSelectedOption>
{
    public void Configure(EntityTypeBuilder<AnswerSelectedOption> builder)
    {
        builder.ToTable("answer_selected_options");
        builder.HasKey(x => new { x.AnswerId, x.OptionId }).HasName("pk_answer_selected_options");
        builder.Property(x => x.AnswerId).HasColumnName("answer_id").HasColumnType("uuid").IsRequired();
        builder.Property(x => x.OptionId).HasColumnName("option_id").HasColumnType("uuid").IsRequired();

        builder.HasOne(x => x.Answer).WithMany(x => x.SelectedOptions)
            .HasForeignKey(x => x.AnswerId)
            .OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_answer_selected_options_answer");
        builder.HasOne(x => x.Option).WithMany(x => x.SelectedInAnswers)
            .HasForeignKey(x => x.OptionId)
            .OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_answer_selected_options_option");
        builder.HasIndex(x => x.OptionId).HasDatabaseName("ix_answer_selected_options_option_id");
    }
}
