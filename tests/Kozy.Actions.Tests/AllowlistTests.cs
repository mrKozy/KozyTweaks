using Kozy.Actions;
using Kozy.Core.Models;
using Xunit;

namespace Kozy.Actions.Tests;

public class AllowlistTests
{
    [Fact]
    public void Registry_Path_Outside_Allowlist_Is_Rejected()
    {
        var action = new RegistrySetAction { Hive = "HKCU", Path = @"Software\NotAllowed\Thing", Name = "x", Value = 0 };
        Assert.Throws<InvalidOperationException>(() => new RegistryHandler().Validate(action));
    }

    [Fact]
    public void Registry_Blocked_Hive_Is_Rejected()
    {
        var action = new RegistrySetAction { Hive = "HKLM", Path = @"SAM\Something", Name = "x", Value = 0 };
        Assert.Throws<InvalidOperationException>(() => new RegistryHandler().Validate(action));
    }

    [Fact]
    public void Service_Outside_Allowlist_Is_Rejected()
    {
        var action = new ServiceConfigAction { Name = "NotAllowed", StartMode = ServiceStartMode.Disabled };
        Assert.Throws<InvalidOperationException>(() => new ServiceHandler().Validate(action));
    }

    [Fact]
    public void Protected_Appx_Package_Is_Rejected()
    {
        var action = new AppxRemoveAction { Names = ["Microsoft.WindowsStore"] };
        Assert.Throws<InvalidOperationException>(() => new AppxHandler().Validate(action));
    }

    [Fact]
    public void Allowed_Appx_Package_Passes()
    {
        var action = new AppxRemoveAction { Names = ["Microsoft.BingNews"] };
        new AppxHandler().Validate(action); // keine Exception
    }

    [Fact]
    public void Bcdedit_Disallowed_Element_Is_Rejected()
    {
        var action = new BcdeditSetAction { Element = "testsigning", Value = "on" };
        Assert.Throws<InvalidOperationException>(() => new BcdeditHandler().Validate(action));
    }

    [Fact]
    public void Bcdedit_Allowed_Element_Passes()
    {
        var action = new BcdeditSetAction { Element = "disabledynamictick", Value = "yes" };
        new BcdeditHandler().Validate(action); // keine Exception
    }
}