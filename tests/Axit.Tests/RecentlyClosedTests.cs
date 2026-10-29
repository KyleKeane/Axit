using Axit.Core;

namespace Axit.Tests;

/// <summary>The Recently closed list of the Axit menu (docs/axit/SPEC.md AX-9.4): the order, the doubles, the cap, the titles and the file.</summary>
public sealed class RecentlyClosedTests
{
    private static string TempFolder() => Path.Combine(Path.GetTempPath(), "axit-tests", Guid.NewGuid().ToString("N"));

    [Fact]
    public void The_newest_comes_first_and_an_item_closed_again_moves_to_the_front_once()
    {
        IReadOnlyList<ClosedItem> items = [];
        items = RecentlyClosed.Add(items, new("axDown", @"C:\work\a.md"));
        items = RecentlyClosed.Add(items, new("axClaude", @"C:\work"));
        items = RecentlyClosed.Add(items, new("axDown", @"c:\WORK\A.MD"));
        Assert.Equal([@"c:\WORK\A.MD", @"C:\work"], items.Select(i => i.Path));

        // The same path under another app is another item: a folder axClaude held is not axDown's file.
        items = RecentlyClosed.Add(items, new("axDown", @"C:\work"));
        Assert.Equal(3, items.Count);
    }

    [Fact]
    public void Ten_are_kept()
    {
        IReadOnlyList<ClosedItem> items = [];
        for (var i = 0; i < 12; i++)
        {
            items = RecentlyClosed.Add(items, new("axDown", $@"C:\work\{i}.md"));
        }

        Assert.Equal(RecentlyClosed.Capacity, items.Count);
        Assert.Equal(@"C:\work\11.md", items[0].Path);
        Assert.Equal(@"C:\work\2.md", items[^1].Path);
    }

    [Theory]
    [InlineData("axDown", @"C:\work\notes.md", @"notes.md (C:\work\notes.md) - axDown")]
    [InlineData("axClaude", @"C:\work\myproject\", @"myproject (C:\work\myproject\) - axClaude, access layer for Claude Code")]
    [InlineData("axClaude", @"C:\", @"C:\ - axClaude, access layer for Claude Code")]
    public void The_title_is_the_name_the_full_path_and_the_app(string app, string path, string title) =>
        Assert.Equal(title, RecentlyClosed.Title(new(app, path)));

    [Fact]
    public void An_app_that_holds_no_file_is_kept_by_its_name_once()
    {
        IReadOnlyList<ClosedItem> items = [];
        items = RecentlyClosed.Add(items, new("axProcess", string.Empty));
        items = RecentlyClosed.Add(items, new("axDown", @"C:\work\a.md"));
        items = RecentlyClosed.Add(items, new("axProcess", string.Empty));
        Assert.Equal(["axProcess", "axDown"], items.Select(i => i.App));
        Assert.Equal("axProcess", RecentlyClosed.Title(items[0]));
    }

    [Fact]
    public void The_file_round_trips_and_a_missing_file_is_an_empty_list()
    {
        var folder = TempFolder();
        var file = Path.Combine(folder, "recently-closed.json");
        try
        {
            Assert.Empty(RecentlyClosed.Read(file));
            RecentlyClosed.Write(file, [new("axDown", @"C:\work\a.md"), new("axClaude", @"C:\work")]);
            var read = RecentlyClosed.Read(file);
            Assert.Equal(["axDown", "axClaude"], read.Select(i => i.App));
            Assert.Equal(@"C:\work", read[1].Path);
        }
        finally
        {
            Directory.Delete(folder, true);
        }
    }
}
