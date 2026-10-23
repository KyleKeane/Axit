using Axit.Core;

namespace Axit.Tests;

/// <summary>Lines and columns of an edit control's text (docs/axdown/SPEC.md AD-6.4, AD-3.6): the caret's place, and a place put back.</summary>
public sealed class TextLinesTests
{
    private const string Text = "ab\r\ncd\r\n\r\nefg";

    [Fact]
    public void The_position_counts_lines_and_columns_from_one()
    {
        Assert.Equal((1, 1, 4), TextLines.Position(Text, 0));
        Assert.Equal((1, 3, 4), TextLines.Position(Text, 2));
        Assert.Equal((2, 1, 4), TextLines.Position(Text, 4));
        Assert.Equal((3, 1, 4), TextLines.Position(Text, 8));
        Assert.Equal((4, 4, 4), TextLines.Position(Text, Text.Length));
        Assert.Equal((1, 1, 1), TextLines.Position(string.Empty, 0));
        // A caret past the text is the end.
        Assert.Equal((4, 4, 4), TextLines.Position(Text, 99));
    }

    [Fact]
    public void A_line_and_column_give_their_index_and_never_the_gap_between_cr_and_lf()
    {
        Assert.Equal(0, TextLines.IndexOf(Text, 1, 1));
        Assert.Equal(1, TextLines.IndexOf(Text, 1, 2));
        Assert.Equal(2, TextLines.IndexOf(Text, 1, 3));
        // A column past the line's end: the end of the line, before its break (index 3 would be between the CR and the LF).
        Assert.Equal(2, TextLines.IndexOf(Text, 1, 9));
        Assert.Equal(4, TextLines.IndexOf(Text, 2, 1));
        // The blank line: its start, whatever the column.
        Assert.Equal(8, TextLines.IndexOf(Text, 3, 5));
        Assert.Equal(13, TextLines.IndexOf(Text, 4, 99));
        // A line past the last: the end of the text; a column before one: the line's start.
        Assert.Equal(Text.Length, TextLines.IndexOf(Text, 9, 1));
        Assert.Equal(4, TextLines.IndexOf(Text, 2, 0));
        Assert.Equal(0, TextLines.IndexOf(string.Empty, 3, 3));
    }

    [Fact]
    public void A_place_put_back_after_a_shorter_reload_stays_on_its_line()
    {
        var (line, column, _) = TextLines.Position("hello world\r\nnext", 9);
        Assert.Equal((1, 10), (line, column));
        // The line got shorter: the caret lands at its end, not inside the line break.
        var shorter = "hi\r\nnext";
        Assert.Equal(2, TextLines.IndexOf(shorter, line, column));
        Assert.Equal((1, 3, 2), TextLines.Position(shorter, 2));
    }
}
