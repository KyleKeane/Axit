using System.Runtime.InteropServices;
using System.Text;
using Axit.Core.Transcript;
using static Axit.Forms.User32;

namespace Axit.Forms;

/// <summary>
/// The read-only transcript view (axClaude's conversation, axTerminal's terminal). Mirrors the visible transcript
/// lines into a native multi-line edit control with minimal edits, keeps the user's caret on the content it was on,
/// and adds NVDA-style single-key navigation with the keys the window hands in (<see cref="TranscriptKeys"/>). It
/// came from axClaude on its second use (docs/axterminal/SPEC.md §5); the requirement numbers below are axClaude's.
/// </summary>
public sealed class TranscriptView : TextBox
{
    private const int WM_SETREDRAW = 0x000B;
    private const int EM_LINEINDEX = 0x00BB;
    private const int EM_LINELENGTH = 0x00C1;
    private const int EM_GETLINE = 0x00C4;
    private const int EM_LINEFROMCHAR = 0x00C9;

    /// <summary>
    /// How long output is held back after a key press while the view has focus. The screen reader reads the caret
    /// line in several steps after a key (find the caret, find its line, fetch the text); text that shifts between
    /// those steps makes it read the wrong line. NVDA waits up to 100 ms for the caret to move before it reads.
    /// </summary>
    private const int NavigationHoldMs = 250;

    /// <summary>The window's keys for the view: the quick keys and the rest (AX-8.3), given once at construction.</summary>
    private readonly TranscriptKeys _keys;

    /// <summary>The lines the caret left through a jump, oldest first, for Backspace (FR-3.12); the cap keeps a long session from holding every jump.</summary>
    private readonly List<Line> _jumpHistory = [];
    private const int JumpHistoryLimit = 100;

    private readonly TranscriptMirror _mirror = new();
    private readonly System.Windows.Forms.Timer _hold = new();
    private IReadOnlyList<Line>? _pending;
    private IReadOnlyList<Line>? _lines;
    private long _lastKeyTick = long.MinValue / 2;

    /// <param name="keys">The window's keys for the view, from its key table.</param>
    /// <param name="accessibleName">What NVDA calls the view: "Conversation" in axClaude, "Terminal" in axTerminal.</param>
    public TranscriptView(TranscriptKeys keys, string accessibleName)
    {
        _keys = keys;
        Multiline = true;
        ReadOnly = true;
        WordWrap = true;
        ScrollBars = ScrollBars.Vertical;
        AcceptsTab = false;
        HideSelection = false;
        MaxLength = 0;
        BackColor = SystemColors.Window;
        ForeColor = SystemColors.WindowText;
        AccessibleName = accessibleName;
        _hold.Tick += (_, _) =>
        {
            _hold.Stop();
            if (_pending is { } lines)
            {
                _pending = null;
                Sync(lines);
            }
        };
    }

    /// <summary>Escape was pressed: the window moves focus to the message field.</summary>
    public event Action? EscapePressed;

    /// <summary>Enter was pressed on a line: its display number (from 1), the display line count and its full text (FR-2.9).</summary>
    public event Action<int, int, string>? LineChosen;

    /// <summary>m was pressed on a line (or Navigate → Bookmark this line): the window has the model bookmark it, or take the bookmark away (FR-3.9).</summary>
    public event Action<Line>? BookmarkToggled;

    /// <summary>
    /// True while a notice hides the view that the user was reading (D23): the caret then stays on its line as if
    /// the view still had the focus, instead of following the newest output, so that closing the notice returns to
    /// the same place.
    /// </summary>
    [System.ComponentModel.DefaultValue(false)]
    public bool KeepCaret { get; set; }

    /// <summary>The # markers of the heading lines in the text (axClaude FR-4.6a); the window re-syncs after a change.</summary>
    [System.ComponentModel.DefaultValue(true)]
    public bool ShowMarkers { get => _mirror.ShowMarkers; set => _mirror.ShowMarkers = value; }

    /// <summary>Speaks text through a UI Automation notification without moving focus (FR-7.1). Returns false when unsupported.</summary>
    public bool Announce(string text, bool interrupt) => Announcer.Raise(this, text, interrupt);

    /// <summary>Ctrl+3, or Navigate → Latest response (FR-5): focuses the view and puts the caret on the newest output marker; `No response yet` when there is none.</summary>
    public void JumpToLatestResponse()
    {
        Focus();
        for (var i = _mirror.LineCount - 1; i >= 0; i--)
        {
            if (Stops(_keys.Response, i))
            {
                MoveTo(i);
                return;
            }
        }

        Announce($"No {_keys.Response.Name} yet", true);
    }

    /// <summary>m, or Navigate → Bookmark this line: focuses the view and bookmarks the caret's line, or takes its bookmark away (FR-3.9).</summary>
    public void ToggleBookmark()
    {
        Focus();
        if (_mirror.LineCount == 0)
        {
            Announce("Empty", true);
            return;
        }

        BookmarkToggled?.Invoke(_mirror.LineAt(_mirror.LineIndexAt(SelectionStart)));
    }

    /// <summary>Navigate → Next or Previous bookmark: focuses the view and jumps as k or Shift+K would.</summary>
    public void JumpToBookmark(bool backward)
    {
        Focus();
        Jump(_keys.Bookmark, backward);
    }

    /// <summary>
    /// Focuses the view and moves the caret to the next (or previous) line containing the text, wrapping around at
    /// the ends (FR-3.7). The line is announced, prefixed when the search wrapped; "Not found" when nothing matches.
    /// </summary>
    public void Find(string text, bool backward)
    {
        Focus();
        var current = _mirror.LineCount == 0 ? 0 : _mirror.LineIndexAt(SelectionStart);
        var index = _mirror.Find(text, current, backward, out var column, out var wrapped);
        if (index < 0)
        {
            Announce($"Not found: {text}", true);
            return;
        }

        MoveTo(index, column, wrapped ? (backward ? "From the end, " : "From the top, ") : string.Empty);
    }

    /// <summary>
    /// Brings the control in line with the model's visible lines using as few edits as possible. Right after a key
    /// press in the focused view the update waits until the keys stop, so that the screen reader reads a line that
    /// holds still (see <see cref="NavigationHoldMs"/>), and while text is selected it waits until the selection is
    /// gone, so that what is read or copied never changes underneath (FR-3.11); the lines are the model's live list,
    /// so nothing is lost.
    /// </summary>
    public void Sync(IReadOnlyList<Line> lines)
    {
        _lines = lines;
        var sinceKey = Environment.TickCount64 - _lastKeyTick;
        if (Focused && (sinceKey < NavigationHoldMs || SelectionLength > 0))
        {
            _pending = lines;
            _hold.Stop();
            // The rest of the hold after a key; a whole hold while a selection holds the output, since only a key,
            // which restarts the hold, changes that (the remainder went to 1 ms and spun the UI thread until 2026-10-23).
            _hold.Interval = sinceKey < NavigationHoldMs ? (int)Math.Max(1, NavigationHoldMs - sinceKey) : NavigationHoldMs;
            _hold.Start();
            return;
        }

        _pending = null;
        var selectionStart = SelectionStart;
        var selectionEnd = selectionStart + SelectionLength;
        var focused = Focused || KeepCaret;
        if (focused)
        {
            PlaceReadingBreak(selectionStart);
        }
        else
        {
            _mirror.ClearBreaks();
        }

        var update = _mirror.Update(lines);
        if (!update.Changed)
        {
            return;
        }

        SendMessage(Handle, WM_SETREDRAW, IntPtr.Zero, IntPtr.Zero);
        try
        {
            foreach (var edit in update.Edits)
            {
                Select(edit.Start, edit.OldLength);
                SelectedText = edit.Text;
            }

            if (focused)
            {
                // The caret stays on the transcript line and column it was on (FR-3.3), wherever that line moved.
                var start = update.MapPosition(selectionStart);
                var end = update.MapPosition(selectionEnd);
                Select(start, Math.Max(0, end - start));
            }
            else if (_mirror.LineCount > 0)
            {
                Select(_mirror.StartAt(_mirror.LineCount - 1), 0);
            }
        }
        finally
        {
            SendMessage(Handle, WM_SETREDRAW, (IntPtr)1, IntPtr.Zero);
            Invalidate();
        }

        if (!focused)
        {
            // The caret now sits on the newest line; keep the window showing it, as a terminal would.
            ScrollCaretIntoView();
        }
    }

    /// <summary>
    /// Reading breaks (FR-3.10). When the caret sits on the last screen row of the text, the reader has heard
    /// everything there is, so the end is marked: what Claude appends next is rendered on a line of its own, and
    /// Down Arrow reads only that. Once the caret has left the display line that holds the breaks, the line is
    /// joined up again.
    /// </summary>
    private void PlaceReadingBreak(int caret)
    {
        if (_mirror.LineCount == 0)
        {
            return;
        }

        var index = _mirror.LineIndexAt(caret);
        if (_mirror.HasBreaks && _mirror.BreakDisplayLine != _mirror.DisplayLineOf(index))
        {
            _mirror.ClearBreaks();
        }

        var lastRow = (int)SendMessage(Handle, EM_GETLINECOUNT, IntPtr.Zero, IntPtr.Zero) - 1;
        if (index == _mirror.LineCount - 1 && GetLineFromCharIndex(caret) >= lastRow)
        {
            _mirror.BreakAtEnd(caret - _mirror.StartAt(index));
        }
    }

    /// <summary>A key press moved the caret: give the view a chance to join up a line the reader has left.</summary>
    private void SyncAfterKey()
    {
        if (_mirror.HasBreaks && _pending is null && _lines is not null)
        {
            _pending = _lines;
            _hold.Stop();
            _hold.Interval = NavigationHoldMs;
            _hold.Start();
        }
    }

    protected override void OnGotFocus(EventArgs e)
    {
        base.OnGotFocus(e);
        // Output that arrived while the view was unfocused moved the caret without the view following (Sync).
        ScrollCaretIntoView();
    }

    /// <summary>
    /// The edit control's own scroll-to-caret. WinForms' <c>ScrollToCaret</c> first fetches the whole text to see
    /// whether it is empty, a copy of the entire conversation on every call.
    /// </summary>
    private void ScrollCaretIntoView() => SendMessage(Handle, EM_SCROLLCARET, IntPtr.Zero, IntPtr.Zero);

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.KeyCode == _keys.BookmarkToggle && e.Modifiers == Keys.None)
        {
            // Before the hold stamp: the caret stays on its line, so the bookmark line may appear at once.
            e.Handled = e.SuppressKeyPress = true;
            ToggleBookmark();
            return;
        }

        _lastKeyTick = Environment.TickCount64;
        SyncAfterKey();
        if (e.KeyCode == _keys.Escape)
        {
            e.Handled = e.SuppressKeyPress = true;
            if (SelectionLength > 0)
            {
                // A selection is cleared first (FR-3.11); the caret goes back to where the selection began.
                Select(SelectionStart, 0);
                Announce("Selection cleared", true);
                return;
            }

            EscapePressed?.Invoke();
            return;
        }

        if (e.KeyData == _keys.CopySelection && SelectionLength > 0)
        {
            e.Handled = e.SuppressKeyPress = true;
            CopySelection();
            return;
        }

        if (e.KeyData is BundleKeys.ParagraphUp or BundleKeys.ParagraphDown or BundleKeys.SelectParagraphUp or BundleKeys.SelectParagraphDown)
        {
            // The start of the previous or next paragraph, with Shift selecting to there (FR-5): the control's own
            // keys scroll a row and leave the caret. A caret move like the arrows', not a jump for Backspace.
            e.Handled = e.SuppressKeyPress = true;
            EditKeys.Paragraph(this, e.KeyCode == Keys.Down ? 1 : -1, e.Shift);
            return;
        }

        if (!e.Control && !e.Alt)
        {
            if (e.KeyCode == _keys.Enter && !e.Shift)
            {
                e.Handled = e.SuppressKeyPress = true;
                ChooseLine();
                return;
            }

            if (e.KeyCode is BundleKeys.PageUp or BundleKeys.PageDown && !e.Shift)
            {
                e.Handled = e.SuppressKeyPress = true;
                Page(e.KeyCode == Keys.PageDown ? 1 : -1);
                return;
            }

            if (e.KeyCode == _keys.JumpBack && !e.Shift)
            {
                e.Handled = e.SuppressKeyPress = true;
                JumpBack();
                return;
            }

            if (e.KeyCode == _keys.LineNumber)
            {
                e.Handled = e.SuppressKeyPress = true;
                AnnounceLineNumber();
                return;
            }

            foreach (var key in _keys.QuickKeys)
            {
                if (key.Key == e.KeyCode)
                {
                    e.Handled = e.SuppressKeyPress = true;
                    Jump(key, e.Shift);
                    return;
                }
            }
        }

        base.OnKeyDown(e);
    }

    /// <summary>Ctrl+C with a selection (FR-3.11): the selected text goes to the clipboard and the result is spoken.</summary>
    private void CopySelection()
    {
        try
        {
            Clipboard.SetText(SelectedText);
            Announce("Copied", true);
        }
        catch (ExternalException)
        {
            Announce("Could not copy. Try again", true);
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _hold.Dispose();
        }

        base.Dispose(disposing);
    }

    private void Jump(QuickKey key, bool backward)
    {
        var step = backward ? -1 : 1;
        if (_mirror.LineCount > 0)
        {
            var current = _mirror.LineIndexAt(SelectionStart);
            for (var i = current + step; i >= 0 && i < _mirror.LineCount; i += step)
            {
                if (Stops(key, i))
                {
                    MoveTo(i);
                    return;
                }
            }
        }

        Announce($"No {(backward ? "previous" : "next")} {key.Name}", true);
    }

    /// <summary>Whether a quick key stops at a line: the line matches, and the rule about the line before it holds.</summary>
    private bool Stops(QuickKey key, int index) =>
        key.Matches(_mirror.LineAt(index)) && (key.Previous is null || index == 0 || key.Previous(_mirror.LineAt(index), _mirror.LineAt(index - 1)));

    /// <summary>
    /// Ctrl+Alt with a quick key letter (FR-5, D36): every line that key would stop at, from the caret's line to the
    /// end of the conversation, spoken as one notification with the text each jump would say, wrapped rows joined;
    /// a paragraph is read whole, its lines down to the next blank line, so that a list is heard entire. The caret
    /// stays. False for a letter that is no quick key.
    /// </summary>
    public bool ReadToEnd(Keys letter)
    {
        var key = _keys.QuickKeys.FirstOrDefault(k => k.Key == letter);
        if (key is null)
        {
            return false;
        }

        var text = new StringBuilder();
        if (_mirror.LineCount > 0)
        {
            for (var i = _mirror.LineIndexAt(SelectionStart); i < _mirror.LineCount; i++)
            {
                if (!Stops(key, i))
                {
                    continue;
                }

                Utterance.Append(text, SpokenText(i, whole: true), joinedToPrevious: false);
                for (var j = i + 1; key.ReadsWhole && j < _mirror.LineCount && key.Matches(_mirror.LineAt(j)) && !Stops(key, j); j++)
                {
                    // The rest of the paragraph. A row wrapped under the one before is in that one's display text already.
                    if (!_mirror.LineAt(j).JoinedToPrevious)
                    {
                        Utterance.Append(text, _mirror.DisplayTextAt(j), joinedToPrevious: false);
                    }
                }
            }
        }

        Announce(text.Length == 0 ? $"No next {key.Name}" : text.ToString(), true);
        return true;
    }

    /// <summary>
    /// Page Up and Page Down move the caret by one screen of wrapped rows and reach the first and last row
    /// (<see cref="EditKeys.Page"/>, shared with the message field). The line left is remembered for Backspace (FR-3.12)
    /// when the caret moved. The screen reader reads the new row itself, so nothing is announced here.
    /// </summary>
    private void Page(int direction)
    {
        var from = _mirror.LineCount == 0 ? null : _mirror.LineAt(_mirror.LineIndexAt(SelectionStart));
        if (EditKeys.Page(this, direction) && from is not null)
        {
            RememberJump(from);
        }
    }

    /// <summary>The l key: the transcript line the caret is on, spoken only on request. Joined wrapped rows count once.</summary>
    private void AnnounceLineNumber()
    {
        if (_mirror.LineCount == 0)
        {
            Announce("Empty", true);
            return;
        }

        var index = _mirror.LineIndexAt(SelectionStart);
        Announce($"Line {_mirror.DisplayLineOf(index) + 1} of {_mirror.DisplayLineCount}", true);
    }

    /// <summary>Enter: hands the caret's line, as the reader sees it (joined rows as one), to the window to quote in the message.</summary>
    private void ChooseLine()
    {
        if (_mirror.LineCount == 0)
        {
            Announce("Empty", true);
            return;
        }

        var index = _mirror.LineIndexAt(SelectionStart);
        LineChosen?.Invoke(_mirror.DisplayLineOf(index) + 1, _mirror.DisplayLineCount, _mirror.DisplayTextAt(index));
    }

    /// <summary>
    /// Backspace: the caret goes back to the line it left at the last jump, one jump per press (FR-3.12). A line that
    /// is no longer shown is skipped.
    /// </summary>
    private void JumpBack()
    {
        while (_jumpHistory.Count > 0)
        {
            var line = _jumpHistory[^1];
            _jumpHistory.RemoveAt(_jumpHistory.Count - 1);
            var index = _mirror.IndexOf(line);
            if (index >= 0)
            {
                MoveTo(index, prefix: "Back to ", remember: false);
                return;
            }
        }

        Announce("Nothing to go back to", true);
    }

    /// <summary>Records the caret's line before a jump moves it away (FR-3.12).</summary>
    private void RememberJump()
    {
        if (_mirror.LineCount > 0)
        {
            RememberJump(_mirror.LineAt(_mirror.LineIndexAt(SelectionStart)));
        }
    }

    /// <summary>Records <paramref name="line"/> as the one a jump left (FR-3.12); the newest entry is not repeated.</summary>
    private void RememberJump(Line line)
    {
        if (_jumpHistory.Count > 0 && ReferenceEquals(_jumpHistory[^1], line))
        {
            return;
        }

        _jumpHistory.Add(line);
        if (_jumpHistory.Count > JumpHistoryLimit)
        {
            _jumpHistory.RemoveAt(0);
        }
    }

    /// <summary>Puts the caret on a visible line (at the start, or at a column), scrolls to it and announces its text. Every jump is remembered for Backspace unless it is the way back itself.</summary>
    private void MoveTo(int index, int column = 0, string prefix = "", bool remember = true)
    {
        if (remember)
        {
            RememberJump();
        }

        Select(_mirror.StartAt(index) + _mirror.RenderedColumn(index, column), 0);
        ScrollCaretIntoView();
        if (!Announce(prefix + SpokenText(index, whole: false), true))
        {
            // No UI Automation notifications: select the line so the screen reader reports the selection.
            Select(_mirror.StartAt(index), _mirror.RenderedColumn(index, _mirror.TextAt(index).Length));
        }
    }

    /// <summary>
    /// What a jump to a line says: its text (`blank` when empty), a heading with its level, a bookmark with the line
    /// it marks. <paramref name="whole"/> takes the rows wrapped under the line as well, as the reader sees them;
    /// otherwise only the row the caret is on, as the edit control wraps it, which is what Up and Down read, so that
    /// Down after a jump goes on with the next row instead of repeating it (asked 2026-10-23).
    /// </summary>
    private string SpokenText(int index, bool whole)
    {
        var line = _mirror.LineAt(index);
        var text = whole ? _mirror.DisplayTextAt(index) : RowAt(SelectionStart);
        if (line.HeadingLevel > 0)
        {
            // The # marker is for the eye and the export; the ear gets the level instead.
            text = text.TrimStart('#').TrimStart(' ');
        }

        if (text.Length == 0)
        {
            text = "blank";
        }

        if (line.HeadingLevel > 0)
        {
            text += $" heading level {line.HeadingLevel}";
        }

        if (line.Kind == LineKind.Bookmark && index + 1 < _mirror.LineCount)
        {
            // The bookmark stands in front of the line it marks: say that line too, so one k press tells where it is.
            var marked = whole ? _mirror.DisplayTextAt(index + 1) : RowAt(_mirror.StartAt(index + 1));
            text += ", " + (marked.Length == 0 ? "blank" : marked);
        }

        return text;
    }

    /// <summary>The text of the edit control's row that holds <paramref name="position"/>, without its line break.</summary>
    private string RowAt(int position)
    {
        var row = SendMessage(Handle, EM_LINEFROMCHAR, position, IntPtr.Zero);
        var start = SendMessage(Handle, EM_LINEINDEX, row, IntPtr.Zero);
        var length = (int)SendMessage(Handle, EM_LINELENGTH, start, IntPtr.Zero);
        if (length <= 0)
        {
            return "";
        }

        // EM_GETLINE takes the buffer's size in its first character and does not end the text with a null.
        var buffer = new char[Math.Max(length, 1)];
        buffer[0] = (char)length;
        var copied = (int)SendLineMessage(Handle, EM_GETLINE, row, buffer);
        return new string(buffer, 0, Math.Clamp(copied, 0, length)).TrimEnd('\r', '\n');
    }

    [DllImport("user32.dll", CharSet = CharSet.Unicode, EntryPoint = "SendMessageW")]
    private static extern IntPtr SendLineMessage(IntPtr hWnd, int msg, IntPtr wParam, [In, Out] char[] buffer);
}
