using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MomOi.API.Migrations
{
    /// <inheritdoc />
    public partial class KeepPaymentsOnAccountDeletion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_payment_transactions_users_user_id",
                table: "payment_transactions");

            migrationBuilder.AlterColumn<string>(
                name: "user_id",
                table: "payment_transactions",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddForeignKey(
                name: "fk_payment_transactions_users_user_id",
                table: "payment_transactions",
                column: "user_id",
                principalTable: "AspNetUsers",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_payment_transactions_users_user_id",
                table: "payment_transactions");

            migrationBuilder.AlterColumn<string>(
                name: "user_id",
                table: "payment_transactions",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "fk_payment_transactions_users_user_id",
                table: "payment_transactions",
                column: "user_id",
                principalTable: "AspNetUsers",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
