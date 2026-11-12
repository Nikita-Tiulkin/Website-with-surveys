using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PollingSystem.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddUserLogin : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Existing users need an explicitly agreed login backfill before this schema change.
            migrationBuilder.Sql("""
                DO $migration$
                BEGIN
                    IF EXISTS (SELECT 1 FROM users) THEN
                        RAISE EXCEPTION 'AddUserLogin requires an empty users table. Prepare a staged migration to populate unique logins for existing users before enforcing NOT NULL and UNIQUE.';
                    END IF;
                END;
                $migration$;
                """);

            migrationBuilder.AddColumn<string>(
                name: "login",
                table: "users",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false);

            migrationBuilder.CreateIndex(
                name: "ux_users_login",
                table: "users",
                column: "login",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ux_users_login",
                table: "users");

            migrationBuilder.DropColumn(
                name: "login",
                table: "users");
        }
    }
}
