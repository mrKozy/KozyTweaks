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

    public string FormattedTime => Session.StartedAt.ToLocalTime().ToString("dd.MM.yyyy HH:mm:ss");
    public string Details => $"{Session.Tweaks.Count} Optimierungen · OS Build {Session.OsBuild}";

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
                // Unlesbare Journal-Dateien überspringen
            }
        }

        StatusMessage = $"{Sessions.Count} Sitzung(en) im Verlauf gefunden.";
        IsBusy = false;
    }

    [RelayCommand]
    public async Task RollbackSessionAsync(SessionItemViewModel? sessionVm)
    {
        var target = sessionVm ?? SelectedSession;
        if (target == null)
        {
            StatusMessage = "Bitte wählen Sie eine Sitzung zum Zurücksetzen aus.";
            return;
        }

        IsBusy = true;
        StatusMessage = $"Setze Sitzung vom {target.FormattedTime} zurück...";

        await Task.Run(() =>
        {
            _service.Engine.Revert(target.Session, _service.Catalog.Tweaks);
        });

        StatusMessage = "Sitzung erfolgreich zurückgesetzt!";
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
            StatusMessage = $"Fehler beim Öffnen der Systemwiederherstellung: {ex.Message}";
        }
    }
}
