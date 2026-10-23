using Axit.Core;

namespace Axit.Tests;

/// <summary>The name of a path in a title or a label (docs/axit/SPEC.md AX-9.5): the last part, or the drive's root itself.</summary>
public sealed class PathNameTests
{
    [Theory]
    [InlineData(@"C:\src\myproject", "myproject")]
    [InlineData(@"C:\src\myproject\", "myproject")]
    [InlineData(@"C:\notes\notes.md", "notes.md")]
    [InlineData(@"C:\", @"C:\")]
    [InlineData(@"\\server\share\docs", "docs")]
    public void The_last_part_or_the_root(string path, string expected)
    {
        Assert.Equal(expected, PathName.Of(path));
    }
}
