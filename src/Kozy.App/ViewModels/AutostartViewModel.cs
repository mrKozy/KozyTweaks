using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Kozy.Actions;
using Kozy.App.Services;

namespace Kozy.App.ViewModels;

public partial class AutostartItemViewModel : ObservableObject
{
    public StartupEntry Entry { get; }

    public string Name => Entry.Name;
    public string Command => Entry.Command;
    public string ScopeString => Entry.Scope == StartupScope.CurrentUser ? "Aktueller Benutzer" : "Gesamtes System";

    [ObservableProperty]
    private bool _isEnabled;

    public AutostartItemViewModel(StartupEntry entry)
    {
        Entry = entry;
        _isEnabled = entry.IsEnabled;
    }
}

public partial class AutostartViewModel : ObservableObject
{
    private readonly KozyService _service = KozyService.Instance;

    public ObservableCollection<AutostartItemViewModel> Items { get; } = [];

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string _statusMessage = "";

    public AutostartViewModel()
    {
        Refresh();
    }

    [RelayCommand]
    public void Refresh()
    {
        IsBusy = true;
        Items.Clear();

        var entries = _service.StartupManager.GetAll();
        foreach (var entry in entries)
        {
            var vm = new AutostartItemViewModel(entry);
            vm.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(AutostartItemViewModel.IsEnabled))
                {
                    _service.StartupManager.SetEnabled(vm.Entry.Scope, vm.Name, vm.IsEnabled);
                    StatusMessage = $"Autostart für '{vm.Name}' {(vm.IsEnabled ? "aktiviert" : "deaktiviert")}.";
                }
            };
            Items.Add(vm);
        }

        StatusMessage = $"{Items.Count} Autostart-Einträge geladen.";
        IsBusy = false;
    }
}
