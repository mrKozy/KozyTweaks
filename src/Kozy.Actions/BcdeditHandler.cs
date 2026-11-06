using Kozy.Core;
using Kozy.Core.Models;

namespace Kozy.Actions;

public sealed record BcdSnapshot(bool Exists, string? Value);

public sealed class BcdeditHandler : IActionHandler
{
    public void Validate(TweakAction action)
    {
        var element = ElementOf(action);
        if (!Allowlists.IsBcdElementAllowed(element))
        {
            throw new InvalidOperationException($"BCD-Element '{element}' ist nicht erlaubt (Kapitel 5.6).");
        }
    }

    public object? Capture(TweakAction action)
    {
        var element = ElementOf(action);
        var value = ParseElement(Enum().StdOut, element);
        return value is null ? new BcdSnapshot(false, null) : new BcdSnapshot(true, value);
    }

    public void Apply(TweakAction action)
    {
        switch (action)
        {
            case BcdeditSetAction s:
                Run("bcdedit.exe", "/set", "{current}", s.Element, s.Value);
                break;
            case BcdeditDeleteAction d:
                Run("bcdedit.exe", "/deletevalue", "{current}", d.Element);
                break;
            default:
                throw new ArgumentException($"BcdeditHandler kann '{action.GetType().Name}' nicht verarbeiten.");
        }
    }

    public bool Verify(TweakAction action)
    {
        var element = ElementOf(action);
        var current = ParseElement(Enum().StdOut, element);

        return action switch
        {
            BcdeditSetAction s => string.Equals(current, s.Value, StringComparison.OrdinalIgnoreCase),
            BcdeditDeleteAction => current is null,
            _ => false,
        };
    }

    public void Restore(TweakAction action, object? captured)
    {
        var snap = (BcdSnapshot)(captured ?? throw new InvalidOperationException("Captured-Zustand fehlt."));
        var element = ElementOf(action);

        if (snap.Exists)
        {
            Run("bcdedit.exe", "/set", "{current}", element, snap.Value!);
        }
        else
        {
            Run("bcdedit.exe", "/deletevalue", "{current}", element);
        }
    }

    public TweakStatus Detect(TweakAction action) => Verify(action) ? TweakStatus.Applied : TweakStatus.NotApplied;

    public static string? ParseElement(string output, string element)
    {
        foreach (var raw in output.Split('\n'))
        {
            var line = raw.Trim();
            if (!line.StartsWith(element, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var rest = line[element.Length..].Trim();
            return rest.Length == 0 ? null : rest;
        }

        return null;
    }

    private static string ElementOf(TweakAction a) => a switch
    {
        BcdeditSetAction s => s.Element,
        BcdeditDeleteAction d => d.Element,
        _ => throw new ArgumentException($"BcdeditHandler kann '{a.GetType().Name}' nicht verarbeiten."),
    };

    private static CommandResult Enum() => Run("bcdedit.exe", "/enum", "{current}");

    private static CommandResult Run(string tool, params string[] args) => WindowsCommandRunner.Run(tool, args);
}