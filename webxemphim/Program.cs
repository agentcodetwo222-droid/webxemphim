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
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
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
