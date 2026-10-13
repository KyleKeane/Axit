namespace Axit.Core;

/// <summary>
/// Where Ctrl+Up and Ctrl+Down go in a text whose lines end with <c>\r\n</c> (docs/axit/SPEC.md AX-8.2, used by
/// <c>Axit.Forms.EditParagraphs</c>): the starts of its paragraphs, where a paragraph starts at the first line, or at
/// a line with text directly after an empty line, the rule of AxClaude's <c>p</c> key. Offsets are character
/// indexes into the text, the caret's included; the text's length is the position after its last character.
/// </summary>
public static class Paragraphs
{
    /// <summary>The start of the next paragraph after the line the caret is in, or the end of the text when there is none.</summary>
    public static int Next(string text, int caret)
    {
        var at = Math.Clamp(caret, 0, text.Length);
        for (var start = NextLineStart(text, at); start < text.Length; start = NextLineStart(text, start))
        {
            if (StartsParagraph(text, start))
            {
                return start;
            }
        }

        return text.Length;
    }

    /// <summary>
    /// The start of the paragraph the caret is in, or of the one before it when the caret is at that start already;
    /// 0 when there is none before the caret.
    /// </summary>
    public static int Previous(string text, int caret)
    {
        var at = Math.Clamp(caret, 0, text.Length);
        var start = LineStart(text, at);
        while (true)
        {
            if (start < at && StartsParagraph(text, start))
            {
                return start;
            }

            if (start == 0)
            {
                return 0;
            }

            start = LineStart(text, start - 1);
        }
    }

    /// <summary>A line with text that is the first line or follows an empty line.</summary>
    private static bool StartsParagraph(string text, int start) =>
        start < text.Length && text[start] != '\r' && (start == 0 || LineStart(text, start - 1) == start - 2);

    /// <summary>The start of the line the position is in: the position after the last line break before it, or 0.</summary>
    private static int LineStart(string text, int at) => at <= 0 ? 0 : text.LastIndexOf('\n', at - 1) + 1;

    /// <summary>The start of the line after the one the position is in, or the text's length.</summary>
    private static int NextLineStart(string text, int at)
    {
        var found = text.IndexOf('\n', at);
        return found < 0 ? text.Length : found + 1;
    }
}
