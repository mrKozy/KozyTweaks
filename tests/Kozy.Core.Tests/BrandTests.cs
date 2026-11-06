using Kozy.Core;
using Xunit;

namespace Kozy.Core.Tests;

public class BrandTests
{
    [Fact]
    public void Name_IsNotEmpty()
    {
        Assert.False(string.IsNullOrWhiteSpace(Brand.Name));
    }
}