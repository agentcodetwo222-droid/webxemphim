namespace webxemphim.Infrastructure
{
    /// <summary>
    /// Luu tru cau hinh ket noi mang:
    ///   - Tailscale  : VPN mesh, ket noi Client &lt;-&gt; Server noi bo
    ///   - SSTP       : VPN tunnel qua HTTPS port 443
    ///   - Railway    : server deploy webxemphim
    ///
    /// Cach su dung:
    ///   var cfg = NetworkConfig.Tailscale;
    ///   Console.WriteLine(cfg.ServerIp);
    ///
    /// Bien moi truong (set tren Railway Variables hoac may local):
    ///   TAILSCALE_SERVER_IP, TAILSCALE_CLIENT_IP, TAILSCALE_DOMAIN
    ///   SSTP_ENDPOINT, SSTP_USERNAME, SSTP_VIRTUAL_HUB
    ///   RAILWAY_PUBLIC_URL
    /// </summary>
    public static class NetworkConfig
    {
        public static readonly TailscaleConfig Tailscale = new();
        public static readonly SstpConfig      Sstp      = new();
        public static readonly RailwayConfig   Railway   = new();

        private static string E(string key, string fallback)
            => Environment.GetEnvironmentVariable(key) ?? fallback;

        /// <summary>In thong tin ket noi ra console khi app khoi dong.</summary>
        public static void PrintConnections(ILogger logger)
        {
            var lines = new[]
            {
                "======================================",
                "  NETWORK CONNECTIONS",
                "--------------------------------------",
                "  [TAILSCALE]",
                $"  Server IP  : {Tailscale.ServerIp}",
                $"  Client IP  : {Tailscale.ClientIp}",
                $"  Domain     : {Tailscale.Domain}",
                $"  Subnet     : {Tailscale.Subnet}",
                $"  App URL    : {Tailscale.AppUrl}",
                "--------------------------------------",
                "  [SSTP VPN]",
                $"  Endpoint   : {Sstp.Endpoint}:{Sstp.Port}",
                $"  Protocol   : {Sstp.Protocol}",
                $"  Auth       : {Sstp.AuthMethod}",
                $"  Encryption : {Sstp.Encryption}",
                $"  Virtual Hub: {Sstp.VirtualHub}",
                $"  IP Pool    : {Sstp.IpPoolStart} - {Sstp.IpPoolEnd}",
                "--------------------------------------",
                "  [RAILWAY]",
                $"  App URL    : {Railway.AppUrl}",
                $"  Region     : {Railway.Region}",
                $"  DB Host    : {Railway.DbHost}:{Railway.DbPort}",
                $"  DB Name    : {Railway.DbName}",
                "======================================"
            };

            foreach (var line in lines)
            {
                Console.WriteLine(line);
                logger.LogInformation("{Line}", line);
            }
        }

        // ── Tailscale ─────────────────────────────────────────────────────────

        /// <summary>
        /// Tailscale: VPN mesh ket noi truc tiep khong can port forward.
        ///
        /// Setup:
        ///   1. Cai Tailscale tren ca 2 may: https://tailscale.com/download
        ///   2. Dang nhap cung 1 account tren ca 2 may
        ///   3. 2 may tu dong thay nhau, lay IP 100.x.x.x
        ///   4. Set TAILSCALE_SERVER_IP = IP cua may Server
        ///      Set TAILSCALE_CLIENT_IP = IP cua may Client
        ///
        /// Uu diem:
        ///   - Khong can config firewall / port forward
        ///   - NAT traversal tu dong
        ///   - MagicDNS: truy cap bang ten may thay vi IP
        ///   - Mien phi den 3 may
        /// </summary>
        public class TailscaleConfig
        {
            /// <summary>IP Tailscale cua Server (100.x.x.x)</summary>
            public string ServerIp   { get; init; } = E("TAILSCALE_SERVER_IP", "100.x.x.x");

            /// <summary>IP Tailscale cua Client (100.x.x.x)</summary>
            public string ClientIp   { get; init; } = E("TAILSCALE_CLIENT_IP", "100.x.x.x");

            /// <summary>MagicDNS domain (ten-may.tailxxxxxx.ts.net)</summary>
            public string Domain     { get; init; } = E("TAILSCALE_DOMAIN", "sstp-server.tailxxxxxx.ts.net");

            /// <summary>Subnet Tailscale (mac dinh 100.64.0.0/10)</summary>
            public string Subnet     { get; init; } = E("TAILSCALE_SUBNET", "100.64.0.0/10");

            /// <summary>Auth Key (lay tai tailscale.com/settings/keys)</summary>
            public string AuthKey    { get; init; } = E("TAILSCALE_AUTH_KEY", "");

            /// <summary>URL truy cap webxemphim qua Tailscale</summary>
            public string AppUrl     => $"http://{ServerIp}:8080";

            /// <summary>Huong dan ket noi</summary>
            public string SetupGuide =>
                "1. Cai Tailscale\n" +
                "2. tailscale up\n" +
                $"3. Truy cap: http://{Domain}:8080";
        }

        // ── SSTP ──────────────────────────────────────────────────────────────

        /// <summary>
        /// SSTP: VPN tunnel qua HTTPS port 443.
        ///
        /// Yeu cau:
        ///   - SoftEther VPN Server tren may Server
        ///   - SSL Certificate (Let's Encrypt hoac self-signed)
        ///   - Port 443 mo tren firewall
        ///
        /// Client ket noi (Windows):
        ///   Settings -> VPN -> Add
        ///   Type   : Secure Socket Tunneling Protocol (SSTP)
        ///   Server : &lt;Endpoint&gt;
        ///   User   : &lt;Username&gt;
        ///   Pass   : (set bien moi truong SSTP_PASSWORD)
        /// </summary>
        public class SstpConfig
        {
            /// <summary>Domain hoac IP cua SSTP server</summary>
            public string Endpoint    { get; init; } = E("SSTP_ENDPOINT",     "mysstp.duckdns.org");

            /// <summary>Port SSTP (luon la 443)</summary>
            public int    Port        { get; init; } = int.Parse(E("SSTP_PORT", "443"));

            /// <summary>Giao thuc day du</summary>
            public string Protocol    { get; init; } = "SSTP over TLS 1.3";

            /// <summary>Phuong thuc xac thuc</summary>
            public string AuthMethod  { get; init; } = E("SSTP_AUTH_METHOD",  "MS-CHAPv2");

            /// <summary>Ten dang nhap VPN</summary>
            public string Username    { get; init; } = E("SSTP_USERNAME",     "vpnuser");

            /// <summary>Thuat toan ma hoa</summary>
            public string Encryption  { get; init; } = "AES-256-GCM";

            /// <summary>Virtual Hub trong SoftEther</summary>
            public string VirtualHub  { get; init; } = E("SSTP_VIRTUAL_HUB",       "VPN");

            /// <summary>IP pool bat dau cap cho client</summary>
            public string IpPoolStart { get; init; } = E("SSTP_IP_POOL_START", "192.168.30.10");

            /// <summary>IP pool ket thuc</summary>
            public string IpPoolEnd   { get; init; } = E("SSTP_IP_POOL_END",   "192.168.30.20");

            /// <summary>Huong dan ket noi Windows Client</summary>
            public string SetupGuide  =>
                $"VPN Type : SSTP\n" +
                $"Server   : {Endpoint}:{Port}\n" +
                $"Auth     : {AuthMethod}\n" +
                $"User     : {Username}";
        }

        // ── Railway ───────────────────────────────────────────────────────────

        /// <summary>
        /// Railway: noi deploy webxemphim.
        /// Cac bien duoc Railway tu inject khi deploy.
        /// </summary>
        public class RailwayConfig
        {
            /// <summary>URL cong khai cua app</summary>
            public string AppUrl  { get; init; } = E("RAILWAY_PUBLIC_URL",
                                                     E("RAILWAY_STATIC_URL",
                                                       "https://webxemphim.up.railway.app"));

            /// <summary>Region Railway deploy</summary>
            public string Region  { get; init; } = E("RAILWAY_REGION",   "asia-southeast1");

            /// <summary>Host PostgreSQL</summary>
            public string DbHost  { get; init; } = E("PGHOST",           "postgres.railway.internal");

            /// <summary>Port PostgreSQL</summary>
            public string DbPort  { get; init; } = E("PGPORT",           "5432");

            /// <summary>Ten database</summary>
            public string DbName  { get; init; } = E("PGDATABASE",       "railway");
        }
    }
}
