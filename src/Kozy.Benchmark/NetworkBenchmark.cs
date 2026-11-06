using System.Net.NetworkInformation;

namespace Kozy.Benchmark;

public sealed record PingTestOptions
{
    public string Host { get; init; } = "1.1.1.1";
    public int Count { get; init; } = 20;
    public int TimeoutMs { get; init; } = 1000;
    public int DelayBetweenPingsMs { get; init; } = 100;
}

public sealed record PingTestResult
{
    public required string Host { get; init; }
    public required int PacketsSent { get; init; }
    public required int PacketsReceived { get; init; }
    public required double PacketLossPercent { get; init; }
    public required double MinRttMs { get; init; }
    public required double AvgRttMs { get; init; }
    public required double MaxRttMs { get; init; }
    public required double JitterMs { get; init; }
}

public static class NetworkBenchmark
{
    public static async Task<PingTestResult> RunPingTestAsync(
        PingTestOptions? options = null,
        IProgress<int>? progress = null,
        CancellationToken cancellationToken = default)
    {
        options ??= new PingTestOptions();
        using var pinger = new Ping();

        var rtts = new List<double>();
        var sent = 0;
        var received = 0;

        for (var i = 0; i < options.Count; i++)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                break;
            }

            sent++;
            try
            {
                var reply = await pinger.SendPingAsync(options.Host, options.TimeoutMs);
                if (reply.Status == IPStatus.Success)
                {
                    received++;
                    rtts.Add(reply.RoundtripTime);
                }
            }
            catch
            {
                // Ping-Fehler zählt als Paketverlust
            }

            progress?.Report((int)((double)(i + 1) / options.Count * 100));

            if (options.DelayBetweenPingsMs > 0 && i < options.Count - 1)
            {
                await Task.Delay(options.DelayBetweenPingsMs, cancellationToken);
            }
        }

        var loss = sent > 0 ? (double)(sent - received) / sent * 100.0 : 100.0;
        var min = rtts.Count > 0 ? rtts.Min() : 0.0;
        var max = rtts.Count > 0 ? rtts.Max() : 0.0;
        var avg = rtts.Count > 0 ? rtts.Average() : 0.0;

        // Jitter berechnen: Mittelwert der absoluten Differenzen aufeinanderfolgender RTTs
        double jitter = 0.0;
        if (rtts.Count > 1)
        {
            double diffSum = 0.0;
            for (var j = 1; j < rtts.Count; j++)
            {
                diffSum += Math.Abs(rtts[j] - rtts[j - 1]);
            }
            jitter = diffSum / (rtts.Count - 1);
        }

        return new PingTestResult
        {
            Host = options.Host,
            PacketsSent = sent,
            PacketsReceived = received,
            PacketLossPercent = Math.Round(loss, 1),
            MinRttMs = Math.Round(min, 1),
            AvgRttMs = Math.Round(avg, 1),
            MaxRttMs = Math.Round(max, 1),
            JitterMs = Math.Round(jitter, 2),
        };
    }
}
