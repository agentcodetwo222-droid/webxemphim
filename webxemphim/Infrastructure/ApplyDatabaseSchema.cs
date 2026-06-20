using Npgsql;
using webxemphim.Models;

namespace webxemphim.Infrastructure
{
    /// <summary>
    /// Apply database_redesign.sql khi env APPLY_SCHEMA=true (Railway one-time deploy).
    /// </summary>
    public static class ApplyDatabaseSchema
    {
        public static async Task RunAsync(IServiceProvider services)
        {
            if (Environment.GetEnvironmentVariable("APPLY_SCHEMA") != "true")
                return;

            using var scope = services.CreateScope();
            var logger = scope.ServiceProvider
                .GetRequiredService<ILoggerFactory>()
                .CreateLogger("ApplyDatabaseSchema");

            var connStr = ResolveConnectionString(scope.ServiceProvider);
            var sqlPath = FindSqlFile();

            if (sqlPath == null)
            {
                logger.LogError("Khong tim thay database_redesign.sql");
                return;
            }

            var sql = await File.ReadAllTextAsync(sqlPath);
            logger.LogWarning("APPLY_SCHEMA=true — dang xoa va tao lai toan bo schema tu {Path}", sqlPath);

            await using var conn = new NpgsqlConnection(connStr);
            await conn.OpenAsync();
            await using var cmd = new NpgsqlCommand(sql, conn) { CommandTimeout = 300 };
            await cmd.ExecuteNonQueryAsync();

            logger.LogWarning("Apply schema hoan tat.");
        }

        private static string ResolveConnectionString(IServiceProvider sp)
        {
            var databaseUrl =
                Environment.GetEnvironmentVariable("DATABASE_URL") ??
                Environment.GetEnvironmentVariable("POSTGRES_URL") ??
                Environment.GetEnvironmentVariable("RAILWAY_DATABASE_URL");

            if (!string.IsNullOrEmpty(databaseUrl))
            {
                if (databaseUrl.StartsWith("postgres", StringComparison.OrdinalIgnoreCase))
                {
                    var uri = new Uri(databaseUrl);
                    var host = uri.Host;
                    var port = uri.Port > 0 ? uri.Port : 5432;
                    var db = uri.AbsolutePath.TrimStart('/');
                    var userInfo = uri.UserInfo.Split(':', 2);
                    var user = Uri.UnescapeDataString(userInfo[0]);
                    var pass = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : "";
                    return $"Host={host};Port={port};Database={db};Username={user};Password={pass};SSL Mode=Prefer;Trust Server Certificate=true";
                }
                return databaseUrl;
            }

            var config = sp.GetRequiredService<IConfiguration>();
            return config.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("Khong tim thay connection string.");
        }

        private static string? FindSqlFile()
        {
            var candidates = new[]
            {
                Path.Combine(AppContext.BaseDirectory, "database_redesign.sql"),
                Path.Combine(AppContext.BaseDirectory, "..", "database_redesign.sql"),
                Path.Combine(Directory.GetCurrentDirectory(), "database_redesign.sql"),
                Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "database_redesign.sql"))
            };

            foreach (var path in candidates)
            {
                var full = Path.GetFullPath(path);
                if (File.Exists(full)) return full;
            }
            return null;
        }
    }
}
