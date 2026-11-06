using Kozy.Actions;
using Kozy.Core.Models;
using Xunit;

namespace Kozy.Actions.Tests;

public class SpiMouseHandlerTests
{
    [Fact]
    public void Roundtrip_Captures_Applies_And_Restores_Mouse_Settings()
    {
        var current = new[] { 10, 6, 3 };
        var handler = new SpiMouseHandler(
            get: () => [current[0], current[1], current[2]],
            set: (speed, threshold1, threshold2) => current = [speed, threshold1, threshold2]);

        var action = new SpiMouseAction { Speed = 0, Threshold1 = 0, Threshold2 = 0 };

        var captured = (SpiMouseSnapshot)handler.Capture(action)!;
        Assert.Equal(10, captured.Speed);
        Assert.Equal(6, captured.Threshold1);
        Assert.Equal(3, captured.Threshold2);

        handler.Apply(action);
        Assert.True(handler.Verify(action));

        handler.Restore(action, captured);

        Assert.Equal([10, 6, 3], current);
    }
}
