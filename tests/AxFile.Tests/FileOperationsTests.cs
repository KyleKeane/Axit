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
        Assert.Null(await FileOperations.CopyOrMoveAsync([notes], docs, move: false));
        Assert.True(File.Exists(Path.Combine(docs, "notes.md")));
        Assert.True(File.Exists(notes));

        // A name already there gets a number: both files stay.
        Assert.Null(await FileOperations.CopyOrMoveAsync([Path.Combine(docs, "notes.md")], _folder, move: false));
        Assert.Equal(2, Directory.GetFiles(_folder).Length);
        Assert.True(File.Exists(notes));

        var sub = Path.Combine(_folder, "sub");
        Directory.CreateDirectory(sub);
        File.WriteAllText(Path.Combine(sub, "inner.txt"), "x");
        Assert.Null(await FileOperations.CopyOrMoveAsync([sub], docs, move: true));
        Assert.False(Directory.Exists(sub));
        Assert.True(File.Exists(Path.Combine(docs, "sub", "inner.txt")));

        var reason = await FileOperations.CopyOrMoveAsync([Path.Combine(_folder, "nope.txt")], docs, move: false);
        Assert.NotNull(reason);
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
