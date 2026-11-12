using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using PollingSystem.Data.Entities;
using PollingSystem.Data.Enums;

namespace PollingSystem.Data.Configurations;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users", table =>
            table.HasCheckConstraint("ck_users_role", "role IN ('user', 'admin')"));

        builder.HasKey(x => x.Id).HasName("pk_users");
        builder.Property(x => x.Id).HasColumnName("id").HasColumnType("uuid").IsRequired();
        builder.Property(x => x.Login).HasColumnName("login").HasMaxLength(50).IsRequired();
        builder.Property(x => x.Email).HasColumnName("email").HasMaxLength(256).IsRequired();
        builder.Property(x => x.NormalizedEmail).HasColumnName("normalized_email").HasMaxLength(256).IsRequired();
        builder.Property(x => x.PasswordHash).HasColumnName("password_hash").HasColumnType("text").IsRequired();
        builder.Property(x => x.Role).HasColumnName("role")
            .HasConversion(new ValueConverter<UserRole, string>(
                value => ToDatabase(value),
                value => FromDatabase(value)))
            .HasMaxLength(16).IsRequired().HasDefaultValue(UserRole.User).HasSentinel(UserRole.User);
        builder.Property(x => x.CreatedAt).HasColumnName("created_at")
            .HasColumnType("timestamp with time zone").IsRequired().HasDefaultValueSql("CURRENT_TIMESTAMP");
        builder.HasIndex(x => x.NormalizedEmail).IsUnique().HasDatabaseName("ux_users_normalized_email");
        builder.HasIndex(x => x.Login).IsUnique().HasDatabaseName("ux_users_login");

        builder.HasMany(x => x.Surveys).WithOne(x => x.Author).HasForeignKey(x => x.AuthorId)
            .OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_surveys_author");
        builder.HasMany(x => x.SurveyResponses).WithOne(x => x.User).HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_survey_responses_user");
    }

    public static string ToDatabase(UserRole role)
    {
        return role switch
        {
            UserRole.User => "user",
            UserRole.Admin => "admin",
            _ => throw new ArgumentOutOfRangeException(
                nameof(role), role, "Unknown user role")
        };
    }

    public static UserRole FromDatabase(string value)
    {
        return value switch
        {
            "user" => UserRole.User,
            "admin" => UserRole.Admin,
            _ => throw new InvalidOperationException(
                $"Unknown user role: {value}")
        };
    }
}
