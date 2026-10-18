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
        items = RecentlyClosed.Add(items, new("AxDown", @"C:\work\a.md"));
        items = RecentlyClosed.Add(items, new("AxClaude", @"C:\work"));
        items = RecentlyClosed.Add(items, new("AxDown", @"c:\WORK\A.MD"));
        Assert.Equal([@"c:\WORK\A.MD", @"C:\work"], items.Select(i => i.Path));

        // The same path under another app is another item: a folder AxClaude held is not AxDown's file.
        items = RecentlyClosed.Add(items, new("AxDown", @"C:\work"));
        Assert.Equal(3, items.Count);
    }

    [Fact]
    public void Ten_are_kept()
    {
        IReadOnlyList<ClosedItem> items = [];
        for (var i = 0; i < 12; i++)
        {
            items = RecentlyClosed.Add(items, new("AxDown", $@"C:\work\{i}.md"));
        }

        Assert.Equal(RecentlyClosed.Capacity, items.Count);
        Assert.Equal(@"C:\work\11.md", items[0].Path);
        Assert.Equal(@"C:\work\2.md", items[^1].Path);
    }

    [Theory]
    [InlineData("AxDown", @"C:\work\notes.md", @"notes.md (C:\work\notes.md) - AxDown")]
    [InlineData("AxClaude", @"C:\work\myproject\", @"myproject (C:\work\myproject\) - AxClaude")]
    [InlineData("AxClaude", @"C:\", @"C:\ - AxClaude")]
    public void The_title_is_the_name_the_full_path_and_the_app(string app, string path, string title) =>
        Assert.Equal(title, RecentlyClosed.Title(new(app, path)));

    [Fact]
    public void The_file_round_trips_and_a_missing_file_is_an_empty_list()
    {
        var folder = TempFolder();
        var file = Path.Combine(folder, "recently-closed.json");
        try
        {
            Assert.Empty(RecentlyClosed.Read(file));
            RecentlyClosed.Write(file, [new("AxDown", @"C:\work\a.md"), new("AxClaude", @"C:\work")]);
            var read = RecentlyClosed.Read(file);
            Assert.Equal(["AxDown", "AxClaude"], read.Select(i => i.App));
            Assert.Equal(@"C:\work", read[1].Path);
        }
        finally
        {
            Directory.Delete(folder, true);
        }
    }
}
