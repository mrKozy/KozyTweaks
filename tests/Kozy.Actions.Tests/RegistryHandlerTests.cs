using Kozy.Actions;
using Kozy.Core.Models;
using Xunit;

namespace Kozy.Actions.Tests;

public class RegistryHandlerTests
{
    private const string Hive = "HKCU";
    private const string TestPath = @"Software\kozy-test";

    private static RegistrySetAction Action(string name) =>
        new() { Hive = Hive, Path = TestPath, Name = name, Kind = RegistryValueKind.DWord, Value = 123 };

    private static RegistryValueSnapshot Snap(string name) =>
        (RegistryValueSnapshot)new RegistryHandler().Capture(Action(name))!;

    [Fact]
    public void Set_Roundtrip_When_Value_Did_Not_Exist()
    {
        var handler = new RegistryHandler();
        var name = "v-" + Guid.NewGuid().ToString("N");
        var action = Action(name);

        try
        {
            using (var key = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(TestPath))
            {
                key.DeleteValue(name, throwOnMissingValue: false);
            }

            var captured = (RegistryValueSnapshot)handler.Capture(action)!;
            Assert.False(captured.Exists);

            handler.Apply(action);
            Assert.True(handler.Verify(action));

            handler.Restore(action, captured);
            Assert.False(Snap(name).Exists);
        }
        finally
        {
            using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(TestPath, true);
            key?.DeleteValue(name, throwOnMissingValue: false);
        }
    }

    [Fact]
    public void Set_Roundtrip_Restores_Existing_Value()
    {
        var handler = new RegistryHandler();
        var name = "v-" + Guid.NewGuid().ToString("N");
        var action = Action(name);

        try
        {
            using (var key = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(TestPath))
            {
                key.SetValue(name, 7, Microsoft.Win32.RegistryValueKind.DWord);
            }

            var captured = (RegistryValueSnapshot)handler.Capture(action)!;
            Assert.True(captured.Exists);
            Assert.Equal(7, (int)captured.Value!);

            handler.Apply(action);
            Assert.True(handler.Verify(action));

            handler.Restore(action, captured);
            var after = Snap(name);
            Assert.True(after.Exists);
            Assert.Equal(7, (int)after.Value!);
        }
        finally
        {
            using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(TestPath, true);
            key?.DeleteValue(name, throwOnMissingValue: false);
        }
    }

    [Fact]
    public void Delete_Roundtrip()
    {
        var handler = new RegistryHandler();
        var name = "d-" + Guid.NewGuid().ToString("N");
        var deleteAction = new RegistryDeleteAction { Hive = Hive, Path = TestPath, Name = name };

        try
        {
            using (var key = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(TestPath))
            {
                key.SetValue(name, "abc", Microsoft.Win32.RegistryValueKind.String);
            }

            var captured = (RegistryValueSnapshot)handler.Capture(deleteAction)!;
            Assert.True(captured.Exists);

            handler.Apply(deleteAction);
            Assert.True(handler.Verify(deleteAction));

            handler.Restore(deleteAction, captured);
            Assert.Equal("abc", Microsoft.Win32.Registry.CurrentUser
                .OpenSubKey(TestPath)!.GetValue(name));
        }
        finally
        {
            using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(TestPath, true);
            key?.DeleteValue(name, throwOnMissingValue: false);
        }
    }
}