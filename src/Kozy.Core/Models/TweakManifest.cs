namespace Kozy.Core.Models;

public sealed record TweakManifest
{
    public int SchemaVersion { get; init; } = 1;

    /// <summary>Globale, unveränderliche ID (Regex siehe ManifestValidator).</summary>
    public required string Id { get; init; }

    public int Version { get; init; } = 1;

    public required LocalizedText Name { get; init; }

    public required LocalizedText Description { get; init; }

    public required string Category { get; init; }

    public Tier Tier { get; init; }

    public Risk Risk { get; init; } = Risk.Safe;

    public bool RequiresReboot { get; init; }

    public TweakSupports? Supports { get; init; }

    public IReadOnlyList<string> ConflictsWith { get; init; } = [];

    public IReadOnlyList<string> DependsOn { get; init; } = [];

    public bool Irreversible { get; init; }

    public BenchmarkHint BenchmarkHint { get; init; } = BenchmarkHint.None;

    public required IReadOnlyList<TweakAction> Actions { get; init; }
}