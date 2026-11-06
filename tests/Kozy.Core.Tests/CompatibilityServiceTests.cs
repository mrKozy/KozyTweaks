using Kozy.Core;
using Kozy.Core.Models;
using Xunit;

namespace Kozy.Core.Tests;

public class CompatibilityServiceTests
{
    private static TweakManifest Tweak(TweakSupports supports) => new()
    {
        Id = "x.test",
        Name = new LocalizedText { De = "x" },
        Description = new LocalizedText { De = "x" },
        Category = "gaming",
        Supports = supports,
        Actions = [new RegistrySetAction { Hive = "HKCU", Path = "T", Name = "N", Value = 0 }],
    };

    private static SystemInfo Sys(int build = 26100, string edition = "Pro", DeviceClass dc = DeviceClass.Desktop, GpuVendor? gpu = GpuVendor.Nvidia) =>
        new() { OsBuild = build, Edition = edition, DeviceClass = dc, GpuVendor = gpu };

    [Fact]
    public void Build_Below_Min_Is_Incompatible()
    {
        var t = Tweak(new TweakSupports { MinBuild = 22631 });
        Assert.False(CompatibilityService.IsCompatible(t, Sys(build: 22000)));
    }

    [Fact]
    public void Build_In_Range_Is_Compatible()
    {
        var t = Tweak(new TweakSupports { MinBuild = 19045, MaxBuild = 30000 });
        Assert.True(CompatibilityService.IsCompatible(t, Sys(build: 26100)));
    }

    [Fact]
    public void Build_Above_Max_Is_Incompatible()
    {
        var t = Tweak(new TweakSupports { MinBuild = 19045, MaxBuild = 26100 });
        Assert.False(CompatibilityService.IsCompatible(t, Sys(build: 26200)));
    }

    [Fact]
    public void Edition_Mismatch_Is_Incompatible()
    {
        var t = Tweak(new TweakSupports { MinBuild = 19045, Editions = ["Home"] });
        Assert.False(CompatibilityService.IsCompatible(t, Sys(edition: "Pro")));
    }

    [Fact]
    public void DeviceClass_Mismatch_Is_Incompatible()
    {
        var t = Tweak(new TweakSupports { MinBuild = 19045, DeviceClass = [DeviceClass.Laptop] });
        Assert.False(CompatibilityService.IsCompatible(t, Sys(dc: DeviceClass.Desktop)));
    }

    [Fact]
    public void GpuVendor_Mismatch_Is_Incompatible()
    {
        var t = Tweak(new TweakSupports { MinBuild = 19045, GpuVendor = GpuVendor.Amd });
        Assert.False(CompatibilityService.IsCompatible(t, Sys(gpu: GpuVendor.Nvidia)));
    }
}