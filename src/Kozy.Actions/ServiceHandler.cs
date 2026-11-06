using System.Text.RegularExpressions;
using Kozy.Core;
using Kozy.Core.Models;

namespace Kozy.Actions;

public sealed record ServiceSnapshot(int StartType, bool Running)
{
    public static ServiceSnapshot Read(string name, Func<string[], CommandResult>? runSc = null)
    {
        runSc ??= DefaultRunSc;

        var qc = runSc(new[] { "qc", name });
        var startType = ExtractInt(qc.StdOut, "START_TYPE") ?? 3;

        var query = runSc(new[] { "query", name });
        var state = ExtractInt(query.StdOut, "STATE") ?? 1;

        return new ServiceSnapshot(startType, state == 4);
    }

    private static CommandResult DefaultRunSc(string[] arguments) =>
        WindowsCommandRunner.Run("sc.exe", arguments);

    private static int? ExtractInt(string output, string label)
    {
        foreach (var raw in output.Split('\n'))
        {
            var line = raw.Trim();
            var idx = line.IndexOf(label, StringComparison.OrdinalIgnoreCase);
            if (idx < 0)
            {
                continue;
            }

            var rest = line[(idx + label.Length)..];
            var match = Regex.Match(rest, @"\d+");
            if (match.Success)
            {
                return int.Parse(match.Value);
            }
        }

        return null;
    }
}

public sealed class ServiceHandler : IActionHandler
{
    private readonly Func<string, ServiceSnapshot> _readSnapshot;
    private readonly Func<string[], CommandResult> _runSc;

    public ServiceHandler(
        Func<string, ServiceSnapshot>? readSnapshot = null,
        Func<string[], CommandResult>? runSc = null)
    {
        _runSc = runSc ?? DefaultRunSc;
        _readSnapshot = readSnapshot ?? (name => ServiceSnapshot.Read(name, _runSc));
    }

    public void Validate(TweakAction action)
    {
        var svc = AsService(action);
        if (!Allowlists.IsServiceAllowed(svc.Name))
        {
            throw new InvalidOperationException($"Dienst '{svc.Name}' ist nicht in der erlaubten Liste (Kapitel 5.6).");
        }
    }

    public object? Capture(TweakAction action) => _readSnapshot(AsService(action).Name);

    public void Apply(TweakAction action)
    {
        var svc = AsService(action);
        var start = svc.StartMode switch
        {
            ServiceStartMode.Auto => "auto",
            ServiceStartMode.Manual => "demand",
            _ => "disabled",
        };

        var config = _runSc(new[] { "config", svc.Name, "start=", start });
        if (config.ExitCode != 0)
        {
            throw new InvalidOperationException($"sc config fehlgeschlagen: {config.StdErr.Trim()}");
        }

        if (svc.Stop)
        {
            _runSc(new[] { "stop", svc.Name });
        }
    }

    public bool Verify(TweakAction action)
    {
        var svc = AsService(action);
        var snap = _readSnapshot(svc.Name);
        var expected = svc.StartMode switch
        {
            ServiceStartMode.Auto => 2,
            ServiceStartMode.Manual => 3,
            _ => 4,
        };

        return snap.StartType == expected && (!svc.Stop || !snap.Running);
    }

    public void Restore(TweakAction action, object? captured)
    {
        var svc = AsService(action);
        var snap = (ServiceSnapshot)(captured ?? throw new InvalidOperationException("Captured-Zustand fehlt."));

        var start = snap.StartType switch { 2 => "auto", 4 => "disabled", _ => "demand" };
        _runSc(new[] { "config", svc.Name, "start=", start });
        if (snap.Running)
        {
            _runSc(new[] { "start", svc.Name });
        }
    }

    public TweakStatus Detect(TweakAction action) => Verify(action) ? TweakStatus.Applied : TweakStatus.NotApplied;

    private static ServiceConfigAction AsService(TweakAction a) =>
        a as ServiceConfigAction ?? throw new ArgumentException($"ServiceHandler kann '{a.GetType().Name}' nicht verarbeiten.");

    private static CommandResult DefaultRunSc(string[] arguments) =>
        WindowsCommandRunner.Run("sc.exe", arguments);
}
