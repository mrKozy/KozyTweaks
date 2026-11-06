using Microsoft.Win32;

namespace Kozy.Actions;

public enum StartupScope
{
    CurrentUser,
    LocalMachine,
}

public sealed record StartupEntry
{
    public required string Name { get; init; }
    public required string Command { get; init; }
    public required StartupScope Scope { get; init; }
    public required bool IsEnabled { get; init; }
}

/// <summary>
/// Verwaltet Autostart-Programme über die Windows-Registry (HKCU/HKLM Run und StartupApproved\Run).
/// Einträge werden nie gelöscht, sondern nur über das Status-Byte (0x02 aktiv, 0x03 deaktiviert) gesteuert.
/// </summary>
public sealed class StartupManager
{
    public const string DefaultRunSubKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
    public const string DefaultApprovedSubKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\Run";

    private readonly string _runSubKey;
    private readonly string _approvedSubKey;

    public StartupManager(string? runSubKey = null, string? approvedSubKey = null)
    {
        _runSubKey = runSubKey ?? DefaultRunSubKey;
        _approvedSubKey = approvedSubKey ?? DefaultApprovedSubKey;
    }

    public IReadOnlyList<StartupEntry> GetAll()
    {
        var list = new List<StartupEntry>();

        list.AddRange(GetFromHive(Registry.CurrentUser, StartupScope.CurrentUser));
        list.AddRange(GetFromHive(Registry.LocalMachine, StartupScope.LocalMachine));

        return list;
    }

    public void SetEnabled(StartupEntry entry, bool enabled)
    {
        var root = entry.Scope == StartupScope.CurrentUser ? Registry.CurrentUser : Registry.LocalMachine;
        SetEnabledInHive(root, entry.Name, enabled);
    }

    public void SetEnabled(StartupScope scope, string name, bool enabled)
    {
        var root = scope == StartupScope.CurrentUser ? Registry.CurrentUser : Registry.LocalMachine;
        SetEnabledInHive(root, name, enabled);
    }

    private IEnumerable<StartupEntry> GetFromHive(RegistryKey root, StartupScope scope)
    {
        using var runKey = root.OpenSubKey(_runSubKey, false);
        if (runKey == null)
        {
            yield break;
        }

        using var approvedKey = root.OpenSubKey(_approvedSubKey, false);

        foreach (var valueName in runKey.GetValueNames())
        {
            var command = runKey.GetValue(valueName)?.ToString() ?? string.Empty;
            var isEnabled = true;

            if (approvedKey?.GetValue(valueName) is byte[] bytes && bytes.Length > 0)
            {
                // 0x02 = aktiv, 0x03 = deaktiviert
                isEnabled = bytes[0] != 0x03;
            }

            yield return new StartupEntry
            {
                Name = valueName,
                Command = command,
                Scope = scope,
                IsEnabled = isEnabled,
            };
        }
    }

    private void SetEnabledInHive(RegistryKey root, string name, bool enabled)
    {
        using var approvedKey = root.CreateSubKey(_approvedSubKey, true);
        if (approvedKey == null)
        {
            return;
        }

        var existing = approvedKey.GetValue(name) as byte[];
        byte[] bytes;

        if (existing != null && existing.Length >= 1)
        {
            bytes = (byte[])existing.Clone();
            bytes[0] = enabled ? (byte)0x02 : (byte)0x03;
        }
        else
        {
            bytes = new byte[12];
            bytes[0] = enabled ? (byte)0x02 : (byte)0x03;
        }

        approvedKey.SetValue(name, bytes, RegistryValueKind.Binary);
    }
}
