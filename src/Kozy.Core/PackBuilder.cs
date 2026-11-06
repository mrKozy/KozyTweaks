using System.Security.Cryptography;
using Kozy.Core.Models;

namespace Kozy.Core;

public sealed record PackBuildOptions
{
    public required string SourceDirectory { get; init; }

    public required string OutputDirectory { get; init; }

    public required string Version { get; init; }

    public string Channel { get; init; } = "stable";

    public required string KeyId { get; init; }

    public required string SigningKey { get; init; }

    public DateTimeOffset? GeneratedAt { get; init; }
}

public sealed record PackBuildResult
{
    public required int ManifestCount { get; init; }

    public required PackManifest Manifest { get; init; }

    public required string ManifestPath { get; init; }

    public required string SignaturePath { get; init; }

    public required IReadOnlyList<string> PackPaths { get; init; }
}

public static class PackBuilder
{
    public static PackBuildResult Build(PackBuildOptions options)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(options.SourceDirectory);
        ArgumentException.ThrowIfNullOrWhiteSpace(options.OutputDirectory);
        ArgumentException.ThrowIfNullOrWhiteSpace(options.Version);
        ArgumentException.ThrowIfNullOrWhiteSpace(options.Channel);
        ArgumentException.ThrowIfNullOrWhiteSpace(options.KeyId);
        ArgumentException.ThrowIfNullOrWhiteSpace(options.SigningKey);

        if (!Directory.Exists(options.SourceDirectory))
        {
            throw new DirectoryNotFoundException($"Quellordner nicht gefunden: {options.SourceDirectory}");
        }

        var manifests = LoadManifests(options.SourceDirectory);
        EnsureUniqueIds(manifests);

        Directory.CreateDirectory(options.OutputDirectory);

        var packPaths = new List<string>();
        var manifestEntries = new List<PackManifestEntry>();

        foreach (var group in manifests
                     .OrderBy(m => m.Tier)
                     .ThenBy(m => m.Id, StringComparer.Ordinal)
                     .GroupBy(m => m.Tier))
        {
            var fileName = $"pack-{group.Key.ToString().ToLowerInvariant()}-{options.Version}.json";
            var fullPath = Path.Combine(options.OutputDirectory, fileName);
            var packBytes = CanonicalJson.Serialize(group.ToArray(), ManifestSerializer.Options);

            File.WriteAllBytes(fullPath, packBytes);

            manifestEntries.Add(new PackManifestEntry
            {
                Tier = group.Key,
                Version = options.Version,
                Sha256 = Convert.ToHexStringLower(SHA256.HashData(packBytes)),
                Size = packBytes.LongLength,
                File = fileName,
            });
            packPaths.Add(fullPath);
        }

        var packManifest = new PackManifest
        {
            Channel = options.Channel,
            GeneratedAt = options.GeneratedAt ?? DateTimeOffset.UtcNow,
            Kid = options.KeyId,
            Packs = manifestEntries,
        };

        var manifestBytes = CanonicalJson.Serialize(packManifest, ManifestSerializer.Options);
        var manifestPath = Path.Combine(options.OutputDirectory, "manifest.json");
        File.WriteAllBytes(manifestPath, manifestBytes);

        var signaturePath = Path.Combine(options.OutputDirectory, "manifest.json.sig");
        var signature = PackSigningService.SignBytes(manifestBytes, options.SigningKey);
        File.WriteAllText(signaturePath, signature, Utf8NoBom);

        return new PackBuildResult
        {
            ManifestCount = manifests.Count,
            Manifest = packManifest,
            ManifestPath = manifestPath,
            SignaturePath = signaturePath,
            PackPaths = packPaths,
        };
    }

    private static List<TweakManifest> LoadManifests(string sourceDirectory)
    {
        var files = Directory.EnumerateFiles(sourceDirectory, "*.json", SearchOption.AllDirectories)
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (files.Length == 0)
        {
            throw new InvalidOperationException("Keine Tweak-Manifeste gefunden.");
        }

        var manifests = new List<TweakManifest>(files.Length);
        var errors = new List<string>();

        foreach (var file in files)
        {
            try
            {
                var manifest = ManifestSerializer.LoadFromFile(file);
                var validationErrors = ManifestValidator.Validate(manifest);
                if (validationErrors.Count > 0)
                {
                    errors.AddRange(validationErrors.Select(error => $"{file}: {error.Field}: {error.Message}"));
                    continue;
                }

                manifests.Add(manifest);
            }
            catch (Exception ex)
            {
                errors.Add($"{file}: {ex.Message}");
            }
        }

        if (errors.Count > 0)
        {
            throw new InvalidOperationException(string.Join(Environment.NewLine, errors));
        }

        return manifests;
    }

    private static void EnsureUniqueIds(IReadOnlyCollection<TweakManifest> manifests)
    {
        var duplicates = manifests
            .GroupBy(m => m.Id, StringComparer.Ordinal)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .OrderBy(id => id, StringComparer.Ordinal)
            .ToArray();

        if (duplicates.Length == 0)
        {
            return;
        }

        throw new InvalidOperationException(
            $"Doppelte Tweak-IDs gefunden: {string.Join(", ", duplicates)}");
    }

    private static readonly System.Text.UTF8Encoding Utf8NoBom = new(false);
}
