using Kozy.Core.Models;

namespace Kozy.Core;

public interface ISystemInfoProvider
{
    SystemInfo Get();
}

public sealed record ApplyOptions(bool CreateRestorePoint = true, bool StopOnError = true);

public sealed record ApplyResult(JournalSession Session, int Applied, int Failed, int Skipped);

public sealed record RevertResult(int Restored, int SkippedIrreversible, int MissingManifest);

public sealed class VerifyFailedException : Exception
{
    public VerifyFailedException(string message) : base(message) { }
}

/// <summary>
/// Führt den verbindlichen Apply/Revert-Algorithmus aus Kapitel 5.5 aus.
/// Konkrete Systemzugriffe sind in IActionHandler gekapselt, dadurch sind
/// die Szenarien mit Fake-Handlern testbar.
/// </summary>
public sealed class Engine
{
    private readonly IReadOnlyDictionary<Type, IActionHandler> _handlers;
    private readonly ISystemInfoProvider _system;
    private readonly IRestorePointService _restorePoints;
    private readonly JournalStore _journalStore;
    private readonly string _appVersion;
    private readonly Func<DateTimeOffset> _clock;

    public Engine(
        IReadOnlyDictionary<Type, IActionHandler> handlers,
        ISystemInfoProvider system,
        IRestorePointService restorePoints,
        JournalStore journalStore,
        string appVersion,
        Func<DateTimeOffset>? clock = null)
    {
        _handlers = handlers;
        _system = system;
        _restorePoints = restorePoints;
        _journalStore = journalStore;
        _appVersion = appVersion;
        _clock = clock ?? (() => DateTimeOffset.UtcNow);
    }

    public ApplyResult Apply(IReadOnlyCollection<TweakManifest> selected, ApplyOptions? options = null)
    {
        options ??= new ApplyOptions();

        var system = _system.Get();
        var ordered = DependencyResolver.Resolve(
            CompatibilityService.Filter(selected, system).ToList());

        RestorePointInfo? restorePoint = null;
        if (options.CreateRestorePoint)
        {
            restorePoint = _restorePoints.Create($"kozy - {ordered.Count} Tweaks");
        }

        var session = new JournalSession
        {
            SessionId = Guid.NewGuid().ToString("N"),
            StartedAt = _clock(),
            AppVersion = _appVersion,
            OsBuild = system.OsBuild,
            RestorePoint = restorePoint,
            Tweaks = new List<JournalTweak>(),
        };

        var tweakEntries = (List<JournalTweak>)session.Tweaks;
        var applied = 0;
        var failed = 0;
        var skipped = 0;

        foreach (var tweak in ordered)
        {
            if (DetectTweak(tweak) == TweakStatus.Applied)
            {
                skipped++;
                continue;
            }

            var entries = new List<JournalAction>();
            var stack = new Stack<(TweakAction Action, object? Captured)>();

            try
            {
                for (var i = 0; i < tweak.Actions.Count; i++)
                {
                    var action = tweak.Actions[i];
                    var handler = HandlerFor(action);

                    handler.Validate(action);
                    var captured = handler.Capture(action);
                    stack.Push((action, captured));

                    handler.Apply(action);
                    var verified = handler.Verify(action);

                    entries.Add(new JournalAction
                    {
                        Index = i,
                        Type = ActionTypeNames.Of(action),
                        Captured = captured,
                        Verified = verified,
                    });

                    if (!verified)
                    {
                        throw new VerifyFailedException($"Verify fehlgeschlagen für '{tweak.Id}'.");
                    }
                }

                applied++;
                tweakEntries.Add(new JournalTweak { Id = tweak.Id, Status = "applied", Actions = entries });
            }
            catch (Exception)
            {
                while (stack.Count > 0)
                {
                    var (action, captured) = stack.Pop();
                    HandlerFor(action).Restore(action, captured);
                }

                failed++;
                tweakEntries.Add(new JournalTweak { Id = tweak.Id, Status = "failed", Actions = entries });

                if (options.StopOnError)
                {
                    break;
                }
            }
        }

        _journalStore.Save(session);
        return new ApplyResult(session, applied, failed, skipped);
    }

    public RevertResult Revert(JournalSession session, IEnumerable<TweakManifest> manifests) =>
        Revert(session, manifests.ToDictionary(m => m.Id, StringComparer.OrdinalIgnoreCase));

    public RevertResult Revert(JournalSession session, IReadOnlyDictionary<string, TweakManifest> manifests)
    {
        var restored = 0;
        var skippedIrreversible = 0;
        var missing = 0;

        foreach (var tweakEntry in session.Tweaks.Reverse())
        {
            if (!manifests.TryGetValue(tweakEntry.Id, out var manifest))
            {
                missing++;
                continue;
            }

            if (manifest.Irreversible)
            {
                skippedIrreversible++;
                continue;
            }

            foreach (var jAction in tweakEntry.Actions
                         .Where(a => a.Verified)
                         .OrderByDescending(a => a.Index))
            {
                if (jAction.Index < 0 || jAction.Index >= manifest.Actions.Count)
                {
                    continue;
                }

                var action = manifest.Actions[jAction.Index];
                HandlerFor(action).Restore(action, jAction.Captured);
                restored++;
            }
        }

        return new RevertResult(restored, skippedIrreversible, missing);
    }

    public TweakStatus Detect(TweakManifest tweak) => DetectTweak(tweak);

    private TweakStatus DetectTweak(TweakManifest tweak)
    {
        var statuses = tweak.Actions.Select(a => HandlerFor(a).Detect(a)).ToList();

        if (statuses.All(s => s == TweakStatus.Applied))
        {
            return TweakStatus.Applied;
        }

        if (statuses.All(s => s == TweakStatus.NotApplied))
        {
            return TweakStatus.NotApplied;
        }

        if (statuses.Any(s => s == TweakStatus.Unknown))
        {
            return TweakStatus.Unknown;
        }

        return TweakStatus.Partial;
    }

    private IActionHandler HandlerFor(TweakAction action)
    {
        if (_handlers.TryGetValue(action.GetType(), out var handler))
        {
            return handler;
        }

        throw new NotSupportedException($"Kein Handler für Aktionstyp '{action.GetType().Name}' registriert.");
    }
}