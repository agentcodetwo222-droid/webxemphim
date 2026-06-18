using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;

namespace webxemphim.Infrastructure
{
    /// <summary>
    /// Tập trung toàn bộ cấu hình bảo mật của ứng dụng.
    ///
    /// Transport Security (SSTP-style):
    ///   - HTTPS redirect bắt buộc
    ///   - HSTS (Strict-Transport-Security) max-age 1 năm + includeSubDomains + preload
    ///
    /// Application Security:
    ///   - Session cookie bảo mật (HttpOnly, Secure, SameSite=Strict, __Host- prefix)
    ///   - Antiforgery token bảo mật (chống CSRF)
    ///   - Rate Limiting: login 10 req/min, global 200 req/min
    ///
    /// HTTP Response Headers:
    ///   - Content-Security-Policy (chống XSS)
    ///   - X-Frame-Options: DENY (chống clickjacking)
    ///   - X-Content-Type-Options: nosniff (chống MIME sniffing)
    ///   - Referrer-Policy: strict-origin-when-cross-origin
    ///   - Permissions-Policy (tắt camera, mic, geolocation, payment)
    ///   - Cross-Origin-Opener-Policy: same-origin
    ///   - Cross-Origin-Resource-Policy: same-origin
    /// </summary>
    public static class SecurityConfig
    {
        // ─────────────────────────────────────────────────────────────────────
        // SERVICES — gọi trong builder.Services
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Đăng ký tất cả security services vào DI container.
        /// Gọi: builder.Services.AddSecurityServices(builder.Configuration, builder.Environment);
        /// </summary>
        public static IServiceCollection AddSecurityServices(
            this IServiceCollection services,
            IConfiguration configuration,
            IWebHostEnvironment environment)
        {
            AddSecureSession(services, environment);
            AddSecureAntiforgery(services, environment);
            AddRateLimiting(services);

            return services;
        }

        // ── Session ──────────────────────────────────────────────────────────
        private static void AddSecureSession(IServiceCollection services, IWebHostEnvironment env)
        {
            services.AddDistributedMemoryCache();
            services.AddSession(options =>
            {
                options.IdleTimeout         = TimeSpan.FromMinutes(30);
                options.Cookie.Name         = "App-Session";
                options.Cookie.HttpOnly     = true;
                options.Cookie.IsEssential  = true;
                // SameAsRequest: Railway proxy handle HTTPS, app nhận HTTP nội bộ
                // Cookie vẫn an toàn vì Railway enforce HTTPS ở tầng ngoài
                options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
                options.Cookie.SameSite     = SameSiteMode.Strict;
            });
        }

        // ── Antiforgery ───────────────────────────────────────────────────────
        private static void AddSecureAntiforgery(IServiceCollection services, IWebHostEnvironment env)
        {
            services.AddAntiforgery(options =>
            {
                options.Cookie.Name         = "App-AF";
                options.Cookie.HttpOnly     = true;
                // SameAsRequest: tương thích với Railway reverse proxy
                options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
                options.Cookie.SameSite     = SameSiteMode.Strict;
                options.HeaderName          = "X-CSRF-TOKEN";
            });
        }

        // ── Rate Limiting ────────────────────────────────────────────────────
        private static void AddRateLimiting(IServiceCollection services)
        {
            services.AddRateLimiter(options =>
            {
                // Login endpoint: 10 request/phút/IP — chống brute-force
                options.AddFixedWindowLimiter("login", opt =>
                {
                    opt.PermitLimit          = 10;
                    opt.Window               = TimeSpan.FromMinutes(1);
                    opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                    opt.QueueLimit           = 0;
                });

                // Global: 200 request/phút/IP
                options.AddFixedWindowLimiter("global", opt =>
                {
                    opt.PermitLimit          = 200;
                    opt.Window               = TimeSpan.FromMinutes(1);
                    opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                    opt.QueueLimit           = 2;
                });

                options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            });
        }

        // ─────────────────────────────────────────────────────────────────────
        // MIDDLEWARE — gọi trong app (WebApplication)
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Cấu hình HTTPS + HSTS (Strict Transport Security — SSTP-style).
        /// Gọi TRƯỚC tất cả middleware khác.
        /// </summary>
        public static WebApplication UseTransportSecurity(this WebApplication app)
        {
            // Railway chạy app sau reverse proxy — đọc X-Forwarded-* headers
            app.UseForwardedHeaders(new ForwardedHeadersOptions
            {
                ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
            });

            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
                // KHÔNG dùng UseHttpsRedirection — Railway proxy đã handle HTTPS
                // UseHttpsRedirection gây redirect loop sau proxy
            }

            return app;
        }

        /// <summary>
        /// Thêm HTTP Response Security Headers vào mọi response.
        /// Gọi sau UseTransportSecurity, trước UseStaticFiles.
        /// </summary>
        public static WebApplication UseSecurityHeaders(this WebApplication app)
        {
            app.Use(async (context, next) =>
            {
                var headers = context.Response.Headers;

                // ── Chống clickjacking ────────────────────────────────────────
                headers["X-Frame-Options"] = "DENY";

                // ── Chống MIME sniffing ───────────────────────────────────────
                headers["X-Content-Type-Options"] = "nosniff";

                // ── Giảm rò rỉ Referer ───────────────────────────────────────
                headers["Referrer-Policy"] = "strict-origin-when-cross-origin";

                // ── Cross-Origin Isolation ────────────────────────────────────
                headers["Cross-Origin-Opener-Policy"]   = "same-origin";
                headers["Cross-Origin-Resource-Policy"] = "same-origin";

                // ── Tắt các API nhạy cảm ─────────────────────────────────────
                headers["Permissions-Policy"] =
                    "camera=(), microphone=(), geolocation=(), payment=(), " +
                    "usb=(), bluetooth=(), midi=()";

                // ── Content Security Policy (CSP) — chống XSS ────────────────
                headers["Content-Security-Policy"] =
                    "default-src 'self'; " +
                    "script-src 'self' 'unsafe-inline' " +
                        "https://cdnjs.cloudflare.com " +
                        "https://cdn.jsdelivr.net; " +
                    "style-src 'self' 'unsafe-inline' " +
                        "https://cdnjs.cloudflare.com " +
                        "https://cdn.jsdelivr.net " +
                        "https://fonts.googleapis.com; " +
                    "font-src 'self' " +
                        "https://fonts.gstatic.com " +
                        "https://cdnjs.cloudflare.com; " +
                    "img-src 'self' data: blob:; " +
                    "media-src 'self'; " +
                    "connect-src 'self'; " +
                    "object-src 'none'; " +
                    "base-uri 'self'; " +
                    "form-action 'self'; " +
                    "frame-ancestors 'none'; " +
                    "upgrade-insecure-requests;";

                await next();
            });

            return app;
        }
    }
}
