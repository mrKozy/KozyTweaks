using System.Text.Json;
using Kozy.Core.Models;

namespace Kozy.Core;

public static class ManifestSerializer
{
    public static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = false,
        WriteIndented = true,
    };

    public static TweakManifest Deserialize(string json) =>
        JsonSerializer.Deserialize<TweakManifest>(json, Options)
            ?? throw new JsonException("Manifest konnte nicht deserialisiert werden.");

    public static TweakManifest LoadFromFile(string path) =>
        Deserialize(File.ReadAllText(path));
}