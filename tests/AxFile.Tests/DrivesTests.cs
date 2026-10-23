using AxFile.Core;

namespace AxFile.Tests;

/// <summary>The drives in completion and a drive's root as a typed path (docs/axfile/SPEC.md AF-3.3, AF-3.10).</summary>
public sealed class DrivesTests
{
    private static string SystemRoot => Path.GetPathRoot(Environment.SystemDirectory)!;

    [Fact]
    public void Completion_without_a_folder_lists_the_drives_that_start_with_the_text()
    {
        var all = PathCompletion.Candidates(null, string.Empty, showHidden: false, showDotFiles: false);
        Assert.Contains(all, completion => string.Equals(completion.Text, SystemRoot, StringComparison.OrdinalIgnoreCase));
        Assert.All(all, completion => Assert.EndsWith(", drive", completion.Label));

        var some = PathCompletion.Candidates(null, SystemRoot[..1].ToLowerInvariant(), showHidden: false, showDotFiles: false);
        Assert.NotEmpty(some);
        Assert.All(some, completion => Assert.StartsWith(SystemRoot[..1], completion.Text, StringComparison.OrdinalIgnoreCase));
        Assert.Empty(PathCompletion.Candidates(null, "~", showHidden: false, showDotFiles: false));
    }

    [Fact]
    public void A_drive_root_resolves_to_itself_with_its_separator()
    {
        Assert.Equal((SystemRoot, (string?)null), FolderListing.Resolve(SystemRoot));
        Assert.Equal((SystemRoot, (string?)null), FolderListing.Resolve(SystemRoot.TrimEnd('\\') + "/"));
        Assert.Null(FolderListing.Parent(SystemRoot));
    }
}
