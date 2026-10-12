using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MomOi.API.Migrations
{
    /// <inheritdoc />
    public partial class SoftDeleteAndPaymentConcurrency : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "deleted_at",
                table: "vaccination_records");

            migrationBuilder.DropColumn(
                name: "is_deleted",
                table: "vaccination_records");

            migrationBuilder.DropColumn(
                name: "deleted_at",
                table: "usda_food_items");

            migrationBuilder.DropColumn(
                name: "is_deleted",
                table: "usda_food_items");

            migrationBuilder.DropColumn(
                name: "deleted_at",
                table: "symptom_logs");

            migrationBuilder.DropColumn(
                name: "is_deleted",
                table: "symptom_logs");

            migrationBuilder.DropColumn(
                name: "deleted_at",
                table: "recipes");

            migrationBuilder.DropColumn(
                name: "is_deleted",
                table: "recipes");

            migrationBuilder.DropColumn(
                name: "deleted_at",
                table: "pregnancy_logs");

            migrationBuilder.DropColumn(
                name: "is_deleted",
                table: "pregnancy_logs");

            migrationBuilder.DropColumn(
                name: "deleted_at",
                table: "postpartum_logs");

            migrationBuilder.DropColumn(
                name: "is_deleted",
                table: "postpartum_logs");

            migrationBuilder.DropColumn(
                name: "deleted_at",
                table: "payment_transactions");

            migrationBuilder.DropColumn(
                name: "is_deleted",
                table: "payment_transactions");

            migrationBuilder.DropColumn(
                name: "deleted_at",
                table: "notification_alerts");

            migrationBuilder.DropColumn(
                name: "is_deleted",
                table: "notification_alerts");

            migrationBuilder.DropColumn(
                name: "deleted_at",
                table: "mom_health_profiles");

            migrationBuilder.DropColumn(
                name: "is_deleted",
                table: "mom_health_profiles");

            migrationBuilder.DropColumn(
                name: "deleted_at",
                table: "meal_logs");

            migrationBuilder.DropColumn(
                name: "is_deleted",
                table: "meal_logs");

            migrationBuilder.DropColumn(
                name: "deleted_at",
                table: "lifestyle_entries");

            migrationBuilder.DropColumn(
                name: "is_deleted",
                table: "lifestyle_entries");

            migrationBuilder.DropColumn(
                name: "deleted_at",
                table: "lifestyle_alerts");

            migrationBuilder.DropColumn(
                name: "is_deleted",
                table: "lifestyle_alerts");

            migrationBuilder.DropColumn(
                name: "deleted_at",
                table: "exercise_logs");

            migrationBuilder.DropColumn(
                name: "is_deleted",
                table: "exercise_logs");

            migrationBuilder.DropColumn(
                name: "deleted_at",
                table: "epds_assessments");

            migrationBuilder.DropColumn(
                name: "is_deleted",
                table: "epds_assessments");

            migrationBuilder.DropColumn(
                name: "deleted_at",
                table: "diet_plans");

            migrationBuilder.DropColumn(
                name: "is_deleted",
                table: "diet_plans");

            migrationBuilder.DropColumn(
                name: "deleted_at",
                table: "daily_monitoring_logs");

            migrationBuilder.DropColumn(
                name: "is_deleted",
                table: "daily_monitoring_logs");

            migrationBuilder.DropColumn(
                name: "deleted_at",
                table: "cycle_logs");

            migrationBuilder.DropColumn(
                name: "is_deleted",
                table: "cycle_logs");

            migrationBuilder.DropColumn(
                name: "deleted_at",
                table: "critical_alert_logs");

            migrationBuilder.DropColumn(
                name: "is_deleted",
                table: "critical_alert_logs");

            migrationBuilder.DropColumn(
                name: "deleted_at",
                table: "chat_sessions");

            migrationBuilder.DropColumn(
                name: "is_deleted",
                table: "chat_sessions");

            migrationBuilder.DropColumn(
                name: "deleted_at",
                table: "chat_messages");

            migrationBuilder.DropColumn(
                name: "is_deleted",
                table: "chat_messages");

            migrationBuilder.DropColumn(
                name: "deleted_at",
                table: "business_rules");

            migrationBuilder.DropColumn(
                name: "is_deleted",
                table: "business_rules");

            migrationBuilder.DropColumn(
                name: "deleted_at",
                table: "baby_profiles");

            migrationBuilder.DropColumn(
                name: "is_deleted",
                table: "baby_profiles");

            migrationBuilder.DropColumn(
                name: "deleted_at",
                table: "baby_food_logs");

            migrationBuilder.DropColumn(
                name: "is_deleted",
                table: "baby_food_logs");

            // CỐ Ý KHÔNG tạo cột "xmin" trên payment_transactions.
            // EF sinh ra lệnh AddColumn vì nó thấy model khai báo một concurrency token
            // tên xmin, nhưng xmin là CỘT HỆ THỐNG mà PostgreSQL tự duy trì sẵn cho mọi
            // bảng — chạy AddColumn sẽ lỗi "column xmin already exists".
            // Model vẫn dùng được cột này bình thường; chỉ riêng migration phải bỏ qua.
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Không DropColumn "xmin": cột hệ thống của PostgreSQL, không thuộc quyền
            // quản lý của migration (xem ghi chú ở Up).

            migrationBuilder.AddColumn<DateTime>(
                name: "deleted_at",
                table: "vaccination_records",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_deleted",
                table: "vaccination_records",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "deleted_at",
                table: "usda_food_items",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_deleted",
                table: "usda_food_items",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "deleted_at",
                table: "symptom_logs",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_deleted",
                table: "symptom_logs",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "deleted_at",
                table: "recipes",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_deleted",
                table: "recipes",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "deleted_at",
                table: "pregnancy_logs",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_deleted",
                table: "pregnancy_logs",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "deleted_at",
                table: "postpartum_logs",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_deleted",
                table: "postpartum_logs",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "deleted_at",
                table: "payment_transactions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_deleted",
                table: "payment_transactions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "deleted_at",
                table: "notification_alerts",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_deleted",
                table: "notification_alerts",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "deleted_at",
                table: "mom_health_profiles",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_deleted",
                table: "mom_health_profiles",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "deleted_at",
                table: "meal_logs",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_deleted",
                table: "meal_logs",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "deleted_at",
                table: "lifestyle_entries",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_deleted",
                table: "lifestyle_entries",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "deleted_at",
                table: "lifestyle_alerts",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_deleted",
                table: "lifestyle_alerts",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "deleted_at",
                table: "exercise_logs",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_deleted",
                table: "exercise_logs",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "deleted_at",
                table: "epds_assessments",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_deleted",
                table: "epds_assessments",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "deleted_at",
                table: "diet_plans",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_deleted",
                table: "diet_plans",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "deleted_at",
                table: "daily_monitoring_logs",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_deleted",
                table: "daily_monitoring_logs",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "deleted_at",
                table: "cycle_logs",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_deleted",
                table: "cycle_logs",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "deleted_at",
                table: "critical_alert_logs",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_deleted",
                table: "critical_alert_logs",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "deleted_at",
                table: "chat_sessions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_deleted",
                table: "chat_sessions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "deleted_at",
                table: "chat_messages",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_deleted",
                table: "chat_messages",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "deleted_at",
                table: "business_rules",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_deleted",
                table: "business_rules",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "deleted_at",
                table: "baby_profiles",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_deleted",
                table: "baby_profiles",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "deleted_at",
                table: "baby_food_logs",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_deleted",
                table: "baby_food_logs",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }
    }
}
