using System.Text.Json;

namespace Kozy.Core;

public static class CanonicalJson
{
    public static byte[] Serialize<T>(T value, JsonSerializerOptions? options = null)
    {
        var element = JsonSerializer.SerializeToElement(value, options);
        using var buffer = new MemoryStream();
        using var writer = new Utf8JsonWriter(buffer);
        WriteElement(element, writer);
        writer.Flush();
        return buffer.ToArray();
    }

    private static void WriteElement(JsonElement element, Utf8JsonWriter writer)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                writer.WriteStartObject();
                foreach (var property in element.EnumerateObject().OrderBy(p => p.Name, StringComparer.Ordinal))
                {
                    writer.WritePropertyName(property.Name);
                    WriteElement(property.Value, writer);
                }

                writer.WriteEndObject();
                break;

            case JsonValueKind.Array:
                writer.WriteStartArray();
                foreach (var item in element.EnumerateArray())
                {
                    WriteElement(item, writer);
                }

                writer.WriteEndArray();
                break;

            default:
                element.WriteTo(writer);
                break;
        }
    }
}
