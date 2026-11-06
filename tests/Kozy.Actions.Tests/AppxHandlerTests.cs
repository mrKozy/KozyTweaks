using System.Text.RegularExpressions;
using Kozy.Actions;
using Kozy.Core.Models;
using Xunit;

namespace Kozy.Actions.Tests;

public class AppxHandlerTests
{
    [Fact]
    public void Validate_Rejects_Protected_Package()
    {
        var handler = new AppxHandler();
        var action = new AppxRemoveAction { Names = ["Microsoft.WindowsStore"] };

        var ex = Assert.Throws<InvalidOperationException>(() => handler.Validate(action));

        Assert.Contains("geschützt", ex.Message);
    }

    [Fact]
    public void Capture_Apply_And_Verify_Reflect_Installed_Packages()
    {
        var installed = new Dictionary<string, string?>
        {
            ["Microsoft.BingNews"] = "Microsoft.BingNews_1.0.0.0_neutral__8wekyb3d8bbwe",
            ["Microsoft.BingWeather"] = "Microsoft.BingWeather_1.0.0.0_neutral__8wekyb3d8bbwe",
        };

        var handler = new AppxHandler(
            powershell: script =>
            {
                var match = Regex.Match(script, @"-Name '([^']+)'");
                if (match.Success)
                {
                    installed[match.Groups[1].Value] = null;
                }

                return new CommandResult(0, string.Empty, string.Empty);
            },
            installedFullName: name => installed.TryGetValue(name, out var value) ? value : null);

        var action = new AppxRemoveAction
        {
            Names = ["Microsoft.BingNews", "Microsoft.BingWeather"],
            AllUsers = true,
        };

        var captured = (AppxSnapshot)handler.Capture(action)!;
        Assert.Equal(2, captured.Packages.Count);

        handler.Apply(action);

        Assert.True(handler.Verify(action));
        Assert.Equal(TweakStatus.Applied, handler.Detect(action));
    }

    [Fact]
    public void Restore_Throws_For_Irreversible_Action()
    {
        var handler = new AppxHandler();
        var action = new AppxRemoveAction { Names = ["Microsoft.BingNews"] };

        Assert.Throws<NotSupportedException>(() => handler.Restore(action, new AppxSnapshot([])));
    }
}
