using Kozy.Core;
using Kozy.Core.Models;
using Xunit;

namespace Kozy.Core.Tests;

public class EngineTests
{
    private sealed class FakeHandler : IActionHandler
    {
        public object? Captured { get; set; } = "state-before";
        public bool ApplyFails;
        public TweakStatus DetectStatus = TweakStatus.NotApplied;
        public List<string> Log { get; } = [];

        public void Validate(TweakAction action) => Log.Add("validate");
        public object? Capture(TweakAction action) { Log.Add("capture"); return Captured; }
        public void Apply(TweakAction action) { Log.Add("apply"); if (ApplyFails) throw new InvalidOperationException("apply"); }
        public bool Verify(TweakAction action) { Log.Add("verify"); return true; }
        public void Restore(TweakAction action, object? captured) { Log.Add("restore"); }
        public TweakStatus Detect(TweakAction action) { Log.Add("detect"); return DetectStatus; }
    }

    private sealed class FakeSystem : ISystemInfoProvider
    {
        public SystemInfo Info { get; set; } = new() { OsBuild = 26100, Edition = "Pro", DeviceClass = DeviceClass.Desktop };
        public SystemInfo Get() => Info;
    }

    private sealed class FakeRestore : IRestorePointService
    {
        public int CreatedCount;
        public RestorePointInfo? Create(string description)
        {
            CreatedCount++;
            return new RestorePointInfo { Created = true, SequenceNumber = 1 };
        }
    }

    private sealed class Ctx : IDisposable
    {
        public required Engine Engine { get; init; }
        public required FakeHandler Set { get; init; }
        public required FakeHandler Delete { get; init; }
        public required JournalStore Store { get; init; }
        public required string Dir { get; init; }

        public void Dispose()
        {
            if (Directory.Exists(Dir))
            {
                Directory.Delete(Dir, true);
            }
        }
    }

    private static TweakManifest Tweak(string id, params TweakAction[] actions) => new()
    {
        Id = id,
        Name = new LocalizedText { De = id },
        Description = new LocalizedText { De = id },
        Category = "gaming",
        Actions = actions,
    };

    private static RegistrySetAction Reg() => new() { Hive = "HKCU", Path = "Test", Name = "X", Value = 0 };
    private static RegistryDeleteAction Del() => new() { Hive = "HKCU", Path = "Test", Name = "Y" };

    private static Ctx Create()
    {
        var dir = Path.Combine(Path.GetTempPath(), "kozy-engine-" + Guid.NewGuid().ToString("N"));
        var set = new FakeHandler();
        var del = new FakeHandler();
        var store = new JournalStore(dir);
        var engine = new Engine(
            new Dictionary<Type, IActionHandler>
            {
                [typeof(RegistrySetAction)] = set,
                [typeof(RegistryDeleteAction)] = del,
            },
            new FakeSystem(),
            new FakeRestore(),
            store,
            "1.0.0",
            () => new DateTimeOffset(2026, 11, 5, 12, 0, 0, TimeSpan.Zero));

        return new Ctx { Engine = engine, Set = set, Delete = del, Store = store, Dir = dir };
    }

    [Fact]
    public void Apply_Success_Writes_Journal()
    {
        using var ctx = Create();
        var result = ctx.Engine.Apply([Tweak("gaming.test", Reg())]);

        Assert.Equal(1, result.Applied);
        Assert.Equal(0, result.Failed);
        Assert.Equal("applied", Assert.Single(result.Session.Tweaks).Status);
        Assert.Contains("apply", ctx.Set.Log);
        Assert.Single(ctx.Store.EnumerateSessionFiles());
    }

    [Fact]
    public void Apply_Failure_Rolls_Back()
    {
        using var ctx = Create();
        ctx.Set.ApplyFails = true;

        var result = ctx.Engine.Apply([Tweak("gaming.test", Reg())]);

        Assert.Equal(0, result.Applied);
        Assert.Equal(1, result.Failed);
        Assert.Equal("failed", Assert.Single(result.Session.Tweaks).Status);
        Assert.Contains("restore", ctx.Set.Log);
    }

    [Fact]
    public void Apply_Skips_Already_Applied()
    {
        using var ctx = Create();
        ctx.Set.DetectStatus = TweakStatus.Applied;

        var result = ctx.Engine.Apply([Tweak("gaming.test", Reg())]);

        Assert.Equal(0, result.Applied);
        Assert.Equal(1, result.Skipped);
        Assert.DoesNotContain("apply", ctx.Set.Log);
    }

    [Fact]
    public void Partial_Failure_Restores_Applied_Actions()
    {
        using var ctx = Create();
        ctx.Delete.ApplyFails = true;

        var result = ctx.Engine.Apply([Tweak("gaming.test", Reg(), Del())]);

        Assert.Equal(1, result.Failed);
        Assert.Contains("restore", ctx.Set.Log);
        Assert.Contains("restore", ctx.Delete.Log);
    }

    [Fact]
    public void Revert_Restores_Applied_Actions()
    {
        using var ctx = Create();
        var tweak = Tweak("gaming.test", Reg());
        var result = ctx.Engine.Apply([tweak]);

        var reverted = ctx.Engine.Revert(result.Session, new Dictionary<string, TweakManifest> { [tweak.Id] = tweak });

        Assert.Equal(1, reverted.Restored);
        Assert.Contains("restore", ctx.Set.Log);
    }

    [Fact]
    public void Revert_Skips_Irreversible()
    {
        using var ctx = Create();
        var tweak = Tweak("gaming.test", Reg()) with { Irreversible = true };
        var result = ctx.Engine.Apply([tweak]);

        var reverted = ctx.Engine.Revert(result.Session, new Dictionary<string, TweakManifest> { [tweak.Id] = tweak });

        Assert.Equal(0, reverted.Restored);
        Assert.Equal(1, reverted.SkippedIrreversible);
    }
}