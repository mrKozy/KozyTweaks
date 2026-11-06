using System.Net;
using System.Text;
using System.Text.Json;
using Kozy.Core;
using Kozy.Core.Models;
using Moq;
using Moq.Protected;

namespace Kozy.Core.Tests;

public sealed class PackUpdateServiceTests : IDisposable
{
    private const string TestKid = "test-update-2026";
    private static readonly byte[] TestPrivateKey = GenerateTestKey();
    private static readonly byte[] TestPublicKey = PackSigningService.GetPublicKey(TestPrivateKey);
    private readonly string _tempCacheDir;

    public PackUpdateServiceTests()
    {
        PackVerificationService.RegisterPublicKey(TestKid, TestPublicKey);
        _tempCacheDir = Path.Combine(Path.GetTempPath(), $"kozy-test-{Guid.NewGuid()}");
        Directory.CreateDirectory(_tempCacheDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempCacheDir))
        {
            Directory.Delete(_tempCacheDir, recursive: true);
        }
    }

    [Fact]
    public async Task FetchAndVerifyAsync_ValidManifestAndPacks_ReturnsVerifiedResult()
    {
        // Arrange
        var packContent = CreateTestPackContent();
        var packHash = ComputeSha256(packContent);

        var manifest = new PackManifest
        {
            Channel = "stable",
            GeneratedAt = DateTimeOffset.UtcNow,
            Kid = TestKid,
            Packs =
            [
                new PackManifestEntry
                {
                    Tier = Tier.Basic,
                    Version = "1.0.0",
                    Sha256 = packHash,
                    Size = packContent.Length,
                    File = "pack-basic-1.0.0.json",
                }
            ]
        };

        var manifestJson = JsonSerializer.Serialize(manifest, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        });

        var canonicalBytes = CanonicalJson.Serialize(manifest);
        var signature = PackSigningService.SignBytes(canonicalBytes, TestPrivateKey);
        var signatureBase64 = Convert.ToBase64String(signature);

        var httpClient = CreateMockHttpClient(manifestJson, signatureBase64, packContent);
        var service = new PackUpdateService(httpClient, _tempCacheDir);

        // Act
        var result = await service.FetchAndVerifyAsync("https://packs.kozy.app/v1", "stable");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("stable", result.Channel);
        Assert.Equal(TestKid, result.Manifest.Kid);
        Assert.Single(result.Manifest.Packs);
        Assert.Single(result.PackFiles);
        Assert.True(result.PackFiles.ContainsKey("pack-basic-1.0.0.json"));
    }

    [Fact]
    public async Task FetchAndVerifyAsync_MissingSignatureHeader_ThrowsException()
    {
        // Arrange
        var manifest = new PackManifest
        {
            Channel = "stable",
            GeneratedAt = DateTimeOffset.UtcNow,
            Kid = TestKid,
            Packs = []
        };

        var manifestJson = JsonSerializer.Serialize(manifest, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        });

        var httpClient = CreateMockHttpClient(manifestJson, signatureBase64: null, Array.Empty<byte>());
        var service = new PackUpdateService(httpClient, _tempCacheDir);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<PackVerificationException>(() =>
            service.FetchAndVerifyAsync("https://packs.kozy.app/v1", "stable"));

        Assert.Contains("X-Signature", ex.Message);
    }

    [Fact]
    public async Task FetchAndVerifyAsync_InvalidPackHash_ThrowsException()
    {
        // Arrange
        var packContent = CreateTestPackContent();
        var wrongHash = "0000000000000000000000000000000000000000000000000000000000000000";

        var manifest = new PackManifest
        {
            Channel = "stable",
            GeneratedAt = DateTimeOffset.UtcNow,
            Kid = TestKid,
            Packs =
            [
                new PackManifestEntry
                {
                    Tier = Tier.Basic,
                    Version = "1.0.0",
                    Sha256 = wrongHash,
                    Size = packContent.Length,
                    File = "pack-basic-1.0.0.json",
                }
            ]
        };

        var manifestJson = JsonSerializer.Serialize(manifest, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        });

        var canonicalBytes = CanonicalJson.Serialize(manifest);
        var signature = PackSigningService.SignBytes(canonicalBytes, TestPrivateKey);
        var signatureBase64 = Convert.ToBase64String(signature);

        var httpClient = CreateMockHttpClient(manifestJson, signatureBase64, packContent);
        var service = new PackUpdateService(httpClient, _tempCacheDir);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<PackVerificationException>(() =>
            service.FetchAndVerifyAsync("https://packs.kozy.app/v1", "stable"));

        Assert.Contains("Hash", ex.Message);
    }

    [Fact]
    public async Task FetchAndVerifyAsync_WithBearerToken_IncludesAuthorizationHeader()
    {
        // Arrange
        var packContent = CreateTestPackContent();
        var packHash = ComputeSha256(packContent);

        var manifest = new PackManifest
        {
            Channel = "stable",
            GeneratedAt = DateTimeOffset.UtcNow,
            Kid = TestKid,
            Packs =
            [
                new PackManifestEntry
                {
                    Tier = Tier.Basic,
                    Version = "1.0.0",
                    Sha256 = packHash,
                    Size = packContent.Length,
                    File = "pack-basic-1.0.0.json",
                }
            ]
        };

        var manifestJson = JsonSerializer.Serialize(manifest, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        });

        var canonicalBytes = CanonicalJson.Serialize(manifest);
        var signature = PackSigningService.SignBytes(canonicalBytes, TestPrivateKey);
        var signatureBase64 = Convert.ToBase64String(signature);

        string? capturedAuthHeader = null;
        var httpClient = CreateMockHttpClient(manifestJson, signatureBase64, packContent, authHeaderCapture: header =>
        {
            capturedAuthHeader = header;
        });

        var service = new PackUpdateService(httpClient, _tempCacheDir);

        // Act
        await service.FetchAndVerifyAsync("https://packs.kozy.app/v1", "stable", bearerToken: "test-token-123");

        // Assert
        Assert.NotNull(capturedAuthHeader);
        Assert.StartsWith("Bearer ", capturedAuthHeader);
        Assert.Contains("test-token-123", capturedAuthHeader);
    }

    [Fact]
    public async Task LoadFromCacheAsync_ValidCache_ReturnsResult()
    {
        // Arrange
        var packContent = CreateTestPackContent();
        var packHash = ComputeSha256(packContent);

        var manifest = new PackManifest
        {
            Channel = "stable",
            GeneratedAt = DateTimeOffset.UtcNow,
            Kid = TestKid,
            Packs =
            [
                new PackManifestEntry
                {
                    Tier = Tier.Basic,
                    Version = "1.0.0",
                    Sha256 = packHash,
                    Size = packContent.Length,
                    File = "pack-basic-1.0.0.json",
                }
            ]
        };

        var manifestJson = JsonSerializer.Serialize(manifest, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        });

        var canonicalBytes = CanonicalJson.Serialize(manifest);
        var signature = PackSigningService.SignBytes(canonicalBytes, TestPrivateKey);
        var signatureBase64 = Convert.ToBase64String(signature);

        // Cache vorbereiten
        await File.WriteAllTextAsync(Path.Combine(_tempCacheDir, "manifest-stable.json"), manifestJson);
        await File.WriteAllTextAsync(Path.Combine(_tempCacheDir, "manifest-stable.json.sig"), signatureBase64);
        await File.WriteAllBytesAsync(Path.Combine(_tempCacheDir, "pack-basic-1.0.0.json"), packContent);

        var httpClient = new HttpClient(); // Nicht verwendet
        var service = new PackUpdateService(httpClient, _tempCacheDir);

        // Act
        var result = await service.LoadFromCacheAsync("stable");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("stable", result.Channel);
        Assert.Equal(TestKid, result.Manifest.Kid);
    }

    [Fact]
    public async Task LoadFromCacheAsync_MissingCache_ReturnsNull()
    {
        // Arrange
        var httpClient = new HttpClient();
        var service = new PackUpdateService(httpClient, _tempCacheDir);

        // Act
        var result = await service.LoadFromCacheAsync("stable");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void ClearCache_RemovesCachedFiles()
    {
        // Arrange
        var manifestPath = Path.Combine(_tempCacheDir, "manifest-stable.json");
        var signaturePath = Path.Combine(_tempCacheDir, "manifest-stable.json.sig");
        File.WriteAllText(manifestPath, "test");
        File.WriteAllText(signaturePath, "test");

        var httpClient = new HttpClient();
        var service = new PackUpdateService(httpClient, _tempCacheDir);

        // Act
        service.ClearCache("stable");

        // Assert
        Assert.False(File.Exists(manifestPath));
        Assert.False(File.Exists(signaturePath));
    }

    private static HttpClient CreateMockHttpClient(
        string manifestJson,
        string? signatureBase64,
        byte[] packContent,
        Action<string?>? authHeaderCapture = null)
    {
        var handlerMock = new Mock<HttpMessageHandler>();

        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync((HttpRequestMessage request, CancellationToken _) =>
            {
                authHeaderCapture?.Invoke(request.Headers.Authorization?.ToString());

                if (request.RequestUri?.AbsolutePath.Contains("manifest") == true)
                {
                    var response = new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(manifestJson, Encoding.UTF8, "application/json")
                    };

                    if (signatureBase64 != null)
                    {
                        response.Headers.Add("X-Signature", signatureBase64);
                    }

                    return response;
                }

                if (request.RequestUri?.AbsolutePath.Contains("packs/") == true)
                {
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new ByteArrayContent(packContent)
                    };
                }

                return new HttpResponseMessage(HttpStatusCode.NotFound);
            });

        return new HttpClient(handlerMock.Object);
    }

    private static byte[] CreateTestPackContent()
    {
        return Encoding.UTF8.GetBytes("""
            [
              {
                "schemaVersion": 1,
                "id": "test.pack",
                "version": 1,
                "name": { "de": "Test", "en": "Test" },
                "description": { "de": "Test", "en": "Test" },
                "category": "gaming",
                "tier": "basic",
                "risk": "safe",
                "requiresReboot": false,
                "supports": {
                  "minBuild": 19045,
                  "maxBuild": null,
                  "editions": ["Pro"],
                  "deviceClass": ["desktop"],
                  "gpuVendor": null
                },
                "conflictsWith": [],
                "dependsOn": [],
                "irreversible": false,
                "benchmarkHint": "none",
                "actions": [
                  {
                    "type": "registry.set",
                    "hive": "HKCU",
                    "path": "Test",
                    "name": "Value",
                    "kind": "DWord",
                    "value": 1
                  }
                ]
              }
            ]
            """);
    }

    private static byte[] GenerateTestKey()
    {
        var key = new byte[32];
        Random.Shared.NextBytes(key);
        return key;
    }

    private static string ComputeSha256(byte[] data)
    {
        var hash = System.Security.Cryptography.SHA256.HashData(data);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
