using Axit.Core;

namespace Axit.Tests;

/// <summary>A menu item's or a button's text as spoken (docs/axit/SPEC.md AX-6.4, AX-10.2): no mnemonic marker, no trailing dots.</summary>
public sealed class MenuTextTests
{
    [Theory]
    [InlineData("Save &as...", "Save as")]
    [InlineData("&Font…", "Font")]
    [InlineData("Find && &replace", "Find & replace")]
    [InlineData("Update and &restart", "Update and restart")]
    [InlineData("Close", "Close")]
    public void The_text_without_its_marker_and_dots(string text, string expected)
    {
        Assert.Equal(expected, MenuText.Plain(text));
    }
}
