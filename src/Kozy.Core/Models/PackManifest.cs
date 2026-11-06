namespace Kozy.Core.Models;

public sealed record PackManifest
{
    public required string Channel { get; init; }

    public required DateTimeOffset GeneratedAt { get; init; }

    public required string Kid { get; init; }

    public required IReadOnlyList<PackManifestEntry> Packs { get; init; }
}

public sealed record PackManifestEntry
{
    public required Tier Tier { get; init; }

    public required string Version { get; init; }

    public required string Sha256 { get; init; }

    public required long Size { get; init; }

    public required string File { get; init; }
}
