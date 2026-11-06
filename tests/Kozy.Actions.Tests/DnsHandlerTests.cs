using System.Text.RegularExpressions;
using Kozy.Actions;
using Kozy.Core.Models;
using Xunit;

namespace Kozy.Actions.Tests;

public class DnsHandlerTests
{
    [Fact]
    public void Validate_Rejects_Invalid_Ip_Address()
    {
        var handler = new DnsHandler();
        var action = new DnsSetAction { Servers = ["1.1.1.1", "not-an-ip"] };

        var ex = Assert.Throws<InvalidOperationException>(() => handler.Validate(action));

        Assert.Contains("Ungültige IP-Adresse", ex.Message);
    }

    [Fact]
    public void Roundtrip_Captures_Applies_And_Restores_Dns_Settings()
    {
        var currentServers = new Dictionary<int, string[]>
        {
            [7] = ["9.9.9.9"],
            [11] = [],
        };

        var handler = new DnsHandler(
            powershell: script =>
            {
                var index = int.Parse(Regex.Match(script, @"-InterfaceIndex\s+(\d+)").Groups[1].Value);
                if (script.Contains("-ResetServerAddresses", StringComparison.Ordinal))
                {
                    currentServers[index] = [];
                }
                else
                {
                    currentServers[index] = Regex.Matches(script, @"'([^']+)'")
                        .Select(m => m.Groups[1].Value)
                        .ToArray();
                }

                return new CommandResult(0, string.Empty, string.Empty);
            },
            activeInterfaceIndices: () => [7, 11],
            currentServers: index => currentServers[index]);

        var action = new DnsSetAction { Servers = ["1.1.1.1", "1.0.0.1"] };

        var captured = (DnsSnapshot)handler.Capture(action)!;
        Assert.Equal("9.9.9.9", captured.Adapters[0].Servers.Single());
        Assert.Empty(captured.Adapters[1].Servers);

        handler.Apply(action);
        Assert.True(handler.Verify(action));

        handler.Restore(action, captured);

        Assert.Equal(["9.9.9.9"], currentServers[7]);
        Assert.Empty(currentServers[11]);
    }
}
