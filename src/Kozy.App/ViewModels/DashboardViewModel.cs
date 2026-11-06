using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Kozy.App.Services;
using Kozy.Core;
using Kozy.Core.Models;

namespace Kozy.App.ViewModels;

public partial class DashboardViewModel : ObservableObject
{
    private readonly KozyService _service = KozyService.Instance;

    public string OsName => _service.OsName;
    public string OsBuildString => _service.OsBuildString;
    public string CpuName => _service.CpuName;
    public string GpuName => _service.GpuName;
    public string RamString => _service.RamString;
    public string DeviceTypeString => _service.DeviceTypeString;

    [ObservableProperty]
    private int _totalTweaksCount;

    [ObservableProperty]
    private int _appliedTweaksCount;

    [ObservableProperty]
    private string _statusMessage = "Ready";

    [ObservableProperty]
    private bool _isBusy;

    public DashboardViewModel()
    {
        TotalTweaksCount = _service.Catalog.Tweaks.Count;
        RefreshStatus();
    }

    [RelayCommand]
    public async Task RefreshStatusAsync()
    {
        IsBusy = true;
        StatusMessage = "Checking system state...";

        await Task.Run(() =>
        {
            var count = 0;
            foreach (var tweak in _service.Catalog.Tweaks)
            {
                if (_service.Engine.Detect(tweak) == TweakStatus.Applied)
                {
                    count++;
                }
            }
            AppliedTweaksCount = count;
        });

        StatusMessage = $"Ready. {AppliedTweaksCount} of {TotalTweaksCount} optimizations active.";
        IsBusy = false;
    }

    public void RefreshStatus()
    {
        _ = RefreshStatusAsync();
    }

    [RelayCommand]
    public async Task ApplySafePresetAsync()
    {
        IsBusy = true;
        StatusMessage = "Applying safe optimization preset...";

        var safeTweaks = _service.Catalog.GetByPreset(TweakPreset.Safe);

        var result = await Task.Run(() => _service.Engine.Apply(safeTweaks, new ApplyOptions(
            CreateRestorePoint: true,
            StopOnError: false
        )));

        StatusMessage = $"Completed: {result.Applied} applied, {result.Skipped} unchanged, {result.Failed} failed.";
        await RefreshStatusAsync();
        IsBusy = false;
    }

    [RelayCommand]
    public async Task CreateRestorePointAsync()
    {
        IsBusy = true;
        StatusMessage = "Creating Windows System Restore Point...";

        var service = new WindowsRestorePointService();
        var point = await Task.Run(() => service.Create("kozy - Pre-Optimization"));

        StatusMessage = point?.Created == true
            ? "System restore point created successfully."
            : "Notice: System restore point could not be created (or 24-hour limit is active).";

        IsBusy = false;
    }
}
