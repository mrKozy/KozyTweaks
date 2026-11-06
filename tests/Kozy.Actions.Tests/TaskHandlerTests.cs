using Kozy.Actions;
using Xunit;

namespace Kozy.Actions.Tests;

public class TaskHandlerTests
{
    [Fact]
    public void Split_Task_Path()
    {
        var (path, name) = TaskHandler.Split(@"\Microsoft\Windows\Application Experience\Microsoft Compatibility Appraiser");

        Assert.Equal(@"\Microsoft\Windows\Application Experience\", path);
        Assert.Equal("Microsoft Compatibility Appraiser", name);
    }
}