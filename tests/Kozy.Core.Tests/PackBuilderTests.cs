using Kozy.Core;
using Kozy.Core.Models;
using System.Text.Json;
using Xunit;

namespace Kozy.Core.Tests;

public sealed class PackBuilderTests
{
    [Fact]
    public void Build_ValidPacks_CreatesManifestAndSignature()
    {
        using var tempDir = new TempDirectory();
        var sourceDir = Path.Combine(tempDir.Path, "source");
        var outputDir = Path.Combine(tempDir.Path, "output");
        Directory.CreateDirectory(sourceDir);

        // Create a sample tweak with a valid action
        var tweak = new TweakManifest
        {
            Id = "test.sample",
            Name = new LocalizedText { De = "Test" },
            Description = new LocalizedText { De = "Test tweak" },
            Category = "test",
            Tier = Tier.Basic,
            Actions = new TweakAction[]
            {
                new RegistrySetAction
                {
                    Hive = "HKCU",
                    Path = @"Software\Test",
                    Name = "TestValue",
                    Kind = RegistryValueKind.DWord,
                    Value = 1
                }
            }
        };
        File.WriteAllText(
            Path.Combine(sourceDir, "test.json"),
            JsonSerializer.Serialize(tweak, ManifestSerializer.Options));

        // Generate test key
        var rng = new Random(42);
        var privateKey = new byte[32];
        rng.NextBytes(privateKey);
        var encodedKey = Convert.ToBase64String(privateKey);

        var options = new PackBuildOptions
        {
            SourceDirectory = sourceDir,
            OutputDirectory = outputDir,
            Version = "1.0.0",
            Channel = "stable",
            KeyId = "test-key",
            SigningKey = encodedKey
        };

        var result = PackBuilder.Build(options);

        Assert.Equal(1, result.ManifestCount);
        Assert.True(File.Exists(result.ManifestPath));
        Assert.True(File.Exists(result.SignaturePath));
        Assert.Single(result.PackPaths);
    }

    [Fact]
    public void Build_NonExistentDirectory_ThrowsDirectoryNotFoundException()
    {
        var options = new PackBuildOptions
        {
            SourceDirectory = "C:\\NonExistent",
            OutputDirectory = "C:\\Output",
            Version = "1.0.0",
            Channel = "stable",
            KeyId = "key",
            SigningKey = Convert.ToBase64String(new byte[32])
        };

        Assert.Throws<DirectoryNotFoundException>(() => PackBuilder.Build(options));
    }

    [Fact]
    public void Build_EmptyDirectory_ThrowsInvalidOperationException()
    {
        using var tempDir = new TempDirectory();
        var sourceDir = Path.Combine(tempDir.Path, "empty");
        Directory.CreateDirectory(sourceDir);

        var options = new PackBuildOptions
        {
            SourceDirectory = sourceDir,
            OutputDirectory = Path.Combine(tempDir.Path, "output"),
            Version = "1.0.0",
            Channel = "stable",
            KeyId = "key",
            SigningKey = Convert.ToBase64String(new byte[32])
        };

        var ex = Assert.Throws<InvalidOperationException>(() => PackBuilder.Build(options));
        Assert.Contains("Keine Tweak-Manifeste gefunden", ex.Message);
    }
}

file sealed class TempDirectory : IDisposable
{
    public string Path { get; }

    public TempDirectory()
    {
        Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"kozy-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(Path);
    }

    public void Dispose()
    {
        if (Directory.Exists(Path))
        {
            try
            {
                Directory.Delete(Path, recursive: true);
            }
            catch
            {
                // Best effort cleanup
            }
        }
    }
}
