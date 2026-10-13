namespace Axit.Core;

/// <summary>
/// Where Ctrl+Up and Ctrl+Down go in a text whose lines end with <c>\r\n</c> (docs/axit/SPEC.md AX-8.2, used by
/// <c>Axit.Forms.EditLines</c>): one line up or down, a whole line however many rows it wraps to on screen, at the
/// same column, or at the end of a shorter line; the caret stays on the first and last line. Indexes are character
/// offsets into the text, the caret's included; the text's length is the position after its last character.
/// </summary>
public static class TextLines
{
    /// <summary>The start of the line the caret is in: the position after the last line break before it, or 0.</summary>
    public static int StartOf(string text, int caret)
    {
        if (caret <= 0)
        {
            return 0;
        }

        return text.LastIndexOf('\n', Math.Min(caret, text.Length) - 1) + 1;
    }

    /// <summary>The end of the line the caret is in: the position of its line break, or the text's length on the last line.</summary>
    public static int EndOf(string text, int caret)
    {
        var found = text.IndexOf('\r', Math.Clamp(caret, 0, text.Length));
        return found < 0 ? text.Length : found;
    }

    /// <summary>The caret's column: how far it is from the start of its line.</summary>
    public static int Column(string text, int caret) => Math.Clamp(caret, 0, text.Length) - StartOf(text, caret);

    /// <summary>Where Ctrl+Up goes: <paramref name="column"/> on the line before, or its end when it is shorter; the caret itself on the first line.</summary>
    public static int Up(string text, int caret, int column)
    {
        var start = StartOf(text, caret);
        if (start == 0)
        {
            return Math.Clamp(caret, 0, text.Length);
        }

        var previous = StartOf(text, start - 1);
        return Math.Min(previous + column, EndOf(text, previous));
    }

    /// <summary>Where Ctrl+Down goes: <paramref name="column"/> on the next line, or its end when it is shorter; the caret itself on the last line.</summary>
    public static int Down(string text, int caret, int column)
    {
        var at = Math.Clamp(caret, 0, text.Length);
        var found = text.IndexOf('\n', at);
        if (found < 0)
        {
            return at;
        }

        var next = found + 1;
        return Math.Min(next + column, EndOf(text, next));
    }
}
