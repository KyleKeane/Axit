using AxFile.Core;

namespace AxFile.Tests;

/// <summary>Completion in the Go to folder field (docs/axfile/SPEC.md AF-3.3, AF-D12), over a temporary folder.</summary>
public sealed class PathCompletionTests : IDisposable
{
    private readonly TempFolder _folder = new("axfile-completion");

    public PathCompletionTests()
    {
        Directory.CreateDirectory(Path.Combine(_folder.Path, "Documents", "inner"));
        Directory.CreateDirectory(Path.Combine(_folder.Path, "downloads"));
        File.WriteAllText(Path.Combine(_folder.Path, "draft10.txt"), "x");
        File.WriteAllText(Path.Combine(_folder.Path, "draft2.txt"), "x");
        File.WriteAllText(Path.Combine(_folder.Path, "notes.md"), "x");
        File.WriteAllText(Path.Combine(_folder.Path, ".dotfile"), "x");
    }

    public void Dispose() => _folder.Dispose();

    [Fact]
    public void A_typed_path_splits_at_its_last_backslash()
    {
        Assert.Equal((@"C:\Data\", "Pu"), PathCompletion.Split(@"C:\Data\Pu", null));
        Assert.Equal((@"C:\", string.Empty), PathCompletion.Split(@"C:\", null));
        Assert.Equal((@"C:\Data\", "x"), PathCompletion.Split(" \"C:/Data/x\" ", null));
    }

    [Fact]
    public void A_name_alone_or_a_relative_folder_is_in_the_folder_shown()
    {
        Assert.Equal((_folder.Path, "do"), PathCompletion.Split("do", _folder.Path));
        Assert.Equal((Path.Combine(_folder.Path, @"Documents\"), "in"), PathCompletion.Split(@"Documents\in", _folder.Path));
    }

    [Fact]
    public void An_empty_text_a_drive_letter_or_any_text_at_This_PC_completes_among_the_drives()
    {
        Assert.Equal((null, string.Empty), PathCompletion.Split(string.Empty, null));
        Assert.Equal((null, "C:"), PathCompletion.Split("C:", _folder.Path));
        Assert.Equal((null, "c"), PathCompletion.Split("c", FolderListing.ThisPC));
        Assert.Equal((null, @"x\y"), PathCompletion.Split(@"x\y", FolderListing.ThisPC));
    }

    [Fact]
    public void Environment_variables_are_expanded()
    {
        Environment.SetEnvironmentVariable("AXFILE_COMPLETION_TEST", _folder.Path);
        Assert.Equal((_folder.Path + "\\", "no"), PathCompletion.Split(@"%AXFILE_COMPLETION_TEST%\no", null));
    }

    [Fact]
    public void Candidates_start_with_the_text_folders_first_in_natural_order_a_folder_ending_in_a_backslash()
    {
        var found = PathCompletion.Candidates(_folder.Path, "D", showHidden: false, showDotFiles: false);

        Assert.Equal(
            [
                new Completion(Path.Combine(_folder.Path, @"Documents\"), "Documents, folder"),
                new Completion(Path.Combine(_folder.Path, @"downloads\"), "downloads, folder"),
                new Completion(Path.Combine(_folder.Path, "draft2.txt"), "draft2.txt"),
                new Completion(Path.Combine(_folder.Path, "draft10.txt"), "draft10.txt"),
            ],
            found);
    }

    [Fact]
    public void An_empty_start_lists_the_whole_folder_with_period_names_as_the_list_shows_them()
    {
        Assert.Equal(5, PathCompletion.Candidates(_folder.Path + "\\", string.Empty, showHidden: false, showDotFiles: false).Count);
        Assert.Equal(6, PathCompletion.Candidates(_folder.Path, string.Empty, showHidden: false, showDotFiles: true).Count);
        Assert.Equal(Path.Combine(_folder.Path, @"Documents\inner\"), PathCompletion.Candidates(Path.Combine(_folder.Path, "Documents"), "i", false, false).Single().Text);
    }

    [Fact]
    public void A_folder_that_cannot_be_read_has_no_candidates()
    {
        Assert.Empty(PathCompletion.Candidates(Path.Combine(_folder.Path, "missing"), string.Empty, false, false));
    }

    [Fact]
    public void A_completion_is_said_with_its_place()
    {
        Assert.Equal("Documents, folder, 2 of 5", PathCompletion.Spoken(new Completion(@"C:\Documents\", "Documents, folder"), 1, 5));
    }
}
