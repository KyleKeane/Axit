namespace Axit.Core;

/// <summary>
/// Lines and columns of a text as an edit control holds it, with <c>\r\n</c> line ends (docs/axdown/SPEC.md AD-6.4,
/// AD-3.6): where the caret is, and where a line and column are, so that a caret put back after a reload lands on its
/// line and never between a CR and its LF (2026-10-23; the window's own arithmetic before, which could). Pure, tested.
/// </summary>
public static class TextLines
{
    /// <summary>The caret's line and column, 1-based, counting the text's lines (not the wrapped rows), and the number of lines.</summary>
    public static (int Line, int Column, int Lines) Position(string text, int caret)
    {
        caret = Math.Clamp(caret, 0, text.Length);
        var line = 1;
        var lineStart = 0;
        for (var i = 0; i < caret; i++)
        {
            if (text[i] == '\n')
            {
                line++;
                lineStart = i + 1;
            }
        }

        var lines = line;
        for (var i = caret; i < text.Length; i++)
        {
            if (text[i] == '\n')
            {
                lines++;
            }
        }

        return (line, caret - lineStart + 1, lines);
    }

    /// <summary>
    /// The index of a line and column, 1-based, clamped to the text: a column past the line's end is the end of the
    /// line, before its line break; a line past the last is the end of the text.
    /// </summary>
    public static int IndexOf(string text, int line, int column)
    {
        var index = 0;
        for (var current = 1; current < line && index < text.Length; index++)
        {
            if (text[index] == '\n')
            {
                current++;
            }
        }

        var end = text.IndexOf('\n', index);
        var lineEnd = end < 0 ? text.Length : end;
        if (lineEnd > index && text[lineEnd - 1] == '\r')
        {
            // The caret never lands between the CR and the LF (AD-3.6).
            lineEnd--;
        }

        return index + Math.Clamp(column - 1, 0, lineEnd - index);
    }
}
