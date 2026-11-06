using System.Globalization;
using System.Text.Json;
using Kozy.Core.Models;

namespace Kozy.Actions;

/// <summary>
/// Wandelt den polymorphen Manifest-Wert (nativ oder JsonElement) in den zum
/// Registry-Typ passenden .NET-Typ um.
/// </summary>
public static class RegistryValueConverter
{
    public static void Validate(RegistryValueKind kind, object value) => _ = Convert(kind, value);

    public static object Convert(RegistryValueKind kind, object value) => kind switch
    {
        RegistryValueKind.DWord => ToInt32(value),
        RegistryValueKind.QWord => ToInt64(value),
        RegistryValueKind.String or RegistryValueKind.ExpandString => ToStringValue(value),
        RegistryValueKind.Binary => ToBytes(value),
        RegistryValueKind.MultiString => ToStringArray(value),
        _ => throw new ArgumentException($"Unbekannter Werttyp '{kind}'."),
    };

    public static int ToInt32(object value) =>
        value is JsonElement e ? e.GetInt32() : System.Convert.ToInt32(value, CultureInfo.InvariantCulture);

    public static long ToInt64(object value) =>
        value is JsonElement e ? e.GetInt64() : System.Convert.ToInt64(value, CultureInfo.InvariantCulture);

    public static string ToStringValue(object value) =>
        value is JsonElement e ? (e.GetString() ?? string.Empty) : System.Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty;

    public static byte[] ToBytes(object value)
    {
        if (value is byte[] bytes)
        {
            return bytes;
        }

        if (value is JsonElement e)
        {
            return e.EnumerateArray().Select(x => (byte)x.GetInt32()).ToArray();
        }

        if (value is System.Collections.IEnumerable enumerable)
        {
            return enumerable.Cast<object>().Select(x => (byte)ToInt32(x)).ToArray();
        }

        throw new ArgumentException("Binary-Wert muss ein Byte-Array sein.");
    }

    public static string[] ToStringArray(object value)
    {
        if (value is string[] strings)
        {
            return strings;
        }

        if (value is JsonElement e)
        {
            return e.EnumerateArray().Select(x => x.GetString() ?? string.Empty).ToArray();
        }

        if (value is System.Collections.IEnumerable enumerable)
        {
            return enumerable.Cast<object>().Select(ToStringValue).ToArray();
        }

        return [ToStringValue(value)];
    }
}