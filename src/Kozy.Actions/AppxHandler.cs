using System.Text.RegularExpressions;
using Kozy.Core;
using Kozy.Core.Models;

namespace Kozy.Actions;

public sealed record AppxSnapshot(IReadOnlyList<string> Packages);

public sealed class AppxHandler : IActionHandler
{
    private readonly Func<string, CommandResult> _powershell;
    private readonly Func<string, string?> _installedFullName;

    public AppxHandler(
        Func<string, CommandResult>? powershell = null,
        Func<string, string?>? installedFullName = null)
    {
        _powershell = powershell ?? RunPs;
        _installedFullName = installedFullName ?? (name => GetInstalledFullName(_powershell, name));
    }

    public void Validate(TweakAction action)
    {
        var appx = AsAppx(action);
        foreach (var name in appx.Names)
        {
            if (Allowlists.IsAppxProtected(name))
            {
                throw new InvalidOperationException($"Paket '{name}' ist geschützt und darf nicht entfernt werden.");
            }

            if (!Regex.IsMatch(name, @"^[A-Za-z0-9._-]+$"))
            {
                throw new InvalidOperationException($"Ungültiger Paketname '{name}'.");
            }
        }
    }

    public object? Capture(TweakAction action)
    {
        var appx = AsAppx(action);
        var installed = appx.Names
            .Select(_installedFullName)
            .Where(f => f is not null)
            .Cast<string>()
            .ToList();

        return new AppxSnapshot(installed);
    }

    public void Apply(TweakAction action)
    {
        var appx = AsAppx(action);
        var allUsers = appx.AllUsers ? " -AllUsers" : string.Empty;

        foreach (var name in appx.Names)
        {
            _powershell($"Get-AppxPackage -Name '{name}' | Remove-AppxPackage{allUsers}");
        }
    }

    public bool Verify(TweakAction action) => AsAppx(action).Names.All(n => _installedFullName(n) is null);

    public void Restore(TweakAction action, object? captured) =>
        throw new NotSupportedException("Appx-Entfernung ist irreversibel und wird nicht zurückgenommen.");

    public TweakStatus Detect(TweakAction action) => Verify(action) ? TweakStatus.Applied : TweakStatus.NotApplied;

    private static AppxRemoveAction AsAppx(TweakAction a) =>
        a as AppxRemoveAction ?? throw new ArgumentException($"AppxHandler kann '{a.GetType().Name}' nicht verarbeiten.");

    private static string? GetInstalledFullName(Func<string, CommandResult> powershell, string name)
    {
        var result = powershell($"(Get-AppxPackage -Name '{name}').PackageFullName");
        var value = result.StdOut.Trim();
        return string.IsNullOrEmpty(value) ? null : value;
    }

    private static CommandResult RunPs(string script) =>
        WindowsCommandRunner.Run("powershell.exe", "-NoProfile", "-Command", script);
}
