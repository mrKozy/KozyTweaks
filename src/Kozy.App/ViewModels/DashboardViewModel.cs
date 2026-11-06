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
    private string _statusMessage = "Bereit";

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
        StatusMessage = "Prüfe Systemzustand...";

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

        StatusMessage = $"Bereit. {AppliedTweaksCount} von {TotalTweaksCount} Optimierungen aktiv.";
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
        StatusMessage = "Wende sicheres Optimierungs-Preset an...";

        var safeTweaks = _service.Catalog.GetByPreset(TweakPreset.Safe);

        var result = await Task.Run(() => _service.Engine.Apply(safeTweaks, new ApplyOptions(
            CreateRestorePoint: true,
            StopOnError: false
        )));

        StatusMessage = $"Abgeschlossen: {result.Applied} angewendet, {result.Skipped} unverändert, {result.Failed} Fehler.";
        await RefreshStatusAsync();
        IsBusy = false;
    }

    [RelayCommand]
    public async Task CreateRestorePointAsync()
    {
        IsBusy = true;
        StatusMessage = "Erstelle Windows-Wiederherstellungspunkt...";

        var service = new WindowsRestorePointService();
        var point = await Task.Run(() => service.Create("kozy - Vor Optimierung"));

        StatusMessage = point?.Created == true
            ? "Wiederherstellungspunkt erfolgreich erstellt."
            : "Hinweis: Wiederherstellungspunkt konnte nicht erstellt werden (oder 24h-Limit aktiv).";

        IsBusy = false;
    }
}
