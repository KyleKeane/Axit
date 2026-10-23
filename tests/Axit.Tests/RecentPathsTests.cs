using Axit.Core;

namespace Axit.Tests;

/// <summary>The recent-path lists every settings class keeps (docs/axit/SPEC.md AX-9.5): the newest first, once each, ten at most, cleaned after loading.</summary>
public sealed class RecentPathsTests
{
    [Fact]
    public void Remember_puts_the_newest_first_once_whatever_its_case_and_keeps_ten()
    {
        var paths = new List<string>();
        for (var i = 0; i < 12; i++)
        {
            RecentPaths.Remember(paths, $@"C:\work\{i}.md");
        }

        RecentPaths.Remember(paths, @"c:\WORK\11.md");
        RecentPaths.Remember(paths, @"C:\work\3.md");
        Assert.Equal(RecentPaths.Max, paths.Count);
        Assert.Equal([@"C:\work\3.md", @"c:\WORK\11.md", @"C:\work\10.md"], paths.Take(3));
        Assert.DoesNotContain(@"C:\work\0.md", paths);
    }

    [Fact]
    public void A_smaller_cap_holds()
    {
        var paths = new List<string>();
        for (var i = 0; i < 5; i++)
        {
            RecentPaths.Remember(paths, $@"C:\work\{i}", max: 3);
        }

        Assert.Equal([@"C:\work\4", @"C:\work\3", @"C:\work\2"], paths);
    }

    [Fact]
    public void Sanitize_cleans_what_the_file_held()
    {
        Assert.Empty(RecentPaths.Sanitize(null));
        var list = new List<string> { " ", @"C:\a", "", @"C:\b" };
        var cleaned = RecentPaths.Sanitize(list, max: 1);
        Assert.Same(list, cleaned);
        Assert.Equal([@"C:\a"], cleaned);
    }
}
