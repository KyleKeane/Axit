using System.Globalization;
using System.Text;

namespace AxCalc.Core;

/// <summary>
/// An answer as text (SPEC.md AC-3.7) and in other bases (AC-4): plain digits for a whole number under 10^15, up to
/// 15 significant digits otherwise; hexadecimal, binary and octal for a whole number under 2^63.
/// </summary>
public static class NumberText
{
    private const double WholeLimit = 1e15;

    /// <summary>2^63: the first whole number a long cannot hold.</summary>
    private const double BaseLimit = 9223372036854775808d;

    public static string Format(double value)
    {
        if (value == 0)
        {
            return "0";
        }

        if (Math.Abs(value) < WholeLimit && value == Math.Floor(value))
        {
            return ((long)value).ToString(CultureInfo.InvariantCulture);
        }

        return value.ToString("G15", CultureInfo.InvariantCulture);
    }

    /// <summary>"255, hex FF, binary 1111 1111, octal 377" (AC-4.1), or why the value has no other bases.</summary>
    public static string Bases(double value)
    {
        if (Whole(value) is not { } whole)
        {
            return value != Math.Floor(value) ? $"{Format(value)} is not a whole number" : $"{Format(value)} is too large for other bases";
        }

        var sign = whole < 0 ? "-" : string.Empty;
        var magnitude = Math.Abs(whole);
        return $"{Format(value)}, hex {sign}{Grouped(Digits(magnitude, 16))}, binary {sign}{Grouped(Digits(magnitude, 2))}, octal {sign}{Digits(magnitude, 8)}";
    }

    /// <summary>The value as a literal for code, "0xFF", "0b11111111" or "0o377" (AC-4.2); null when it has no other bases.</summary>
    public static string? Code(double value, int radix)
    {
        if (Whole(value) is not { } whole)
        {
            return null;
        }

        var prefix = radix switch { 16 => "0x", 2 => "0b", _ => "0o" };
        return (whole < 0 ? "-" : string.Empty) + prefix + Digits(Math.Abs(whole), radix);
    }

    /// <summary>The value as a long when it is a whole number whose magnitude is under 2^63.</summary>
    private static long? Whole(double value) =>
        value == Math.Floor(value) && Math.Abs(value) < BaseLimit ? (long)value : null;

    private static string Digits(long magnitude, int radix) => Convert.ToString(magnitude, radix).ToUpperInvariant();

    /// <summary>Groups of four digits from the right, a space between them, so that a reading has pauses.</summary>
    private static string Grouped(string digits)
    {
        var text = new StringBuilder();
        for (var i = 0; i < digits.Length; i++)
        {
            if (i > 0 && (digits.Length - i) % 4 == 0)
            {
                text.Append(' ');
            }

            text.Append(digits[i]);
        }

        return text.ToString();
    }
}
