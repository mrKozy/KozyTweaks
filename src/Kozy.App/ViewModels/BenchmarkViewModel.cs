using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Kozy.Benchmark;
using Microsoft.Win32;
using System.IO;

namespace Kozy.App.ViewModels;

public partial class BenchmarkViewModel : ObservableObject
{
    [ObservableProperty]
    private string _targetHost = "1.1.1.1";

    [ObservableProperty]
    private int _pingCount = 20;

    [ObservableProperty]
    private int _progressPercent;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string _statusMessage = "Ready to test";

    [ObservableProperty]
    private string _minRtt = "-";

    [ObservableProperty]
    private string _avgRtt = "-";

    [ObservableProperty]
    private string _maxRtt = "-";

    [ObservableProperty]
    private string _jitter = "-";

    [ObservableProperty]
    private string _packetLoss = "-";

    // Frametime metrics
    [ObservableProperty]
    private bool _hasFrametimeResults;

    [ObservableProperty]
    private string _frametimeSummary = "";

    [ObservableProperty]
    private string _fpsAvg = "-";

    [ObservableProperty]
    private string _fps1PercentLow = "-";

    [ObservableProperty]
    private string _fps01PercentLow = "-";

    [ObservableProperty]
    private string _fpsStDev = "-";

    [RelayCommand]
    public async Task RunPingTestAsync()
    {
        if (string.IsNullOrWhiteSpace(TargetHost))
        {
            StatusMessage = "Please specify a target host.";
            return;
        }

        IsBusy = true;
        StatusMessage = $"Sending {PingCount} pings to {TargetHost}...";
        ProgressPercent = 0;

        var progress = new Progress<int>(p => ProgressPercent = p);

        var result = await NetworkBenchmark.RunPingTestAsync(new PingTestOptions
        {
            Host = TargetHost.Trim(),
            Count = PingCount,
            DelayBetweenPingsMs = 50,
            TimeoutMs = 1000,
        }, progress);

        MinRtt = $"{result.MinRttMs} ms";
        AvgRtt = $"{result.AvgRttMs} ms";
        MaxRtt = $"{result.MaxRttMs} ms";
        Jitter = $"{result.JitterMs} ms";
        PacketLoss = $"{result.PacketLossPercent}%";

        StatusMessage = $"Test complete: {result.PacketsReceived}/{result.PacketsSent} packets received ({PacketLoss} loss).";
        IsBusy = false;
    }

    [RelayCommand]
    public void ImportPresentMonCsv()
    {
        var dialog = new OpenFileDialog
        {
            Filter = "PresentMon CSV files (*.csv)|*.csv|All files (*.*)|*.*",
            Title = "Select PresentMon Benchmark CSV",
        };

        if (dialog.ShowDialog() == true)
        {
            try
            {
                var content = File.ReadAllText(dialog.FileName);
                var times = FrametimeBenchmark.ParsePresentMonCsv(content);
                if (times.Count == 0)
                {
                    StatusMessage = "No valid frametimes found in CSV.";
                    return;
                }

                var metrics = FrametimeBenchmark.Calculate(times);

                FpsAvg = $"{metrics.AvgFps:F1} FPS";
                Fps1PercentLow = $"{metrics.OnePercentLowPercentileFps:F1} FPS";
                Fps01PercentLow = $"{metrics.ZeroPointOnePercentLowPercentileFps:F1} FPS";
                FpsStDev = $"{metrics.StandardDeviationMs:F2} ms";
                FrametimeSummary = $"{metrics.TotalFrames} frames analyzed (avg {metrics.AvgFrameTimeMs:F2} ms frametime)";
                HasFrametimeResults = true;
                StatusMessage = "PresentMon benchmark imported successfully!";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error reading CSV: {ex.Message}";
            }
        }
    }
}
