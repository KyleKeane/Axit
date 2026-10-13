using System.Runtime.InteropServices;
using Axit.Core;

namespace Axit.Forms;

/// <summary>
/// Ctrl+Up and Ctrl+Down in a multi-line edit control (docs/axit/SPEC.md AX-8.2): the caret goes to the start of
/// the previous or next paragraph, a logical line (the text between two line breaks, however many rows it wraps
/// to), and with Shift the selection grows to there from its anchor, as in Word and RichEdit. The EDIT control
/// itself scrolls the view one row on Ctrl+Up and Ctrl+Down and leaves the caret where it is; NVDA, whose own
/// scripts for these keys read the paragraph at the caret once the key has gone through, then read the same
/// paragraph again. The screen reader reads the new paragraph, or the change of selection, itself; nothing is
/// announced here. The arithmetic is <see cref="Paragraphs"/> in Core, where it is tested.
/// </summary>
public static class EditParagraphs
{
    private const int EM_SETSEL = 0x00B1;
    private const int EM_SCROLLCARET = 0x00B7;

    /// <summary>
    /// Moves the caret to the start of the next (<paramref name="direction"/> 1) or previous (-1) paragraph; with
    /// <paramref name="extend"/> the selection grows from its anchor to there instead.
    /// </summary>
    public static void Move(TextBoxBase box, int direction, bool extend)
    {
        // One copy of the text, on a key press (never per frame: it is about a millisecond at 20 000 lines).
        var text = box.Text;
        var start = box.SelectionStart;
        var end = start + box.SelectionLength;
        var caret = start == end || !CaretAtEnd(box, start) ? start : end;
        var anchor = caret == start ? end : start;
        var target = direction < 0 ? Paragraphs.Previous(text, caret) : Paragraphs.Next(text, caret);
        // EM_SETSEL puts the anchor at the first index and the caret at the second, whichever of the two is lower.
        SendMessage(box.Handle, EM_SETSEL, (IntPtr)(extend ? anchor : target), (IntPtr)target);
        SendMessage(box.Handle, EM_SCROLLCARET, IntPtr.Zero, IntPtr.Zero);
    }

    /// <summary>
    /// Whether the caret is at the end of the selection rather than at its start. The control keeps the anchor to
    /// itself, but it draws the caret at the active end: a caret that is not where the selection's first character
    /// is drawn is at the end.
    /// </summary>
    private static bool CaretAtEnd(TextBoxBase box, int start)
    {
        if (!GetCaretPos(out var caret))
        {
            return true;
        }

        var first = box.GetPositionFromCharIndex(start);
        return Math.Abs(caret.X - first.X) > 2 || Math.Abs(caret.Y - first.Y) > 2;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT
    {
        public int X;
        public int Y;
    }

    [DllImport("user32.dll")]
    private static extern bool GetCaretPos(out POINT point);

    [DllImport("user32.dll")]
    private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);
}
