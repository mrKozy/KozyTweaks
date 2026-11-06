using System.Text.RegularExpressions;
using Kozy.Core;
using Kozy.Core.Models;
using Microsoft.Win32;

namespace Kozy.Actions;

public sealed record PowerValueSnapshot(int Ac, int Dc);
public sealed record PowerSchemeSnapshot(string ActiveSchemeGuid);
public sealed record HibernateSnapshot(bool Enabled);

public sealed class PowercfgHandler : IActionHandler
{
    public void Validate(TweakAction action)
    {
        _ = action switch
        {
            PowercfgValueAction => 0,
            PowercfgSchemeAction => 0,
            PowercfgHibernateAction => 0,
            _ => throw new ArgumentException($"PowercfgHandler kann '{action.GetType().Name}' nicht verarbeiten."),
        };
    }

    public object? Capture(TweakAction action) => action switch
    {
        PowercfgValueAction v => CaptureValue(v),
        PowercfgSchemeAction => CaptureScheme(),
        PowercfgHibernateAction => CaptureHibernate(),
        _ => throw new ArgumentException($"PowercfgHandler kann '{action.GetType().Name}' nicht verarbeiten."),
    };

    public void Apply(TweakAction action)
    {
        switch (action)
        {
            case PowercfgValueAction v:
                ApplyValue(v);
                break;
            case PowercfgSchemeAction s:
                ApplyScheme(s);
                break;
            case PowercfgHibernateAction h:
                ApplyHibernate(h.Enabled);
                break;
            default:
                throw new ArgumentException($"PowercfgHandler kann '{action.GetType().Name}' nicht verarbeiten.");
        }
    }

    public bool Verify(TweakAction action) => action switch
    {
        PowercfgValueAction v => VerifyValue(v),
        PowercfgHibernateAction h => ReadHibernateEnabled() == h.Enabled,
        // Schema-Änderungen werden in T-15 auf der VM verifiziert.
        PowercfgSchemeAction => true,
        _ => false,
    };

    public void Restore(TweakAction action, object? captured)
    {
        switch (action)
        {
            case PowercfgValueAction v:
                RestoreValue(v, (PowerValueSnapshot)(captured ?? throw new InvalidOperationException("Captured-Zustand fehlt.")));
                break;
            case PowercfgSchemeAction:
                RestoreScheme((PowerSchemeSnapshot)(captured ?? throw new InvalidOperationException("Captured-Zustand fehlt.")));
                break;
            case PowercfgHibernateAction:
                ApplyHibernate(((HibernateSnapshot)(captured ?? throw new InvalidOperationException("Captured-Zustand fehlt."))).Enabled);
                break;
            default:
                throw new ArgumentException($"PowercfgHandler kann '{action.GetType().Name}' nicht verarbeiten.");
        }
    }

    public TweakStatus Detect(TweakAction action) => Verify(action) ? TweakStatus.Applied : TweakStatus.NotApplied;

    // --- Wert-Indizes (AC/DC) ---

    private static string SchemeGuid(string scheme) =>
        scheme.Equals("ACTIVE", StringComparison.OrdinalIgnoreCase) ? ActiveScheme() : scheme;

    private static PowerValueSnapshot CaptureValue(PowercfgValueAction v)
    {
        var result = Run("powercfg.exe", "/query", SchemeGuid(v.Scheme), v.Subgroup, v.Setting);
        var (ac, dc) = ParseAcDcIndex(result.StdOut);
        return new PowerValueSnapshot(ac, dc);
    }

    private static void ApplyValue(PowercfgValueAction v)
    {
        var guid = SchemeGuid(v.Scheme);
        if (v.Dc is int dc)
        {
            Run("powercfg.exe", "/setdcvalueindex", guid, v.Subgroup, v.Setting, dc.ToString());
        }

        Run("powercfg.exe", "/setacvalueindex", guid, v.Subgroup, v.Setting, v.Ac.ToString());
        Run("powercfg.exe", "/setactive", guid);
    }

    private static bool VerifyValue(PowercfgValueAction v)
    {
        var result = Run("powercfg.exe", "/query", SchemeGuid(v.Scheme), v.Subgroup, v.Setting);
        var (ac, dc) = ParseAcDcIndex(result.StdOut);
        return ac == v.Ac && (v.Dc is null || dc == v.Dc.Value);
    }

    private static void RestoreValue(PowercfgValueAction v, PowerValueSnapshot snap)
    {
        var guid = SchemeGuid(v.Scheme);
        Run("powercfg.exe", "/setdcvalueindex", guid, v.Subgroup, v.Setting, snap.Dc.ToString());
        Run("powercfg.exe", "/setacvalueindex", guid, v.Subgroup, v.Setting, snap.Ac.ToString());
        Run("powercfg.exe", "/setactive", guid);
    }

    // --- Schema ---

    private static PowerSchemeSnapshot CaptureScheme() => new(ActiveScheme());

    private static void ApplyScheme(PowercfgSchemeAction s)
    {
        if (s.Op == PowerSchemeOp.Duplicate)
        {
            if (s.SourceGuid is null)
            {
                throw new InvalidOperationException("sourceGuid fehlt für 'duplicate'.");
            }

            var result = Run("powercfg.exe", "/duplicatescheme", s.SourceGuid);
            var newGuid = ParseFirstGuid(result.StdOut);
            if (newGuid is not null)
            {
                if (s.Name is not null)
                {
                    Run("powercfg.exe", "/changename", newGuid, s.Name);
                }

                Run("powercfg.exe", "/setactive", newGuid);
            }
        }
        else
        {
            if (s.SourceGuid is null)
            {
                throw new InvalidOperationException("sourceGuid fehlt für 'setactive'.");
            }

            Run("powercfg.exe", "/setactive", s.SourceGuid);
        }
    }

    private static void RestoreScheme(PowerSchemeSnapshot snap) =>
        Run("powercfg.exe", "/setactive", snap.ActiveSchemeGuid);

    // --- Ruhezustand ---

    private static HibernateSnapshot CaptureHibernate() => new(ReadHibernateEnabled());

    private static bool ReadHibernateEnabled()
    {
        using var key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\Power");
        return key?.GetValue("HibernateEnabled") is int v && v != 0;
    }

    private static void ApplyHibernate(bool enabled) =>
        Run("powercfg.exe", "/hibernate", enabled ? "on" : "off");

    // --- Parser (auch für Tests) ---

    public static (int Ac, int Dc) ParseAcDcIndex(string output)
    {
        var ac = 0;
        var dc = 0;

        foreach (var raw in output.Split('\n'))
        {
            var line = raw.Trim();
            if (line.StartsWith("Current AC Power Setting Index", StringComparison.OrdinalIgnoreCase))
            {
                ac = ParseIndexValue(line);
            }
            else if (line.StartsWith("Current DC Power Setting Index", StringComparison.OrdinalIgnoreCase))
            {
                dc = ParseIndexValue(line);
            }
        }

        return (ac, dc);
    }

    public static string? ParseFirstGuid(string output)
    {
        var match = Regex.Match(output, @"[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}");
        return match.Success ? match.Value : null;
    }

    public static string? ParseActiveSchemeGuid(string output) => ParseFirstGuid(output);

    private static int ParseIndexValue(string line)
    {
        var match = Regex.Match(line, @"0x([0-9a-fA-F]+)");
        return match.Success ? Convert.ToInt32(match.Groups[1].Value, 16) : 0;
    }

    private static string ActiveScheme()
    {
        var result = Run("powercfg.exe", "/getactivescheme");
        return ParseActiveSchemeGuid(result.StdOut)
            ?? throw new InvalidOperationException("Aktives Energieschema konnte nicht ermittelt werden.");
    }

    private static CommandResult Run(string tool, params string[] args) => WindowsCommandRunner.Run(tool, args);
}