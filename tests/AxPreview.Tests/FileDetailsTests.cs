using AxPreview.Core;

namespace AxPreview.Tests;

/// <summary>The Details list (docs/axpreview/SPEC.md AP-6.2) over a temporary file.</summary>
public sealed class FileDetailsTests : IDisposable
{
    private readonly string _folder = Path.Combine(Path.GetTempPath(), "axpreview-details-" + Guid.NewGuid().ToString("N"));

    public FileDetailsTests()
    {
        Directory.CreateDirectory(_folder);
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
    public void The_file_group_names_the_file_its_folder_size_type_and_dates()
    {
        var path = Path.Combine(_folder, "notes.txt");
        File.WriteAllBytes(path, new byte[2048]);
        var lines = FileDetails.FileGroup(path);
        Assert.Equal("Name: notes.txt", lines[0]);
        Assert.Equal($"Folder: {_folder}", lines[1]);
        Assert.Equal("Size: 2 KB, 2,048 bytes", lines[2]);
        Assert.StartsWith("Type: ", lines[3]);
        Assert.EndsWith("(.txt)", lines[3]);
        Assert.StartsWith("Modified: ", lines[4]);
        Assert.DoesNotContain(lines, line => line.StartsWith("Attributes", StringComparison.Ordinal));
    }

    [Fact]
    public void The_attributes_worth_saying_are_said_in_words()
    {
        Assert.Equal("Read-only, Hidden", FileDetails.Attributes(FileAttributes.ReadOnly | FileAttributes.Hidden | FileAttributes.Archive));
        Assert.Equal(string.Empty, FileDetails.Attributes(FileAttributes.Archive | FileAttributes.Normal));
    }

    [Fact]
    public void A_line_gives_its_name_and_its_value_and_a_heading_has_no_name()
    {
        Assert.Equal("1920 x 1080", FileDetails.ValueOf("Dimensions: 1920 x 1080"));
        Assert.Equal("Dimensions", FileDetails.NameOf("Dimensions: 1920 x 1080"));
        Assert.Equal("C:\\x: y", FileDetails.ValueOf("Folder: C:\\x: y"));
        Assert.Null(FileDetails.NameOf(FileDetails.FileHeading));
    }

    [Fact]
    public void The_list_has_the_groups_in_order_and_windows_repeats_nothing_of_the_file_group()
    {
        var path = Path.Combine(_folder, "readme.md");
        File.WriteAllText(path, "# Read me");
        var lines = FileDetails.Lines(path, ["Encoding: UTF-8"]);
        Assert.Equal(FileDetails.FileHeading, lines[0]);
        var contents = lines.ToList().IndexOf(FileDetails.ContentsHeading);
        Assert.Equal("Encoding: UTF-8", lines[contents + 1]);
        var windows = lines.ToList().IndexOf(FileDetails.WindowsHeading);
        if (windows >= 0)
        {
            foreach (var line in lines.Skip(windows + 1))
            {
                Assert.NotNull(FileDetails.NameOf(line));
                Assert.DoesNotContain(FileDetails.NameOf(line), new[] { "Name", "Size", "Type", "Modified", "Created", "Accessed", "Folder" });
            }
        }
    }
}
