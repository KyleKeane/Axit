using AxFile.Core;

namespace AxFile.Tests;

/// <summary>The operations over a temporary folder (docs/axfile/SPEC.md AF-4); the Recycle Bin itself is not exercised.</summary>
public sealed class FileOperationsTests : IDisposable
{
    private readonly string _folder = Path.Combine(Path.GetTempPath(), "axfile-ops-" + Guid.NewGuid().ToString("N"));

    public FileOperationsTests()
    {
        Directory.CreateDirectory(_folder);
        File.WriteAllText(Path.Combine(_folder, "notes.md"), "# Notes");
        Directory.CreateDirectory(Path.Combine(_folder, "docs"));
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

    [Theory]
    [InlineData("", "Type a name")]
    [InlineData("   ", "Type a name")]
    [InlineData(".", "That name is not allowed")]
    [InlineData("..", "That name is not allowed")]
    [InlineData("a/b", "A name cannot contain \\ / : * ? \" < > |")]
    [InlineData("what?", "A name cannot contain \\ / : * ? \" < > |")]
    public void A_bad_name_is_refused_with_a_reason(string name, string expected)
    {
        Assert.Equal(expected, FileOperations.CheckName(name));
    }

    [Theory]
    [InlineData("notes")]
    [InlineData("My Folder 2")]
    [InlineData("a.b.c")]
    public void A_good_name_passes(string name)
    {
        Assert.Null(FileOperations.CheckName(name));
    }

    [Fact]
    public void A_new_folder_is_made_and_a_taken_name_is_an_error()
    {
        var path = FileOperations.CreateFolder(_folder, " new ");
        Assert.Equal(Path.Combine(_folder, "new"), path);
        Assert.True(Directory.Exists(path));
        var error = Assert.Throws<IOException>(() => FileOperations.CreateFolder(_folder, "notes.md"));
        Assert.Contains("already", error.Message);
    }

    [Theory]
    [InlineData("notes.md", "notes.md ")]
    [InlineData("notes .md", "notes .md")]
    [InlineData("notes​.md", "notes.md")]
    [InlineData("café.txt", "café.txt")]
    [InlineData("my  file.txt", "my file.txt")]
    [InlineData("NOTES.MD‍", "notes.md")]
    public void A_name_that_reads_the_same_as_another_is_found(string typed, string existing)
    {
        Assert.Equal(existing, FileOperations.Lookalike(typed, ["a.txt", existing, "b.txt"]));
    }

    [Theory]
    [InlineData("notes.md", "notes.md")]
    [InlineData("Notes.md", "notes.md")]
    [InlineData("notes2.md", "notes.md")]
    [InlineData("note.md", "notes.md")]
    public void The_same_name_and_a_different_name_are_no_lookalikes(string typed, string existing)
    {
        Assert.Null(FileOperations.Lookalike(typed, ["a.txt", existing]));
    }

    [Fact]
    public void A_new_file_is_empty_and_the_next_one_gets_a_number()
    {
        var first = FileOperations.CreateFile(_folder);
        var second = FileOperations.CreateFile(_folder);
        Assert.Equal(Path.Combine(_folder, "New file.txt"), first);
        Assert.Equal(Path.Combine(_folder, "New file - 2.txt"), second);
        Assert.Equal(0, new FileInfo(first).Length);
        Assert.True(File.Exists(second));
    }

    [Fact]
    public void A_file_and_a_folder_are_renamed_in_place()
    {
        var file = FileOperations.Rename(Path.Combine(_folder, "notes.md"), "todo.md");
        Assert.Equal(Path.Combine(_folder, "todo.md"), file);
        Assert.True(File.Exists(file));
        Assert.False(File.Exists(Path.Combine(_folder, "notes.md")));

        // A change of case only is a rename too.
        var folder = FileOperations.Rename(Path.Combine(_folder, "docs"), "Docs");
        Assert.Equal(Path.Combine(_folder, "Docs"), folder);
        Assert.Equal(["Docs"], Directory.GetDirectories(_folder).Select(Path.GetFileName));
    }

    [Fact]
    public void A_rename_to_the_same_name_does_nothing_and_to_a_taken_name_is_an_error()
    {
        var same = FileOperations.Rename(Path.Combine(_folder, "notes.md"), "notes.md");
        Assert.Equal(Path.Combine(_folder, "notes.md"), same);
        var error = Assert.Throws<IOException>(() => FileOperations.Rename(Path.Combine(_folder, "notes.md"), "docs"));
        Assert.Contains("already", error.Message);
    }

    [Fact]
    public async Task Files_and_folders_are_copied_and_moved_into_a_folder_by_the_shell()
    {
        var docs = Path.Combine(_folder, "docs");
        var notes = Path.Combine(_folder, "notes.md");
        Assert.Null(await CopyOrMoveAsync([notes], docs, move: false));
        Assert.True(File.Exists(Path.Combine(docs, "notes.md")));
        Assert.True(File.Exists(notes));

        // A name already there gets a dash and a number: both files stay.
        Assert.Null(await CopyOrMoveAsync([Path.Combine(docs, "notes.md")], _folder, move: false));
        Assert.True(File.Exists(Path.Combine(_folder, "notes - 2.md")));
        Assert.True(File.Exists(notes));

        var sub = Path.Combine(_folder, "sub");
        Directory.CreateDirectory(sub);
        File.WriteAllText(Path.Combine(sub, "inner.txt"), "x");
        Assert.Null(await CopyOrMoveAsync([sub], docs, move: true));
        Assert.False(Directory.Exists(sub));
        Assert.True(File.Exists(Path.Combine(docs, "sub", "inner.txt")));

        // A folder whose name is taken lands under "name - 2" too.
        Directory.CreateDirectory(sub);
        Assert.Null(await CopyOrMoveAsync([sub], docs, move: true));
        Assert.True(Directory.Exists(Path.Combine(docs, "sub - 2")));

        var reason = await CopyOrMoveAsync([Path.Combine(_folder, "nope.txt")], docs, move: false);
        Assert.NotNull(reason);
    }

    private static Task<string?> CopyOrMoveAsync(IReadOnlyList<string> sources, string folder, bool move) =>
        FileOperations.CopyOrMoveAsync(sources, FileOperations.Destinations(sources, folder), move);

    [Fact]
    public void A_free_name_adds_a_dash_and_a_number_and_destinations_avoid_each_other()
    {
        Assert.Equal(Path.Combine(_folder, "new.txt"), FileOperations.FreeName(_folder, "new", ".txt"));
        Assert.Equal(Path.Combine(_folder, "notes - 2.md"), FileOperations.FreeName(_folder, "notes", ".md"));
        Assert.Equal(Path.Combine(_folder, "docs - 2"), FileOperations.FreeName(_folder, "docs", string.Empty));
        Assert.Equal(Path.Combine(_folder, "docs - 3"), FileOperations.FreeName(_folder, "docs", string.Empty, [Path.Combine(_folder, "docs - 2")]));

        var other = Path.Combine(_folder, "docs", "notes.md");
        File.WriteAllText(other, "x");
        var elsewhere = Path.Combine(_folder, "docs", "new.txt");
        File.WriteAllText(elsewhere, "x");
        Assert.Equal(
            [Path.Combine(_folder, "notes - 2.md"), Path.Combine(_folder, "new.txt")],
            FileOperations.Destinations([other, elsewhere], _folder));
        Assert.Equal(
            [Path.Combine(_folder, "new.txt"), Path.Combine(_folder, "new - 2.txt")],
            FileOperations.Destinations([elsewhere, elsewhere], _folder));
    }

    [Fact]
    public void Messages_are_one_line()
    {
        Assert.Equal("Access denied", FileOperations.Message(new UnauthorizedAccessException("x")));
        Assert.Equal("The folder is gone", FileOperations.Message(new DirectoryNotFoundException("x")));
        Assert.Equal("a b", FileOperations.Message(new IOException("a\r\nb")));
    }

    [Fact]
    public void Media_player_reasons_name_the_known_codes()
    {
        Assert.Equal("this kind of file is not supported", MediaPlayer.Reason(unchecked((int)0xC00D36C4)));
        Assert.Equal("error 0xC00D0000", MediaPlayer.Reason(unchecked((int)0xC00D0000)));
        Assert.Contains("cannot find the file", MediaPlayer.Reason(unchecked((int)0x80070002)));
    }
}
