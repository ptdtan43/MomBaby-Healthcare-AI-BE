using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using MomOi.API.Data;
using System;

#nullable disable

namespace MomOi.API.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(AppDbContext))]
    [Migration("20261021150000_AddBabyPhoto")]
    public partial class AddBabyPhoto : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "photo_content_type",
                table: "baby_profiles",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "photo_storage_key",
                table: "baby_profiles",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "photo_updated_at",
                table: "baby_profiles",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "photo_content_type", table: "baby_profiles");
            migrationBuilder.DropColumn(name: "photo_storage_key", table: "baby_profiles");
            migrationBuilder.DropColumn(name: "photo_updated_at", table: "baby_profiles");
        }
    }
}