using CommunityToolkit.Mvvm.ComponentModel;
using Kozy.Core.Models;

namespace Kozy.App.Models;

public partial class TweakItemViewModel : ObservableObject
{
    public TweakManifest Manifest { get; }

    public string Id => Manifest.Id;
    public string Name => Manifest.Name.De;
    public string Description => Manifest.Description.De;
    public string Category => Manifest.Category;
    public Risk Risk => Manifest.Risk;
    public Tier Tier => Manifest.Tier;
    public bool RequiresReboot => Manifest.RequiresReboot;
    public bool Irreversible => Manifest.Irreversible;

    public string RiskLabel => Risk switch
    {
        Risk.Safe => "Sicher",
        Risk.Moderate => "Ausgewogen",
        Risk.Advanced => "Erweitert",
        Risk.Experimental => "Experimentell",
        _ => "Unbekannt",
    };

    public string RiskBadgeColor => Risk switch
    {
        Risk.Safe => "#107C41",         // Fluent Green
        Risk.Moderate => "#D83B01",     // Fluent Orange/Amber
        Risk.Advanced => "#C239B3",     // Fluent Magenta
        Risk.Experimental => "#D13438", // Fluent Red
        _ => "#8A8886",
    };

    [ObservableProperty]
    private bool _isSelected;

    [ObservableProperty]
    private bool _isApplied;

    [ObservableProperty]
    private bool _isBusy;

    public TweakItemViewModel(TweakManifest manifest, bool isApplied = false)
    {
        Manifest = manifest;
        _isApplied = isApplied;
    }
}
