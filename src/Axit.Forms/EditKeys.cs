using System.Runtime.InteropServices;
using Axit.Core;
using static Axit.Forms.User32;

namespace Axit.Forms;

/// <summary>
/// The bundle's page and paragraph keys in an edit control (AX-8.2): Page Up and Page Down one screen at a time and
/// to the ends (Page), Ctrl+Up and Ctrl+Down to the start of the previous or next paragraph, with
/// Shift growing the selection to there (Paragraph). The control's own keys do neither: they scroll
/// a row and leave the caret, or nothing while the text fits. Subscribe Handle to the control's KeyDown. One class since 2026-10-25 (inventory item 11): it was three, EditKeys, EditPaging and EditParagraphs.
/// </summary>
public static class EditKeys
{
    public static void Handle(object? sender, KeyEventArgs e)
    {
        if (sender is not TextBoxBase box)
        {
            return;
        }

        if (e.KeyData is BundleKeys.PageUp or BundleKeys.PageDown)
        {
            Page(box, e.KeyCode == Keys.PageDown ? 1 : -1);
        }
        else if (e.KeyData is BundleKeys.ParagraphUp or BundleKeys.ParagraphDown or BundleKeys.SelectParagraphUp or BundleKeys.SelectParagraphDown)
        {
            Paragraph(box, e.KeyCode == Keys.Down ? 1 : -1, e.Shift);
        }
        else
        {
            return;
        }

        e.Handled = e.SuppressKeyPress = true;
    }

    // ---- Page Up and Page Down (was EditPaging) ----
    // Page Up and Page Down for the conversation and the message field (FR-2.1, FR-5). The caret moves by one
    // screen of wrapped rows, counted from the caret rather than from whatever is scrolled into view, and lands on the
    // first or last row when less than a screen is left; it goes to the start of its new row, and the view scrolls with
    // it so that the row keeps its place on screen. The edit control's own keys scroll the view by a page and keep the
    // caret on the same screen row instead, which strands the caret when its row is off screen, never reaches the first
    // or last row, and does nothing at all while the text fits the control. The screen reader reads the new row itself;
    // nothing is announced here.

    private const int EM_LINESCROLL = 0x00B6;
    private const int EM_GETFIRSTVISIBLELINE = 0x00CE;
    private const int EM_CHARFROMPOS = 0x00D7;

    /// <summary>Moves the caret one screen of rows down (<paramref name="direction"/> 1) or up (-1). False when it was on the last (first) row already and stays.</summary>
    public static bool Page(TextBoxBase box, int direction)
    {
        var handle = box.Handle;
        var lineCount = (int)SendMessage(handle, EM_GETLINECOUNT, IntPtr.Zero, IntPtr.Zero);
        var first = (int)SendMessage(handle, EM_GETFIRSTVISIBLELINE, IntPtr.Zero, IntPtr.Zero);
        // The row at the bottom edge of the control, or the last row of the text when that ends higher up. The row
        // is the high word of the reply, 16 bits, so it is the row modulo 65 536: its distance from the first visible
        // row is right as long as a screen is shorter than that, where the row itself is wrong past 65 536 rows. The
        // low word's character index is only good below 64K characters.
        var bottomPoint = (IntPtr)((Math.Max(0, box.ClientSize.Height - 1) << 16) | 1);
        var atBottom = (int)SendMessage(handle, EM_CHARFROMPOS, IntPtr.Zero, bottomPoint);
        var rows = atBottom == -1 ? 0 : (((atBottom >> 16) & 0xFFFF) - (first & 0xFFFF)) & 0xFFFF;
        var last = first + rows;
        var page = Math.Max(1, rows);
        var caretRow = box.GetLineFromCharIndex(box.SelectionStart);
        var target = Math.Clamp(caretRow + direction * page, 0, Math.Max(0, lineCount - 1));
        if (target == caretRow)
        {
            return false;
        }

        box.Select(box.GetFirstCharIndexFromLine(target), 0);
        // Scroll so that the row keeps its place on screen, or to the top when it was scrolled out of view. The scroll
        // is explicit because the control's own scroll-to-caret is ignored while it does not have the focus.
        var offset = caretRow >= first && caretRow <= last ? caretRow - first : 0;
        var now = (int)SendMessage(handle, EM_GETFIRSTVISIBLELINE, IntPtr.Zero, IntPtr.Zero);
        SendMessage(handle, EM_LINESCROLL, IntPtr.Zero, (IntPtr)(Math.Max(0, target - offset) - now));
        SendMessage(handle, EM_SCROLLCARET, IntPtr.Zero, IntPtr.Zero);
        return true;
    }

    // ---- Ctrl+Up and Ctrl+Down by paragraph (was EditParagraphs) ----
    // Ctrl+Up and Ctrl+Down in a multi-line edit control (docs/axit/SPEC.md AX-8.2): the caret goes to the start of
    // the previous or next paragraph, where a paragraph starts at the first line or at a line with text directly after
    // a blank line (the rule of axClaude's p key), and with Shift the selection grows to there from its anchor, as in
    // Word. The EDIT control itself scrolls the view one row on Ctrl+Up and Ctrl+Down and leaves the caret where it is,
    // as Notepad does, which a screen reader, reading at the caret after the key, reports as the same text again. The
    // screen reader reads the new paragraph, or the change of selection, itself; nothing is announced here. The
    // arithmetic is Paragraphs in Core, where it is tested.

    private const int EM_SETSEL = 0x00B1;

    /// <summary>
    /// Moves the caret to the start of the next (<paramref name="direction"/> 1) or previous (-1) paragraph; with
    /// <paramref name="extend"/> the selection grows from its anchor to there instead.
    /// </summary>
    public static void Paragraph(TextBoxBase box, int direction, bool extend)
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
}
