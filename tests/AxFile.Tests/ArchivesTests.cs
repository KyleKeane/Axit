using System.IO.Compression;
using AxFile.Core;

namespace AxFile.Tests;

/// <summary>Zip files (docs/axfile/SPEC.md AF-4.12): what a compress holds, how it is named, and what an extract makes, over a temporary folder.</summary>
public sealed class ArchivesTests : IDisposable
{
    private readonly string _folder = Path.Combine(Path.GetTempPath(), "axfile-zip-" + Guid.NewGuid().ToString("N"));

    public ArchivesTests()
    {
        Directory.CreateDirectory(Path.Combine(_folder, "docs", "empty"));
        File.WriteAllText(Path.Combine(_folder, "docs", "a.txt"), "a");
        File.WriteAllText(Path.Combine(_folder, "notes.md"), "# notes");
    }

    public void Dispose()
    {
        try
        {
            Directory.Delete(_folder, recursive: true);
        }
        catch (IOException)
        {
        }
    }

    [Fact]
    public void One_entry_gives_a_zip_of_its_name_and_a_folder_keeps_its_tree()
    {
        var zip = Archives.Compress([Path.Combine(_folder, "docs")], _folder);
        Assert.Equal(Path.Combine(_folder, "docs.zip"), zip);
        using var archive = ZipFile.OpenRead(zip);
        Assert.Equal(["docs/a.txt", "docs/empty/"], archive.Entries.Select(e => e.FullName).Order());
        Assert.False(File.Exists(zip + ".tmp"));
    }

    [Fact]
    public void Several_entries_give_a_zip_named_after_the_folder_and_a_taken_name_gets_a_number()
    {
        var first = Archives.Compress([Path.Combine(_folder, "docs"), Path.Combine(_folder, "notes.md")], _folder);
        Assert.Equal(Path.Combine(_folder, Path.GetFileName(_folder) + ".zip"), first);
        var second = Archives.Compress([Path.Combine(_folder, "notes.md")], _folder);
        Assert.Equal(Path.Combine(_folder, "notes.zip"), second);
        var third = Archives.Compress([Path.Combine(_folder, "notes.md")], _folder);
        Assert.Equal(Path.Combine(_folder, "notes - 2.zip"), third);
        using var archive = ZipFile.OpenRead(first);
        Assert.Equal(["docs/a.txt", "docs/empty/", "notes.md"], archive.Entries.Select(e => e.FullName).Order());
    }

    [Fact]
    public void A_zip_extracts_into_a_new_folder_of_its_name_next_to_it()
    {
        var zip = Archives.Compress([Path.Combine(_folder, "docs")], _folder);
        var target = Archives.Extract(zip, _folder);
        Assert.Equal(Path.Combine(_folder, "docs - 2"), target);
        Assert.Equal("a", File.ReadAllText(Path.Combine(target, "docs", "a.txt")));
        Assert.True(Directory.Exists(Path.Combine(target, "docs", "empty")));
        Assert.Equal(Path.Combine(_folder, "docs - 3"), Archives.Extract(zip, _folder));
    }

    [Fact]
    public void Both_count_their_files_as_they_go()
    {
        var counts = new List<(int Done, int Total)>();
        var zip = Archives.Compress([Path.Combine(_folder, "docs"), Path.Combine(_folder, "notes.md")], _folder, (done, total) => counts.Add((done, total)));
        Assert.Equal([(1, 2), (2, 2)], counts);

        counts.Clear();
        Archives.Extract(zip, _folder, (done, total) => counts.Add((done, total)));
        Assert.Equal([(1, 2), (2, 2)], counts);
    }

    [Fact]
    public void A_folder_entry_written_with_a_backslash_is_a_folder()
    {
        var zip = Path.Combine(_folder, "old.zip");
        using (var archive = ZipFile.Open(zip, ZipArchiveMode.Create))
        {
            archive.CreateEntry("docs\\");
            using var writer = new StreamWriter(archive.CreateEntry("docs\\a.txt").Open());
            writer.Write("a");
        }

        var target = Archives.Extract(zip, _folder);
        Assert.Equal(Path.Combine(_folder, "old"), target);
        Assert.Equal("a", File.ReadAllText(Path.Combine(target, "docs", "a.txt")));
    }

    /// <summary>S-7: a zip from the internet hands its mark of the web to every file taken out of it, as File Explorer does.</summary>
    [Fact]
    public void An_extracted_file_keeps_the_zips_mark_of_the_web()
    {
        var zip = Path.Combine(_folder, "downloaded.zip");
        using (var archive = ZipFile.Open(zip, ZipArchiveMode.Create))
        {
            using var writer = new StreamWriter(archive.CreateEntry("run.txt").Open());
            writer.Write("x");
        }

        const string mark = "[ZoneTransfer]\r\nZoneId=3\r\n";
        File.WriteAllText(zip + ":Zone.Identifier", mark);
        var target = Archives.Extract(zip, _folder);
        Assert.Equal(mark, Archives.DownloadMark(Path.Combine(target, "run.txt")));
    }

    /// <summary>S-8: a link inside a folder being compressed is left out, never followed into what it points at.</summary>
    [Fact]
    public void A_link_inside_a_folder_is_not_followed_into_the_zip()
    {
        var outside = Path.Combine(_folder, "private");
        Directory.CreateDirectory(outside);
        File.WriteAllText(Path.Combine(outside, "secret.txt"), "s");
        try
        {
            Directory.CreateSymbolicLink(Path.Combine(_folder, "docs", "link"), outside);
        }
        catch (IOException)
        {
            // Making a link needs Developer Mode or an administrator; without it there is nothing to check.
            return;
        }
        catch (UnauthorizedAccessException)
        {
            return;
        }

        var zip = Archives.Compress([Path.Combine(_folder, "docs")], _folder);
        using var archive = ZipFile.OpenRead(zip);
        Assert.DoesNotContain(archive.Entries, entry => entry.FullName.Contains("secret", StringComparison.Ordinal));
    }
    [Fact]
    public void An_entry_that_would_land_outside_the_folder_is_refused()
    {
        var zip = Path.Combine(_folder, "bad.zip");
        using (var archive = ZipFile.Open(zip, ZipArchiveMode.Create))
        {
            archive.CreateEntry("../escaped.txt");
        }

        Assert.Throws<IOException>(() => Archives.Extract(zip, _folder));
        Assert.False(File.Exists(Path.Combine(Path.GetDirectoryName(_folder)!, "escaped.txt")));
    }

    [Theory]
    [InlineData("a.zip", true)]
    [InlineData("A.ZIP", true)]
    [InlineData("a.7z", false)]
    [InlineData("zip", false)]
    public void Only_a_zip_extension_is_a_zip(string name, bool expected)
    {
        Assert.Equal(expected, Archives.IsZip(Path.Combine("C:\\x", name)));
    }
}
