namespace Kozy.Core.Models;

/// <summary>Kompatibilitätsangaben eines Tweaks (Kapitel 5.2, Feld "supports").</summary>
public sealed record TweakSupports
{
    public int MinBuild { get; init; }

    /// <summary>null = nach oben offen.</summary>
    public int? MaxBuild { get; init; }

    public IReadOnlyList<string> Editions { get; init; } = [];

    public IReadOnlyList<DeviceClass> DeviceClass { get; init; } = [];

    public GpuVendor? GpuVendor { get; init; }
}