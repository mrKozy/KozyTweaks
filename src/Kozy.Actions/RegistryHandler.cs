using Kozy.Core;
using Kozy.Core.Models;
using Microsoft.Win32;
using RegistryValueKind = Kozy.Core.Models.RegistryValueKind;

namespace Kozy.Actions;

public sealed record RegistryValueSnapshot(bool Exists, string? Kind, object? Value);

/// <summary>
/// Handler für registry.set und registry.delete (Kapitel 5.3 / T-12).
/// Arbeitet mit 32-/64-Bit-Sicht und prüft jeden Pfad gegen die Allowlist (5.6).
/// </summary>
public sealed class RegistryHandler : IActionHandler
{
    public void Validate(TweakAction action)
    {
        switch (action)
        {
            case RegistrySetAction set:
                ValidatePath(set.Hive, set.Path);
                RegistryValueConverter.Validate(set.Kind, set.Value);
                break;
            case RegistryDeleteAction del:
                ValidatePath(del.Hive, del.Path);
                break;
            default:
                throw new ArgumentException($"RegistryHandler kann '{action.GetType().Name}' nicht verarbeiten.");
        }
    }

    public object? Capture(TweakAction action) => action switch
    {
        RegistrySetAction set => CaptureValue(set.Hive, set.Path, set.Name, set.View),
        RegistryDeleteAction del => CaptureValue(del.Hive, del.Path, del.Name, del.View),
        _ => throw new ArgumentException($"RegistryHandler kann '{action.GetType().Name}' nicht verarbeiten."),
    };

    public void Apply(TweakAction action)
    {
        switch (action)
        {
            case RegistrySetAction set:
                ApplySet(set);
                break;
            case RegistryDeleteAction del:
                ApplyDelete(del);
                break;
            default:
                throw new ArgumentException($"RegistryHandler kann '{action.GetType().Name}' nicht verarbeiten.");
        }
    }

    public bool Verify(TweakAction action) => action switch
    {
        RegistrySetAction set => VerifySet(set),
        RegistryDeleteAction del => !ValueExists(del.Hive, del.Path, del.Name, del.View),
        _ => throw new ArgumentException($"RegistryHandler kann '{action.GetType().Name}' nicht verarbeiten."),
    };

    public void Restore(TweakAction action, object? captured)
    {
        var snapshot = (captured as RegistryValueSnapshot)
            ?? throw new InvalidOperationException("Captured-Zustand fehlt oder hat den falschen Typ.");

        (string hive, string path, string name, int view) = action switch
        {
            RegistrySetAction set => (set.Hive, set.Path, set.Name, set.View),
            RegistryDeleteAction del => (del.Hive, del.Path, del.Name, del.View),
            _ => throw new ArgumentException($"RegistryHandler kann '{action.GetType().Name}' nicht verarbeiten."),
        };

        RestoreValue(hive, path, name, view, snapshot);
    }

    public TweakStatus Detect(TweakAction action) => action switch
    {
        RegistrySetAction set => VerifySet(set) ? TweakStatus.Applied : TweakStatus.NotApplied,
        RegistryDeleteAction del => ValueExists(del.Hive, del.Path, del.Name, del.View)
            ? TweakStatus.NotApplied
            : TweakStatus.Applied,
        _ => TweakStatus.Unknown,
    };

    private static void ValidatePath(string hive, string path)
    {
        if (!Allowlists.IsRegistryAllowed(hive, path))
        {
            throw new InvalidOperationException($"Registry-Pfad '{hive}\\{path}' ist nicht erlaubt (Allowlist, Kapitel 5.6).");
        }
    }

    private static RegistryValueSnapshot CaptureValue(string hive, string path, string name, int view)
    {
        using var key = OpenBase(hive, view).OpenSubKey(path);
        if (key is null)
        {
            return new RegistryValueSnapshot(false, null, null);
        }

        var value = key.GetValue(name, null);
        if (value is null)
        {
            return new RegistryValueSnapshot(false, null, null);
        }

        return new RegistryValueSnapshot(true, key.GetValueKind(name).ToString(), value);
    }

    private static bool ValueExists(string hive, string path, string name, int view)
    {
        using var key = OpenBase(hive, view).OpenSubKey(path);
        return key is not null && key.GetValue(name, null) is not null;
    }

    private static void ApplySet(RegistrySetAction a)
    {
        using var key = OpenBase(a.Hive, a.View).CreateSubKey(a.Path, true);
        key.SetValue(a.Name, RegistryValueConverter.Convert(a.Kind, a.Value), ToWinKind(a.Kind));
    }

    private static void ApplyDelete(RegistryDeleteAction a)
    {
        using var key = OpenBase(a.Hive, a.View).OpenSubKey(a.Path, true);
        key?.DeleteValue(a.Name, throwOnMissingValue: false);
    }

    private static bool VerifySet(RegistrySetAction a)
    {
        using var key = OpenBase(a.Hive, a.View).OpenSubKey(a.Path);
        if (key is null)
        {
            return false;
        }

        var current = key.GetValue(a.Name, null);
        if (current is null)
        {
            return false;
        }

        return key.GetValueKind(a.Name) == ToWinKind(a.Kind)
            && ValuesEqual(RegistryValueConverter.Convert(a.Kind, a.Value), current);
    }

    private static void RestoreValue(string hive, string path, string name, int view, RegistryValueSnapshot snapshot)
    {
        using var key = OpenBase(hive, view).OpenSubKey(path, true);

        if (!snapshot.Exists)
        {
            key?.DeleteValue(name, throwOnMissingValue: false);
            return;
        }

        var kind = ParseKind(snapshot.Kind);
        var value = RegistryValueConverter.Convert(kind, snapshot.Value ?? string.Empty);

        if (key is null)
        {
            using var created = OpenBase(hive, view).CreateSubKey(path, true);
            created.SetValue(name, value, ToWinKind(kind));
            return;
        }

        key.SetValue(name, value, ToWinKind(kind));
    }

    private static RegistryValueKind ParseKind(string? kind) =>
        Enum.TryParse<RegistryValueKind>(kind, ignoreCase: true, out var parsed)
            ? parsed
            : throw new InvalidOperationException($"Unbekannter gespeicherter Werttyp '{kind}'.");

    private static Microsoft.Win32.RegistryValueKind ToWinKind(RegistryValueKind kind) => kind switch
    {
        RegistryValueKind.DWord => Microsoft.Win32.RegistryValueKind.DWord,
        RegistryValueKind.QWord => Microsoft.Win32.RegistryValueKind.QWord,
        RegistryValueKind.String => Microsoft.Win32.RegistryValueKind.String,
        RegistryValueKind.ExpandString => Microsoft.Win32.RegistryValueKind.ExpandString,
        RegistryValueKind.Binary => Microsoft.Win32.RegistryValueKind.Binary,
        RegistryValueKind.MultiString => Microsoft.Win32.RegistryValueKind.MultiString,
        _ => throw new ArgumentException($"Unbekannter Werttyp '{kind}'."),
    };

    private static RegistryKey OpenBase(string hive, int view) => hive.ToUpperInvariant() switch
    {
        "HKCU" => RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, ToView(view)),
        "HKLM" => RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, ToView(view)),
        _ => throw new ArgumentException($"Ungültige Hive '{hive}'."),
    };

    private static RegistryView ToView(int view) => view == 32 ? RegistryView.Registry32 : RegistryView.Registry64;

    private static bool ValuesEqual(object expected, object actual)
    {
        if (expected is byte[] eb && actual is byte[] ab)
        {
            return eb.AsSpan().SequenceEqual(ab);
        }

        if (expected is string[] es && actual is string[] as_)
        {
            return es.SequenceEqual(as_);
        }

        return Equals(expected, actual);
    }
}