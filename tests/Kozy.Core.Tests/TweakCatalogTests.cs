using Kozy.Core;
using Kozy.Core.Models;
using Xunit;

namespace Kozy.Core.Tests;

public class TweakCatalogTests : IDisposable
{
    private readonly string _tempDir;

    public TweakCatalogTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"kozy-catalog-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, true);
        }
    }

    [Fact]
    public void LoadFromDirectory_FiltersByPresetCorrectly()
    {
        var safeTweak = new TweakManifest
        {
            Id = "test.safe",
            Name = new LocalizedText { De = "Safe Tweak" },
            Description = new LocalizedText { De = "Description" },
            Category = "gaming",
            Risk = Risk.Safe,
            Actions = [new RegistrySetAction { Hive = "HKCU", Path = "P", Name = "N", Value = 1 }],
        };

        var modTweak = new TweakManifest
        {
            Id = "test.moderate",
            Name = new LocalizedText { De = "Mod Tweak" },
            Description = new LocalizedText { De = "Description" },
            Category = "gaming",
            Risk = Risk.Moderate,
            Actions = [new RegistrySetAction { Hive = "HKCU", Path = "P", Name = "N", Value = 1 }],
        };

        var expTweak = new TweakManifest
        {
            Id = "test.experimental",
            Name = new LocalizedText { De = "Exp Tweak" },
            Description = new LocalizedText { De = "Description" },
            Category = "gaming",
            Risk = Risk.Experimental,
            Actions = [new RegistrySetAction { Hive = "HKCU", Path = "P", Name = "N", Value = 1 }],
        };

        File.WriteAllText(Path.Combine(_tempDir, "safe.json"), System.Text.Json.JsonSerializer.Serialize(safeTweak, ManifestSerializer.Options));
        File.WriteAllText(Path.Combine(_tempDir, "mod.json"), System.Text.Json.JsonSerializer.Serialize(modTweak, ManifestSerializer.Options));
        File.WriteAllText(Path.Combine(_tempDir, "exp.json"), System.Text.Json.JsonSerializer.Serialize(expTweak, ManifestSerializer.Options));

        var catalog = TweakCatalog.LoadFromDirectory(_tempDir);

        Assert.Equal(3, catalog.Tweaks.Count);

        var safe = catalog.GetByPreset(TweakPreset.Safe);
        Assert.Single(safe);
        Assert.Equal("test.safe", safe[0].Id);

        var balanced = catalog.GetByPreset(TweakPreset.Balanced);
        Assert.Equal(2, balanced.Count);

        var max = catalog.GetByPreset(TweakPreset.Max);
        Assert.Equal(2, max.Count); // Experimental is never included in presets
    }
}
