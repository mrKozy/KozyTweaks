using Kozy.Actions;
using Kozy.Core.Models;
using Xunit;

namespace Kozy.Actions.Tests;

public class CleanerHandlerTests
{
    [Fact]
    public void Capture_Then_Apply_Deletes_Contents()
    {
        var dir = Path.Combine(Path.GetTempPath(), "kozy-cleaner-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);

        try
        {
            File.WriteAllText(Path.Combine(dir, "a.tmp"), new string('x', 100));

            var handler = new CleanerHandler(new Dictionary<string, string> { ["UserTemp"] = dir });
            var action = new CleanerPathsAction { Targets = ["UserTemp"] };

            var sizeBefore = (long)handler.Capture(action)!;
            Assert.True(sizeBefore > 0);

            handler.Apply(action);

            Assert.Equal(0L, (long)handler.Capture(action)!);
        }
        finally
        {
            if (Directory.Exists(dir))
            {
                Directory.Delete(dir, true);
            }
        }
    }

    [Fact]
    public void Unknown_Target_Is_Rejected()
    {
        var handler = new CleanerHandler(new Dictionary<string, string> { ["UserTemp"] = "C:\\tmp" });
        var action = new CleanerPathsAction { Targets = ["UnknownTarget"] };

        Assert.Throws<InvalidOperationException>(() => handler.Validate(action));
    }
}