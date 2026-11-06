using CommunityToolkit.Mvvm.ComponentModel;

namespace Kozy.App.ViewModels;

public partial class MainViewModel : ObservableObject
{
    public DashboardViewModel Dashboard { get; } = new();
    public TweaksViewModel Tweaks { get; } = new();
    public CleanerViewModel Cleaner { get; } = new();
    public AutostartViewModel Autostart { get; } = new();
    public BenchmarkViewModel Benchmark { get; } = new();
    public HistoryViewModel History { get; } = new();
    public SettingsViewModel Settings { get; } = new();

    [ObservableProperty]
    private object _currentView;

    public MainViewModel()
    {
        _currentView = Dashboard;
    }

    public void NavigateTo(string pageName)
    {
        CurrentView = pageName switch
        {
            "Dashboard" => Dashboard,
            "Tweaks" => Tweaks,
            "Cleaner" => Cleaner,
            "Autostart" => Autostart,
            "Benchmark" => Benchmark,
            "History" => History,
            "Settings" => Settings,
            _ => Dashboard,
        };
    }
}
