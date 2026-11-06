using Kozy.Actions;
using Xunit;

namespace Kozy.Actions.Tests;

public class PowercfgHandlerTests
{
    [Fact]
    public void Parse_Ac_Dc_Index_English()
    {
        const string output = """
        Power Scheme GUID: 381b4222-f694-41f0-9685-ff5bb260df2e (Balanced)
          Subgroup GUID: 54533251-82be-4824-96c1-47b60b740d00
            Power Setting GUID: 0cc5b647-c1df-4637-891a-dec35c318583
              Current AC Power Setting Index: 0x00000064
              Current DC Power Setting Index: 0x00000005
        """;

        var (ac, dc) = PowercfgHandler.ParseAcDcIndex(output);

        Assert.Equal(100, ac);
        Assert.Equal(5, dc);
    }

    [Fact]
    public void Parse_Active_Scheme_Guid()
    {
        const string output = "Active Power Scheme GUID: 381b4222-f694-41f0-9685-ff5bb260df2e (Balanced)";

        Assert.Equal("381b4222-f694-41f0-9685-ff5bb260df2e", PowercfgHandler.ParseActiveSchemeGuid(output));
    }
}