using AxFile.Core;

namespace AxFile.Tests;

/// <summary>A folder's entries as the list shows them (docs/axfile/SPEC.md AF-2.2, AF-2.4, AF-3.1 to AF-3.3), over a temporary folder.</summary>
public sealed class FolderListingTests : IDisposable
{
    private readonly TempFolder _folder = new("axfile-tests");

    public FolderListingTests()
    {
        Directory.CreateDirectory(Path.Combine(_folder.Path, "b"));
        Directory.CreateDirectory(Path.Combine(_folder.Path, "a"));
        File.WriteAllText(Path.Combine(_folder.Path, "10.txt"), "ten");
        File.WriteAllText(Path.Combine(_folder.Path, "2.txt"), "two");
        File.WriteAllText(Path.Combine(_folder.Path, "Zed.mp3"), "not really");
        var hidden = Path.Combine(_folder.Path, "hidden.txt");
        File.WriteAllText(hidden, "x");
        File.SetAttributes(hidden, FileAttributes.Hidden);
        Directory.CreateDirectory(Path.Combine(_folder.Path, ".config"));
        File.WriteAllText(Path.Combine(_folder.Path, ".env"), "x");
    }

    public void Dispose() => _folder.Dispose();

    [Fact]
    public void Folders_first_then_files_in_natural_order_without_hidden_or_dot_entries_which_are_counted()
    {
        var listing = FolderListing.Read(_folder.Path, showHidden: false, showDotFiles: false);
        var entries = listing.Entries;
        Assert.Equal(["a", "b", "2.txt", "10.txt", "Zed.mp3"], entries.Select(e => e.Name));
        Assert.Equal(["a, folder", "b, folder", "2.txt", "10.txt", "Zed.mp3"], entries.Select(e => e.Label));
        Assert.True(entries[0].IsFolder);
        Assert.Equal(3, entries[2].Size);
        Assert.Equal(Path.Combine(_folder.Path, "2.txt"), entries[2].Path);
        Assert.Equal(1, listing.Hidden);
        Assert.Equal(2, listing.Dot);
    }

    [Fact]
    public void Hidden_entries_and_dot_entries_show_when_asked()
    {
        var hidden = FolderListing.Read(_folder.Path, showHidden: true, showDotFiles: false);
        Assert.Contains(hidden.Entries, e => e.Name == "hidden.txt");
        Assert.Equal(0, hidden.Hidden);
        Assert.Equal(2, hidden.Dot);
        var dot = FolderListing.Read(_folder.Path, showHidden: false, showDotFiles: true);
        Assert.Equal([".config", "a", "b", ".env", "2.txt", "10.txt", "Zed.mp3"], dot.Entries.Select(e => e.Name));
        Assert.Equal(0, dot.Dot);
    }

    [Fact]
    public void The_not_shown_line_names_each_kind_that_was_left_out()
    {
        Assert.Equal("Not shown: 3 hidden", FolderListing.NotShownText(3, 0));
        Assert.Equal("Not shown: 2 starting with a period", FolderListing.NotShownText(0, 2));
        Assert.Equal("Not shown: 3 hidden, 2 starting with a period", FolderListing.NotShownText(3, 2));
    }

    [Fact]
    public void A_missing_folder_throws()
    {
        Assert.ThrowsAny<IOException>(() => FolderListing.Read(Path.Combine(_folder.Path, "nope"), showHidden: false, showDotFiles: false));
    }

    [Fact]
    public void Sort_is_natural_and_stable()
    {
        var sorted = FolderListing.Sort(
        [
            new Entry("file 10", "x", false, 0, DateTime.Now),
            new Entry("Folder", "x", true, 0, DateTime.Now),
            new Entry("file 9", "x", false, 0, DateTime.Now),
            new Entry("another", "x", true, 0, DateTime.Now),
        ]);
        Assert.Equal(["another", "Folder", "file 9", "file 10"], sorted.Select(e => e.Name));
    }


    [Fact]
    public void Sort_by_date_and_by_size_keep_folders_first_and_break_ties_by_name()
    {
        var when = new DateTime(2026, 10, 18);
        Entry[] entries =
        [
            new("old.txt", "x", false, 500, when.AddDays(-2)),
            new("new.txt", "x", false, 10, when),
            new("big.txt", "x", false, 5000, when.AddDays(-1)),
            new("same.txt", "x", false, 10, when),
            new("docs", "x", true, 0, when.AddDays(-9)),
        ];
        Assert.Equal(["docs", "new.txt", "same.txt", "big.txt", "old.txt"], FolderListing.Sort(entries, SortBy.Date).Select(e => e.Name));
        Assert.Equal(["docs", "big.txt", "old.txt", "new.txt", "same.txt"], FolderListing.Sort(entries, SortBy.Size).Select(e => e.Name));
        Assert.Equal(["docs", "big.txt", "new.txt", "old.txt", "same.txt"], FolderListing.Arrange(entries, SortBy.Name, string.Empty).Select(e => e.Name));
    }

    [Fact]
    public void The_filter_keeps_the_names_that_contain_the_text_in_any_case()
    {
        Entry[] entries =
        [
            new("Chapter 1.mp3", "x", false, 0, DateTime.Now),
            new("notes.md", "x", false, 0, DateTime.Now),
            new("chap", "x", true, 0, DateTime.Now),
        ];
        Assert.Equal(["chap", "Chapter 1.mp3"], FolderListing.Arrange(entries, SortBy.Name, "CHAP").Select(e => e.Name));
        Assert.Equal(3, FolderListing.Filter(entries, string.Empty).Count);
        Assert.Empty(FolderListing.Filter(entries, "zzz"));
        Assert.Equal("date", FolderListing.SortName(SortBy.Date));
    }

    [Theory]
    [InlineData(24, "24 items")]
    [InlineData(1, "1 item")]
    [InlineData(0, "Empty")]
    public void The_list_is_named_with_the_count_alone(int count, string expected)
    {
        Assert.Equal(expected, FolderListing.ListName(count));
    }

    [Fact]
    public void With_a_filter_the_list_is_named_with_the_match_count_and_the_text()
    {
        Assert.Equal("3 of 24 items, filter chap", FolderListing.ListName(3, 24, "chap"));
        Assert.Equal("0 of 24 items, filter zzz", FolderListing.ListName(0, 24, "zzz"));
        Assert.Equal("24 items", FolderListing.ListName(24, 24, string.Empty));
    }

    [Fact]
    public void Information_names_the_size_or_the_folder_and_the_date()
    {
        var when = new DateTime(2026, 10, 18, 14, 2, 30);
        Assert.Equal("report.pdf, 1.2 MB, modified 2026-10-18 14:02", FolderListing.Information(new Entry("report.pdf", "x", false, 1258291, when)));
        Assert.Equal("docs, folder, modified 2026-10-18 14:02", FolderListing.Information(new Entry("docs", "x", true, 0, when)));
    }

    [Fact]
    public void This_PC_lists_the_ready_drives_as_drive_entries_with_their_free_space()
    {
        var drives = FolderListing.Drives();
        var systemRoot = Path.GetPathRoot(Environment.SystemDirectory)!;
        var system = Assert.Single(drives, d => string.Equals(d.Path, systemRoot, StringComparison.OrdinalIgnoreCase));
        Assert.True(system.IsDrive);
        Assert.True(system.IsFolder);
        Assert.EndsWith($"({systemRoot.TrimEnd('\\')}), drive", system.Label);
        Assert.Contains(" free of ", FolderListing.Information(system));
        Assert.True(FolderListing.IsThisPC(FolderListing.ThisPC));
        Assert.False(FolderListing.IsThisPC("C:\\This PC"));
    }

    [Fact]
    public void The_parent_stops_at_the_drive()
    {
        Assert.Equal("C:\\a", FolderListing.Parent("C:\\a\\b"));
        Assert.Equal("C:\\a", FolderListing.Parent("C:\\a\\b\\"));
        Assert.Equal("C:\\", FolderListing.Parent("C:\\a"));
        Assert.Null(FolderListing.Parent("C:\\"));
    }

    [Fact]
    public void A_typed_path_resolves_with_variables_and_files()
    {
        Environment.SetEnvironmentVariable("AXFILE_TEST_FOLDER", _folder.Path);
        Assert.Equal((_folder.Path, (string?)null), FolderListing.Resolve("%AXFILE_TEST_FOLDER%"));
        Assert.Equal((_folder.Path, (string?)null), FolderListing.Resolve($"  \"{_folder.Path}\\\"  "));
        var file = Path.Combine(_folder.Path, "2.txt");
        Assert.Equal((_folder.Path, file), FolderListing.Resolve(file));
        Assert.Null(FolderListing.Resolve(Path.Combine(_folder.Path, "nope")));
        Assert.Null(FolderListing.Resolve(string.Empty));
        Assert.Null(FolderListing.Resolve("   "));
    }
}
