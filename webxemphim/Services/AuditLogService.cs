using webxemphim.Models;

namespace webxemphim.Services
{
    public class AuditLogService
    {
        private readonly IServiceScopeFactory _scopeFactory;

        public AuditLogService(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
        }

        public void Log(string category, string level, string message,
                        int? userId, string userName, string ip, string detail = "")
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var schema = scope.ServiceProvider.GetRequiredService<SchemaDataService>();
                    await schema.AddAuditLogAsync(new AuditLog
                    {
                        Timestamp = DateTime.UtcNow,
                        Category  = category,
                        Level     = level,
                        Message   = message,
                        UserId    = userId,
                        UserName  = userName,
                        IpAddress = ip,
                        Detail    = detail
                    });

                    var count = await schema.CountAuditLogsAsync();
                    if (count > 10000)
                        await schema.DeleteOldAuditLogsAsync(10000);
                }
                catch { }
            });
        }

        public void LogLogin(int? userId, string userName, string ip, bool success)
            => Log(success ? "LOGIN_OK" : "LOGIN_FAIL",
                   success ? "SUCCESS"  : "WARNING",
                   success ? $"Dang nhap thanh cong: {userName}" : $"Dang nhap that bai: {userName}",
                   userId, userName, ip);

        public void LogLogout(int userId, string userName, string ip)
            => Log("LOGOUT", "INFO", $"Dang xuat: {userName}", userId, userName, ip);

        public void LogRegister(int userId, string userName, string ip)
            => Log("REGISTER", "INFO", $"Dang ky moi: {userName}", userId, userName, ip);

        public void LogLockout(string identifier, string ip)
            => Log("LOCKOUT", "DANGER", $"Bi khoa: {identifier}", null, identifier, ip,
                   "Dang nhap sai qua 5 lan");

        public void LogDeposit(int userId, string userName, decimal amount, string currency, string ip)
            => Log("DEPOSIT", "FINANCE", $"Nap tien: {userName}",
                   userId, userName, ip, $"{amount:N0} {currency}");

        public void LogBuyVIP(int userId, string userName, string package, string ip)
            => Log("BUY_VIP", "FINANCE", $"Mua VIP: {userName} - {package}",
                   userId, userName, ip);

        public void LogPasswordChange(int userId, string userName, string ip)
            => Log("PASSWORD_CHANGE", "SECURITY", $"Doi mat khau: {userName}",
                   userId, userName, ip);

        public void LogAdminLockUser(int adminId, string adminName, int targetUserId, string ip)
            => Log("ADMIN_LOCK_USER", "DANGER", $"Admin khoa user #{targetUserId}",
                   adminId, adminName, ip);

        public void LogVipExpired(int userId, string userName)
            => Log("VIP_EXPIRED", "INFO", $"VIP het han: {userName}",
                   userId, userName, "system");

        public async Task<List<AuditLog>> GetLatestAsync(
            SchemaDataService schema, int count = 100, string? category = null)
            => await schema.GetLatestAuditLogsAsync(count, category);
    }
}
