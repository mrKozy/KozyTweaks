using Kozy.Core;
using Kozy.Core.Models;
using Xunit;

namespace Kozy.Core.Tests;

public class DependencyResolverTests
{
    private static TweakManifest Tweak(string id, string[]? dependsOn = null, string[]? conflicts = null) => new()
    {
        Id = id,
        Name = new LocalizedText { De = id },
        Description = new LocalizedText { De = id },
        Category = "gaming",
        DependsOn = dependsOn ?? [],
        ConflictsWith = conflicts ?? [],
        Actions = [new RegistrySetAction { Hive = "HKCU", Path = "T", Name = id, Value = 0 }],
    };

    [Fact]
    public void Dependency_Comes_Before_Dependent()
    {
        var a = Tweak("a.b", dependsOn: ["c.d"]);
        var b = Tweak("b.c");
        var c = Tweak("c.d");

        var order = DependencyResolver.Resolve([a, b, c]).Select(m => m.Id).ToList();

        Assert.True(order.IndexOf("c.d") < order.IndexOf("a.b"));
        Assert.Contains("b.c", order);
    }

    [Fact]
    public void Missing_Dependency_Throws()
    {
        var a = Tweak("a.b", dependsOn: ["missing.dep"]);
        Assert.Throws<DependencyException>(() => DependencyResolver.Resolve([a]));
    }

    [Fact]
    public void Cycle_Throws()
    {
        var a = Tweak("a.b", dependsOn: ["b.c"]);
        var b = Tweak("b.c", dependsOn: ["a.b"]);
        Assert.Throws<DependencyException>(() => DependencyResolver.Resolve([a, b]));
    }

    [Fact]
    public void Conflict_Throws()
    {
        var a = Tweak("a.b", conflicts: ["c.d"]);
        var c = Tweak("c.d");
        Assert.Throws<DependencyException>(() => DependencyResolver.Resolve([a, c]));
    }
}