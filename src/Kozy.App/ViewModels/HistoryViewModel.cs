using System.Collections.ObjectModel;
using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Kozy.App.Services;
using Kozy.Core.Models;

namespace Kozy.App.ViewModels;

public partial class SessionItemViewModel : ObservableObject
{
    public JournalSession Session { get; }
    public string FilePath { get; }

    public string FormattedTime => Session.StartedAt.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss");
    public string Details => $"{Session.Tweaks.Count} optimizations · OS Build {Session.OsBuild}";

    public SessionItemViewModel(JournalSession session, string filePath)
    {
        Session = session;
        FilePath = filePath;
    }
}

public partial class HistoryViewModel : ObservableObject
{
    private readonly KozyService _service = KozyService.Instance;

    public ObservableCollection<SessionItemViewModel> Sessions { get; } = [];

    [ObservableProperty]
    private SessionItemViewModel? _selectedSession;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string _statusMessage = "";

    public HistoryViewModel()
    {
        Refresh();
    }

    [RelayCommand]
    public void Refresh()
    {
        IsBusy = true;
        Sessions.Clear();

        foreach (var file in _service.JournalStore.EnumerateSessionFiles().Reverse())
        {
            try
            {
                var session = _service.JournalStore.Load(file);
                Sessions.Add(new SessionItemViewModel(session, file));
            }
            catch
            {
                // Skip unreadable journal files
            }
        }

        StatusMessage = $"{Sessions.Count} session(s) found in history.";
        IsBusy = false;
    }

    [RelayCommand]
    public async Task RollbackSessionAsync(SessionItemViewModel? sessionVm)
    {
        var target = sessionVm ?? SelectedSession;
        if (target == null)
        {
            StatusMessage = "Please select a session to rollback.";
            return;
        }

        IsBusy = true;
        StatusMessage = $"Reverting session from {target.FormattedTime}...";

        await Task.Run(() =>
        {
            _service.Engine.Revert(target.Session, _service.Catalog.Tweaks);
        });

        StatusMessage = "Session reverted successfully!";
        Refresh();
        IsBusy = false;
    }

    [RelayCommand]
    public void OpenSystemRestore()
    {
        try
        {
            Process.Start(new ProcessStartInfo("rstrui.exe") { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error opening System Restore: {ex.Message}";
        }
    }
}
