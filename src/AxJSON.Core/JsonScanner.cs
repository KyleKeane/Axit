using System.Text;

namespace AxJSON.Core;

/// <summary>What a token is: the punctuation, a string, a number, a bare word (true, false, null, or an unquoted key), or the end.</summary>
public enum TokenKind
{
    OpenObject,
    CloseObject,
    OpenArray,
    CloseArray,
    Colon,
    Comma,
    String,
    Number,
    Word,
    End,
}

/// <summary>A token of the text with its place; <paramref name="SingleQuoted"/> marks a string in single quotes, which the formatter repairs (AJ-2.3).</summary>
public sealed record Token(TokenKind Kind, int Start, int End, string Text, bool SingleQuoted = false);

/// <summary>What the scanner or the parser put right on the way, counted for the Format announcement (AJ-2.3).</summary>
public enum RepairKind
{
    Comment,
    SingleQuotedString,
    UnquotedKey,
    TrailingComma,
    MissingComma,
}

/// <summary>
/// Splits a JSON text into tokens, forgivingly (docs/axjson/SPEC.md AJ-2.2, AJ-2.3): comments are skipped and
/// counted as repairs, single-quoted strings and bare words are tokens too, so that the outline can read a file
/// that is nearly JSON and the formatter can repair it. Pure code, tested in <c>tests/AxJSON.Tests</c>.
/// </summary>
public static class JsonScanner
{
    public static (IReadOnlyList<Token> Tokens, List<RepairKind> Repairs) Scan(string text)
    {
        var tokens = new List<Token>();
        var repairs = new List<RepairKind>();
        var i = 0;
        while (i < text.Length)
        {
            var c = text[i];
            if (char.IsWhiteSpace(c))
            {
                i++;
                continue;
            }

            if (c == '/' && i + 1 < text.Length && text[i + 1] == '/')
            {
                repairs.Add(RepairKind.Comment);
                while (i < text.Length && text[i] != '\n')
                {
                    i++;
                }

                continue;
            }

            if (c == '/' && i + 1 < text.Length && text[i + 1] == '*')
            {
                repairs.Add(RepairKind.Comment);
                var close = text.IndexOf("*/", i + 2, StringComparison.Ordinal);
                i = close < 0 ? text.Length : close + 2;
                continue;
            }

            var start = i;
            switch (c)
            {
                case '{':
                    tokens.Add(new Token(TokenKind.OpenObject, start, ++i, "{"));
                    continue;
                case '}':
                    tokens.Add(new Token(TokenKind.CloseObject, start, ++i, "}"));
                    continue;
                case '[':
                    tokens.Add(new Token(TokenKind.OpenArray, start, ++i, "["));
                    continue;
                case ']':
                    tokens.Add(new Token(TokenKind.CloseArray, start, ++i, "]"));
                    continue;
                case ':':
                    tokens.Add(new Token(TokenKind.Colon, start, ++i, ":"));
                    continue;
                case ',':
                    tokens.Add(new Token(TokenKind.Comma, start, ++i, ","));
                    continue;
                case '"':
                case '\'':
                    i = EndOfString(text, i);
                    tokens.Add(new Token(TokenKind.String, start, i, text[start..i], SingleQuoted: c == '\''));
                    continue;
            }

            if (c == '-' || char.IsDigit(c))
            {
                while (i < text.Length && (char.IsLetterOrDigit(text[i]) || text[i] is '.' or '-' or '+'))
                {
                    i++;
                }

                // A run that is not a JSON number ("1abc", "12-5", "1.") is a word, which the parser reports as what
                // it found in a value's place; until 2026-10-23 it passed as a number and Format wrote it back.
                var run = text[start..i];
                tokens.Add(new Token(IsNumber(run) ? TokenKind.Number : TokenKind.Word, start, i, run));
                continue;
            }

            if (char.IsLetter(c) || c == '_' || c == '$')
            {
                while (i < text.Length && (char.IsLetterOrDigit(text[i]) || text[i] is '_' or '$' or '-' or '.'))
                {
                    i++;
                }

                tokens.Add(new Token(TokenKind.Word, start, i, text[start..i]));
                continue;
            }

            // Anything else is one token of its own, which the parser reports as unexpected.
            tokens.Add(new Token(TokenKind.Word, start, ++i, text[start..i]));
        }

        tokens.Add(new Token(TokenKind.End, text.Length, text.Length, string.Empty));
        return (tokens, repairs);
    }

    /// <summary>JSON's number grammar: an optional minus, an integer without leading zeros, an optional fraction, an optional exponent.</summary>
    private static bool IsNumber(string run)
    {
        var i = 0;
        if (i < run.Length && run[i] == '-')
        {
            i++;
        }

        var digits = 0;
        while (i < run.Length && char.IsAsciiDigit(run[i]))
        {
            i++;
            digits++;
        }

        if (digits == 0 || (digits > 1 && run[i - digits] == '0'))
        {
            return false;
        }

        if (i < run.Length && run[i] == '.')
        {
            i++;
            var fraction = 0;
            while (i < run.Length && char.IsAsciiDigit(run[i]))
            {
                i++;
                fraction++;
            }

            if (fraction == 0)
            {
                return false;
            }
        }

        if (i < run.Length && run[i] is 'e' or 'E')
        {
            i++;
            if (i < run.Length && run[i] is '+' or '-')
            {
                i++;
            }

            var exponent = 0;
            while (i < run.Length && char.IsAsciiDigit(run[i]))
            {
                i++;
                exponent++;
            }

            if (exponent == 0)
            {
                return false;
            }
        }

        return i == run.Length;
    }

    /// <summary>The index after the closing quote, or the end of the text (or line) for a string left open.</summary>
    private static int EndOfString(string text, int start)
    {
        var quote = text[start];
        var i = start + 1;
        while (i < text.Length)
        {
            var c = text[i];
            if (c == '\\')
            {
                i += 2;
                continue;
            }

            if (c == quote)
            {
                return i + 1;
            }

            if (c == '\n')
            {
                // An unclosed string ends with its line, so that the rest of the file still reads.
                return i;
            }

            i++;
        }

        return text.Length;
    }

    /// <summary>The characters a string token stands for, with its escapes resolved as far as they are JSON's.</summary>
    public static string Unquote(Token token)
    {
        var inner = token.Text.Length >= 2 && token.Text[^1] == token.Text[0] ? token.Text[1..^1] : token.Text[1..];
        if (!inner.Contains('\\'))
        {
            return inner;
        }

        var builder = new StringBuilder(inner.Length);
        for (var i = 0; i < inner.Length; i++)
        {
            var c = inner[i];
            if (c != '\\' || i + 1 >= inner.Length)
            {
                builder.Append(c);
                continue;
            }

            var next = inner[++i];
            switch (next)
            {
                case 'n':
                    builder.Append('\n');
                    break;
                case 'r':
                    builder.Append('\r');
                    break;
                case 't':
                    builder.Append('\t');
                    break;
                case 'b':
                    builder.Append('\b');
                    break;
                case 'f':
                    builder.Append('\f');
                    break;
                case 'u' when i + 4 < inner.Length && int.TryParse(inner.AsSpan(i + 1, 4), System.Globalization.NumberStyles.HexNumber, null, out var code):
                    builder.Append((char)code);
                    i += 4;
                    break;
                default:
                    builder.Append(next);
                    break;
            }
        }

        return builder.ToString();
    }

    /// <summary>A JSON string literal for the characters, in double quotes with JSON's escapes.</summary>
    public static string Quote(string value)
    {
        var builder = new StringBuilder(value.Length + 2);
        builder.Append('"');
        foreach (var c in value)
        {
            switch (c)
            {
                case '"':
                    builder.Append("\\\"");
                    break;
                case '\\':
                    builder.Append("\\\\");
                    break;
                case '\n':
                    builder.Append("\\n");
                    break;
                case '\r':
                    builder.Append("\\r");
                    break;
                case '\t':
                    builder.Append("\\t");
                    break;
                case '\b':
                    builder.Append("\\b");
                    break;
                case '\f':
                    builder.Append("\\f");
                    break;
                default:
                    if (c < ' ')
                    {
                        builder.Append("\\u").Append(((int)c).ToString("x4"));
                    }
                    else
                    {
                        builder.Append(c);
                    }

                    break;
            }
        }

        builder.Append('"');
        return builder.ToString();
    }
}
