using Kozy.Benchmark;
using Xunit;

namespace Kozy.Core.Tests;

public class BenchmarkTests
{
    [Fact]
    public void Calculate_Metrics_WithKnownDataset()
    {
        // 100 frames: 98 frames of 10ms (100 FPS), 2 frames of 20ms (50 FPS)
        var frames = new List<double>();
        for (var i = 0; i < 98; i++) frames.Add(10.0);
        frames.Add(20.0);
        frames.Add(20.0);

        var metrics = FrametimeBenchmark.Calculate(frames);

        Assert.Equal(100, metrics.TotalFrames);
        Assert.True(metrics.AvgFps > 95.0 && metrics.AvgFps < 101.0);
        Assert.Equal(10.2, metrics.AvgFrameTimeMs);
        Assert.Equal(50.0, metrics.OnePercentLowPercentileFps); // 1000 / 20ms
        Assert.Equal(50.0, metrics.OnePercentLowAverageFps);
        Assert.True(metrics.StandardDeviationMs > 0);
    }

    [Fact]
    public void ParsePresentMonCsv_ParsesValidCsvCorrectly()
    {
        const string csv = """
        Application,ProcessID,SwapChainAddress,Runtime,SyncInterval,PresentFlags,AllowsTearing,PresentMode,CPUStartTime,FrameTime,MsBetweenPresents
        Game.exe,1234,0x1234,DXGI,0,0,1,Hardware: Legacy Flip,10.0,16.66,16.66
        Game.exe,1234,0x1234,DXGI,0,0,1,Hardware: Legacy Flip,26.66,16.67,16.67
        Game.exe,1234,0x1234,DXGI,0,0,1,Hardware: Legacy Flip,43.33,16.65,16.65
        """;

        var times = FrametimeBenchmark.ParsePresentMonCsv(csv);

        Assert.Equal(3, times.Count);
        Assert.Equal(16.66, times[0]);
        Assert.Equal(16.67, times[1]);
        Assert.Equal(16.65, times[2]);
    }

    [Fact]
    public void ParsePresentMonCsv_ThrowsOnMissingFrameTimeColumn()
    {
        const string csv = """
        Application,ProcessID,SwapChainAddress
        Game.exe,1234,0x1234
        """;

        Assert.Throws<FormatException>(() => FrametimeBenchmark.ParsePresentMonCsv(csv));
    }

    [Fact]
    public async Task RunPingTestAsync_AgainstLoopback_ReturnsZeroLoss()
    {
        var options = new PingTestOptions
        {
            Host = "127.0.0.1",
            Count = 3,
            DelayBetweenPingsMs = 10,
            TimeoutMs = 1000,
        };

        var result = await NetworkBenchmark.RunPingTestAsync(options);

        Assert.Equal("127.0.0.1", result.Host);
        Assert.Equal(3, result.PacketsSent);
        Assert.Equal(3, result.PacketsReceived);
        Assert.Equal(0.0, result.PacketLossPercent);
        Assert.True(result.AvgRttMs >= 0);
    }
}
