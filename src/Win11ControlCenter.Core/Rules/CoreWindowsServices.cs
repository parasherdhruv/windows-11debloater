namespace Win11ControlCenter.Core.Rules;

public static class CoreWindowsServices
{
    private static readonly HashSet<string> ProtectedServiceNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "RpcSs",
        "RpcEptMapper",
        "DcomLaunch",
        "EventLog",
        "CryptSvc",
        "WinDefend",
        "SecurityHealthService",
        "MpsSvc",
        "Dhcp",
        "Dnscache",
        "LanmanWorkstation",
        "LanmanServer",
        "Schedule",
        "PlugPlay",
        "Power",
        "AudioEndpointBuilder",
        "AudioSrv",
        "ProfSvc",
        "SamSs",
        "LSM",
        "SystemEventsBroker",
        "TimeBrokerSvc",
        "BrokerInfrastructure",
        "CoreMessagingRegistrar",
        "KeyIso",
        "VaultSvc",
        "StateRepository",
        "AppXSvc",
        "ClipSVC",
        "wuauserv", // Windows Update - strictly protected
        "TrustedInstaller"
    };

    public static bool IsProtectedService(string serviceName)
    {
        if (string.IsNullOrWhiteSpace(serviceName))
            return true;

        return ProtectedServiceNames.Contains(serviceName.Trim());
    }

    public static IEnumerable<string> GetProtectedServiceList() => ProtectedServiceNames;
}

