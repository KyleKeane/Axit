using Axit.Core;

namespace Axit.Tests;

/// <summary>Which paths a menu may check as it opens (docs/axit/SPEC.md AX-9.4, AX-9.5): local ones, never a share.</summary>
public sealed class LocalPathsTests
{
    [Fact]
    public void A_share_and_a_rootless_path_are_not_local_and_a_fixed_drive_is()
    {
        Assert.False(LocalPaths.IsLocal(@"\\server\share\notes.md"));
        Assert.False(LocalPaths.IsLocal("notes.md"));
        Assert.False(LocalPaths.IsLocal(string.Empty));
        // The system drive is a fixed one on every Windows the tests run on.
        Assert.True(LocalPaths.IsLocal(Path.GetTempPath()));
    }

    [Fact]
    public void Existence_is_asked_of_a_local_path_only()
    {
        var asked = new List<string>();
        bool Exists(string path)
        {
            asked.Add(path);
            return false;
        }

        Assert.True(LocalPaths.ExistsQuickly(@"\\server\share\notes.md", Exists));
        Assert.Empty(asked);
        Assert.False(LocalPaths.ExistsQuickly(Path.Combine(Path.GetTempPath(), "nope"), Exists));
        Assert.Single(asked);
    }
}
