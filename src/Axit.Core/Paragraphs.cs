namespace Axit.Core;

/// <summary>
/// Where Ctrl+Up and Ctrl+Down go in a text whose lines end with <c>\r\n</c> (docs/axit/SPEC.md AX-8.2, used by
/// <c>Axit.Forms.EditParagraphs</c>): a paragraph is a logical line, the text between two line breaks, however many
/// rows it wraps to on screen, as in Word and RichEdit. Indexes are character offsets into the text, the caret's
/// included; the text's length is the position after its last character.
/// </summary>
public static class Paragraphs
{
    /// <summary>The start of the paragraph the caret is in: the position after the last line break before it, or 0.</summary>
    public static int StartOf(string text, int caret)
    {
        if (caret <= 0)
        {
            return 0;
        }

        return text.LastIndexOf('\n', Math.Min(caret, text.Length) - 1) + 1;
    }

    /// <summary>
    /// Where Ctrl+Up goes: the start of the caret's paragraph, or, when the caret is at that start already, the start
    /// of the paragraph before it; 0 from the first paragraph.
    /// </summary>
    public static int Previous(string text, int caret)
    {
        var start = StartOf(text, caret);
        return caret > start || start == 0 ? start : StartOf(text, start - 1);
    }

    /// <summary>Where Ctrl+Down goes: the start of the next paragraph, or the end of the text from the last one.</summary>
    public static int Next(string text, int caret)
    {
        var at = Math.Clamp(caret, 0, text.Length);
        var found = text.IndexOf('\n', at);
        return found < 0 ? text.Length : found + 1;
    }
}
