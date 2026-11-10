using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PollingSystem.Data.Migrations
{
    /// <inheritdoc />
    public partial class SimplifyForeignKeyIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_answer_selected_options_option",
                table: "answer_selected_options");

            migrationBuilder.CreateIndex(
                name: "IX_answer_selected_options_answer_id_question_id",
                table: "answer_selected_options",
                columns: new[] { "answer_id", "question_id" });

            migrationBuilder.CreateIndex(
                name: "IX_answer_selected_options_option_id_question_id",
                table: "answer_selected_options",
                columns: new[] { "option_id", "question_id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_answer_selected_options_answer_id_question_id",
                table: "answer_selected_options");

            migrationBuilder.DropIndex(
                name: "IX_answer_selected_options_option_id_question_id",
                table: "answer_selected_options");

            migrationBuilder.CreateIndex(
                name: "ix_answer_selected_options_option",
                table: "answer_selected_options",
                column: "option_id");
        }
    }
}
