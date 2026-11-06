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
    private string _selectedCategory = "All";

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
        Categories.Add("All");
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

        if (!string.IsNullOrEmpty(SelectedCategory) && SelectedCategory != "All")
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
        StatusMessage = "Detecting current system status...";

        await Task.Run(() =>
        {
            foreach (var item in _allItems)
            {
                var status = _service.Engine.Detect(item.Manifest);
                item.IsApplied = status == TweakStatus.Applied;
            }
        });

        StatusMessage = "Statuses updated.";
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

        StatusMessage = $"Preset '{presetName}' selected ({SelectedCount} optimization(s) marked).";
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
            StatusMessage = "No optimizations selected.";
            return;
        }

        IsBusy = true;
        StatusMessage = $"Applying {selected.Count} selected optimization(s)...";

        var result = await Task.Run(() => _service.Engine.Apply(selected, new ApplyOptions(
            CreateRestorePoint: true,
            StopOnError: false
        )));

        StatusMessage = $"Completed: {result.Applied} applied, {result.Skipped} skipped, {result.Failed} failed.";
        await DetectAllAsync();
        IsBusy = false;
    }

    [RelayCommand]
    public async Task RevertSelectedAsync()
    {
        var selected = _allItems.Where(i => i.IsSelected).Select(i => i.Id).ToHashSet();
        if (selected.Count == 0)
        {
            StatusMessage = "No optimizations selected to revert.";
            return;
        }

        IsBusy = true;
        StatusMessage = "Reverting selected optimizations from previous session...";

        var lastSessionFile = _service.JournalStore.EnumerateSessionFiles().LastOrDefault();
        if (lastSessionFile == null)
        {
            StatusMessage = "No previous session found in journal.";
            IsBusy = false;
            return;
        }

        await Task.Run(() =>
        {
            var session = _service.JournalStore.Load(lastSessionFile);
            _service.Engine.Revert(session, _service.Catalog.Tweaks);
        });

        StatusMessage = "Selected optimizations have been reverted.";
        await DetectAllAsync();
        IsBusy = false;
    }
}
