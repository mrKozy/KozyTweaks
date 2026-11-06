using System.Runtime.InteropServices;
using Kozy.Core;
using Kozy.Core.Models;

namespace Kozy.Actions;

public sealed record SpiMouseSnapshot(int Speed, int Threshold1, int Threshold2);

/// <summary>
/// Handler für spi.mouse über SystemParametersInfo(SPI_SETMOUSE) mit
/// SPIF_UPDATEINIFILE | SPIF_SENDCHANGE, damit keine Abmeldung nötig ist (5.3 / T-20).
/// </summary>
public sealed class SpiMouseHandler : IActionHandler
{
    private const uint SpiGetMouse = 0x0003;
    private const uint SpiSetMouse = 0x0004;
    private const uint SpifUpdateIniFile = 0x01;
    private const uint SpifSendChange = 0x02;
    private readonly Func<int[]> _get;
    private readonly Action<int, int, int> _set;

    public SpiMouseHandler(Func<int[]>? get = null, Action<int, int, int>? set = null)
    {
        _get = get ?? Get;
        _set = set ?? Set;
    }

    public void Validate(TweakAction action)
    {
        _ = AsMouse(action);
    }

    public object? Capture(TweakAction action)
    {
        var value = _get();
        return new SpiMouseSnapshot(value[0], value[1], value[2]);
    }

    public void Apply(TweakAction action)
    {
        var m = AsMouse(action);
        _set(m.Speed, m.Threshold1, m.Threshold2);
    }

    public bool Verify(TweakAction action)
    {
        var m = AsMouse(action);
        var value = _get();
        return value[0] == m.Speed && value[1] == m.Threshold1 && value[2] == m.Threshold2;
    }

    public void Restore(TweakAction action, object? captured)
    {
        var snap = (SpiMouseSnapshot)(captured ?? throw new InvalidOperationException("Captured-Zustand fehlt."));
        _set(snap.Speed, snap.Threshold1, snap.Threshold2);
    }

    public TweakStatus Detect(TweakAction action) => Verify(action) ? TweakStatus.Applied : TweakStatus.NotApplied;

    private static SpiMouseAction AsMouse(TweakAction a) =>
        a as SpiMouseAction ?? throw new ArgumentException($"SpiMouseHandler kann '{a.GetType().Name}' nicht verarbeiten.");

    private static int[] Get()
    {
        var value = new int[3];
        _ = SystemParametersInfo(SpiGetMouse, 0, value, 0);
        return value;
    }

    private static void Set(int speed, int threshold1, int threshold2)
    {
        var value = new[] { speed, threshold1, threshold2 };
        _ = SystemParametersInfo(SpiSetMouse, 0, value, SpifUpdateIniFile | SpifSendChange);
    }

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SystemParametersInfo(uint uiAction, uint uiParam, int[] pvParam, uint fWinIni);
}
