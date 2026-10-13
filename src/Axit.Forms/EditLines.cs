using System.Runtime.InteropServices;
using Axit.Core;

namespace Axit.Forms;

/// <summary>
/// Ctrl+Up and Ctrl+Down in a multi-line edit control (docs/axit/SPEC.md AX-8.2): the caret moves one whole line
/// up or down, a logical line however many rows it wraps to (Up and Down move by row), at the same column or at
/// the end of a shorter line, and with Shift the selection grows to there from its anchor. The column is kept
/// across a run of presses, so a short line in between does not lose it, as with the arrow keys. The EDIT control
/// itself scrolls the view one row on Ctrl+Up and Ctrl+Down and leaves the caret where it is; a screen reader,
/// which passes these keys through and then reads at the caret, read the same text again. The screen reader reads
/// the row the caret lands on, or the change of selection, itself; nothing is announced here. The arithmetic is
/// <see cref="TextLines"/> in Core, where it is tested.
/// </summary>
public static class EditLines
{
    private const int EM_SETSEL = 0x00B1;
    private const int EM_SCROLLCARET = 0x00B7;

    // The column a run of presses keeps: good while the caret is still where the last press left it.
    private static IntPtr _lastHandle;
    private static int _lastCaret = -1;
    private static int _wantedColumn;

    /// <summary>
    /// Moves the caret one line down (<paramref name="direction"/> 1) or up (-1) at the same column; with
    /// <paramref name="extend"/> the selection grows from its anchor to there instead. Nothing moves on the first
    /// or last line.
    /// </summary>
    public static void Move(TextBoxBase box, int direction, bool extend)
    {
        // One copy of the text, on a key press (never per frame: it is about a millisecond at 20 000 lines).
        var text = box.Text;
        var start = box.SelectionStart;
        var end = start + box.SelectionLength;
        var caret = start == end || !CaretAtEnd(box, start) ? start : end;
        var anchor = caret == start ? end : start;
        var column = box.Handle == _lastHandle && caret == _lastCaret ? _wantedColumn : TextLines.Column(text, caret);
        var target = direction < 0 ? TextLines.Up(text, caret, column) : TextLines.Down(text, caret, column);
        _lastHandle = box.Handle;
        _lastCaret = target;
        _wantedColumn = column;
        if (target == caret)
        {
            return;
        }

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
