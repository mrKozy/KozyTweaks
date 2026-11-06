using System.Globalization;

namespace Kozy.Benchmark;

public sealed record FrametimeMetrics
{
    public required int TotalFrames { get; init; }
    public required double AvgFps { get; init; }
    public required double OnePercentLowPercentileFps { get; init; }
    public required double OnePercentLowAverageFps { get; init; }
    public required double ZeroPointOnePercentLowPercentileFps { get; init; }
    public required double ZeroPointOnePercentLowAverageFps { get; init; }
    public required double StandardDeviationMs { get; init; }
    public required double AvgFrameTimeMs { get; init; }
}

public static class FrametimeBenchmark
{
    public static FrametimeMetrics Calculate(IReadOnlyList<double> frameTimesMs)
    {
        if (frameTimesMs == null || frameTimesMs.Count == 0)
        {
            throw new ArgumentException("Keine Frametimes übergeben.", nameof(frameTimesMs));
        }

        var count = frameTimesMs.Count;
        var meanMs = frameTimesMs.Average();
        var avgFps = 1000.0 / meanMs;

        // Standardabweichung
        var sumSquares = frameTimesMs.Sum(t => Math.Pow(t - meanMs, 2));
        var stdDev = Math.Sqrt(sumSquares / count);

        // Sortierte Frametimes (aufsteigend: längste Zeit = schlechtestes Frame)
        var sorted = frameTimesMs.OrderBy(t => t).ToList();

        // 1% Low: Index für 99. Perzentil der Framezeiten (höhere Framezeit = niedrigere FPS)
        var p99Index = (int)Math.Floor(count * 0.99);
        p99Index = Math.Clamp(p99Index, 0, count - 1);
        var p99Ms = sorted[p99Index];
        var onePercentLowPercentile = 1000.0 / p99Ms;

        // 1% Low Durchschnitt (Mittelwert der schlechtesten 1% der Frametimes)
        var worst1PercentCount = Math.Max(1, (int)Math.Ceiling(count * 0.01));
        var worst1PercentAvgMs = sorted.Skip(count - worst1PercentCount).Average();
        var onePercentLowAvg = 1000.0 / worst1PercentAvgMs;

        // 0.1% Low Perzentil (99.9. Perzentil)
        var p999Index = (int)Math.Floor(count * 0.999);
        p999Index = Math.Clamp(p999Index, 0, count - 1);
        var p999Ms = sorted[p999Index];
        var zeroPointOnePercentLowPercentile = 1000.0 / p999Ms;

        // 0.1% Low Durchschnitt
        var worst01PercentCount = Math.Max(1, (int)Math.Ceiling(count * 0.001));
        var worst01PercentAvgMs = sorted.Skip(count - worst01PercentCount).Average();
        var zeroPointOnePercentLowAvg = 1000.0 / worst01PercentAvgMs;

        return new FrametimeMetrics
        {
            TotalFrames = count,
            AvgFps = Math.Round(avgFps, 1),
            AvgFrameTimeMs = Math.Round(meanMs, 2),
            OnePercentLowPercentileFps = Math.Round(onePercentLowPercentile, 1),
            OnePercentLowAverageFps = Math.Round(onePercentLowAvg, 1),
            ZeroPointOnePercentLowPercentileFps = Math.Round(zeroPointOnePercentLowPercentile, 1),
            ZeroPointOnePercentLowAverageFps = Math.Round(zeroPointOnePercentLowAvg, 1),
            StandardDeviationMs = Math.Round(stdDev, 2),
        };
    }

    public static IReadOnlyList<double> ParsePresentMonCsv(string csvContent)
    {
        var lines = csvContent.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
        if (lines.Length < 2)
        {
            return [];
        }

        var header = lines[0].Split(',');
        var frameTimeColIndex = -1;

        for (var i = 0; i < header.Length; i++)
        {
            var col = header[i].Trim('\"', ' ');
            if (col.Equals("MsBetweenPresents", StringComparison.OrdinalIgnoreCase) ||
                col.Equals("FrameTime", StringComparison.OrdinalIgnoreCase) ||
                col.Equals("msBetweenPresents", StringComparison.OrdinalIgnoreCase))
            {
                frameTimeColIndex = i;
                break;
            }
        }

        if (frameTimeColIndex == -1)
        {
            throw new FormatException("Spalte 'MsBetweenPresents' oder 'FrameTime' nicht in CSV-Kopfzeile gefunden.");
        }

        var list = new List<double>();
        for (var i = 1; i < lines.Length; i++)
        {
            var parts = lines[i].Split(',');
            if (parts.Length > frameTimeColIndex &&
                double.TryParse(parts[frameTimeColIndex].Trim('\"', ' '), NumberStyles.Float, CultureInfo.InvariantCulture, out var val) &&
                val > 0)
            {
                list.Add(val);
            }
        }

        return list;
    }
}
