namespace Kozy.Actions;

/// <summary>Allowlists und Schutzlisten aus Kapitel 5.6.</summary>
public static class Allowlists
{
    private static readonly string[] AllowedHkcu =
    [
        @"HKCU\Control Panel\",
        @"HKCU\System\GameConfigStore",
        @"HKCU\Software\Microsoft\Windows\CurrentVersion\",
        @"HKCU\Software\kozy-test",   // nur für automatisierte Tests (siehe Plan 15.1)
    ];

    private static readonly string[] AllowedHklm =
    [
        @"HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile",
        @"HKLM\SOFTWARE\Policies\Microsoft\Windows\",
        @"HKLM\SOFTWARE\Policies\Google\Chrome",
        @"HKLM\SYSTEM\CurrentControlSet\Control\GraphicsDrivers",
        @"HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Power",
        @"HKLM\SYSTEM\CurrentControlSet\Services\Tcpip\Parameters\Interfaces\",
    ];

    private static readonly string[] Blocked =
    [
        @"HKLM\SAM",
        @"HKLM\SECURITY",
        @"HKLM\SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate",
        @"HKLM\SOFTWARE\Policies\Microsoft\Windows Defender",
        @"HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System",
    ];

    public static bool IsRegistryAllowed(string hive, string path)
    {
        var normalized = Normalize(hive) + "\\" + Normalize(path.Trim('\\'));

        if (Blocked.Any(b => normalized.StartsWith(Normalize(b), StringComparison.OrdinalIgnoreCase)))
        {
            return false;
        }

        var prefixes = hive.Equals("HKCU", StringComparison.OrdinalIgnoreCase) ? AllowedHkcu : AllowedHklm;
        return prefixes.Any(p => normalized.StartsWith(Normalize(p), StringComparison.OrdinalIgnoreCase));
    }

    private static string Normalize(string s) => s.TrimEnd('\\').Replace('/', '\\');

    /// <summary>Dienste: nur Namen aus fester Liste (Kapitel 5.6).</summary>
    public static readonly string[] AllowedServices = ["SysMain", "DiagTrack", "dmwappushservice"];

    public static bool IsServiceAllowed(string name) =>
        AllowedServices.Contains(name, StringComparer.OrdinalIgnoreCase);

    /// <summary>Cleaner-Ziele (feste Enum).</summary>
    public static readonly string[] CleanerTargets =
    [
        "UserTemp", "WindowsTemp", "INetCache", "ThumbnailCache",
        "D3DShaderCache", "NvidiaShaderCache", "WUDownload",
    ];

    /// <summary>Geschützte Appx-Pakete – nie entfernen.</summary>
    public static readonly string[] ProtectedAppx =
    [
        "Microsoft.WindowsStore", "Microsoft.XboxIdentityProvider", "Microsoft.Xbox.TCUI",
        "Microsoft.XboxGameOverlay", "Microsoft.GamingApp", "Microsoft.WindowsTerminal",
        "Microsoft.Windows.Photos", "Microsoft.WindowsCalculator",
    ];

    public static bool IsAppxProtected(string name) =>
        ProtectedAppx.Any(p => name.StartsWith(p, StringComparison.OrdinalIgnoreCase));

    /// <summary>Erlaubte BCD-Elemente (5.6 blockiert u. a. testsigning/nointegritychecks).</summary>
    public static readonly string[] AllowedBcdElements = ["useplatformclock", "disabledynamictick"];

    public static bool IsBcdElementAllowed(string element) =>
        AllowedBcdElements.Contains(element, StringComparer.OrdinalIgnoreCase);
}