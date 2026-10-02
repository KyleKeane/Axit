namespace AxDown.Core;

/// <summary>A heading in the text: where its line starts, its level (1 to 6), its text without the marks, and the line as written.</summary>
public sealed record Heading(int Start, int Level, string Text, string Line);

/// <summary>
/// The structure of a Markdown text for the navigation keys (SPEC.md §9): headings, which are lines of one to six
/// <c>#</c> followed by a space, after at most three spaces of indentation, outside fenced code blocks (lines
/// opening and closing with <c>```</c> or <c>~~~</c>). Lists, links, block quotes and tables follow when their
/// letters are built (AD-D7). Pure: the text in, positions out; the window moves the caret.
/// </summary>
public static class MarkdownOutline
{
    public static IReadOnlyList<Heading> Headings(string text)
    {
        var headings = new List<Heading>();
        var inFence = false;
        var start = 0;
        while (start <= text.Length)
        {
            var end = text.IndexOf('\n', start);
            var lineEnd = end < 0 ? text.Length : end;
            var line = text.AsSpan(start, lineEnd - start).TrimEnd('\r');
            var trimmed = line.TrimStart(' ');
            if (line.Length - trimmed.Length <= 3)
            {
                if (trimmed.StartsWith("```") || trimmed.StartsWith("~~~"))
                {
                    inFence = !inFence;
                }
                else if (!inFence && HeadingLevel(trimmed) is var level && level > 0)
                {
                    headings.Add(new Heading(start, level, HeadingText(trimmed, level), line.ToString()));
                }
            }

            if (end < 0)
            {
                break;
            }

            start = end + 1;
        }

        return headings;
    }

    /// <summary>The first heading on a line after the caret's line; null when there is none.</summary>
    public static Heading? NextHeading(string text, int caret)
    {
        var lineStart = LineStart(text, caret);
        foreach (var heading in Headings(text))
        {
            if (heading.Start > lineStart)
            {
                return heading;
            }
        }

        return null;
    }

    /// <summary>The last heading on a line before the caret's line; null when there is none.</summary>
    public static Heading? PreviousHeading(string text, int caret)
    {
        var lineStart = LineStart(text, caret);
        Heading? found = null;
        foreach (var heading in Headings(text))
        {
            if (heading.Start < lineStart)
            {
                found = heading;
            }
        }

        return found;
    }

    private static int LineStart(string text, int caret)
    {
        caret = Math.Clamp(caret, 0, text.Length);
        var newline = caret > 0 ? text.LastIndexOf('\n', caret - 1) : -1;
        return newline + 1;
    }

    /// <summary>1 to 6 for <c># </c> to <c>###### </c> (or hashes alone); 0 for anything else, including <c>#tag</c>.</summary>
    private static int HeadingLevel(ReadOnlySpan<char> line)
    {
        var level = 0;
        while (level < line.Length && line[level] == '#')
        {
            level++;
        }

        if (level is 0 or > 6)
        {
            return 0;
        }

        return level == line.Length || line[level] is ' ' or '\t' ? level : 0;
    }

    /// <summary>The heading without its opening marks, trailing closing hashes and surrounding spaces.</summary>
    private static string HeadingText(ReadOnlySpan<char> line, int level)
    {
        var text = line[level..].Trim();
        var closing = text.Length;
        while (closing > 0 && text[closing - 1] == '#')
        {
            closing--;
        }

        if (closing < text.Length && (closing == 0 || text[closing - 1] is ' ' or '\t'))
        {
            text = text[..closing].TrimEnd();
        }

        return text.ToString();
    }
}
