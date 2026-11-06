using System.Management;
using Kozy.Core;
using Kozy.Core.Models;
using Microsoft.Win32;

namespace Kozy.Actions;

public sealed class WindowsSystemInfoProvider : ISystemInfoProvider
{
    public SystemInfo Get()
    {
        return new SystemInfo
        {
            OsBuild = GetOsBuild(),
            Edition = GetEdition(),
            DeviceClass = GetDeviceClass(),
            GpuVendor = GetGpuVendor(),
        };
    }

    private static int GetOsBuild()
    {
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion");
            if (key?.GetValue("CurrentBuildNumber") is string buildStr && int.TryParse(buildStr, out var build))
            {
                return build;
            }
        }
        catch { }

        return Environment.OSVersion.Version.Build;
    }

    private static string GetEdition()
    {
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion");
            if (key?.GetValue("EditionID") is string edition && !string.IsNullOrWhiteSpace(edition))
            {
                if (edition.Contains("Pro", StringComparison.OrdinalIgnoreCase)) return "Pro";
                if (edition.Contains("Core", StringComparison.OrdinalIgnoreCase) || edition.Contains("Home", StringComparison.OrdinalIgnoreCase)) return "Home";
                if (edition.Contains("Enterprise", StringComparison.OrdinalIgnoreCase)) return "Enterprise";
                if (edition.Contains("Education", StringComparison.OrdinalIgnoreCase)) return "Education";
                return edition;
            }
        }
        catch { }

        return "Pro";
    }

    private static DeviceClass GetDeviceClass()
    {
        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT BatteryStatus FROM Win32_Battery");
            using var collection = searcher.Get();
            if (collection.Count > 0)
            {
                return DeviceClass.Laptop;
            }
        }
        catch { }

        return DeviceClass.Desktop;
    }

    private static GpuVendor? GetGpuVendor()
    {
        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT Name, AdapterCompatibility FROM Win32_VideoController");
            using var collection = searcher.Get();

            foreach (var item in collection)
            {
                var name = item["Name"]?.ToString() ?? string.Empty;
                var compat = item["AdapterCompatibility"]?.ToString() ?? string.Empty;
                var combined = $"{name} {compat}";

                if (combined.Contains("NVIDIA", StringComparison.OrdinalIgnoreCase))
                {
                    return GpuVendor.Nvidia;
                }
                if (combined.Contains("AMD", StringComparison.OrdinalIgnoreCase) || combined.Contains("Advanced Micro Devices", StringComparison.OrdinalIgnoreCase) || combined.Contains("Radeon", StringComparison.OrdinalIgnoreCase))
                {
                    return GpuVendor.Amd;
                }
                if (combined.Contains("Intel", StringComparison.OrdinalIgnoreCase))
                {
                    return GpuVendor.Intel;
                }
            }
        }
        catch { }

        return null;
    }
}
