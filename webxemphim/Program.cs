using Microsoft.EntityFrameworkCore;
using webxemphim.Infrastructure;
using webxemphim.Models;
using webxemphim.Services;

var builder = WebApplication.CreateBuilder(args);

// ── MVC + Razor Pages ──────────────────────────────────────────────────────
builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add(new Microsoft.AspNetCore.Mvc.AutoValidateAntiforgeryTokenAttribute());
});
builder.Services.AddRazorPages();

// ── SECURITY: Tất cả cấu hình bảo mật tập trung tại SecurityConfig ────────
builder.Services.AddSecurityServices(builder.Configuration, builder.Environment);

// ── SECURITY: Encryption Service (AES-256-GCM) ────────────────────────────
builder.Services.AddSingleton<EncryptionService>();

// ── Database ───────────────────────────────────────────────────────────────
// Railway inject DATABASE_URL dạng: postgresql://user:pass@host:port/dbname
// Ưu tiên: DATABASE_URL (Railway) → DefaultConnection (local user-secrets)
var connectionString = GetConnectionString(builder.Configuration);
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(
        connectionString,
        npgsqlOptions => npgsqlOptions.CommandTimeout(30)
    ));

var app = builder.Build();

// ── Seed Admin ─────────────────────────────────────────────────────────────
// resetIfExists: false → chỉ tạo nếu chưa có
// resetIfExists: true  → xóa admin cũ và tạo lại (dùng khi reset)
await SeedAdmin.RunAsync(app.Services, resetIfExists: false);

// ── Middleware pipeline ────────────────────────────────────────────────────

// 1. Transport Security: HTTPS redirect + HSTS (SSTP-style)
app.UseTransportSecurity();

// 2. HTTP Response Security Headers (CSP, X-Frame-Options, v.v.)
app.UseSecurityHeaders();

// 3. Rate limiting
app.UseRateLimiter();

// 4. Static files, routing, session, authorization
app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages();

app.Run();

// ── Helper: đọc connection string từ Railway DATABASE_URL hoặc user-secrets ──
static string GetConnectionString(IConfiguration config)
{
    // Railway có thể inject database URL qua nhiều tên biến khác nhau
    // Thử lần lượt theo thứ tự ưu tiên
    var databaseUrl =
        Environment.GetEnvironmentVariable("DATABASE_URL")          ??
        Environment.GetEnvironmentVariable("POSTGRES_URL")          ??
        Environment.GetEnvironmentVariable("POSTGRESQL_URL")        ??
        Environment.GetEnvironmentVariable("RAILWAY_DATABASE_URL")  ??
        Environment.GetEnvironmentVariable("DATABASE_PRIVATE_URL");

    if (!string.IsNullOrEmpty(databaseUrl))
    {
        try
        {
            // Chuyển postgresql:// URI → Npgsql connection string
            var uri  = new Uri(databaseUrl);
            var host = uri.Host;
            var port = uri.Port > 0 ? uri.Port : 5432;
            var db   = uri.AbsolutePath.TrimStart('/');
            var userInfo = uri.UserInfo.Split(':', 2);
            var user = Uri.UnescapeDataString(userInfo[0]);
            var pass = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : "";

            return $"Host={host};Port={port};Database={db};Username={user};Password={pass};" +
                   "SSL Mode=Require;Trust Server Certificate=true";
        }
        catch
        {
            // Nếu URI parse thất bại, thử dùng thẳng làm connection string
            return databaseUrl;
        }
    }

    // Local development: dùng user-secrets
    var local = config.GetConnectionString("DefaultConnection");
    if (!string.IsNullOrEmpty(local))
        return local;

    throw new InvalidOperationException(
        "Không tìm thấy connection string. " +
        "Railway: cần add PostgreSQL service và link vào app. " +
        "Local: chạy 'dotnet user-secrets set ConnectionStrings:DefaultConnection <value>'");
}
