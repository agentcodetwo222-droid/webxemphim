using Microsoft.Extensions.Configuration;
using Npgsql;

static string ResolveConnectionString(string[] args)
{
    if (args.Length > 0 && !string.IsNullOrWhiteSpace(args[0]))
        return args[0];

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
            return $"Host={host};Port={port};Database={db};Username={user};Password={pass};SSL Mode=Require;Trust Server Certificate=true";
        }
        return databaseUrl;
    }

    var root = Directory.GetCurrentDirectory();
    while (!Directory.Exists(Path.Combine(root, "webxemphim")) && Directory.GetParent(root) != null)
        root = Directory.GetParent(root)!.FullName;

    var config = new ConfigurationBuilder()
        .SetBasePath(Path.Combine(root, "webxemphim"))
        .AddUserSecrets("48f05dc3-41cb-4724-83cc-941e58ee99fc")
        .Build();

    var local = config.GetConnectionString("DefaultConnection");
    if (!string.IsNullOrEmpty(local))
        return local;

    throw new InvalidOperationException(
        "Thieu connection string. Truyen tham so, set DATABASE_URL, hoac cau hinh user-secrets.");
}

var root = Directory.GetCurrentDirectory();
while (!File.Exists(Path.Combine(root, "database_redesign.sql")) && Directory.GetParent(root) != null)
    root = Directory.GetParent(root)!.FullName;

var sqlPath = Path.Combine(root, "database_redesign.sql");
if (!File.Exists(sqlPath))
    throw new FileNotFoundException("Khong tim thay database_redesign.sql", sqlPath);

var connStr = ResolveConnectionString(args);
var sql = await File.ReadAllTextAsync(sqlPath);

Console.WriteLine($"Applying schema from: {sqlPath}");
Console.WriteLine($"Target: {connStr.Split(';').FirstOrDefault(s => s.StartsWith("Host=")) ?? "database"}");

await using var conn = new NpgsqlConnection(connStr);
await conn.OpenAsync();
await using var cmd = new NpgsqlCommand(sql, conn) { CommandTimeout = 120 };
await cmd.ExecuteNonQueryAsync();

Console.WriteLine("Schema applied successfully.");
