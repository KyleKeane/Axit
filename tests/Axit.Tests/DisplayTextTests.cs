using Axit.Core;

namespace Axit.Tests;

/// <summary>Text from outside shown as a list's line (security audit S-42).</summary>
public sealed class DisplayTextTests
{
    [Fact]
    public void A_hidden_override_is_made_visible_and_plain_text_is_left()
    {
        Assert.Equal("invoice‹U+202E›txt.exe", DisplayText.Visible("invoice\u202Etxt.exe"));
        Assert.Equal("a‹U+001B›[31mb", DisplayText.Visible("a\u001b[31mb"));
        Assert.Equal("Café, 日本語, notes.md", DisplayText.Visible("Café, 日本語, notes.md"));
    }
}