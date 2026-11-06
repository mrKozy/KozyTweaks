using System.Text.Json;
using System.Text.Json.Serialization;

namespace Kozy.Core.Models;

/// <summary>
/// Serialisiert Enum-Werte als Kleinbuchstaben (z. B. "safe", "basic"),
/// passend zu den Manifest-Konventionen aus Kapitel 5.2/5.3.
/// </summary>
public sealed class LowerCaseEnumConverter<TEnum> : JsonConverter<TEnum>
    where TEnum : struct, Enum
{
    public override TEnum Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var value = reader.GetString();
        if (Enum.TryParse<TEnum>(value, ignoreCase: true, out var result))
        {
            return result;
        }

        throw new JsonException($"Ungültiger Wert '{value}' für {typeof(TEnum).Name}.");
    }

    public override void Write(Utf8JsonWriter writer, TEnum value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString().ToLowerInvariant());
    }
}