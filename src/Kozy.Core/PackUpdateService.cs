using System.Net.Http.Headers;
using System.Text.Json;
using Kozy.Core.Models;

namespace Kozy.Core;

public sealed class PackUpdateService
{
    private readonly HttpClient _httpClient;
    private readonly string _cacheDirectory;

    public PackUpdateService(HttpClient httpClient, string cacheDirectory)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _cacheDirectory = cacheDirectory ?? throw new ArgumentNullException(nameof(cacheDirectory));

        if (!Directory.Exists(_cacheDirectory))
        {
            Directory.CreateDirectory(_cacheDirectory);
        }
    }

    /// <summary>
    /// Lädt ein Pack-Manifest vom Server, verifiziert Signatur und Hashes.
    /// </summary>
    /// <param name="baseUri">Basis-URI des Pack-Repositories (z.B. https://packs.kozy.app/v1)</param>
    /// <param name="channel">Channel (stable, beta, etc.)</param>
    /// <param name="bearerToken">Optional: Bearer-Token für authentifizierte Anfragen</param>
    /// <param name="installedVersions">Optional: Installierte Versionen für Downgrade-Schutz</param>
    /// <returns>Verifiziertes Manifest mit heruntergeladenen Pack-Dateien</returns>
    public async Task<PackUpdateResult> FetchAndVerifyAsync(
        string baseUri,
        string channel,
        string? bearerToken = null,
        IReadOnlyDictionary<Tier, string>? installedVersions = null,
        CancellationToken cancellationToken = default)
    {
        // 1. Manifest herunterladen
        var manifestUri = $"{baseUri.TrimEnd('/')}/packs/manifest?channel={Uri.EscapeDataString(channel)}";
        var request = new HttpRequestMessage(HttpMethod.Get, manifestUri);

        if (!string.IsNullOrWhiteSpace(bearerToken))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);
        }

        using var manifestResponse = await _httpClient.SendAsync(request, cancellationToken);
        manifestResponse.EnsureSuccessStatusCode();

        var manifestJson = await manifestResponse.Content.ReadAsStringAsync(cancellationToken);

        // 2. Signatur aus Header extrahieren
        if (!manifestResponse.Headers.TryGetValues("X-Signature", out var signatureValues))
        {
            throw new PackVerificationException(
                "Server hat keine X-Signature-Header gesendet. Manifest kann nicht verifiziert werden.");
        }

        var signatureBase64 = signatureValues.First();

        // 3. Manifest deserialisieren (pre-verification)
        var manifest = JsonSerializer.Deserialize<PackManifest>(manifestJson, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
        }) ?? throw new PackVerificationException("Manifest ist null nach Deserialisierung.");

        // 4. Packs herunterladen
        var packFiles = new Dictionary<string, byte[]>();

        foreach (var entry in manifest.Packs)
        {
            var packUri = $"{baseUri.TrimEnd('/')}/packs/{entry.Tier.ToString().ToLowerInvariant()}/{entry.Version}";
            var packRequest = new HttpRequestMessage(HttpMethod.Get, packUri);

            if (!string.IsNullOrWhiteSpace(bearerToken))
            {
                packRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);
            }

            using var packResponse = await _httpClient.SendAsync(packRequest, cancellationToken);

            if (!packResponse.IsSuccessStatusCode)
            {
                throw new PackVerificationException(
                    $"Pack-Download fehlgeschlagen: {entry.File} (HTTP {(int)packResponse.StatusCode})");
            }

            var packContent = await packResponse.Content.ReadAsByteArrayAsync(cancellationToken);
            packFiles[entry.File] = packContent;

            // Optional: In Cache schreiben
            await WriteToCacheAsync(entry.File, packContent, cancellationToken);
        }

        // 5. Vollständige Verifikation: Signatur + Hashes + Downgrade-Schutz
        var verifiedManifest = PackVerificationService.VerifyAndLoad(
            manifestJson,
            signatureBase64,
            packFiles,
            installedVersions);

        return new PackUpdateResult
        {
            Manifest = verifiedManifest,
            PackFiles = packFiles,
            Channel = channel,
            FetchedAt = DateTimeOffset.UtcNow,
        };
    }

    /// <summary>
    /// Lädt Packs aus dem lokalen Cache, wenn verfügbar und gültig.
    /// </summary>
    public async Task<PackUpdateResult?> LoadFromCacheAsync(
        string channel,
        IReadOnlyDictionary<Tier, string>? installedVersions = null,
        CancellationToken cancellationToken = default)
    {
        var manifestPath = Path.Combine(_cacheDirectory, $"manifest-{channel}.json");
        var signaturePath = Path.Combine(_cacheDirectory, $"manifest-{channel}.json.sig");

        if (!File.Exists(manifestPath) || !File.Exists(signaturePath))
        {
            return null;
        }

        try
        {
            var manifestJson = await File.ReadAllTextAsync(manifestPath, cancellationToken);
            var signatureBase64 = await File.ReadAllTextAsync(signaturePath, cancellationToken);

            var manifest = JsonSerializer.Deserialize<PackManifest>(manifestJson, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                PropertyNameCaseInsensitive = true,
            }) ?? throw new PackVerificationException("Cached manifest ist null.");

            var packFiles = new Dictionary<string, byte[]>();

            foreach (var entry in manifest.Packs)
            {
                var packPath = Path.Combine(_cacheDirectory, entry.File);
                if (!File.Exists(packPath))
                {
                    return null; // Cache unvollständig
                }

                packFiles[entry.File] = await File.ReadAllBytesAsync(packPath, cancellationToken);
            }

            // Verifikation
            var verifiedManifest = PackVerificationService.VerifyAndLoad(
                manifestJson,
                signatureBase64,
                packFiles,
                installedVersions);

            return new PackUpdateResult
            {
                Manifest = verifiedManifest,
                PackFiles = packFiles,
                Channel = channel,
                FetchedAt = manifest.GeneratedAt,
            };
        }
        catch (PackVerificationException)
        {
            // Cache ungültig → neu laden erforderlich
            return null;
        }
    }

    /// <summary>
    /// Schreibt Manifest und Signatur in den Cache.
    /// </summary>
    public async Task SaveToCacheAsync(
        string channel,
        string manifestJson,
        string signatureBase64,
        CancellationToken cancellationToken = default)
    {
        var manifestPath = Path.Combine(_cacheDirectory, $"manifest-{channel}.json");
        var signaturePath = Path.Combine(_cacheDirectory, $"manifest-{channel}.json.sig");

        await File.WriteAllTextAsync(manifestPath, manifestJson, cancellationToken);
        await File.WriteAllTextAsync(signaturePath, signatureBase64, cancellationToken);
    }

    private async Task WriteToCacheAsync(string fileName, byte[] content, CancellationToken cancellationToken)
    {
        var cachePath = Path.Combine(_cacheDirectory, fileName);
        await File.WriteAllBytesAsync(cachePath, content, cancellationToken);
    }

    /// <summary>
    /// Löscht den Cache für einen Channel.
    /// </summary>
    public void ClearCache(string channel)
    {
        var pattern = $"*{channel}*";
        foreach (var file in Directory.GetFiles(_cacheDirectory, pattern))
        {
            File.Delete(file);
        }
    }
}

public sealed record PackUpdateResult
{
    public required PackManifest Manifest { get; init; }
    public required IReadOnlyDictionary<string, byte[]> PackFiles { get; init; }
    public required string Channel { get; init; }
    public required DateTimeOffset FetchedAt { get; init; }
}
