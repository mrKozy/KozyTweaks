using System.Text;
using System.Text.Json;
using Kozy.Core;
using Kozy.Core.Models;
using Xunit;

namespace Kozy.Core.Tests;

public sealed class PackInstallationServiceTests : IDisposable
{
    private sealed class FakeHandler : IActionHandler
    {
        public bool ApplyFails { get; set; }
        public TweakStatus DetectStatus { get; set; } = TweakStatus.NotApplied;

        public void Validate(TweakAction action) { }
        public object? Capture(TweakAction action) => "captured-value";
        public void Apply(TweakAction action)
        {
            if (ApplyFails)
            {
                throw new InvalidOperationException("Simulated apply failure");
            }
        }
        public bool Verify(TweakAction action) => true;
        public void Restore(TweakAction action, object? captured) { }
        public TweakStatus Detect(TweakAction action) => DetectStatus;
    }

    private sealed class FakeSystem : ISystemInfoProvider
    {
        public SystemInfo Get() => new()
        {
            OsBuild = 26100,
            Edition = "Pro",
            DeviceClass = DeviceClass.Desktop,
        };
    }

    private sealed class FakeRestore : IRestorePointService
    {
        public RestorePointInfo? Create(string description) =>
            new() { Created = true, SequenceNumber = 1 };
    }

    private readonly string _testDir;
    private readonly string _versionsFile;
    private readonly string _journalDir;
    private readonly FakeHandler _handler;
    private readonly Engine _engine;

    public PackInstallationServiceTests()
    {
        _testDir = Path.Combine(Path.GetTempPath(), $"kozy-install-test-{Guid.NewGuid()}");
        Directory.CreateDirectory(_testDir);
        _versionsFile = Path.Combine(_testDir, "pack-versions.json");
        _journalDir = Path.Combine(_testDir, "journal");

        _handler = new FakeHandler();
        var handlers = new Dictionary<Type, IActionHandler>
        {
            [typeof(RegistrySetAction)] = _handler,
        };

        _engine = new Engine(
            handlers,
            new FakeSystem(),
            new FakeRestore(),
            new JournalStore(_journalDir),
            "1.0.0");
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDir))
        {
            Directory.Delete(_testDir, recursive: true);
        }
    }

    [Fact]
    public void GetInstalledVersions_WhenFileDoesNotExist_ReturnsEmptyDictionary()
    {
        var service = new PackInstallationService(_engine, _versionsFile);
        var versions = service.GetInstalledVersions();

        Assert.Empty(versions);
    }

    [Fact]
    public void GetInstalledVersions_WhenFileIsInvalidJson_ReturnsEmptyDictionary()
    {
        File.WriteAllText(_versionsFile, "{ not-valid-json");
        var service = new PackInstallationService(_engine, _versionsFile);
        var versions = service.GetInstalledVersions();

        Assert.Empty(versions);
    }

    [Fact]
    public void Install_ValidPack_AppliesTweaksAndSavesInstalledVersion()
    {
        var service = new PackInstallationService(_engine, _versionsFile);

        var tweak = new TweakManifest
        {
            Id = "gaming.test-tweak",
            Name = new LocalizedText { De = "Test" },
            Description = new LocalizedText { De = "Test Description" },
            Category = "gaming",
            Tier = Tier.Basic,
            Actions =
            [
                new RegistrySetAction
                {
                    Hive = "HKCU",
                    Path = "Software\\Test",
                    Name = "Key1",
                    Value = 1,
                }
            ],
        };

        var packJson = JsonSerializer.Serialize(new List<TweakManifest> { tweak });
        var packBytes = Encoding.UTF8.GetBytes(packJson);
        var packFile = "pack-basic-1.0.0.json";

        var updateResult = new PackUpdateResult
        {
            Channel = "stable",
            FetchedAt = DateTimeOffset.UtcNow,
            Manifest = new PackManifest
            {
                Channel = "stable",
                GeneratedAt = DateTimeOffset.UtcNow,
                Kid = "test-kid",
                Packs =
                [
                    new PackManifestEntry
                    {
                        Tier = Tier.Basic,
                        Version = "1.0.0",
                        File = packFile,
                        Sha256 = "dummy",
                        Size = packBytes.Length,
                    }
                ],
            },
            PackFiles = new Dictionary<string, byte[]>
            {
                [packFile] = packBytes,
            },
        };

        var result = service.Install(updateResult, new ApplyOptions(CreateRestorePoint: false));

        Assert.True(result.OverallSuccess);
        Assert.Equal(1, result.TotalApplied);
        Assert.Equal(0, result.TotalFailed);
        Assert.Equal(0, result.TotalSkipped);

        var versions = service.GetInstalledVersions();
        Assert.True(versions.ContainsKey(Tier.Basic));
        Assert.Equal("1.0.0", versions[Tier.Basic]);
    }

    [Fact]
    public void Install_MissingPackFile_MarksFailed()
    {
        var service = new PackInstallationService(_engine, _versionsFile);

        var updateResult = new PackUpdateResult
        {
            Channel = "stable",
            FetchedAt = DateTimeOffset.UtcNow,
            Manifest = new PackManifest
            {
                Channel = "stable",
                GeneratedAt = DateTimeOffset.UtcNow,
                Kid = "test-kid",
                Packs =
                [
                    new PackManifestEntry
                    {
                        Tier = Tier.Basic,
                        Version = "1.0.0",
                        File = "missing-pack.json",
                        Sha256 = "dummy",
                        Size = 10,
                    }
                ],
            },
            PackFiles = new Dictionary<string, byte[]>(),
        };

        var result = service.Install(updateResult);

        Assert.False(result.OverallSuccess);
        Assert.Single(result.TierResults);
        Assert.False(result.TierResults[0].Success);
        Assert.Contains("missing-pack.json", result.TierResults[0].Error);
    }

    [Fact]
    public void Install_EmptyPack_SucceedsWithZeroApplied()
    {
        var service = new PackInstallationService(_engine, _versionsFile);

        var packBytes = Encoding.UTF8.GetBytes("[]");
        var packFile = "pack-basic-1.0.0.json";

        var updateResult = new PackUpdateResult
        {
            Channel = "stable",
            FetchedAt = DateTimeOffset.UtcNow,
            Manifest = new PackManifest
            {
                Channel = "stable",
                GeneratedAt = DateTimeOffset.UtcNow,
                Kid = "test-kid",
                Packs =
                [
                    new PackManifestEntry
                    {
                        Tier = Tier.Basic,
                        Version = "1.0.0",
                        File = packFile,
                        Sha256 = "dummy",
                        Size = packBytes.Length,
                    }
                ],
            },
            PackFiles = new Dictionary<string, byte[]>
            {
                [packFile] = packBytes,
            },
        };

        var result = service.Install(updateResult);

        Assert.True(result.OverallSuccess);
        Assert.Equal(0, result.TotalApplied);
    }

    [Fact]
    public void Install_WhenApplyFailsWithStopOnError_AbortsRemainingPacks()
    {
        _handler.ApplyFails = true;
        var service = new PackInstallationService(_engine, _versionsFile);

        var tweak1 = new TweakManifest
        {
            Id = "gaming.tweak1",
            Name = new LocalizedText { De = "T1" },
            Description = new LocalizedText { De = "D1" },
            Category = "gaming",
            Tier = Tier.Basic,
            Actions = [new RegistrySetAction { Hive = "HKCU", Path = "P", Name = "N", Value = 1 }],
        };

        var pack1Bytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new List<TweakManifest> { tweak1 }));
        var pack2Bytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new List<TweakManifest> { tweak1 }));

        var updateResult = new PackUpdateResult
        {
            Channel = "stable",
            FetchedAt = DateTimeOffset.UtcNow,
            Manifest = new PackManifest
            {
                Channel = "stable",
                GeneratedAt = DateTimeOffset.UtcNow,
                Kid = "test-kid",
                Packs =
                [
                    new PackManifestEntry
                    {
                        Tier = Tier.Basic,
                        Version = "1.0.0",
                        File = "pack1.json",
                        Sha256 = "dummy",
                        Size = pack1Bytes.Length,
                    },
                    new PackManifestEntry
                    {
                        Tier = Tier.Pro,
                        Version = "1.0.0",
                        File = "pack2.json",
                        Sha256 = "dummy",
                        Size = pack2Bytes.Length,
                    }
                ],
            },
            PackFiles = new Dictionary<string, byte[]>
            {
                ["pack1.json"] = pack1Bytes,
                ["pack2.json"] = pack2Bytes,
            },
        };

        var result = service.Install(updateResult, new ApplyOptions(StopOnError: true, CreateRestorePoint: false));

        Assert.False(result.OverallSuccess);
        Assert.Single(result.TierResults); // Second pack was aborted due to StopOnError
    }
}
