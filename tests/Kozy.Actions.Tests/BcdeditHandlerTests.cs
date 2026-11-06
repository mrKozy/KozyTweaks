using Kozy.Actions;
using Xunit;

namespace Kozy.Actions.Tests;

public class BcdeditHandlerTests
{
    [Fact]
    public void Parse_Element_Returns_Value()
    {
        const string output = """
        Windows Boot Loader
        -------------------
        identifier              {current}
        useplatformclock        Yes
        disabledynamictick      Yes
        """;

        Assert.Equal("Yes", BcdeditHandler.ParseElement(output, "useplatformclock"));
        Assert.Equal("Yes", BcdeditHandler.ParseElement(output, "disabledynamictick"));
    }

    [Fact]
    public void Parse_Element_Missing_Returns_Null()
    {
        const string output = "identifier              {current}\n";

        Assert.Null(BcdeditHandler.ParseElement(output, "useplatformclock"));
    }
}