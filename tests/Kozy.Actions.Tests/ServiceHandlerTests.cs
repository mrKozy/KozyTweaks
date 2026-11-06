using Kozy.Actions;
using Kozy.Core.Models;
using Xunit;

namespace Kozy.Actions.Tests;

public class ServiceHandlerTests
{
    [Fact]
    public void Roundtrip_Captures_Applies_And_Restores_Service_State()
    {
        var current = new ServiceSnapshot(StartType: 3, Running: true);

        var handler = new ServiceHandler(
            readSnapshot: _ => current,
            runSc: arguments =>
            {
                if (arguments[0] == "config")
                {
                    current = current with
                    {
                        StartType = arguments[3] switch
                        {
                            "auto" => 2,
                            "disabled" => 4,
                            _ => 3,
                        },
                    };
                }
                else if (arguments[0] == "stop")
                {
                    current = current with { Running = false };
                }
                else if (arguments[0] == "start")
                {
                    current = current with { Running = true };
                }

                return new CommandResult(0, string.Empty, string.Empty);
            });

        var action = new ServiceConfigAction
        {
            Name = "SysMain",
            StartMode = ServiceStartMode.Disabled,
            Stop = true,
        };

        var captured = (ServiceSnapshot)handler.Capture(action)!;
        Assert.Equal(3, captured.StartType);
        Assert.True(captured.Running);

        handler.Apply(action);
        Assert.True(handler.Verify(action));

        handler.Restore(action, captured);

        Assert.Equal(3, current.StartType);
        Assert.True(current.Running);
    }

    [Fact]
    public void Validate_Rejects_Service_Outside_Allowlist()
    {
        var handler = new ServiceHandler();
        var action = new ServiceConfigAction { Name = "Spooler", StartMode = ServiceStartMode.Manual };

        var ex = Assert.Throws<InvalidOperationException>(() => handler.Validate(action));

        Assert.Contains("nicht in der erlaubten Liste", ex.Message);
    }
}
