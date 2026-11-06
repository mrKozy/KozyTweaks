using Kozy.Core;
using Kozy.Core.Models;

namespace Kozy.Actions;

/// <summary>
/// Handler für cleaner.paths (Kapitel 5.3 / T-19). Die Ziel-Verzeichnisse sind
/// über den Konstruktor austauschbar, damit Tests mit Temp-Fixtures laufen können.
/// </summary>
public sealed class CleanerHandler : IActionHandler
{
    private readonly IReadOnlyDictionary<string, string> _targets;

    public CleanerHandler() : this(DefaultTargets()) { }

    public CleanerHandler(IReadOnlyDictionary<string, string> targets)
    {
        _targets = targets;
    }

    public void Validate(TweakAction action)
    {
        var cleaner = AsCleaner(action);
        foreach (var target in cleaner.Targets)
        {
            if (!_targets.ContainsKey(target))
            {
                throw new InvalidOperationException($"Unbekanntes Cleaner-Ziel '{target}'.");
            }
        }
    }

    public object? Capture(TweakAction action) =>
        AsCleaner(action).Targets.Sum(t => ComputeSize(_targets[t]));

    public void Apply(TweakAction action)
    {
        foreach (var target in AsCleaner(action).Targets)
        {
            DeleteContents(_targets[target]);
        }
    }

    public bool Verify(TweakAction action) => true;

    public void Restore(TweakAction action, object? captured)
    {
        // Irreversibel: gelöschte Dateien werden nicht wiederhergestellt.
    }

    public TweakStatus Detect(TweakAction action) => TweakStatus.Unknown;

    public static long ComputeSize(string root)
    {
        if (!Directory.Exists(root))
        {
            return 0;
        }

        long total = 0;
        try
        {
            foreach (var file in Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories))
            {
                try
                {
                    total += new FileInfo(file).Length;
                }
                catch
                {
                    // gesperrte Datei überspringen
                }
            }
        }
        catch
        {
            // Zugriff verweigert – ignoriert
        }

        return total;
    }

    public static void DeleteContents(string root, long? maxFileSize = null, params string[] allowedExtensions)
    {
        if (!Directory.Exists(root))
        {
            return;
        }

        foreach (var file in Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories))
        {
            try
            {
                // Check file size limit
                if (maxFileSize.HasValue && new FileInfo(file).Length > maxFileSize.Value)
                {
                    continue; // Skip files exceeding size limit
                }

                // Check allowed extensions filter
                if (allowedExtensions.Length > 0)
                {
                    var ext = Path.GetExtension(file);
                    if (!allowedExtensions.Contains(ext, StringComparer.OrdinalIgnoreCase))
                    {
                        continue; // Skip files not in allowed extensions
                    }
                }

                File.Delete(file);
            }
            catch
            {
                // gesperrte Datei überspringen (Fehlertoleranz)
            }
        }

        foreach (var dir in Directory.EnumerateDirectories(root, "*", SearchOption.AllDirectories)
                     .OrderByDescending(d => d.Length))
        {
            try
            {
                // Only delete empty directories
                if (!Directory.EnumerateFiles(dir, "*", SearchOption.AllDirectories).Any())
                {
                    Directory.Delete(dir, false);
                }
            }
            catch
            {
                // nicht leer oder gesperrt
            }
        }
    }

    private static CleanerPathsAction AsCleaner(TweakAction a) =>
        a as CleanerPathsAction ?? throw new ArgumentException($"CleanerHandler kann '{a.GetType().Name}' nicht verarbeiten.");

    private static IReadOnlyDictionary<string, string> DefaultTargets()
    {
        var local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var windows = Environment.GetFolderPath(Environment.SpecialFolder.Windows);

        return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["UserTemp"] = Path.GetTempPath(),
            ["WindowsTemp"] = Path.Combine(windows, "Temp"),
            ["INetCache"] = Path.Combine(local, "Microsoft", "Windows", "INetCache"),
            ["ThumbnailCache"] = Path.Combine(local, "Microsoft", "Windows", "Explorer"),
            ["D3DShaderCache"] = Path.Combine(local, "D3DSCache"),
            ["NvidiaShaderCache"] = Path.Combine(local, "NVIDIA", "DXCache"),
            ["WUDownload"] = Path.Combine(windows, "SoftwareDistribution", "Download"),
        };
    }
}