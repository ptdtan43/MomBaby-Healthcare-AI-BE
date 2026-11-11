using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace MomOi.API.Migrations
{
    /// <inheritdoc />
    public partial class AddDoctorCallbackRequests : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "doctor_callback_requests",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    user_id = table.Column<string>(type: "text", nullable: false),
                    contact_phone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    concern = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    preferred_time = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    status = table.Column<int>(type: "integer", nullable: false),
                    handled_by_user_id = table.Column<string>(type: "text", nullable: true),
                    handled_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    doctor_note = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    fee = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    fee_order_code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    fee_paid_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_doctor_callback_requests", x => x.id);
                    table.ForeignKey(
                        name: "fk_doctor_callback_requests_users_user_id",
                        column: x => x.user_id,
                        principalTable: "AspNetUsers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_doctor_callback_requests_fee_order_code",
                table: "doctor_callback_requests",
                column: "fee_order_code");

            migrationBuilder.CreateIndex(
                name: "ix_doctor_callback_requests_status",
                table: "doctor_callback_requests",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_doctor_callback_requests_user_id_status",
                table: "doctor_callback_requests",
                columns: new[] { "user_id", "status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "doctor_callback_requests");
        }
    }
}
