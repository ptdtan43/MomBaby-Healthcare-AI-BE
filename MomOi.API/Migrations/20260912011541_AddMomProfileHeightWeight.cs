using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MomOi.API.Migrations
{
    /// <inheritdoc />
    /// <summary>
    /// MomHealthProfile.Height và Weight đã có trong model và trong model snapshot, nhưng
    /// không migration nào từng tạo hai cột này, nên bảng thật thiếu chúng và mọi lượt
    /// đăng ký tài khoản đều đổ lỗi 42703. Thân migration phải viết tay vì EF so model với
    /// snapshot — hai bên vốn đã khớp nên bộ sinh tự động cho ra migration rỗng.
    /// Dùng IF NOT EXISTS vì migration DatabaseImprovements đã được sửa để tạo sẵn 2 cột
    /// này ngay từ đầu — trên database mới hoàn toàn (chưa từng migrate) cột đã tồn tại
    /// trước khi tới lượt migration này chạy, AddColumn thường sẽ báo "column already exists".
    /// </summary>
    public partial class AddMomProfileHeightWeight : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "ALTER TABLE mom_health_profiles ADD COLUMN IF NOT EXISTS height real;");

            migrationBuilder.Sql(
                "ALTER TABLE mom_health_profiles ADD COLUMN IF NOT EXISTS weight real;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "ALTER TABLE mom_health_profiles DROP COLUMN IF EXISTS height;");

            migrationBuilder.Sql(
                "ALTER TABLE mom_health_profiles DROP COLUMN IF EXISTS weight;");
        }
    }
}
