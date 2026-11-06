using Kozy.Core;
using Kozy.Core.Models;
using Xunit;

namespace Kozy.Core.Tests;

public class JournalStoreTests
{
    [Fact]
    public void Save_And_Load_Roundtrip()
    {
        var dir = Path.Combine(Path.GetTempPath(), "kozy-journal-" + Guid.NewGuid().ToString("N"));
        try
        {
            var store = new JournalStore(dir);
            var session = new JournalSession
            {
                SessionId = "abc",
                StartedAt = new DateTimeOffset(2026, 11, 5, 12, 0, 0, TimeSpan.Zero),
                AppVersion = "1.0.0",
                OsBuild = 26100,
                RestorePoint = new RestorePointInfo { Created = true, SequenceNumber = 1 },
                Tweaks =
                [
                    new JournalTweak
                    {
                        Id = "gaming.test",
                        Status = "applied",
                        Actions = [new JournalAction { Index = 0, Type = "registry.set", Captured = 1, Verified = true }],
                    },
                ],
            };

            var path = store.Save(session);

            var loaded = store.Load(path);
            Assert.Equal("abc", loaded.SessionId);
            Assert.Equal(26100, loaded.OsBuild);
            Assert.Equal("gaming.test", Assert.Single(loaded.Tweaks).Id);
            Assert.Empty(Directory.EnumerateFiles(dir, "*.tmp-*"));
        }
        finally
        {
            if (Directory.Exists(dir))
            {
                Directory.Delete(dir, true);
            }
        }
    }
}