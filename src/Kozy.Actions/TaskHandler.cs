using System.Text.RegularExpressions;
using Kozy.Core;
using Kozy.Core.Models;

namespace Kozy.Actions;

public sealed class TaskHandler : IActionHandler
{
    public void Validate(TweakAction action)
    {
        var task = AsTask(action);
        if (!Regex.IsMatch(task.Path, @"^\\[A-Za-z0-9 _.\-\\]+$"))
        {
            throw new InvalidOperationException($"Task-Pfad '{task.Path}' enthält ungültige Zeichen.");
        }
    }

    public object? Capture(TweakAction action) => GetState(AsTask(action).Path);

    public void Apply(TweakAction action)
    {
        var task = AsTask(action);
        var (path, name) = Split(task.Path);
        RunPs($"Disable-ScheduledTask -TaskPath '{path}' -TaskName '{name}'");
    }

    public bool Verify(TweakAction action) => GetState(AsTask(action).Path) == "Disabled";

    public void Restore(TweakAction action, object? captured)
    {
        var task = AsTask(action);
        var (path, name) = Split(task.Path);
        var state = captured as string;

        if (state == "Disabled")
        {
            RunPs($"Disable-ScheduledTask -TaskPath '{path}' -TaskName '{name}'");
        }
        else
        {
            RunPs($"Enable-ScheduledTask -TaskPath '{path}' -TaskName '{name}'");
        }
    }

    public TweakStatus Detect(TweakAction action) => Verify(action) ? TweakStatus.Applied : TweakStatus.NotApplied;

    private static TaskDisableAction AsTask(TweakAction a) =>
        a as TaskDisableAction ?? throw new ArgumentException($"TaskHandler kann '{a.GetType().Name}' nicht verarbeiten.");

    public static (string Path, string Name) Split(string full)
    {
        var trimmed = full.TrimEnd('\\');
        var idx = trimmed.LastIndexOf('\\');
        return idx < 0 ? (@"\", trimmed) : (trimmed[..(idx + 1)], trimmed[(idx + 1)..]);
    }

    private static string GetState(string full)
    {
        var (path, name) = Split(full);
        var result = RunPs($"$t = Get-ScheduledTask -TaskPath '{path}' -TaskName '{name}'; if ($t) {{ $t.State }}");
        return result.StdOut.Trim();
    }

    private static CommandResult RunPs(string script) =>
        WindowsCommandRunner.Run("powershell.exe", "-NoProfile", "-Command", script);
}