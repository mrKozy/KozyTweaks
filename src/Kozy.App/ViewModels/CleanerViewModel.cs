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
    private string _statusMessage = "Ready to scan";

    [ObservableProperty]
    private string _totalSizeString = "0 MB";

    public CleanerViewModel()
    {
        var local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var windows = Environment.GetFolderPath(Environment.SpecialFolder.Windows);

        Targets.Add(new CleanerTargetItem("UserTemp", "User Temp Files", "Temporary application files and caches for the current user", Path.GetTempPath()));
        Targets.Add(new CleanerTargetItem("WindowsTemp", "Windows System Temp", "Temporary Windows system files and logs", Path.Combine(windows, "Temp")));
        Targets.Add(new CleanerTargetItem("INetCache", "Browser & Web Cache", "Cached internet files and web components", Path.Combine(local, "Microsoft", "Windows", "INetCache")));
        Targets.Add(new CleanerTargetItem("ThumbnailCache", "Thumbnail Cache", "Cached Windows Explorer icons and preview thumbnails", Path.Combine(local, "Microsoft", "Windows", "Explorer")));
        Targets.Add(new CleanerTargetItem("D3DShaderCache", "DirectX Shader Cache", "Precompiled GPU shader caches (automatically rebuilt when needed)", Path.Combine(local, "D3DSCache")));
        Targets.Add(new CleanerTargetItem("NvidiaShaderCache", "NVIDIA Shader Cache", "NVIDIA graphics pipeline shader files", Path.Combine(local, "NVIDIA", "DXCache")));
        Targets.Add(new CleanerTargetItem("WUDownload", "Windows Update Cache", "Previously installed download packages from Windows Update", Path.Combine(windows, "SoftwareDistribution", "Download")));

        _ = ScanAsync();
    }

    [RelayCommand]
    public async Task ScanAsync()
    {
        IsBusy = true;
        StatusMessage = "Calculating disk space...";

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

        StatusMessage = $"Scan complete. {TotalSizeString} can be safely cleaned.";
        IsBusy = false;
    }

    [RelayCommand]
    public async Task CleanAsync()
    {
        var selected = Targets.Where(t => t.IsSelected && t.SizeBytes > 0).ToList();
        if (selected.Count == 0)
        {
            StatusMessage = "No items selected to clean.";
            return;
        }

        IsBusy = true;
        StatusMessage = "Cleaning selected temporary files...";

        await Task.Run(() =>
        {
            foreach (var target in selected)
            {
                CleanerHandler.DeleteContents(target.PathOnDisk);
            }
        });

        StatusMessage = "Cleaning completed successfully!";
        await ScanAsync();
        IsBusy = false;
    }
}
