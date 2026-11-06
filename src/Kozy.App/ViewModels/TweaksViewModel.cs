using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Kozy.App.Models;
using Kozy.App.Services;
using Kozy.Core;
using Kozy.Core.Models;

namespace Kozy.App.ViewModels;

public partial class TweaksViewModel : ObservableObject
{
    private readonly KozyService _service = KozyService.Instance;
    private readonly List<TweakItemViewModel> _allItems = [];

    public ObservableCollection<TweakItemViewModel> FilteredItems { get; } = [];
    public ObservableCollection<string> Categories { get; } = [];

    [ObservableProperty]
    private string _selectedCategory = "Alle";

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string _statusMessage = "";

    [ObservableProperty]
    private int _selectedCount;

    public TweaksViewModel()
    {
        Categories.Add("Alle");
        foreach (var cat in _service.Catalog.GetCategories())
        {
            Categories.Add(cat);
        }

        foreach (var tweak in _service.Catalog.Tweaks)
        {
            var item = new TweakItemViewModel(tweak);
            item.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(TweakItemViewModel.IsSelected))
                {
                    UpdateSelectedCount();
                }
            };
            _allItems.Add(item);
        }

        ApplyFilter();
        _ = DetectAllAsync();
    }

    partial void OnSelectedCategoryChanged(string value) => ApplyFilter();
    partial void OnSearchTextChanged(string value) => ApplyFilter();

    private void UpdateSelectedCount()
    {
        SelectedCount = _allItems.Count(i => i.IsSelected);
    }

    private void ApplyFilter()
    {
        FilteredItems.Clear();

        var query = _allItems.AsEnumerable();

        if (!string.IsNullOrEmpty(SelectedCategory) && SelectedCategory != "Alle")
        {
            query = query.Where(i => i.Category.Equals(SelectedCategory, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            var term = SearchText.Trim();
            query = query.Where(i =>
                i.Name.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                i.Description.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                i.Id.Contains(term, StringComparison.OrdinalIgnoreCase));
        }

        foreach (var item in query)
        {
            FilteredItems.Add(item);
        }
    }

    [RelayCommand]
    public async Task DetectAllAsync()
    {
        IsBusy = true;
        StatusMessage = "Ermittle aktuellen Systemzustand...";

        await Task.Run(() =>
        {
            foreach (var item in _allItems)
            {
                var status = _service.Engine.Detect(item.Manifest);
                item.IsApplied = status == TweakStatus.Applied;
            }
        });

        StatusMessage = "Zustände aktualisiert.";
        IsBusy = false;
    }

    [RelayCommand]
    public void SelectPreset(string presetName)
    {
        var preset = presetName switch
        {
            "Safe" => TweakPreset.Safe,
            "Balanced" => TweakPreset.Balanced,
            "Max" => TweakPreset.Max,
            _ => TweakPreset.Safe,
        };

        var presetTweaks = _service.Catalog.GetByPreset(preset).Select(t => t.Id).ToHashSet();

        foreach (var item in _allItems)
        {
            item.IsSelected = presetTweaks.Contains(item.Id);
        }

        StatusMessage = $"Preset '{presetName}' gewählt ({SelectedCount} Optimierungen markiert).";
    }

    [RelayCommand]
    public void SelectAll()
    {
        foreach (var item in FilteredItems)
        {
            item.IsSelected = true;
        }
    }

    [RelayCommand]
    public void DeselectAll()
    {
        foreach (var item in _allItems)
        {
            item.IsSelected = false;
        }
    }

    [RelayCommand]
    public async Task ApplySelectedAsync()
    {
        var selected = _allItems.Where(i => i.IsSelected).Select(i => i.Manifest).ToList();
        if (selected.Count == 0)
        {
            StatusMessage = "Keine Optimierungen ausgewählt.";
            return;
        }

        IsBusy = true;
        StatusMessage = $"Wende {selected.Count} ausgewählte Optimierungen an...";

        var result = await Task.Run(() => _service.Engine.Apply(selected, new ApplyOptions(
            CreateRestorePoint: true,
            StopOnError: false
        )));

        StatusMessage = $"Fertig: {result.Applied} angewendet, {result.Skipped} übersprungen, {result.Failed} Fehler.";
        await DetectAllAsync();
        IsBusy = false;
    }

    [RelayCommand]
    public async Task RevertSelectedAsync()
    {
        var selected = _allItems.Where(i => i.IsSelected).Select(i => i.Id).ToHashSet();
        if (selected.Count == 0)
        {
            StatusMessage = "Keine Optimierungen zur Rücknahme ausgewählt.";
            return;
        }

        IsBusy = true;
        StatusMessage = "Nehme ausgewählte Optimierungen aus letzter Sitzung zurück...";

        var lastSessionFile = _service.JournalStore.EnumerateSessionFiles().LastOrDefault();
        if (lastSessionFile == null)
        {
            StatusMessage = "Keine vorherige Sitzung im Journal gefunden.";
            IsBusy = false;
            return;
        }

        await Task.Run(() =>
        {
            var session = _service.JournalStore.Load(lastSessionFile);
            _service.Engine.Revert(session, _service.Catalog.Tweaks);
        });

        StatusMessage = "Ausgewählte Optimierungen wurden zurückgesetzt.";
        await DetectAllAsync();
        IsBusy = false;
    }
}
