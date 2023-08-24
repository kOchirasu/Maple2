using System.Net;

namespace Maple2.Server.Web.Constants;

public static class Target {
    public static readonly IPAddress WebIp = IPAddress.Loopback;
    public static readonly ushort WebPort = 30000;
    private static readonly string SolutionDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../.."));
    public static readonly string DataDir = Path.Combine(SolutionDir, "Maple2.Server.Web/Data");

    static Target() {
        if (IPAddress.TryParse(Environment.GetEnvironmentVariable("WEB_IP"), out IPAddress? webIpAddress)) {
            WebIp = webIpAddress;
        }
        if (ushort.TryParse(Environment.GetEnvironmentVariable("WEB_PORT"), out ushort webPortOverride)) {
            WebPort = webPortOverride;
        }
    }
}
