using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MomOi.API.Migrations
{
    /// <inheritdoc />
    public partial class AddRecipeCategory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "category",
                table: "recipes",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "category",
                table: "recipes");
        }
    }
}
