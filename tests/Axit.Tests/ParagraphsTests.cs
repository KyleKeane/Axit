using Axit.Core;

namespace Axit.Tests;

public class ParagraphsTests
{
    // Lines: "one" at 0, "" at 5, "two" at 7, "three" at 12, "" at 19, "" at 21, "four" at 23; the text ends at 27.
    // Paragraphs start at 0 (the first line), 7 (after a blank line) and 23 (after two); "three" continues "two".
    private const string Text = "one\r\n\r\ntwo\r\nthree\r\n\r\n\r\nfour";

    [Fact]
    public void Ctrl_Down_goes_to_the_next_line_with_text_after_a_blank_line_then_to_the_end()
    {
        Assert.Equal(7, Paragraphs.Next(Text, 0));
        Assert.Equal(7, Paragraphs.Next(Text, 2));
        Assert.Equal(7, Paragraphs.Next(Text, 5));
        Assert.Equal(23, Paragraphs.Next(Text, 7));
        Assert.Equal(23, Paragraphs.Next(Text, 14));
        Assert.Equal(23, Paragraphs.Next(Text, 21));
        Assert.Equal(27, Paragraphs.Next(Text, 23));
        Assert.Equal(27, Paragraphs.Next(Text, 27));
    }

    [Fact]
    public void Ctrl_Up_goes_to_the_start_of_the_paragraph_then_to_the_one_before_then_to_the_top()
    {
        Assert.Equal(23, Paragraphs.Previous(Text, 27));
        Assert.Equal(7, Paragraphs.Previous(Text, 23));
        Assert.Equal(7, Paragraphs.Previous(Text, 21));
        Assert.Equal(7, Paragraphs.Previous(Text, 14));
        Assert.Equal(7, Paragraphs.Previous(Text, 12));
        Assert.Equal(0, Paragraphs.Previous(Text, 7));
        Assert.Equal(0, Paragraphs.Previous(Text, 5));
        Assert.Equal(0, Paragraphs.Previous(Text, 2));
        Assert.Equal(0, Paragraphs.Previous(Text, 0));
    }

    [Fact]
    public void A_text_that_starts_with_a_blank_line_and_one_without_blank_lines()
    {
        const string leading = "\r\nfirst\r\nsecond";
        Assert.Equal(2, Paragraphs.Next(leading, 0));
        Assert.Equal(15, Paragraphs.Next(leading, 2));
        Assert.Equal(2, Paragraphs.Previous(leading, 10));
        Assert.Equal(0, Paragraphs.Previous(leading, 2));

        const string plain = "a\r\nb\r\nc";
        Assert.Equal(7, Paragraphs.Next(plain, 0));
        Assert.Equal(0, Paragraphs.Previous(plain, 7));
        Assert.Equal(0, Paragraphs.Next(string.Empty, 0));
        Assert.Equal(0, Paragraphs.Previous(string.Empty, 0));
    }

    [Fact]
    public void Positions_outside_the_text_are_clamped()
    {
        Assert.Equal(7, Paragraphs.Next(Text, -5));
        Assert.Equal(27, Paragraphs.Next(Text, 99));
        Assert.Equal(23, Paragraphs.Previous(Text, 99));
        Assert.Equal(0, Paragraphs.Previous(Text, -5));
    }
}
