using AxCalc.Core;

namespace AxCalc.Tests;

/// <summary>The expressions of docs/axcalc/SPEC.md AC-3: the notation, the precedence, the names, the errors.</summary>
public sealed class CalculatorTests
{
    private static string Answer(string expression, Calculator? calculator = null) => (calculator ?? new Calculator()).Evaluate(expression).Spoken;

    [Theory]
    [InlineData("1920*1080*4", "8294400")]
    [InlineData("2 + 3 * 4", "14")]
    [InlineData("(2 + 3) * 4", "20")]
    [InlineData("10 / 4", "2.5")]
    [InlineData("7 % 3", "1")]
    [InlineData("-7 mod 3", "-1")]
    [InlineData("2^3^2", "512")]
    [InlineData("2**10", "1024")]
    [InlineData("-2^2", "-4")]
    [InlineData("2^-1", "0.5")]
    [InlineData("5!", "120")]
    [InlineData("3! !", "720")]
    [InlineData("--3", "3")]
    [InlineData("6 × 7 ÷ 2", "21")]
    [InlineData("0.1 + 0.2", "0.3")]
    [InlineData("1/3", "0.333333333333333")]
    [InlineData("-0 * 5", "0")]
    public void Arithmetic_follows_the_usual_precedence(string expression, string expected) =>
        Assert.Equal(expected, Answer(expression));

    [Theory]
    [InlineData("0xFF", "255")]
    [InlineData("0b1010", "10")]
    [InlineData("0o17", "15")]
    [InlineData("0XfF + 1", "256")]
    [InlineData("1_000_000", "1000000")]
    [InlineData(".5", "0.5")]
    [InlineData("6.02e23", "6.02E+23")]
    [InlineData("2e3", "2000")]
    [InlineData("1.5E-3", "0.0015")]
    [InlineData("1e15", "1E+15")]
    [InlineData("999999999999999", "999999999999999")]
    public void Numbers_take_every_notation_of_AC_3_1(string expression, string expected) =>
        Assert.Equal(expected, Answer(expression));

    [Theory]
    [InlineData("sqrt(16)", "4")]
    [InlineData("cbrt(27)", "3")]
    [InlineData("abs(-3)", "3")]
    [InlineData("log(1000)", "3")]
    [InlineData("log2(1024)", "10")]
    [InlineData("ln(e)", "1")]
    [InlineData("sin(30)", "0.5")]
    [InlineData("cos(60)", "0.5")]
    [InlineData("sin(180)", "0")]
    [InlineData("tan(45)", "1")]
    [InlineData("asin(1)", "90")]
    [InlineData("round(2.5)", "3")]
    [InlineData("round(-2.5)", "-3")]
    [InlineData("round(3.14159, 2)", "3.14")]
    [InlineData("floor(-2.5)", "-3")]
    [InlineData("ceil(2.1)", "3")]
    [InlineData("trunc(-2.7)", "-2")]
    [InlineData("min(4, 2, 8)", "2")]
    [InlineData("MAX(4, 2, 8)", "8")]
    [InlineData("hypot(3, 4)", "5")]
    [InlineData("2*pi", "6.28318530717959")]
    [InlineData("π", "3.14159265358979")]
    public void Functions_and_constants_are_those_of_AC_3_3(string expression, string expected) =>
        Assert.Equal(expected, Answer(expression));

    [Fact]
    public void Radians_change_the_trigonometric_functions()
    {
        var calculator = new Calculator { Degrees = false };
        Assert.Equal("1", Answer("sin(pi/2)", calculator));
        Assert.Equal("1.5707963267949", Answer("asin(1)", calculator));
    }

    [Fact]
    public void A_leading_operator_continues_from_the_last_answer()
    {
        var calculator = new Calculator();
        Assert.Equal("Error: no answer yet to continue from", Answer("*2", calculator));
        Assert.Equal("8294400", Answer("1920*1080*4", calculator));
        Assert.Equal("16588800", Answer("*2", calculator));
        Assert.Equal("16588801", Answer("+1", calculator));
        Assert.Equal("-3", Answer("-3", calculator));
        Assert.Equal("-6", Answer("ans * 2", calculator));
        Assert.Equal("36", Answer("^2", calculator));
    }

    [Fact]
    public void An_assignment_sets_a_variable_and_answers_with_its_name()
    {
        var calculator = new Calculator();
        var set = calculator.Evaluate("rate = 1.25");
        Assert.Equal("rate = 1.25", set.Spoken);
        Assert.Equal("rate", set.Name);
        Assert.Equal("500", Answer("400 * rate", calculator));
        Assert.Equal("500", Answer("400 * RATE", calculator));
        Assert.Equal("Error: pi cannot be changed", Answer("pi = 3", calculator));
        Assert.Equal("Error: sqrt cannot be changed", Answer("sqrt = 3", calculator));
        Assert.Equal("Error: no value for x", Answer("x =", calculator));
        Assert.Equal("model = 2", Answer("model = 2", calculator));
        Assert.Equal("4", Answer("model mod 3 + model", calculator));
        Assert.Equal(["model", "rate"], calculator.Variables.Select(pair => pair.Key));
        calculator.ClearVariables();
        Assert.Empty(calculator.Variables);
    }

    [Theory]
    [InlineData("", "Error: empty")]
    [InlineData("foo + 1", "Error: unknown name foo")]
    [InlineData("2 +", "Error: unexpected end")]
    [InlineData("(2 + 3", "Error: missing closing parenthesis")]
    [InlineData("2 + 3)", "Error: unexpected )")]
    [InlineData("1/0", "Error: division by zero")]
    [InlineData("5 % 0", "Error: division by zero")]
    [InlineData("sqrt(-1)", "Error: not a real number")]
    [InlineData("ln(0)", "Error: not a real number")]
    [InlineData("asin(2)", "Error: not a real number")]
    [InlineData("tan(90)", "Error: not a real number")]
    [InlineData("10^400", "Error: too large")]
    [InlineData("2.5!", "Error: factorial takes a whole number from 0 to 170")]
    [InlineData("171!", "Error: factorial takes a whole number from 0 to 170")]
    [InlineData("sqrt(1, 2)", "Error: sqrt takes 1 argument")]
    [InlineData("sqrt 4", "Error: sqrt needs its argument in parentheses")]
    [InlineData("round(1, 16)", "Error: round takes digits from 0 to 15")]
    [InlineData("hypot(3)", "Error: hypot takes 2 arguments")]
    [InlineData("2 pi", "Error: unexpected pi")]
    [InlineData("2e", "Error: unexpected e")]
    [InlineData("0x", "Error: unexpected end of a number")]
    [InlineData("0b102", "Error: unexpected 2")]
    [InlineData("ans", "Error: no answer yet")]
    [InlineData("3 $ 4", "Error: unexpected $")]
    public void Errors_say_what_went_wrong(string expression, string expected) =>
        Assert.Equal(expected, Answer(expression));

    [Fact]
    public void An_error_keeps_the_last_answer()
    {
        var calculator = new Calculator();
        calculator.Evaluate("41 + 1");
        Assert.False(calculator.Evaluate("1/0").Ok);
        Assert.Equal(42, calculator.Answer);
    }

    [Theory]
    [InlineData("rate", true)]
    [InlineData("_x2", true)]
    [InlineData("2x", false)]
    [InlineData("pi", false)]
    [InlineData("Ans", false)]
    [InlineData("mod", false)]
    [InlineData("a-b", false)]
    public void Variable_names_follow_AC_3_4(string name, bool valid) => Assert.Equal(valid, Calculator.IsVariableName(name));
}
