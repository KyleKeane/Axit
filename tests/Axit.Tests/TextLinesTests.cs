using Axit.Core;

namespace Axit.Tests;

public class TextLinesTests
{
    // Four lines: "one two" at 0 to 7, "three" at 9 to 14, an empty line at 16, "four" at 18 to 22.
    private const string Text = "one two\r\nthree\r\n\r\nfour";

    [Fact]
    public void Start_end_and_column_of_the_line_the_caret_is_in()
    {
        Assert.Equal(0, TextLines.StartOf(Text, 0));
        Assert.Equal(0, TextLines.StartOf(Text, 7));
        Assert.Equal(9, TextLines.StartOf(Text, 9));
        Assert.Equal(16, TextLines.StartOf(Text, 16));
        Assert.Equal(18, TextLines.StartOf(Text, 99));
        Assert.Equal(7, TextLines.EndOf(Text, 3));
        Assert.Equal(14, TextLines.EndOf(Text, 9));
        Assert.Equal(16, TextLines.EndOf(Text, 16));
        Assert.Equal(22, TextLines.EndOf(Text, 20));
        Assert.Equal(4, TextLines.Column(Text, 4));
        Assert.Equal(3, TextLines.Column(Text, 12));
        Assert.Equal(0, TextLines.Column(Text, 16));
        Assert.Equal(0, TextLines.StartOf(string.Empty, 0));
        Assert.Equal(0, TextLines.EndOf(string.Empty, 0));
    }

    [Fact]
    public void Ctrl_Down_goes_one_line_down_at_the_same_column_or_to_the_end_of_a_shorter_line()
    {
        Assert.Equal(13, TextLines.Down(Text, 4, 4));
        Assert.Equal(14, TextLines.Down(Text, 7, 7));
        Assert.Equal(16, TextLines.Down(Text, 12, 3));
        Assert.Equal(21, TextLines.Down(Text, 16, 3));
        Assert.Equal(22, TextLines.Down(Text, 16, 9));
    }

    [Fact]
    public void Ctrl_Up_goes_one_line_up_at_the_same_column_or_to_the_end_of_a_shorter_line()
    {
        Assert.Equal(16, TextLines.Up(Text, 20, 2));
        Assert.Equal(9, TextLines.Up(Text, 16, 0));
        Assert.Equal(13, TextLines.Up(Text, 16, 4));
        Assert.Equal(4, TextLines.Up(Text, 13, 4));
        Assert.Equal(7, TextLines.Up(Text, 14, 9));
    }

    [Fact]
    public void The_caret_stays_on_the_first_and_last_line()
    {
        Assert.Equal(4, TextLines.Up(Text, 4, 4));
        Assert.Equal(0, TextLines.Up(Text, 0, 0));
        Assert.Equal(20, TextLines.Down(Text, 20, 2));
        Assert.Equal(22, TextLines.Down(Text, 22, 4));
        Assert.Equal(0, TextLines.Down(string.Empty, 0, 0));
        Assert.Equal(0, TextLines.Up(string.Empty, 0, 0));
    }

    [Fact]
    public void A_long_wrapped_line_is_one_step_and_the_wanted_column_carries_over_a_short_line()
    {
        var text = new string('x', 2000) + "\r\nab\r\n" + new string('y', 2000);
        Assert.Equal(2004, TextLines.Down(text, 500, 500));
        // From the short line with the column still wanted at 500: the same column on the long line below.
        Assert.Equal(2006 + 500, TextLines.Down(text, 2004, 500));
        Assert.Equal(500, TextLines.Up(text, 2004, 500));
    }
}
