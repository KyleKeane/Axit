using AxDown.Core;

namespace AxDown.Tests;

/// <summary>Headings for the navigation keys (SPEC.md §9): what counts, what is skipped, and next and previous from a caret.</summary>
public sealed class MarkdownOutlineTests
{
    private const string Text =
        "# Title\r\n" +            // 0
        "\r\n" +                   // 9
        "text #tag here\r\n" +     // 11
        "## Install ##\r\n" +      // 27
        "   ### Deep\r\n" +        // 42
        "```\r\n" +                // 55
        "# not a heading\r\n" +    // 60
        "```\r\n" +                // 77
        "####### seven\r\n" +      // 82
        "#nospace\r\n" +           // 97
        "######\r\n" +             // 107
        "    # four spaces";       // 115

    [Fact]
    public void Headings_are_hash_lines_outside_code_fences()
    {
        var headings = MarkdownOutline.Headings(Text);
        Assert.Equal(
        [
            (0, 1, "Title", "# Title"),
            (27, 2, "Install", "## Install ##"),
            (42, 3, "Deep", "   ### Deep"),
            (107, 6, "", "######"),
        ], headings.Select(h => (h.Start, h.Level, h.Text, h.Line)).ToList());
    }

    [Fact]
    public void Next_heading_is_after_the_carets_line_and_previous_before_it()
    {
        Assert.Equal(27, MarkdownOutline.NextHeading(Text, 0)!.Start);
        Assert.Equal(27, MarkdownOutline.NextHeading(Text, 5)!.Start);      // inside the first heading's line
        Assert.Equal(27, MarkdownOutline.NextHeading(Text, 15)!.Start);     // in the text line
        Assert.Equal(42, MarkdownOutline.NextHeading(Text, 30)!.Start);
        Assert.Equal(107, MarkdownOutline.NextHeading(Text, 62)!.Start);    // the fenced one is skipped
        Assert.Null(MarkdownOutline.NextHeading(Text, 110));
        Assert.Null(MarkdownOutline.NextHeading(Text, Text.Length));

        Assert.Null(MarkdownOutline.PreviousHeading(Text, 0));
        Assert.Null(MarkdownOutline.PreviousHeading(Text, 5));
        Assert.Equal(0, MarkdownOutline.PreviousHeading(Text, 15)!.Start);
        Assert.Equal(0, MarkdownOutline.PreviousHeading(Text, 30)!.Start);  // from inside the second heading's line
        Assert.Equal(42, MarkdownOutline.PreviousHeading(Text, 90)!.Start);
        Assert.Equal(107, MarkdownOutline.PreviousHeading(Text, Text.Length)!.Start);
    }

    [Fact]
    public void Lf_only_text_and_an_empty_text_work_too()
    {
        var headings = MarkdownOutline.Headings("a\n# One\n~~~\n# no\n~~~\n## Two");
        Assert.Equal([2, 21], headings.Select(h => h.Start).ToList());
        Assert.Equal(["One", "Two"], headings.Select(h => h.Text).ToList());
        Assert.Empty(MarkdownOutline.Headings(string.Empty));
        Assert.Null(MarkdownOutline.NextHeading(string.Empty, 0));
    }

    [Fact]
    public void A_caret_past_the_end_is_treated_as_the_end()
    {
        Assert.Equal(107, MarkdownOutline.PreviousHeading(Text, Text.Length + 50)!.Start);
        Assert.Equal(27, MarkdownOutline.NextHeading(Text, -5)!.Start);
    }
}
