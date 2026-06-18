using Microsoft.EntityFrameworkCore;
using webxemphim.Models;

namespace webxemphim.Infrastructure
{
    /// <summary>
    /// Tiện ích tạo / reset tài khoản Admin trong database.
    ///
    /// Cách dùng — gọi từ Program.cs khi start app:
    ///     await SeedAdmin.RunAsync(app.Services, resetIfExists: false);
    ///
    /// Hoặc truyền resetIfExists: true để XÓA admin cũ và tạo lại.
    /// </summary>
    public static class SeedAdmin
    {
        // ── Thông tin tài khoản Admin mặc định ────────────────────────────
        // Thay đổi trước khi deploy production!
        private const string DefaultEmail    = "admin@webxemphim.com";
        private const string DefaultUserName = "Administrator";
        private const string DefaultPassword = "admin1234";        // BCrypt work factor 12
        private const string DefaultRole     = "Admin";

        /// <summary>
        /// Tạo tài khoản Admin nếu chưa có.
        /// Nếu resetIfExists = true: xóa admin cũ (theo email) rồi tạo lại.
        /// </summary>
        public static async Task RunAsync(
            IServiceProvider services,
            bool resetIfExists = false)
        {
            using var scope   = services.CreateScope();
            var context       = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var loggerFactory = scope.ServiceProvider.GetRequiredService<ILoggerFactory>();
            var logger        = loggerFactory.CreateLogger("SeedAdmin");

            // ── Áp dụng migration còn thiếu (nếu có) ─────────────────────
            await context.Database.MigrateAsync();

            // ── Kiểm tra admin hiện tại ────────────────────────────────────
            var existing = await context.Users
                .Where(u => u.ROLE == DefaultRole)
                .ToListAsync();

            if (existing.Any())
            {
                if (!resetIfExists)
                {
                    logger.LogInformation(
                        "SeedAdmin: đã có {Count} tài khoản Admin — bỏ qua.",
                        existing.Count);
                    return;
                }

                // Reset: xóa toàn bộ admin cũ
                context.Users.RemoveRange(existing);
                await context.SaveChangesAsync();
                logger.LogWarning(
                    "SeedAdmin: đã xóa {Count} tài khoản Admin cũ.",
                    existing.Count);
            }

            // ── Tạo admin mới ─────────────────────────────────────────────
            // BCrypt work factor 12 — OWASP 2024 recommendation
            var hashedPassword = BCrypt.Net.BCrypt.HashPassword(DefaultPassword, workFactor: 12);

            var admin = new User
            {
                UserName  = DefaultUserName,
                EMAIL     = DefaultEmail,
                MK        = hashedPassword,
                ROLE      = DefaultRole,
                Balance   = 0,
                CreatedAt = DateTime.UtcNow
            };

            context.Users.Add(admin);
            await context.SaveChangesAsync();

            // KHÔNG log password ra console
            logger.LogInformation(
                "SeedAdmin: tài khoản Admin đã được tạo. Email={Email} | UserId={Id}",
                DefaultEmail, admin.UserId);

            Console.WriteLine("──────────────────────────────────────────");
            Console.WriteLine("  TÀI KHOẢN ADMIN ĐÃ ĐƯỢC TẠO");
            Console.WriteLine($"  Email   : {DefaultEmail}");
            Console.WriteLine($"  Password: {DefaultPassword}");
            Console.WriteLine("  Hãy đổi mật khẩu ngay sau khi đăng nhập!");
            Console.WriteLine("──────────────────────────────────────────");
        }
    }
}
