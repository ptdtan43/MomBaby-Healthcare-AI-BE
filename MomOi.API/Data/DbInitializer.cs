using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MomOi.API.Models.Identity;
using System;
using System.Threading.Tasks;

namespace MomOi.API.Data
{
    /// <summary>
    /// Khởi tạo dữ liệu nền khi ứng dụng khởi động.
    ///
    /// Nguyên tắc: tách rõ hai loại dữ liệu nền có mức rủi ro khác hẳn nhau.
    ///
    ///  • ROLE — chỉ là 4 dòng chữ, không có bí mật, thiếu thì phân quyền hỏng.
    ///           => LUÔN seed ở mọi môi trường.
    ///
    ///  • TÀI KHOẢN — có mật khẩu, tức là CHÌA KHOÁ VÀO HỆ THỐNG.
    ///           => CHỈ seed khi mật khẩu được cấp qua cấu hình, hoặc khi đang ở
    ///              môi trường Development. Ở Production mà không cấu hình thì BỎ QUA
    ///              và ghi cảnh báo, tuyệt đối không dùng mật khẩu mặc định.
    ///
    /// Vì sao phải làm vậy: mật khẩu mặc định viết cứng trong mã nguồn sẽ nằm trong
    /// repository, mà repository thì thường công khai hoặc chia sẻ rộng. Bất kỳ ai đọc
    /// được mã nguồn đều đăng nhập được với quyền Admin vào hệ thống thật.
    /// </summary>
    public static class DbInitializer
    {
        private static readonly string[] SystemRoles =
        {
            AppRoles.Admin, AppRoles.Staff, AppRoles.Expert, AppRoles.Mom
        };

        /// <summary>Mô tả một tài khoản nền: email, tên hiển thị, vai trò và khoá cấu hình chứa mật khẩu.</summary>
        private sealed record SeedAccount(string Email, string FullName, string Role, string PasswordConfigKey);

        private static readonly SeedAccount[] Accounts =
        {
            new("admin@momoi.com",  "System Admin",   AppRoles.Admin,  "Seed:AdminPassword"),
            new("staff@momoi.com",  "Care Staff",     AppRoles.Staff,  "Seed:StaffPassword"),
            new("expert@momoi.com", "Medical Expert", AppRoles.Expert, "Seed:ExpertPassword"),
        };

        /// <summary>Mật khẩu chỉ dùng khi chạy ở máy lập trình viên. Không bao giờ ra tới Production.</summary>
        private const string DevelopmentOnlyPassword = "Dev@12345";

        public static async Task InitializeAsync(IServiceProvider serviceProvider)
        {
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = serviceProvider.GetRequiredService<UserManager<AppUser>>();
            var environment = serviceProvider.GetRequiredService<IHostEnvironment>();
            var configuration = serviceProvider.GetRequiredService<IConfiguration>();
            var logger = serviceProvider.GetRequiredService<ILoggerFactory>()
                                        .CreateLogger(typeof(DbInitializer).FullName!);

            // ── 1. ROLE — luôn seed ────────────────────────────────────────────────
            foreach (var role in SystemRoles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                    logger.LogInformation("Đã tạo role {Role}.", role);
                }
            }

            // ── 2. TÀI KHOẢN — có điều kiện ────────────────────────────────────────
            foreach (var account in Accounts)
            {
                await SeedAccountAsync(userManager, configuration, environment, logger, account);
            }
        }

        private static async Task SeedAccountAsync(
            UserManager<AppUser> userManager,
            IConfiguration configuration,
            IHostEnvironment environment,
            ILogger logger,
            SeedAccount account)
        {
            var existing = await userManager.FindByEmailAsync(account.Email);

            // Tài khoản đã tồn tại: chỉ đảm bảo đúng vai trò, KHÔNG đụng vào mật khẩu.
            // Nếu quản trị viên đã đổi mật khẩu thì không được ghi đè lại.
            if (existing != null)
            {
                if (!await userManager.IsInRoleAsync(existing, account.Role))
                {
                    await userManager.AddToRoleAsync(existing, account.Role);
                    logger.LogInformation("Đã gán lại role {Role} cho {Email}.", account.Role, account.Email);
                }
                return;
            }

            var password = configuration[account.PasswordConfigKey];

            if (string.IsNullOrWhiteSpace(password))
            {
                if (!environment.IsDevelopment())
                {
                    // Đây là nhánh quan trọng nhất của cả lớp này.
                    logger.LogWarning(
                        "BỎ QUA việc tạo tài khoản {Email}: chưa cấu hình '{ConfigKey}'. " +
                        "Ở môi trường {Environment}, hệ thống KHÔNG dùng mật khẩu mặc định. " +
                        "Muốn tạo tài khoản này, hãy đặt biến môi trường '{EnvVarName}' rồi khởi động lại.",
                        account.Email,
                        account.PasswordConfigKey,
                        environment.EnvironmentName,
                        account.PasswordConfigKey.Replace(':', '_').Replace("_", "__"));
                    return;
                }

                password = DevelopmentOnlyPassword;
                logger.LogInformation(
                    "Tạo {Email} bằng mật khẩu mặc định của môi trường Development.", account.Email);
            }

            var user = new AppUser
            {
                UserName = account.Email,
                Email = account.Email,
                FullName = account.FullName,
                Tier = SubscriptionTier.SuperMomVip,
                EmailConfirmed = true
            };

            var result = await userManager.CreateAsync(user, password);
            if (!result.Succeeded)
            {
                logger.LogError("Không tạo được tài khoản {Email}: {Errors}",
                    account.Email, string.Join("; ", result.Errors.Select(e => e.Description)));
                return;
            }

            await userManager.AddToRoleAsync(user, account.Role);
            logger.LogInformation("Đã tạo tài khoản {Email} với role {Role}.", account.Email, account.Role);
        }
    }
}
