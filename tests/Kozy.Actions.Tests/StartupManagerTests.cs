using Kozy.Actions;
using Microsoft.Win32;
using Xunit;

namespace Kozy.Actions.Tests;

public class StartupManagerTests : IDisposable
{
    private readonly string _testRunPath;
    private readonly string _testApprovedPath;
    private readonly StartupManager _manager;

    public StartupManagerTests()
    {
        var id = Guid.NewGuid().ToString("N");
        _testRunPath = $@"Software\kozy-test\StartupRun_{id}";
        _testApprovedPath = $@"Software\kozy-test\StartupApproved_{id}";
        _manager = new StartupManager(_testRunPath, _testApprovedPath);
    }

    public void Dispose()
    {
        try
        {
            Registry.CurrentUser.DeleteSubKeyTree(_testRunPath, false);
        }
        catch { }

        try
        {
            Registry.CurrentUser.DeleteSubKeyTree(_testApprovedPath, false);
        }
        catch { }
    }

    [Fact]
    public void GetAll_WhenEntryExistsWithoutApprovedKey_DefaultsToEnabled()
    {
        using (var runKey = Registry.CurrentUser.CreateSubKey(_testRunPath))
        {
            runKey.SetValue("MyCoolApp", "C:\\cool.exe");
        }

        var entries = _manager.GetAll();
        var entry = Assert.Single(entries, e => e.Name == "MyCoolApp" && e.Scope == StartupScope.CurrentUser);

        Assert.Equal("C:\\cool.exe", entry.Command);
        Assert.True(entry.IsEnabled);
    }

    [Fact]
    public void SetEnabled_False_SetsByte0x03()
    {
        using (var runKey = Registry.CurrentUser.CreateSubKey(_testRunPath))
        {
            runKey.SetValue("Discord", "C:\\discord.exe");
        }

        _manager.SetEnabled(StartupScope.CurrentUser, "Discord", false);

        var entries = _manager.GetAll();
        var entry = Assert.Single(entries, e => e.Name == "Discord" && e.Scope == StartupScope.CurrentUser);

        Assert.False(entry.IsEnabled);

        using var approvedKey = Registry.CurrentUser.OpenSubKey(_testApprovedPath);
        var bytes = Assert.IsType<byte[]>(approvedKey?.GetValue("Discord"));
        Assert.Equal(0x03, bytes[0]);
    }

    [Fact]
    public void SetEnabled_True_ReEnablesEntry()
    {
        using (var runKey = Registry.CurrentUser.CreateSubKey(_testRunPath))
        {
            runKey.SetValue("Spotify", "C:\\spotify.exe");
        }

        _manager.SetEnabled(StartupScope.CurrentUser, "Spotify", false);
        _manager.SetEnabled(StartupScope.CurrentUser, "Spotify", true);

        var entries = _manager.GetAll();
        var entry = Assert.Single(entries, e => e.Name == "Spotify" && e.Scope == StartupScope.CurrentUser);

        Assert.True(entry.IsEnabled);

        using var approvedKey = Registry.CurrentUser.OpenSubKey(_testApprovedPath);
        var bytes = Assert.IsType<byte[]>(approvedKey?.GetValue("Spotify"));
        Assert.Equal(0x02, bytes[0]);
    }
}
