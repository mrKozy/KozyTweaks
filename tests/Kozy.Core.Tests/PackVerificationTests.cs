using System.Text;
using System.Text.Json;
using Kozy.Core;
using Kozy.Core.Models;

namespace Kozy.Core.Tests;

public sealed class PackVerificationTests
{
    private const string TestKid = "test-key-2026";
    private static readonly byte[] TestPrivateKey = GenerateTestKey();
    private static readonly byte[] TestPublicKey = PackSigningService.GetPublicKey(TestPrivateKey);

    public PackVerificationTests()
    {
        // Registriere Test-Schlüssel
        PackVerificationService.RegisterPublicKey(TestKid, TestPublicKey);
    }

    [Fact]
    public void VerifyAndLoad_ValidManifestAndSignature_Succeeds()
    {
        // Arrange
        var packContent = Encoding.UTF8.GetBytes("""
            [
              {
                "schemaVersion": 1,
                "id": "gaming.test",
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

        var packFiles = new Dictionary<string, byte[]>
        {
            ["pack-basic-1.0.0.json"] = packContent,
        };

        // Act
        var result = PackVerificationService.VerifyAndLoad(manifestJson, signatureBase64, packFiles);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(TestKid, result.Kid);
        Assert.Single(result.Packs);
    }

    [Fact]
    public void VerifyAndLoad_InvalidSignature_ThrowsException()
    {
        // Arrange
        var packContent = Encoding.UTF8.GetBytes("[]");
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

        // Falsche Signatur
        var wrongSignature = new byte[64];
        Random.Shared.NextBytes(wrongSignature);
        var signatureBase64 = Convert.ToBase64String(wrongSignature);

        var packFiles = new Dictionary<string, byte[]>
        {
            ["pack-basic-1.0.0.json"] = packContent,
        };

        // Act & Assert
        var ex = Assert.Throws<PackVerificationException>(() =>
            PackVerificationService.VerifyAndLoad(manifestJson, signatureBase64, packFiles));

        Assert.Contains("ungültig", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void VerifyAndLoad_WrongPackHash_ThrowsException()
    {
        // Arrange
        var packContent = Encoding.UTF8.GetBytes("[]");
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

        var packFiles = new Dictionary<string, byte[]>
        {
            ["pack-basic-1.0.0.json"] = packContent,
        };

        // Act & Assert
        var ex = Assert.Throws<PackVerificationException>(() =>
            PackVerificationService.VerifyAndLoad(manifestJson, signatureBase64, packFiles));

        Assert.Contains("Hash", ex.Message);
        Assert.Contains("stimmt nicht überein", ex.Message);
    }

    [Fact]
    public void VerifyAndLoad_DowngradeAttempt_ThrowsException()
    {
        // Arrange
        var packContent = Encoding.UTF8.GetBytes("[]");
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

        var packFiles = new Dictionary<string, byte[]>
        {
            ["pack-basic-1.0.0.json"] = packContent,
        };

        var installedVersions = new Dictionary<Tier, string>
        {
            [Tier.Basic] = "2.0.0", // Höher als 1.0.0
        };

        // Act & Assert
        var ex = Assert.Throws<PackVerificationException>(() =>
            PackVerificationService.VerifyAndLoad(manifestJson, signatureBase64, packFiles, installedVersions));

        Assert.Contains("Downgrade", ex.Message);
    }

    [Fact]
    public void VerifyAndLoad_UnknownKid_ThrowsException()
    {
        // Arrange
        var manifest = new PackManifest
        {
            Channel = "stable",
            GeneratedAt = DateTimeOffset.UtcNow,
            Kid = "unknown-key-id",
            Packs = []
        };

        var manifestJson = JsonSerializer.Serialize(manifest, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        });

        // Act & Assert
        var ex = Assert.Throws<PackVerificationException>(() =>
            PackVerificationService.VerifyAndLoad(manifestJson, "dGVzdA==", new Dictionary<string, byte[]>()));

        Assert.Contains("Unbekannte Key-ID", ex.Message);
    }

    [Fact]
    public void VerifyPackHash_ValidHash_ReturnsTrue()
    {
        // Arrange
        var content = Encoding.UTF8.GetBytes("test content");
        var hash = ComputeSha256(content);

        // Act
        var result = PackVerificationService.VerifyPackHash(content, hash);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void VerifyPackHash_InvalidHash_ReturnsFalse()
    {
        // Arrange
        var content = Encoding.UTF8.GetBytes("test content");
        var wrongHash = "0000000000000000000000000000000000000000000000000000000000000000";

        // Act
        var result = PackVerificationService.VerifyPackHash(content, wrongHash);

        // Assert
        Assert.False(result);
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
