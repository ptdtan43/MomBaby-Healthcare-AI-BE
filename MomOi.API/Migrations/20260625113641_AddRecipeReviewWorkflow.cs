using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MomOi.API.Migrations
{
    /// <inheritdoc />
    public partial class AddRecipeReviewWorkflow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "expert_note",
                table: "recipes",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "reviewed_at",
                table: "recipes",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "reviewed_by_expert_id",
                table: "recipes",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "status",
                table: "recipes",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "expert_note",
                table: "recipes");

            migrationBuilder.DropColumn(
                name: "reviewed_at",
                table: "recipes");

            migrationBuilder.DropColumn(
                name: "reviewed_by_expert_id",
                table: "recipes");

            migrationBuilder.DropColumn(
                name: "status",
                table: "recipes");
        }
    }
}
