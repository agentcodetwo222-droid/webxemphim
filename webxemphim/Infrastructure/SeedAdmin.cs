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
        // Password lấy từ env var ADMIN_DEFAULT_PASSWORD, fallback về giá trị ngẫu nhiên
        // KHÔNG hardcode password vào source code
        private const string DefaultEmail    = "admin@webxemphim.com";
        private const string DefaultUserName = "Administrator";
        private const string DefaultRole     = "Admin";

        private static string GetDefaultPassword()
        {
            // Ưu tiên: biến môi trường ADMIN_DEFAULT_PASSWORD (set trên Railway)
            var envPass = Environment.GetEnvironmentVariable("ADMIN_DEFAULT_PASSWORD");
            if (!string.IsNullOrEmpty(envPass)) return envPass;

            // Fallback: sinh password ngẫu nhiên 16 ký tự an toàn
            const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghjkmnpqrstuvwxyz23456789!@#$";
            var rng  = System.Security.Cryptography.RandomNumberGenerator.Create();
            var data = new byte[16];
            rng.GetBytes(data);
            return new string(data.Select(b => chars[b % chars.Length]).ToArray());
        }

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
            var defaultPassword = GetDefaultPassword();
            var hashedPassword  = BCrypt.Net.BCrypt.HashPassword(defaultPassword, workFactor: 12);

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

            logger.LogInformation(
                "SeedAdmin: tai khoan Admin da duoc tao. Email={Email} | UserId={Id}",
                DefaultEmail, admin.UserId);

            Console.WriteLine("------------------------------------------");
            Console.WriteLine("  TAI KHOAN ADMIN DA DUOC TAO");
            Console.WriteLine($"  Email   : {DefaultEmail}");
            Console.WriteLine($"  Password: {defaultPassword}");
            Console.WriteLine("  Hay doi mat khau ngay sau khi dang nhap!");
            Console.WriteLine("  (Set env ADMIN_DEFAULT_PASSWORD de tuy chinh)");
            Console.WriteLine("------------------------------------------");
        }
    }
}
