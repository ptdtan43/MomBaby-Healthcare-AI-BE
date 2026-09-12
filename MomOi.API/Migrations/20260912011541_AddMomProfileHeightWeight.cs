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
    /// </summary>
    public partial class AddMomProfileHeightWeight : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<float>(
                name: "height",
                table: "mom_health_profiles",
                type: "real",
                nullable: true);

            migrationBuilder.AddColumn<float>(
                name: "weight",
                table: "mom_health_profiles",
                type: "real",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "height", table: "mom_health_profiles");
            migrationBuilder.DropColumn(name: "weight", table: "mom_health_profiles");
        }
    }
}
