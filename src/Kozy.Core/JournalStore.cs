using System.Text.Json;
using Kozy.Core.Models;

namespace Kozy.Core;

/// <summary>
/// Schreibt Sitzungs-Journale atomar (temp + move) und liest sie wieder ein
/// (Kapitel 5.4, T-09). Eine halb geschriebene Datei trägt eine ".tmp-..."
/// Endung und wird daher nie als gültiges Journal gelesen.
/// </summary>
public sealed class JournalStore
{
    private readonly string _directory;

    public JournalStore(string directory)
    {
        _directory = directory;
    }

    public string Save(JournalSession session)
    {
        var fileName = $"{session.StartedAt:yyyyMMdd-HHmmss}-{session.SessionId}.json";
        return Save(session, fileName);
    }

    public string Save(JournalSession session, string fileName)
    {
        // Sanitize filename to prevent path traversal
        var invalidChars = Path.GetInvalidFileNameChars();
        fileName = string.Join("_", fileName.Split(invalidChars));
        fileName = fileName.Replace("\\", "").Replace("/", "").Replace("..", "");

        Directory.CreateDirectory(_directory);

        var fullPath = Path.Combine(_directory, fileName);
        var tempPath = fullPath + ".tmp-" + Guid.NewGuid().ToString("N");

        var json = JsonSerializer.Serialize(session, ManifestSerializer.Options);
        File.WriteAllText(tempPath, json);
        File.Move(tempPath, fullPath, overwrite: true);

        return fullPath;
    }

    public JournalSession Load(string filePath)
    {
        var fullPath = Path.GetFullPath(filePath);
        var baseDir = Path.GetFullPath(_directory);
        if (!fullPath.StartsWith(baseDir, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"Journal-Pfad '{filePath}' ist nicht im erlaubten Verzeichnis.");
        }
        var json = File.ReadAllText(filePath);
        return JsonSerializer.Deserialize<JournalSession>(json, ManifestSerializer.Options)
            ?? throw new JsonException($"Journal '{filePath}' konnte nicht gelesen werden.");
    }

    public IEnumerable<string> EnumerateSessionFiles() =>
        Directory.Exists(_directory)
            ? Directory.EnumerateFiles(_directory, "*.json").OrderBy(f => f)
            : [];
}