using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Crypto.Signers;

namespace Kozy.Core;

public static class PackSigningService
{
    public static byte[] ParsePrivateKey(string encodedKey)
    {
        var keyBytes = DecodeBytes(encodedKey);
        return NormalizePrivateKey(keyBytes);
    }

    public static byte[] GetPublicKey(string encodedPrivateKey) =>
        GetPublicKey(ParsePrivateKey(encodedPrivateKey));

    public static byte[] GetPublicKey(byte[] privateKey)
    {
        var normalized = NormalizePrivateKey(privateKey);
        var keyParams = new Ed25519PrivateKeyParameters(normalized, 0);
        return keyParams.GeneratePublicKey().GetEncoded();
    }

    public static string SignBytes(byte[] data, string encodedPrivateKey) =>
        Convert.ToBase64String(SignBytes(data, ParsePrivateKey(encodedPrivateKey)));

    public static byte[] SignBytes(byte[] data, byte[] privateKey)
    {
        var normalized = NormalizePrivateKey(privateKey);
        var keyParams = new Ed25519PrivateKeyParameters(normalized, 0);
        var signer = new Ed25519Signer();
        signer.Init(forSigning: true, keyParams);
        signer.BlockUpdate(data, 0, data.Length);
        return signer.GenerateSignature();
    }

    public static bool VerifyBytes(byte[] data, string encodedSignature, byte[] publicKey)
    {
        var signature = DecodeBytes(encodedSignature);
        var keyParams = new Ed25519PublicKeyParameters(publicKey, 0);
        var verifier = new Ed25519Signer();
        verifier.Init(forSigning: false, keyParams);
        verifier.BlockUpdate(data, 0, data.Length);
        return verifier.VerifySignature(signature);
    }

    private static byte[] NormalizePrivateKey(byte[] privateKey)
    {
        const int Ed25519KeySize = 32; // Ed25519PrivateKeyParameters.KeySize
        return privateKey.Length switch
        {
            Ed25519KeySize => privateKey,
            64 => privateKey[..Ed25519KeySize],
            _ => throw new InvalidOperationException(
                $"Ungültige Ed25519-Schlüssellänge {privateKey.Length}. Erwartet werden 32 oder 64 Byte."),
        };
    }

    private static byte[] DecodeBytes(string encoded)
    {
        if (string.IsNullOrWhiteSpace(encoded))
        {
            throw new InvalidOperationException("Ungültiges Format.");
        }

        var value = encoded.Trim();
        if (value.StartsWith("base64:", StringComparison.OrdinalIgnoreCase))
        {
            return Convert.FromBase64String(value[7..]);
        }

        if (value.StartsWith("base64url:", StringComparison.OrdinalIgnoreCase))
        {
            return Base64UrlDecode(value[10..]);
        }

        if (value.StartsWith("hex:", StringComparison.OrdinalIgnoreCase))
        {
            return Convert.FromHexString(value[4..]);
        }

        foreach (var parser in Parsers)
        {
            if (parser(value, out var bytes))
            {
                return bytes;
            }
        }

        throw new InvalidOperationException("Ungültiges Format.");
    }

    private static IReadOnlyList<TryParseBytes> Parsers { get; } =
    [
        TryParseBase64,
        TryParseBase64Url,
        TryParseHex,
    ];

    private static bool TryParseBase64(string value, out byte[] bytes)
    {
        try
        {
            bytes = Convert.FromBase64String(value);
            return true;
        }
        catch (FormatException)
        {
            bytes = [];
            return false;
        }
    }

    private static bool TryParseBase64Url(string value, out byte[] bytes)
    {
        try
        {
            bytes = Base64UrlDecode(value);
            return true;
        }
        catch (FormatException)
        {
            bytes = [];
            return false;
        }
    }

    private static bool TryParseHex(string value, out byte[] bytes)
    {
        var trimmed = value.Replace("-", string.Empty, StringComparison.Ordinal)
            .Replace(" ", string.Empty, StringComparison.Ordinal);

        if (trimmed.Length == 0 || trimmed.Length % 2 != 0)
        {
            bytes = [];
            return false;
        }

        if (!trimmed.All(Uri.IsHexDigit))
        {
            bytes = [];
            return false;
        }

        bytes = Convert.FromHexString(trimmed);
        return true;
    }

    private static byte[] Base64UrlDecode(string value)
    {
        var normalized = value.Replace('-', '+').Replace('_', '/');
        var padded = (normalized.Length % 4) switch
        {
            0 => normalized,
            2 => normalized + "==",
            3 => normalized + "=",
            _ => throw new FormatException("Ungültiges base64url-Format."),
        };

        return Convert.FromBase64String(padded);
    }

    private delegate bool TryParseBytes(string value, out byte[] bytes);
}
