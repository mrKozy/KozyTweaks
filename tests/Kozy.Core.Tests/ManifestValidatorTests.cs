using Kozy.Core;
using Kozy.Core.Models;
using Xunit;

namespace Kozy.Core.Tests;

public class ManifestValidatorTests
{
    private static TweakManifest Manifest(string id, params TweakAction[] actions) => new()
    {
        Id = id,
        Name = new LocalizedText { De = "Test" },
        Description = new LocalizedText { De = "Test" },
        Category = "gaming",
        Actions = actions,
    };

    private static RegistrySetAction Reg() => new() { Hive = "HKCU", Path = "Test", Name = "X", Value = 0 };

    [Fact]
    public void Valid_Manifest_Has_No_Errors()
    {
        var errors = ManifestValidator.Validate(Manifest("gaming.test", Reg()));
        Assert.Empty(errors);
    }

    [Fact]
    public void Invalid_Id_Is_Rejected()
    {
        var errors = ManifestValidator.Validate(Manifest("InvalidID", Reg()));
        Assert.Contains(errors, e => e.Field == "id");
    }

    [Fact]
    public void Missing_Actions_Is_Rejected()
    {
        var errors = ManifestValidator.Validate(Manifest("gaming.test"));
        Assert.Contains(errors, e => e.Field == "actions");
    }
}