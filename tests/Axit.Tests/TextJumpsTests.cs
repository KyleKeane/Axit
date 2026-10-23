using System.Windows.Forms;
using Axit.Core;
using Axit.Forms;

namespace Axit.Tests;

/// <summary>The shared jump of an edit control's text by Markdown structure (docs/axit/SPEC.md AX-7.2): the key pairs, the caret, what is announced, and the menu labels.</summary>
public sealed class TextJumpsTests
{
    private const string Text = "# One\r\n\r\ntext with a [link](https://x.org)\r\n\r\n## Two\r\n\r\n- item";

    private static (TextBox Text, List<string> Spoken, Func<string, bool, bool> Announce) Editor()
    {
        var box = new TextBox { Multiline = true, Text = Text };
        var spoken = new List<string>();
        return (box, spoken, (text, _) => { spoken.Add(text); return true; });
    }

    [Fact]
    public void A_pair_key_moves_the_caret_and_speaks_the_element_and_the_rest_is_left_alone()
    {
        var (box, spoken, announce) = Editor();
        var jumps = AxDown.AxDownKeys.Jumps;

        Assert.True(TextJumps.Handle(box, jumps, AxDown.AxDownKeys.NextHeading, announce));
        Assert.Equal(Text.IndexOf("## Two", StringComparison.Ordinal), box.SelectionStart);
        Assert.Equal(["Two, heading level 2"], spoken);

        Assert.True(TextJumps.Handle(box, jumps, AxDown.AxDownKeys.PreviousLink, announce));
        Assert.Equal(Text.IndexOf("[link]", StringComparison.Ordinal), box.SelectionStart);
        Assert.Equal("link, link", spoken[^1]);

        Assert.True(TextJumps.Handle(box, jumps, AxDown.AxDownKeys.NextTable, announce));
        Assert.Equal(Text.IndexOf("[link]", StringComparison.Ordinal), box.SelectionStart);   // the caret stays
        Assert.Equal("No next table", spoken[^1]);

        Assert.False(TextJumps.Handle(box, jumps, Keys.Control | Keys.S, announce));
        Assert.Equal(3, spoken.Count);
    }

    /// <summary>The heading letters every notice's text takes (AX-7.2): h, Shift+H, 1 to 6, the same values as AxClaude's transcript keys.</summary>
    [Fact]
    public void The_heading_letters_work_in_a_notice_text()
    {
        var (box, spoken, announce) = Editor();
        Assert.True(TextJumps.Handle(box, TextJump.HeadingLetters, Keys.D2, announce));
        Assert.Equal(["Two, heading level 2"], spoken);
        Assert.True(TextJumps.Handle(box, TextJump.HeadingLetters, Keys.Shift | Keys.H, announce));
        Assert.Equal("One, heading level 1", spoken[^1]);
        Assert.False(TextJumps.Handle(box, TextJump.HeadingLetters, Keys.K, announce));
        Assert.Equal(AxClaude.AxClaudeKeys.Heading, TextJump.HeadingLetters[0].Next);
        Assert.Equal(AxClaude.AxClaudeKeys.HeadingLevel6, TextJump.HeadingLetters[6].Next);
    }

    /// <summary>Every F1 text is written under Markdown headings, one per group of keys, so that the letters walk it (AX-7.2).</summary>
    [Theory]
    [InlineData("AxClaude")]
    [InlineData("AxDown")]
    [InlineData("AxJSON")]
    [InlineData("AxGit")]
    [InlineData("AxTerminal")]
    [InlineData("AxFile")]
    [InlineData("AxMedia")]
    [InlineData("AxPreview")]
    [InlineData("AxStart")]
    public void Every_F1_text_has_headings_for_the_letters_to_walk(string app)
    {
        var text = app switch
        {
            "AxClaude" => AxClaude.HelpText.Shortcuts,
            "AxDown" => AxDown.HelpText.Shortcuts,
            "AxJSON" => AxJSON.HelpText.Shortcuts,
            "AxGit" => AxGit.HelpText.Shortcuts,
            "AxTerminal" => AxTerminal.HelpText.Shortcuts,
            "AxFile" => AxFile.HelpText.Shortcuts,
            "AxMedia" => AxMedia.HelpText.Shortcuts,
            "AxPreview" => AxPreview.HelpText.Shortcuts,
            _ => AxStart.HelpText.Shortcuts,
        };
        var headings = MarkdownOutline.Headings(text);
        Assert.True(headings.Count >= 3, $"{app}: {headings.Count} headings");
        Assert.Contains(headings, heading => heading.Spoken == "Notices, heading level 2");
        Assert.StartsWith("In this text: h and Shift+H move by heading", text);
    }

    [Fact]
    public void Menu_labels_carry_nvda_s_letter_as_the_mnemonic_and_differ_within_a_submenu()
    {
        var labels = AxDown.AxDownKeys.Jumps.Select(TextJumps.MenuLabel).ToList();
        Assert.Equal(
        [
            "&Heading", "Heading level &1", "Heading level &2", "Heading level &3", "Heading level &4", "Heading level &5",
            "Heading level &6", "Lin&k", "&List", "L&ist item", "Block &quote", "&Table",
        ], labels);
        var mnemonics = labels.Select(l => char.ToUpperInvariant(l[l.IndexOf('&') + 1])).ToList();
        Assert.Equal(mnemonics.Count, mnemonics.Distinct().Count());
    }
}
