using AxCalc.Core;

namespace AxCalc.Tests;

/// <summary>An answer's text (docs/axcalc/SPEC.md AC-3.7), its other bases (AC-4) and its history line (AC-5.1).</summary>
public sealed class NumberTextTests
{
    [Theory]
    [InlineData(8294400d, "8294400")]
    [InlineData(-12d, "-12")]
    [InlineData(2.5d, "2.5")]
    [InlineData(1e15, "1E+15")]
    [InlineData(1e-7, "1E-07")]
    [InlineData(-0d, "0")]
    public void An_answer_is_plain_digits_when_it_can_be(double value, string expected) =>
        Assert.Equal(expected, NumberText.Format(value));

    [Fact]
    public void The_bases_are_grouped_by_four_for_reading()
    {
        Assert.Equal("255, hex FF, binary 1111 1111, octal 377", NumberText.Bases(255));
        Assert.Equal("16588800, hex FD 2000, binary 1111 1101 0010 0000 0000 0000, octal 77220000", NumberText.Bases(16588800));
        Assert.Equal("-10, hex -A, binary -1010, octal -12", NumberText.Bases(-10));
        Assert.Equal("0, hex 0, binary 0, octal 0", NumberText.Bases(0));
        Assert.Equal("255.5 is not a whole number", NumberText.Bases(255.5));
        Assert.Equal("1E+19 is too large for other bases", NumberText.Bases(1e19));
    }

    [Fact]
    public void A_code_literal_has_its_prefix_and_no_spaces()
    {
        Assert.Equal("0xFF", NumberText.Code(255, 16));
        Assert.Equal("0b11111111", NumberText.Code(255, 2));
        Assert.Equal("0o377", NumberText.Code(255, 8));
        Assert.Equal("-0xA", NumberText.Code(-10, 16));
        Assert.Null(NumberText.Code(0.5, 16));
    }

    [Fact]
    public void A_history_line_reads_expression_equals_answer()
    {
        var calculator = new Calculator();
        Assert.Equal("1920*1080*4 = 8294400", CalcEntry.From("  1920*1080*4 ", calculator.Evaluate("1920*1080*4")).Label);
        var assignment = CalcEntry.From("rate = 1.25", calculator.Evaluate("rate = 1.25"));
        Assert.Equal("rate = 1.25", assignment.Label);
        Assert.Equal("rate = 1.25", assignment.Expression);
        Assert.Equal("rate = 2.5", CalcEntry.From("rate = 1.25*2", calculator.Evaluate("rate = 1.25*2")).Label);
    }
}
