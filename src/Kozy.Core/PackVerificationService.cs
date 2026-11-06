using System.Security.Cryptography;
using System.Text.Json;
using Kozy.Core.Models;

namespace Kozy.Core;

public sealed class PackVerificationException : Exception
{
    public PackVerificationException(string message) : base(message) { }
}

public static class PackVerificationService
{
    /// <summary>
    /// Bekannte öffentliche Schlüssel (Kid → Public Key Bytes).
    /// In Produktion würden diese aus einem sicheren Store geladen.
    /// </summary>
    private static readonly Dictionary<string, byte[]> KnownPublicKeys = new()
    {
        // Beispiel-Schlüssel, muss durch echten Produktionsschlüssel ersetzt werden
        ["pack-2026-01"] = Convert.FromBase64String("AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA="),
    };

    /// <summary>
    /// Registriert einen öffentlichen Schlüssel für Pack-Verifikation.
    /// </summary>
    public static void RegisterPublicKey(string kid, byte[] publicKey)
    {
        if (publicKey.Length != 32)
        {
            throw new ArgumentException("Ed25519 public key muss 32 Bytes lang sein.", nameof(publicKey));
        }

        KnownPublicKeys[kid] = publicKey;
    }

    /// <summary>
    /// Verifiziert Manifest-Signatur und Pack-Hashes.
    /// </summary>
    /// <param name="manifestJson">JSON-Inhalt der manifest.json</param>
    /// <param name="signatureBase64">Base64-kodierte Signatur aus manifest.json.sig</param>
    /// <param name="packFiles">Dictionary mit Pack-Dateinamen und deren Inhalt</param>
    /// <param name="installedVersions">Optional: bereits installierte Versionen pro Tier für Downgrade-Schutz</param>
    /// <exception cref="PackVerificationException">Bei Verifikationsfehlern</exception>
    public static PackManifest VerifyAndLoad(
        string manifestJson,
        string signatureBase64,
        IReadOnlyDictionary<string, byte[]> packFiles,
        IReadOnlyDictionary<Tier, string>? installedVersions = null)
    {
        // 1. Manifest deserialisieren
        var manifest = DeserializeManifest(manifestJson);

        // 2. Kid prüfen
        if (!KnownPublicKeys.TryGetValue(manifest.Kid, out var publicKey))
        {
            throw new PackVerificationException(
                $"Unbekannte Key-ID '{manifest.Kid}'. Pack-Signatur kann nicht verifiziert werden.");
        }

        // 3. Signatur prüfen (über kanonisches JSON)
        var canonicalBytes = CanonicalJson.Serialize(manifest);
        var isValid = PackSigningService.VerifyBytes(canonicalBytes, signatureBase64, publicKey);

        if (!isValid)
        {
            throw new PackVerificationException(
                "Pack-Signatur ist ungültig. Das Manifest wurde möglicherweise manipuliert.");
        }

        // 4. SHA-256-Hashes aller Packs prüfen
        foreach (var entry in manifest.Packs)
        {
            if (!packFiles.TryGetValue(entry.File, out var packContent))
            {
                throw new PackVerificationException(
                    $"Pack-Datei '{entry.File}' fehlt.");
            }

            var actualHash = ComputeSha256(packContent);
            var expectedHash = entry.Sha256.ToLowerInvariant();

            if (actualHash != expectedHash)
            {
                throw new PackVerificationException(
                    $"SHA-256-Hash von '{entry.File}' stimmt nicht überein. Erwartet: {expectedHash}, Tatsächlich: {actualHash}");
            }

            // 5. Downgrade-Schutz
            if (installedVersions != null &&
                installedVersions.TryGetValue(entry.Tier, out var installedVersion))
            {
                if (CompareVersions(entry.Version, installedVersion) < 0)
                {
                    throw new PackVerificationException(
                        $"Downgrade von {entry.Tier} Pack erkannt: {entry.Version} < {installedVersion}. Downgrade ist nicht erlaubt.");
                }
            }
        }

        return manifest;
    }

    /// <summary>
    /// Verifiziert einen einzelnen Pack-Hash.
    /// </summary>
    public static bool VerifyPackHash(byte[] packContent, string expectedSha256)
    {
        var actualHash = ComputeSha256(packContent);
        return actualHash.Equals(expectedSha256, StringComparison.OrdinalIgnoreCase);
    }

    private static PackManifest DeserializeManifest(string json)
    {
        try
        {
            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                PropertyNameCaseInsensitive = true,
            };

            return JsonSerializer.Deserialize<PackManifest>(json, options)
                ?? throw new PackVerificationException("Manifest ist null nach Deserialisierung.");
        }
        catch (JsonException ex)
        {
            throw new PackVerificationException($"Ungültiges Manifest-JSON: {ex.Message}");
        }
    }

    private static string ComputeSha256(byte[] data)
    {
        var hash = SHA256.HashData(data);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    /// <summary>
    /// Vergleicht zwei Versionsnummern (semantic versioning light).
    /// </summary>
    /// <returns>-1 wenn v1 &lt; v2, 0 wenn gleich, 1 wenn v1 &gt; v2</returns>
    private static int CompareVersions(string v1, string v2)
    {
        var parts1 = ParseVersion(v1);
        var parts2 = ParseVersion(v2);

        for (var i = 0; i < Math.Min(parts1.Length, parts2.Length); i++)
        {
            var cmp = parts1[i].CompareTo(parts2[i]);
            if (cmp != 0)
            {
                return cmp;
            }
        }

        return parts1.Length.CompareTo(parts2.Length);
    }

    private static int[] ParseVersion(string version)
    {
        var clean = version.Split('-')[0]; // Strip pre-release suffix
        return clean.Split('.')
            .Select(p => int.TryParse(p, out var num) ? num : 0)
            .ToArray();
    }
}
