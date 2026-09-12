using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MomOi.API.Migrations
{
    /// <inheritdoc />
    public partial class AddPaymentTransactionFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "duration_months",
                table: "payment_transactions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "order_code",
                table: "payment_transactions",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "plan_code",
                table: "payment_transactions",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "provider_txn_no",
                table: "payment_transactions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "raw_callback",
                table: "payment_transactions",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_payment_transactions_order_code",
                table: "payment_transactions",
                column: "order_code",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_payment_transactions_order_code",
                table: "payment_transactions");

            migrationBuilder.DropColumn(
                name: "duration_months",
                table: "payment_transactions");

            migrationBuilder.DropColumn(
                name: "order_code",
                table: "payment_transactions");

            migrationBuilder.DropColumn(
                name: "plan_code",
                table: "payment_transactions");

            migrationBuilder.DropColumn(
                name: "provider_txn_no",
                table: "payment_transactions");

            migrationBuilder.DropColumn(
                name: "raw_callback",
                table: "payment_transactions");
        }
    }
}
