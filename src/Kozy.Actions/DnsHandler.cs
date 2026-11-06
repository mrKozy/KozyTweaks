using System.Net;
using Kozy.Core;
using Kozy.Core.Models;

namespace Kozy.Actions;

public sealed record DnsSnapshot(IReadOnlyList<(int InterfaceIndex, string[] Servers)> Adapters);

public sealed class DnsHandler : IActionHandler
{
    private readonly Func<string, CommandResult> _powershell;
    private readonly Func<int[]> _activeInterfaceIndices;
    private readonly Func<int, string[]> _currentServers;

    public DnsHandler(
        Func<string, CommandResult>? powershell = null,
        Func<int[]>? activeInterfaceIndices = null,
        Func<int, string[]>? currentServers = null)
    {
        _powershell = powershell ?? RunPs;
        _activeInterfaceIndices = activeInterfaceIndices ?? (() => ActiveInterfaceIndices(_powershell));
        _currentServers = currentServers ?? (index => CurrentServers(_powershell, index));
    }

    public void Validate(TweakAction action)
    {
        var dns = AsDns(action);
        if (dns.Servers.Count == 0)
        {
            throw new InvalidOperationException("Mindestens ein DNS-Server erforderlich.");
        }

        foreach (var server in dns.Servers)
        {
            if (!IPAddress.TryParse(server, out _))
            {
                throw new InvalidOperationException($"Ungültige IP-Adresse '{server}'.");
            }
        }
    }

    public object? Capture(TweakAction action)
    {
        var list = _activeInterfaceIndices().Select(i => (i, _currentServers(i))).ToList();
        return new DnsSnapshot(list);
    }

    public void Apply(TweakAction action)
    {
        var dns = AsDns(action);
        var list = string.Join("','", dns.Servers);
        foreach (var index in _activeInterfaceIndices())
        {
            _powershell($"Set-DnsClientServerAddress -InterfaceIndex {index} -ServerAddresses ('{list}')");
        }
    }

    public bool Verify(TweakAction action)
    {
        var dns = AsDns(action);
        return _activeInterfaceIndices().All(i => _currentServers(i).SequenceEqual(dns.Servers));
    }

    public void Restore(TweakAction action, object? captured)
    {
        var snap = (DnsSnapshot)(captured ?? throw new InvalidOperationException("Captured-Zustand fehlt."));

        foreach (var (index, servers) in snap.Adapters)
        {
            if (servers.Length == 0)
            {
                _powershell($"Set-DnsClientServerAddress -InterfaceIndex {index} -ResetServerAddresses");
            }
            else
            {
                var list = string.Join("','", servers);
                _powershell($"Set-DnsClientServerAddress -InterfaceIndex {index} -ServerAddresses ('{list}')");
            }
        }
    }

    public TweakStatus Detect(TweakAction action) => Verify(action) ? TweakStatus.Applied : TweakStatus.NotApplied;

    private static DnsSetAction AsDns(TweakAction a) =>
        a as DnsSetAction ?? throw new ArgumentException($"DnsHandler kann '{a.GetType().Name}' nicht verarbeiten.");

    private static int[] ActiveInterfaceIndices(Func<string, CommandResult> powershell)
    {
        var result = powershell("(Get-NetAdapter -Physical | Where-Object {$_.Status -eq 'Up'}).ifIndex");
        return result.StdOut
            .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(s => int.TryParse(s.Trim(), out var i) ? i : -1)
            .Where(i => i > 0)
            .ToArray();
    }

    private static string[] CurrentServers(Func<string, CommandResult> powershell, int index)
    {
        var result = powershell($"(Get-DnsClientServerAddress -InterfaceIndex {index} -AddressFamily IPv4).ServerAddresses");
        return result.StdOut
            .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(s => s.Trim())
            .Where(s => s.Length > 0)
            .ToArray();
    }

    private static CommandResult RunPs(string script) =>
        WindowsCommandRunner.Run("powershell.exe", "-NoProfile", "-Command", script);
}
