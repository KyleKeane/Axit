using System.Globalization;
using System.Text.RegularExpressions;

namespace AxCalc.Core;

/// <summary>
/// What one Enter in the field gave (SPEC.md AC-3): the value and its text, the variable it set (an assignment,
/// AC-3.6), or the error to say (AC-3.8). <see cref="Spoken"/> is the announcement.
/// </summary>
public sealed record Evaluation(double Value, string Answer, string? Name, string? Error)
{
    public bool Ok => Error is null;

    /// <summary>"8294400", "rate = 1.25" or "Error: division by zero" (AC-8).</summary>
    public string Spoken => Error is not null ? "Error: " + Error : Name is null ? Answer : $"{Name} = {Answer}";

    public static Evaluation Failed(string error) => new(0, string.Empty, null, error);
}

/// <summary>
/// axCalc's evaluator (SPEC.md AC-3): a recursive descent parser over doubles, with the variables, the last answer
/// and the angle unit. Plain code with no library (AC-D3): one method per level of the grammar, loosest first.
/// <code>
/// expression := term (("+" | "-") term)*
/// term       := unary (("*" | "×" | "/" | "÷" | "%" | "mod") unary)*
/// unary      := ("-" | "+") unary | power
/// power      := postfix (("^" | "**") unary)?        right to left, so -2^2 is -4 and 2^3^2 is 512
/// postfix    := primary "!"*
/// primary    := number | name | name "(" arguments ")" | "(" expression ")"
/// </code>
/// </summary>
public sealed partial class Calculator
{
    /// <summary>The functions of AC-3.3, for the reserved names and the help.</summary>
    public static readonly IReadOnlyList<string> Functions =
    [
        "sqrt", "cbrt", "abs", "exp", "ln", "log", "log2", "sin", "cos", "tan", "asin", "acos", "atan", "floor", "ceil",
        "trunc", "round", "min", "max", "hypot",
    ];

    private static readonly HashSet<string> Reserved = new(Functions.Concat(["pi", "π", "e", "ans", "mod"]), StringComparer.OrdinalIgnoreCase);

    private readonly Dictionary<string, double> _variables = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>The last answer, which <c>ans</c> and a leading operator continue from (AC-3.5); null before the first.</summary>
    public double? Answer { get; set; }

    /// <summary>The trigonometric functions take and give degrees, or radians when false (AC-3.3).</summary>
    public bool Degrees { get; set; } = true;

    /// <summary>The variables set with <c>name = expression</c>, in the order of their names.</summary>
    public IReadOnlyList<KeyValuePair<string, double>> Variables =>
        _variables.OrderBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase).ToList();

    /// <summary>Whether a name can be a variable: a letter or underscore, then letters, digits and underscores, and none of the constants, functions or <c>ans</c> (AC-3.4, AC-3.6).</summary>
    public static bool IsVariableName(string name) => NameRegex().IsMatch(name) && !Reserved.Contains(name);

    public void SetVariable(string name, double value) => _variables[name] = value;

    public void ClearVariables() => _variables.Clear();

    /// <summary>One expression or assignment: the value becomes the last answer when it was answered.</summary>
    public Evaluation Evaluate(string text)
    {
        var trimmed = text.Trim();
        if (trimmed.Length == 0)
        {
            return Evaluation.Failed("empty");
        }

        string? name = null;
        if (AssignmentRegex().Match(trimmed) is { Success: true } assignment)
        {
            name = assignment.Groups[1].Value;
            if (Reserved.Contains(name))
            {
                return Evaluation.Failed($"{name} cannot be changed");
            }

            trimmed = assignment.Groups[2].Value.Trim();
            if (trimmed.Length == 0)
            {
                return Evaluation.Failed($"no value for {name}");
            }
        }

        if ("+*/×÷%^".Contains(trimmed[0]))
        {
            // AC-3.5: "*2" continues from the last answer; a leading minus is a negative number instead.
            if (Answer is null)
            {
                return Evaluation.Failed("no answer yet to continue from");
            }

            trimmed = "ans" + trimmed;
        }

        double value;
        try
        {
            value = new Parser(trimmed, this).ParseAll();
        }
        catch (CalcException ex)
        {
            return Evaluation.Failed(ex.Message);
        }

        if (double.IsNaN(value))
        {
            return Evaluation.Failed("not a real number");
        }

        if (double.IsInfinity(value))
        {
            return Evaluation.Failed("too large");
        }

        if (value == 0)
        {
            value = 0; // -0 is 0 (AC-3.7)
        }

        if (name is not null)
        {
            _variables[name] = value;
        }

        Answer = value;
        return new Evaluation(value, NumberText.Format(value), name, null);
    }

    // "rate = 1.25": a name, one equals sign, the expression.
    [GeneratedRegex(@"^([A-Za-z_][A-Za-z0-9_]*)\s*=(?!=)(.*)$")]
    private static partial Regex AssignmentRegex();

    [GeneratedRegex(@"^[A-Za-z_][A-Za-z0-9_]*$")]
    private static partial Regex NameRegex();

    /// <summary>An error in the expression; its message is what follows "Error: " (AC-3.8).</summary>
    private sealed class CalcException(string message) : Exception(message);

    /// <summary>One expression, read left to right; each method is a level of the grammar above.</summary>
    private sealed class Parser(string text, Calculator calculator)
    {
        private int _position;

        public double ParseAll()
        {
            var value = Expression();
            SkipSpaces();
            if (_position < text.Length)
            {
                throw Unexpected();
            }

            return value;
        }

        private double Expression()
        {
            var value = Term();
            while (true)
            {
                SkipSpaces();
                if (Eat('+'))
                {
                    value += Term();
                }
                else if (Eat('-'))
                {
                    value -= Term();
                }
                else
                {
                    return value;
                }
            }
        }

        private double Term()
        {
            var value = Unary();
            while (true)
            {
                SkipSpaces();
                if (At("**"))
                {
                    return value;
                }

                if (Eat('*') || Eat('×'))
                {
                    value *= Unary();
                }
                else if (Eat('/') || Eat('÷'))
                {
                    value /= Divisor();
                }
                else if (Eat('%') || EatWord("mod"))
                {
                    value %= Divisor();
                }
                else
                {
                    return value;
                }
            }
        }

        private double Divisor()
        {
            var divisor = Unary();
            return divisor == 0 ? throw new CalcException("division by zero") : divisor;
        }

        private double Unary()
        {
            SkipSpaces();
            if (Eat('-'))
            {
                return -Unary();
            }

            return Eat('+') ? Unary() : Power();
        }

        private double Power()
        {
            var value = Postfix();
            SkipSpaces();
            if (At("**"))
            {
                _position += 2;
                return Math.Pow(value, Unary());
            }

            return Eat('^') ? Math.Pow(value, Unary()) : value;
        }

        private double Postfix()
        {
            var value = Primary();
            SkipSpaces();
            while (Eat('!'))
            {
                value = Factorial(value);
                SkipSpaces();
            }

            return value;
        }

        private double Primary()
        {
            SkipSpaces();
            if (_position >= text.Length)
            {
                throw new CalcException("unexpected end");
            }

            var c = text[_position];
            if (Eat('('))
            {
                var value = Expression();
                SkipSpaces();
                return Eat(')') ? value : throw new CalcException("missing closing parenthesis");
            }

            if (char.IsAsciiDigit(c) || c == '.')
            {
                return Number();
            }

            if (char.IsAsciiLetter(c) || c is '_' or 'π')
            {
                return Name();
            }

            throw Unexpected();
        }

        /// <summary>AC-3.1: a decimal number with its fraction and exponent, or a 0x, 0b, 0o integer; underscores between digits are separators.</summary>
        private double Number()
        {
            if (text[_position] == '0' && _position + 1 < text.Length && "xXbBoO".Contains(text[_position + 1]))
            {
                var radix = char.ToLowerInvariant(text[_position + 1]) switch { 'x' => 16, 'b' => 2, _ => 8 };
                _position += 2;
                var start = _position;
                double whole = 0;
                while (_position < text.Length && (DigitValue(text[_position]) is var digit && digit >= 0 && digit < radix || text[_position] == '_'))
                {
                    if (text[_position] != '_')
                    {
                        whole = whole * radix + DigitValue(text[_position]);
                    }

                    _position++;
                }

                return _position == start ? throw new CalcException("unexpected end of a number") : whole;
            }

            var begin = _position;
            ReadDigits();
            if (_position < text.Length && text[_position] == '.')
            {
                _position++;
                ReadDigits();
            }

            if (_position < text.Length && text[_position] is 'e' or 'E')
            {
                // An exponent only when a digit follows (with its sign): "2e3" is 2000, "2e" is 2 times e, which is an error.
                var mark = _position + 1;
                if (mark < text.Length && text[mark] is '+' or '-')
                {
                    mark++;
                }

                if (mark < text.Length && char.IsAsciiDigit(text[mark]))
                {
                    _position = mark;
                    ReadDigits();
                }
            }

            var digits = text[begin.._position].Replace("_", string.Empty);
            if (digits == ".")
            {
                throw new CalcException("unexpected .");
            }

            return double.Parse(digits, NumberStyles.Float, CultureInfo.InvariantCulture);
        }

        private void ReadDigits()
        {
            while (_position < text.Length && (char.IsAsciiDigit(text[_position]) || text[_position] == '_'))
            {
                _position++;
            }
        }

        private static int DigitValue(char c) => c switch
        {
            >= '0' and <= '9' => c - '0',
            >= 'a' and <= 'f' => c - 'a' + 10,
            >= 'A' and <= 'F' => c - 'A' + 10,
            _ => -1,
        };

        /// <summary>A constant, a variable, <c>ans</c>, or a function with its arguments (AC-3.3, AC-3.4).</summary>
        private double Name()
        {
            var word = Word();
            SkipSpaces();
            if (Eat('('))
            {
                var arguments = new List<double>();
                SkipSpaces();
                if (!Eat(')'))
                {
                    do
                    {
                        arguments.Add(Expression());
                        SkipSpaces();
                    }
                    while (Eat(','));

                    if (!Eat(')'))
                    {
                        throw new CalcException("missing closing parenthesis");
                    }
                }

                return Call(word.ToLowerInvariant(), arguments);
            }

            switch (word.ToLowerInvariant())
            {
                case "pi":
                case "π":
                    return Math.PI;
                case "e":
                    return Math.E;
                case "ans":
                    return calculator.Answer ?? throw new CalcException("no answer yet");
            }

            if (Functions.Contains(word.ToLowerInvariant()))
            {
                throw new CalcException($"{word.ToLowerInvariant()} needs its argument in parentheses");
            }

            return calculator._variables.TryGetValue(word, out var value) ? value : throw new CalcException($"unknown name {word}");
        }

        private double Call(string name, List<double> arguments)
        {
            double One()
            {
                return arguments.Count == 1 ? arguments[0] : throw new CalcException($"{name} takes 1 argument");
            }

            double Positive(double x)
            {
                return x > 0 ? x : throw new CalcException("not a real number");
            }

            return name switch
            {
                "sqrt" => Math.Sqrt(One()),
                "cbrt" => Math.Cbrt(One()),
                "abs" => Math.Abs(One()),
                "exp" => Math.Exp(One()),
                "ln" => Math.Log(Positive(One())),
                "log" => Math.Log10(Positive(One())),
                "log2" => Math.Log2(Positive(One())),
                "sin" => Trig(Math.Sin(ToRadians(One()))),
                "cos" => Trig(Math.Cos(ToRadians(One()))),
                "tan" => Tangent(One()),
                "asin" => FromRadians(Math.Asin(One())),
                "acos" => FromRadians(Math.Acos(One())),
                "atan" => FromRadians(Math.Atan(One())),
                "floor" => Math.Floor(One()),
                "ceil" => Math.Ceiling(One()),
                "trunc" => Math.Truncate(One()),
                "round" => Round(arguments),
                "min" => arguments.Count > 0 ? arguments.Min() : throw new CalcException("min takes 1 or more arguments"),
                "max" => arguments.Count > 0 ? arguments.Max() : throw new CalcException("max takes 1 or more arguments"),
                "hypot" => arguments.Count == 2 ? Math.Sqrt(arguments[0] * arguments[0] + arguments[1] * arguments[1]) : throw new CalcException("hypot takes 2 arguments"),
                _ => throw new CalcException($"unknown function {name}"),
            };
        }

        private double ToRadians(double x) => calculator.Degrees ? x * Math.PI / 180 : x;

        private double FromRadians(double x) => calculator.Degrees ? Trig(x * 180 / Math.PI) : x;

        /// <summary>sin(30°) is 0.49999999999999994 in doubles: rounded at the 15th place, the answer is 0.5, as on paper.</summary>
        private static double Trig(double x) => Math.Round(x, 15);

        private double Tangent(double x)
        {
            if (calculator.Degrees && Math.Abs(x % 180) == 90)
            {
                throw new CalcException("not a real number");
            }

            return Trig(Math.Tan(ToRadians(x)));
        }

        private static double Round(List<double> arguments)
        {
            if (arguments.Count is < 1 or > 2)
            {
                throw new CalcException("round takes 1 or 2 arguments");
            }

            var digits = arguments.Count == 2 ? arguments[1] : 0;
            if (digits != Math.Floor(digits) || digits is < 0 or > 15)
            {
                throw new CalcException("round takes digits from 0 to 15");
            }

            return Math.Round(arguments[0], (int)digits, MidpointRounding.AwayFromZero);
        }

        private static double Factorial(double n)
        {
            if (n != Math.Floor(n) || n is < 0 or > 170)
            {
                throw new CalcException("factorial takes a whole number from 0 to 170");
            }

            double result = 1;
            for (var i = 2; i <= (int)n; i++)
            {
                result *= i;
            }

            return result;
        }

        private string Word()
        {
            var start = _position;
            if (text[_position] == 'π')
            {
                _position++;
                return "π";
            }

            while (_position < text.Length && (char.IsAsciiLetterOrDigit(text[_position]) || text[_position] == '_'))
            {
                _position++;
            }

            return text[start.._position];
        }

        private CalcException Unexpected()
        {
            if (_position >= text.Length)
            {
                return new CalcException("unexpected end");
            }

            if (char.IsAsciiLetter(text[_position]) || text[_position] == '_')
            {
                return new CalcException("unexpected " + Word());
            }

            return new CalcException("unexpected " + text[_position]);
        }

        private void SkipSpaces()
        {
            while (_position < text.Length && char.IsWhiteSpace(text[_position]))
            {
                _position++;
            }
        }

        private bool At(string token) => string.CompareOrdinal(text, _position, token, 0, token.Length) == 0;

        private bool Eat(char c)
        {
            if (_position < text.Length && text[_position] == c)
            {
                _position++;
                return true;
            }

            return false;
        }

        /// <summary>A word operator ("mod") only as a whole word, so that a variable "model" is not "mod" and "el".</summary>
        private bool EatWord(string word)
        {
            var end = _position + word.Length;
            if (end <= text.Length && string.Compare(text, _position, word, 0, word.Length, StringComparison.OrdinalIgnoreCase) == 0
                && (end == text.Length || !(char.IsAsciiLetterOrDigit(text[end]) || text[end] == '_')))
            {
                _position = end;
                return true;
            }

            return false;
        }
    }
}
