using System.Text.RegularExpressions;
using Kozy.Core.Models;

namespace Kozy.Core;

public sealed record ValidationError(string Id, string Field, string Message);

public static partial class ManifestValidator
{
    [GeneratedRegex(@"^[a-z]+(\.[a-z0-9-]+)+$")]
    private static partial Regex IdRegex();

    public static IReadOnlyList<ValidationError> Validate(TweakManifest m)
    {
        var errors = new List<ValidationError>();

        if (!IdRegex().IsMatch(m.Id))
        {
            errors.Add(new(m.Id, "id",
                $"Ungültige ID '{m.Id}'. Erwartet: Kleinbuchstaben, durch Punkte getrennte Segmente aus a-z, 0-9, '-'."));
        }

        if (m.SchemaVersion != 1)
        {
            errors.Add(new(m.Id, "schemaVersion", "Nur schemaVersion 1 wird unterstützt."));
        }

        if (m.Version < 1)
        {
            errors.Add(new(m.Id, "version", "version muss >= 1 sein."));
        }

        if (string.IsNullOrWhiteSpace(m.Name.De))
        {
            errors.Add(new(m.Id, "name.de", "Deutscher Name fehlt."));
        }

        if (string.IsNullOrWhiteSpace(m.Description.De))
        {
            errors.Add(new(m.Id, "description.de", "Deutsche Beschreibung fehlt."));
        }

        if (string.IsNullOrWhiteSpace(m.Category))
        {
            errors.Add(new(m.Id, "category", "Kategorie fehlt."));
        }

        if (m.Actions is null || m.Actions.Count == 0)
        {
            errors.Add(new(m.Id, "actions", "Mindestens eine Aktion erforderlich."));
        }

        if (m.Supports is { } s)
        {
            if (s.MinBuild <= 0)
            {
                errors.Add(new(m.Id, "supports.minBuild", "minBuild muss > 0 sein."));
            }

            if (s.MaxBuild is int max && max < s.MinBuild)
            {
                errors.Add(new(m.Id, "supports.maxBuild", "maxBuild darf nicht kleiner als minBuild sein."));
            }
        }

        return errors;
    }
}