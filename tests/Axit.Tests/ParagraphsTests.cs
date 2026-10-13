using Axit.Core;

namespace Axit.Tests;

public class ParagraphsTests
{
    // Three paragraphs: "one two" at 0, "three" at 9, "" (an empty line) at 16, "four" at 18; the text ends at 22.
    private const string Text = "one two\r\nthree\r\n\r\nfour";

    [Fact]
    public void Start_of_the_paragraph_the_caret_is_in()
    {
        Assert.Equal(0, Paragraphs.StartOf(Text, 0));
        Assert.Equal(0, Paragraphs.StartOf(Text, 4));
        Assert.Equal(0, Paragraphs.StartOf(Text, 7));
        Assert.Equal(9, Paragraphs.StartOf(Text, 9));
        Assert.Equal(9, Paragraphs.StartOf(Text, 12));
        Assert.Equal(16, Paragraphs.StartOf(Text, 16));
        Assert.Equal(18, Paragraphs.StartOf(Text, 22));
        Assert.Equal(18, Paragraphs.StartOf(Text, 99));
        Assert.Equal(0, Paragraphs.StartOf(string.Empty, 0));
    }

    [Fact]
    public void Ctrl_Up_goes_to_the_start_of_the_paragraph_then_to_the_one_before()
    {
        Assert.Equal(18, Paragraphs.Previous(Text, 22));
        Assert.Equal(16, Paragraphs.Previous(Text, 18));
        Assert.Equal(9, Paragraphs.Previous(Text, 16));
        Assert.Equal(9, Paragraphs.Previous(Text, 12));
        Assert.Equal(0, Paragraphs.Previous(Text, 9));
        Assert.Equal(0, Paragraphs.Previous(Text, 3));
        Assert.Equal(0, Paragraphs.Previous(Text, 0));
    }

    [Fact]
    public void Ctrl_Down_goes_to_the_start_of_the_next_paragraph_then_to_the_end()
    {
        Assert.Equal(9, Paragraphs.Next(Text, 0));
        Assert.Equal(9, Paragraphs.Next(Text, 4));
        Assert.Equal(16, Paragraphs.Next(Text, 9));
        Assert.Equal(18, Paragraphs.Next(Text, 16));
        Assert.Equal(22, Paragraphs.Next(Text, 18));
        Assert.Equal(22, Paragraphs.Next(Text, 22));
        Assert.Equal(0, Paragraphs.Next(string.Empty, 0));
    }

    [Fact]
    public void A_long_wrapped_paragraph_is_one_step()
    {
        var text = new string('x', 2000) + "\r\nnext";
        Assert.Equal(2002, Paragraphs.Next(text, 500));
        Assert.Equal(0, Paragraphs.Previous(text, 2002));
    }
}
