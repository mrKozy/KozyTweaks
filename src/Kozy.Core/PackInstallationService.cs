using System.Text.Json;
using Kozy.Core.Models;

namespace Kozy.Core;

public sealed class PackInstallationException : Exception
{
    public PackInstallationException(string message, Exception? innerException = null)
        : base(message, innerException) { }
}

/// <summary>
/// Installiert verifizierte Packs durch Anwendung der enthaltenen Tweaks.
/// </summary>
public sealed class PackInstallationService
{
    private readonly Engine _engine;
    private readonly string _installedVersionsFile;

    public PackInstallationService(Engine engine, string installedVersionsFile)
    {
        _engine = engine ?? throw new ArgumentNullException(nameof(engine));
        _installedVersionsFile = installedVersionsFile ?? throw new ArgumentNullException(nameof(installedVersionsFile));
    }

    /// <summary>
    /// Installiert Packs aus einem verifizierten PackUpdateResult.
    /// </summary>
    /// <param name="updateResult">Verifiziertes Update-Ergebnis mit Manifest und Pack-Dateien</param>
    /// <param name="options">Optionen für die Anwendung (Restore Point, StopOnError)</param>
    /// <returns>Installations-Ergebnis mit Statistiken</returns>
    public PackInstallationResult Install(PackUpdateResult updateResult, ApplyOptions? options = null)
    {
        options ??= new ApplyOptions();

        var tierResults = new List<PackTierInstallation>();

        foreach (var packEntry in updateResult.Manifest.Packs)
        {
            try
            {
                // Pack-Datei laden
                if (!updateResult.PackFiles.TryGetValue(packEntry.File, out var packContent))
                {
                    throw new PackInstallationException(
                        $"Pack-Datei '{packEntry.File}' nicht in UpdateResult gefunden.");
                }

                // Tweaks deserialisieren
                var tweaks = DeserializeTweaks(packContent);

                if (tweaks.Count == 0)
                {
                    tierResults.Add(new PackTierInstallation
                    {
                        Tier = packEntry.Tier,
                        Version = packEntry.Version,
                        Applied = 0,
                        Failed = 0,
                        Skipped = 0,
                        Success = true,
                    });
                    continue;
                }

                // Engine-Apply ausführen
                var applyResult = _engine.Apply(tweaks, options);

                tierResults.Add(new PackTierInstallation
                {
                    Tier = packEntry.Tier,
                    Version = packEntry.Version,
                    Applied = applyResult.Applied,
                    Failed = applyResult.Failed,
                    Skipped = applyResult.Skipped,
                    SessionId = applyResult.Session.SessionId,
                    Success = applyResult.Failed == 0,
                });

                // Erfolgreiche Installation: Version speichern
                if (applyResult.Failed == 0)
                {
                    SaveInstalledVersion(packEntry.Tier, packEntry.Version);
                }
                else if (options.StopOnError)
                {
                    break;
                }
            }
            catch (Exception ex)
            {
                tierResults.Add(new PackTierInstallation
                {
                    Tier = packEntry.Tier,
                    Version = packEntry.Version,
                    Applied = 0,
                    Failed = 0,
                    Skipped = 0,
                    Error = ex.Message,
                    Success = false,
                });

                // Bei StopOnError sofort abbrechen
                if (options.StopOnError)
                {
                    break;
                }
            }
        }

        return new PackInstallationResult
        {
            Channel = updateResult.Channel,
            InstalledAt = DateTimeOffset.UtcNow,
            TierResults = tierResults,
            TotalApplied = tierResults.Sum(r => r.Applied),
            TotalFailed = tierResults.Sum(r => r.Failed),
            TotalSkipped = tierResults.Sum(r => r.Skipped),
            OverallSuccess = tierResults.All(r => r.Success),
        };
    }

    /// <summary>
    /// Lädt die aktuell installierten Pack-Versionen.
    /// </summary>
    public IReadOnlyDictionary<Tier, string> GetInstalledVersions()
    {
        if (!File.Exists(_installedVersionsFile))
        {
            return new Dictionary<Tier, string>();
        }

        try
        {
            var json = File.ReadAllText(_installedVersionsFile);
            var dict = JsonSerializer.Deserialize<Dictionary<string, string>>(json);

            if (dict == null)
            {
                return new Dictionary<Tier, string>();
            }

            // String-Keys zurück zu Tier-Enum konvertieren
            var result = new Dictionary<Tier, string>();
            foreach (var kvp in dict)
            {
                if (Enum.TryParse<Tier>(kvp.Key, ignoreCase: true, out var tier))
                {
                    result[tier] = kvp.Value;
                }
            }

            return result;
        }
        catch
        {
            return new Dictionary<Tier, string>();
        }
    }

    private void SaveInstalledVersion(Tier tier, string version)
    {
        var current = GetInstalledVersions();
        var updated = new Dictionary<string, string>(current.ToDictionary(
            kvp => kvp.Key.ToString().ToLowerInvariant(),
            kvp => kvp.Value))
        {
            [tier.ToString().ToLowerInvariant()] = version
        };

        var directory = Path.GetDirectoryName(_installedVersionsFile);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var json = JsonSerializer.Serialize(updated, new JsonSerializerOptions
        {
            WriteIndented = true,
        });

        File.WriteAllText(_installedVersionsFile, json);
    }

    private static List<TweakManifest> DeserializeTweaks(byte[] packContent)
    {
        try
        {
            var json = System.Text.Encoding.UTF8.GetString(packContent);
            var tweaks = JsonSerializer.Deserialize<List<TweakManifest>>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
            });

            return tweaks ?? new List<TweakManifest>();
        }
        catch (JsonException ex)
        {
            throw new PackInstallationException($"Fehler beim Deserialisieren der Tweaks: {ex.Message}", ex);
        }
    }
}

public sealed record PackInstallationResult
{
    public required string Channel { get; init; }
    public required DateTimeOffset InstalledAt { get; init; }
    public required IReadOnlyList<PackTierInstallation> TierResults { get; init; }
    public required int TotalApplied { get; init; }
    public required int TotalFailed { get; init; }
    public required int TotalSkipped { get; init; }
    public required bool OverallSuccess { get; init; }
}

public sealed record PackTierInstallation
{
    public required Tier Tier { get; init; }
    public required string Version { get; init; }
    public required int Applied { get; init; }
    public required int Failed { get; init; }
    public required int Skipped { get; init; }
    public string? SessionId { get; init; }
    public string? Error { get; init; }
    public required bool Success { get; init; }
}
