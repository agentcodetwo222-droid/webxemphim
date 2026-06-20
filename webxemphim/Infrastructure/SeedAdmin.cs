using Microsoft.EntityFrameworkCore;
using webxemphim.Models;
using webxemphim.Services;

namespace webxemphim.Infrastructure
{
    public static class SeedAdmin
    {
        private const string DefaultEmail    = "admin@webxemphim.com";
        private const string DefaultUserName = "Administrator";
        private const string DefaultRole     = "Admin";

        private static string GetDefaultPassword()
        {
            var envPass = Environment.GetEnvironmentVariable("ADMIN_DEFAULT_PASSWORD");
            if (!string.IsNullOrEmpty(envPass)) return envPass;

            const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghjkmnpqrstuvwxyz23456789!@#$";
            var rng  = System.Security.Cryptography.RandomNumberGenerator.Create();
            var data = new byte[16];
            rng.GetBytes(data);
            return new string(data.Select(b => chars[b % chars.Length]).ToArray());
        }

        public static async Task RunAsync(IServiceProvider services, bool resetIfExists = false)
        {
            using var scope   = services.CreateScope();
            var schema        = scope.ServiceProvider.GetRequiredService<SchemaDataService>();
            var context       = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var loggerFactory = scope.ServiceProvider.GetRequiredService<ILoggerFactory>();
            var logger        = loggerFactory.CreateLogger("SeedAdmin");

            if (!await context.Database.CanConnectAsync())
            {
                logger.LogWarning("SeedAdmin: khong ket noi duoc database — bo qua.");
                return;
            }

            var allUsers = await schema.GetAllUsersAsync();
            var existing = allUsers.Where(u => u.ROLE == DefaultRole).ToList();

            if (existing.Any())
            {
                if (!resetIfExists)
                {
                    logger.LogInformation("SeedAdmin: da co {Count} tai khoan Admin — bo qua.", existing.Count);
                    return;
                }

                foreach (var u in existing)
                    await schema.DeleteUserAsync(u.UserId);

                logger.LogWarning("SeedAdmin: da xoa {Count} tai khoan Admin cu.", existing.Count);
            }

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

            await schema.AddUserAsync(admin);

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
