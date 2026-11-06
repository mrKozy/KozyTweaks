using System.Collections.ObjectModel;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Kozy.Actions;

namespace Kozy.App.ViewModels;

public partial class CleanerTargetItem : ObservableObject
{
    public string Key { get; }
    public string DisplayName { get; }
    public string Description { get; }
    public string PathOnDisk { get; }

    [ObservableProperty]
    private bool _isSelected = true;

    [ObservableProperty]
    private long _sizeBytes;

    [ObservableProperty]
    private string _sizeString = "0 Bytes";

    partial void OnSizeBytesChanged(long value)
    {
        SizeString = value switch
        {
            > 1024 * 1024 * 1024 => $"{value / (1024.0 * 1024 * 1024):F2} GB",
            > 1024 * 1024 => $"{value / (1024.0 * 1024):F1} MB",
            > 1024 => $"{value / 1024.0:F0} KB",
            _ => $"{value} Bytes",
        };
    }

    public CleanerTargetItem(string key, string displayName, string description, string path)
    {
        Key = key;
        DisplayName = displayName;
        Description = description;
        PathOnDisk = path;
    }
}

public partial class CleanerViewModel : ObservableObject
{
    public ObservableCollection<CleanerTargetItem> Targets { get; } = [];

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string _statusMessage = "Bereit zum Scannen";

    [ObservableProperty]
    private string _totalSizeString = "0 MB";

    public CleanerViewModel()
    {
        var local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var windows = Environment.GetFolderPath(Environment.SpecialFolder.Windows);

        Targets.Add(new CleanerTargetItem("UserTemp", "Benutzer-Temp", "Temporäre Anwendungsdateien des aktuellen Benutzers", Path.GetTempPath()));
        Targets.Add(new CleanerTargetItem("WindowsTemp", "Windows-Temp", "Temporäre Systemdateien", Path.Combine(windows, "Temp")));
        Targets.Add(new CleanerTargetItem("INetCache", "Browser-Cache (INetCache)", "Temporäre Internet-Dateien und Web-Caches", Path.Combine(local, "Microsoft", "Windows", "INetCache")));
        Targets.Add(new CleanerTargetItem("ThumbnailCache", "Miniaturansichten-Cache", "Gecachte Explorer-Icons und Thumbnails", Path.Combine(local, "Microsoft", "Windows", "Explorer")));
        Targets.Add(new CleanerTargetItem("D3DShaderCache", "DirectX Shader-Cache", "Kompilierte GPU-Shader (werden bei Bedarf neu gebaut)", Path.Combine(local, "D3DSCache")));
        Targets.Add(new CleanerTargetItem("NvidiaShaderCache", "NVIDIA Shader-Cache", "NVIDIA Grafik-Shader-Dateien", Path.Combine(local, "NVIDIA", "DXCache")));
        Targets.Add(new CleanerTargetItem("WUDownload", "Windows Update Cache", "Bereits installierte Download-Pakete von Windows Update", Path.Combine(windows, "SoftwareDistribution", "Download")));

        _ = ScanAsync();
    }

    [RelayCommand]
    public async Task ScanAsync()
    {
        IsBusy = true;
        StatusMessage = "Berechne Speicherplatz...";

        long grandTotal = 0;

        await Task.Run(() =>
        {
            foreach (var target in Targets)
            {
                var size = CleanerHandler.ComputeSize(target.PathOnDisk);
                target.SizeBytes = size;
                grandTotal += size;
            }
        });

        TotalSizeString = grandTotal switch
        {
            > 1024 * 1024 * 1024 => $"{grandTotal / (1024.0 * 1024 * 1024):F2} GB",
            > 1024 * 1024 => $"{grandTotal / (1024.0 * 1024):F1} MB",
            _ => $"{grandTotal / 1024.0:F0} KB",
        };

        StatusMessage = $"Scan abgeschlossen. {TotalSizeString} können bereinigt werden.";
        IsBusy = false;
    }

    [RelayCommand]
    public async Task CleanAsync()
    {
        var selected = Targets.Where(t => t.IsSelected && t.SizeBytes > 0).ToList();
        if (selected.Count == 0)
        {
            StatusMessage = "Keine Daten zum Bereinigen markiert.";
            return;
        }

        IsBusy = true;
        StatusMessage = "Bereinige temporäre Dateien...";

        await Task.Run(() =>
        {
            foreach (var target in selected)
            {
                CleanerHandler.DeleteContents(target.PathOnDisk);
            }
        });

        StatusMessage = "Bereinigung abgeschlossen!";
        await ScanAsync();
        IsBusy = false;
    }
}
