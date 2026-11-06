using System.Diagnostics;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Kozy.App.Services;

namespace Kozy.App.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly KozyService _service = KozyService.Instance;

    public string AppVersion => "1.0.0 (Open Source Edition)";
    public string DataDirectory => _service.AppDataDirectory;

    [RelayCommand]
    public void OpenDataFolder()
    {
        try
        {
            Directory.CreateDirectory(_service.AppDataDirectory);
            Process.Start(new ProcessStartInfo
            {
                FileName = _service.AppDataDirectory,
                UseShellExecute = true,
            });
        }
        catch { }
    }

    [RelayCommand]
    public void OpenGitHub()
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "https://github.com",
                UseShellExecute = true,
            });
        }
        catch { }
    }
}
