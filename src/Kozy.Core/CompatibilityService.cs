using Kozy.Core.Models;

namespace Kozy.Core;

public sealed record SystemInfo
{
    public required int OsBuild { get; init; }
    public required string Edition { get; init; }
    public required DeviceClass DeviceClass { get; init; }
    public GpuVendor? GpuVendor { get; init; }
}

public static class CompatibilityService
{
    public static bool IsCompatible(TweakManifest manifest, SystemInfo system)
    {
        var s = manifest.Supports;
        if (s is null)
        {
            return true;
        }

        if (system.OsBuild < s.MinBuild)
        {
            return false;
        }

        if (s.MaxBuild is int max && system.OsBuild > max)
        {
            return false;
        }

        if (s.Editions.Count > 0 && !s.Editions.Contains(system.Edition))
        {
            return false;
        }

        if (s.DeviceClass.Count > 0 && !s.DeviceClass.Contains(system.DeviceClass))
        {
            return false;
        }

        if (s.GpuVendor is GpuVendor gpu && system.GpuVendor != gpu)
        {
            return false;
        }

        return true;
    }

    public static IEnumerable<TweakManifest> Filter(IEnumerable<TweakManifest> manifests, SystemInfo system) =>
        manifests.Where(m => IsCompatible(m, system));
}