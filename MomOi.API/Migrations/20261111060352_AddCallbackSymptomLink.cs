using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MomOi.API.Migrations
{
    /// <inheritdoc />
    public partial class AddCallbackSymptomLink : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "symptom_log_id",
                table: "doctor_callback_requests",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_doctor_callback_requests_symptom_log_id",
                table: "doctor_callback_requests",
                column: "symptom_log_id");

            migrationBuilder.AddForeignKey(
                name: "fk_doctor_callback_requests_symptom_logs_symptom_log_id",
                table: "doctor_callback_requests",
                column: "symptom_log_id",
                principalTable: "symptom_logs",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_doctor_callback_requests_symptom_logs_symptom_log_id",
                table: "doctor_callback_requests");

            migrationBuilder.DropIndex(
                name: "ix_doctor_callback_requests_symptom_log_id",
                table: "doctor_callback_requests");

            migrationBuilder.DropColumn(
                name: "symptom_log_id",
                table: "doctor_callback_requests");
        }
    }
}
