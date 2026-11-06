using System.IO;
using System.Management;
using Kozy.Actions;
using Kozy.Core;
using Kozy.Core.Models;
using Microsoft.Win32;

namespace Kozy.App.Services;

public sealed class KozyService
{
    private static KozyService? _instance;
    public static KozyService Instance => _instance ??= new KozyService();

    public Engine Engine { get; }
    public TweakCatalog Catalog { get; }
    public StartupManager StartupManager { get; }
    public JournalStore JournalStore { get; }
    public string AppDataDirectory { get; }

    public string OsName { get; private set; } = "Windows";
    public string OsBuildString { get; private set; } = "";
    public string CpuName { get; private set; } = "CPU";
    public string GpuName { get; private set; } = "GPU";
    public string RamString { get; private set; } = "RAM";
    public string DeviceTypeString { get; private set; } = "Desktop";

    private KozyService()
    {
        AppDataDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            "kozy");

        var journalDir = Path.Combine(AppDataDirectory, "journal");
        JournalStore = new JournalStore(journalDir);

        var handlers = HandlerCatalog.Create();
        var systemInfoProvider = new WindowsSystemInfoProvider();
        var restorePoints = new WindowsRestorePointService();

        Engine = new Engine(handlers, systemInfoProvider, restorePoints, JournalStore, "1.0.0");
        StartupManager = new StartupManager();

        var packsDir = TweakCatalog.FindDefaultPacksDirectory();
        Catalog = TweakCatalog.LoadFromDirectory(packsDir);

        DetectHardwareSpecs(systemInfoProvider.Get());
    }

    private void DetectHardwareSpecs(SystemInfo sysInfo)
    {
        OsBuildString = $"Windows Build {sysInfo.OsBuild} ({sysInfo.Edition})";
        DeviceTypeString = sysInfo.DeviceClass == DeviceClass.Laptop ? "Laptop / Notebook" : "Desktop PC";

        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion");
            if (key?.GetValue("ProductName") is string prod)
            {
                OsName = prod;
            }
        }
        catch { }

        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT Name FROM Win32_Processor");
            foreach (var item in searcher.Get())
            {
                CpuName = item["Name"]?.ToString()?.Trim() ?? "Prozessor";
                break;
            }
        }
        catch { }

        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT Name FROM Win32_VideoController");
            foreach (var item in searcher.Get())
            {
                var name = item["Name"]?.ToString()?.Trim();
                if (!string.IsNullOrEmpty(name))
                {
                    GpuName = name;
                    break;
                }
            }
        }
        catch { }

        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT TotalPhysicalMemory FROM Win32_ComputerSystem");
            foreach (var item in searcher.Get())
            {
                if (ulong.TryParse(item["TotalPhysicalMemory"]?.ToString(), out var bytes))
                {
                    var gb = Math.Round((double)bytes / (1024 * 1024 * 1024), 1);
                    RamString = $"{gb:0.#} GB RAM";
                }
                break;
            }
        }
        catch { }
    }
}
