using System.Text.Json;
using Kozy.Core;
using Kozy.Core.Models;
using Xunit;

namespace Kozy.Core.Tests;

public class ManifestSerializerTests
{
    [Fact]
    public void Deserializes_Manifest_And_Polymorphic_Action()
    {
        const string json = """
        {
          "schemaVersion": 1,
          "id": "gaming.disable-gamedvr",
          "version": 1,
          "name": { "de": "Game-DVR deaktivieren" },
          "description": { "de": "Schaltet Hintergrundaufnahme ab." },
          "category": "gaming",
          "tier": "basic",
          "risk": "safe",
          "actions": [
            { "type": "registry.set", "hive": "HKCU", "path": "Test", "name": "GameDVR_Enabled", "kind": "DWord", "value": 0 }
          ]
        }
        """;

        var manifest = ManifestSerializer.Deserialize(json);

        Assert.Equal("gaming.disable-gamedvr", manifest.Id);
        Assert.Equal(Tier.Basic, manifest.Tier);
        Assert.Equal(Risk.Safe, manifest.Risk);

        var action = Assert.IsType<RegistrySetAction>(Assert.Single(manifest.Actions));
        Assert.Equal(RegistryValueKind.DWord, action.Kind);
        Assert.Equal(0, ((JsonElement)action.Value).GetInt32());
    }

    [Fact]
    public void Unknown_Action_Type_Is_Rejected()
    {
        const string json = """
        {
          "schemaVersion": 1,
          "id": "gaming.test",
          "name": { "de": "x" },
          "description": { "de": "x" },
          "category": "gaming",
          "tier": "basic",
          "actions": [ { "type": "fancy.newaction" } ]
        }
        """;

        Assert.NotNull(Record.Exception(() => ManifestSerializer.Deserialize(json)));
    }
}