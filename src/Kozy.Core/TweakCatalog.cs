using Kozy.Core.Models;

namespace Kozy.Core;

public enum TweakPreset
{
    Safe,
    Balanced,
    Max,
}

public sealed class TweakCatalog
{
    private readonly List<TweakManifest> _tweaks = [];

    public IReadOnlyList<TweakManifest> Tweaks => _tweaks;

    public static TweakCatalog LoadFromDirectory(string directoryPath)
    {
        var catalog = new TweakCatalog();

        if (!Directory.Exists(directoryPath))
        {
            return catalog;
        }

        var files = Directory.EnumerateFiles(directoryPath, "*.json", SearchOption.AllDirectories);
        foreach (var file in files)
        {
            // Überspringe Nicht-Tweak-Dateien wie manifest.json oder pack-*.json
            var fileName = Path.GetFileName(file);
            if (fileName.StartsWith("manifest", StringComparison.OrdinalIgnoreCase) ||
                fileName.StartsWith("pack-", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            try
            {
                var manifest = ManifestSerializer.LoadFromFile(file);
                var errors = ManifestValidator.Validate(manifest);
                if (errors.Count == 0)
                {
                    catalog._tweaks.Add(manifest);
                }
            }
            catch
            {
                // Fehlerhafte JSONs ignorieren
            }
        }

        return catalog;
    }

    public static string FindDefaultPacksDirectory()
    {
        var candidates = new[]
        {
            Path.Combine(AppContext.BaseDirectory, "packs"),
            Path.Combine(Directory.GetCurrentDirectory(), "packs"),
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "packs"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "kozy", "packs"),
        };

        foreach (var candidate in candidates)
        {
            if (Directory.Exists(candidate) && Directory.EnumerateFiles(candidate, "*.json", SearchOption.AllDirectories).Any())
            {
                return Path.GetFullPath(candidate);
            }
        }

        return Path.Combine(AppContext.BaseDirectory, "packs");
    }

    public IReadOnlyList<TweakManifest> GetByPreset(TweakPreset preset) => preset switch
    {
        TweakPreset.Safe => _tweaks.Where(t => t.Risk == Risk.Safe && !t.Irreversible).ToList(),
        TweakPreset.Balanced => _tweaks.Where(t => (t.Risk == Risk.Safe || t.Risk == Risk.Moderate) && !t.Irreversible).ToList(),
        TweakPreset.Max => _tweaks.Where(t => t.Risk != Risk.Experimental && !t.Irreversible).ToList(),
        _ => [],
    };

    public IReadOnlyList<TweakManifest> GetByCategory(string category) =>
        _tweaks.Where(t => t.Category.Equals(category, StringComparison.OrdinalIgnoreCase)).ToList();

    public IReadOnlyList<string> GetCategories() =>
        _tweaks.Select(t => t.Category).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(c => c).ToList();
}
