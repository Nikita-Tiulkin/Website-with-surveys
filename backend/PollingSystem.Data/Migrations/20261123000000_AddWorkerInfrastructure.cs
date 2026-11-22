using System;
using Microsoft.EntityFrameworkCore.Migrations;
using PollingSystem.Data;

#nullable disable

namespace PollingSystem.Data.Migrations;

public partial class AddWorkerInfrastructure : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "processed_events",
            columns: table => new
            {
                event_id = table.Column<Guid>(type: "uuid", nullable: false),
                processed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_processed_events", x => x.event_id);
            });

        migrationBuilder.CreateTable(
            name: "worker_leases",
            columns: table => new
            {
                name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                owner_id = table.Column<Guid>(type: "uuid", nullable: false),
                expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_worker_leases", x => x.name);
            });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "processed_events");
        migrationBuilder.DropTable(name: "worker_leases");
    }
}
