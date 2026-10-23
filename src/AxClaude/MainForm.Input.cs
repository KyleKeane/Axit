using AxClaude.Core.Transcript;
using Axit.Forms;

namespace AxClaude;

internal sealed partial class MainForm
{
    // ---- the message field and the sending (FR-2); the paced write queue is the console window's ----

    private void OnInputKeyDown(object? sender, KeyEventArgs e)
    {
        // The keys come from the tables (AxClaudeKeys, BundleKeys), matched with their modifiers.
        switch (e.KeyData)
        {
            case AxClaudeKeys.Enter:
                // Enter sends; Shift+Enter is the field's own new line (FR-2.1).
                e.Handled = e.SuppressKeyPress = true;
                SendInput();
                break;
            case AxClaudeKeys.Escape:
                // A guard (FR-2.3, D20): Escape is a reflex key for screen reader users, and Claude takes it as an interrupt.
                e.SuppressKeyPress = true;
                Announce("Shift+Escape interrupts", true);
                break;
                // Plain Up and Down only move the caret (D13): text appearing in the field on an arrow key confused the reading.
        }
    }

    /// <summary>The literal lines around a quoted conversation line in the message, so Claude can tell it from the user's words (FR-2.9).</summary>
    private const string QuoteMarker = "_ start of copied line from conversation history _";
    private const string CommentMarker = "_ start of comment on the copied line _";

    /// <summary>
    /// Enter on a conversation line appends the line to the message under the marker and a line naming its number,
    /// separated from what is there by a blank line, then the comment marker, and moves the focus into the field
    /// with the caret on the empty line under it, ready for the comment (FR-2.9). The conversation caret stays
    /// where it is, so Ctrl+2 comes back to the same line.
    /// </summary>
    private void QuoteLineInMessage(int number, int count, string text)
    {
        if (Area.Current is not null)
        {
            // The field is hidden behind Claude's question: the quote would land unseen and the comment in the list.
            Announce("Answer Claude first; the message field is hidden while it asks", true);
            return;
        }

        var existing = Input.Text.TrimEnd('\r', '\n');
        var quote = $"{QuoteMarker}\r\nLine {number} of {count}:\r\n{text}\r\n{CommentMarker}\r\n";
        Input.Text = existing.Length == 0 ? quote : existing + "\r\n\r\n" + quote;
        Input.Select(Input.TextLength, 0);
        Announce($"Line {number} copied", true);
        GoTo(BottomControl);
    }

    /// <summary>
    /// Text that answers what Claude waits on: any text on an "Enter text for" row; else one of the question's keys,
    /// or several when it allows several; or, when the question could not be read, y, n or a number. Never a key of a
    /// screen.
    /// </summary>
    private bool IsAnswer(string text) =>
        _model.AwaitsText
        || _model.PendingScreen is null
        && (_model.PendingQuestion is { } question
            ? question.Accepts(text)
            : text is "y" or "n" or "Y" or "N" || text.All(char.IsAsciiDigit));

    private void SendInput()
    {
        if (!ProcessRunning)
        {
            // The text stays in the field: nothing was sent, and it is wanted again after a restart.
            Announce("Claude stopped. Ctrl+Shift+R starts it", true);
            return;
        }

        var text = Input.Text.Replace("\r\n", "\n").Trim();
        if (_settings.QuestionNotices && text.Length > 0 && _model.PromptPending && !IsAnswer(text) && text != _keptMessage)
        {
            // D33: text sent into a question is lost (Claude answers "Please answer y or n." and drops it) or taken
            // as a key of the screen. It stays in the field; the same text sent again goes anyway (a typed answer).
            _keptMessage = text;
            var about = _model.PendingQuestion is { } waiting ? $": {waiting.Title}"
                : _model.PendingScreen is { } screen ? $": {screen.Title}" : string.Empty;
            Announce($"Not sent, Claude asks{about}. Enter again sends", true);
            return;
        }

        _keptMessage = null;
        Input.Clear();
        if (text.Length == 0)
        {
            Write("\r");
            return;
        }

        var answer = _model.PromptPending;
        if (answer && IsAnswer(text))
        {
            // Typed like the control area's answers, one keystroke each ("1,3" for several answers, the words of an
            // answer of the user's own on one line).
            SendAnswer(text.Replace('\n', ' '));
            Announce("Sent", false);
            return;
        }

        // A question that follows belongs to the slash command sent last (QuestionRouter); an answer keeps it for the
        // next step of the same screen, a message ends it.
        if (!answer)
        {
            _lastCommand = SlashCommands.Find(text);
        }

        // A message sent into a question on purpose (the kept message sent again, or the guard off) gets its block too (FR-2.10).
        var queued = _model.Working;
        _model.Send(text, evenWhilePrompting: true);
        _messageSent = true;
        QueueWrites(text);
        Announce(queued && !answer ? "Queued" : "Sent", false);
        if (_settings.SoundOnBell)
        {
            Sounds.Sent();
        }
    }

    /// <summary>
    /// Queues a message or a command for Claude: the text and Enter as separate writes, since text plus CR in one
    /// chunk is treated as a paste (SPEC.md 4.4); the lines of a multi-line message are joined with a lone newline
    /// write, Claude's Ctrl+J.
    /// </summary>
    private void QueueWrites(string text)
    {
        var lines = text.Split('\n');
        for (var i = 0; i < lines.Length; i++)
        {
            Enqueue(lines[i], i == lines.Length - 1 ? 100 : 30);
            if (i < lines.Length - 1)
            {
                Enqueue("\n", 30);
            }
        }

        Enqueue("\r", 50);
        StartWrites();
    }

    /// <summary>
    /// Claude → Send queued messages now (Ctrl+Shift+S): Claude's Ctrl+X Ctrl+S, which makes it take up a
    /// message it holds without finishing the current step first (FR-2.8). The chord goes as two writes.
    /// </summary>
    private void SendNow()
    {
        if (_model.QueuedMessages == 0)
        {
            Announce("Nothing queued", false);
            return;
        }

        Enqueue("\x18", 30);
        Enqueue("\x13", 30);
        StartWrites();

        Announce("Sending now", false);
    }

    /// <summary>A one-shot timer for the question flow's settles (D33); the write queue has its own pump in the console window.</summary>
    private static void Later(int milliseconds, Action action)
    {
        var timer = new System.Windows.Forms.Timer { Interval = milliseconds };
        timer.Tick += (_, _) =>
        {
            timer.Stop();
            timer.Dispose();
            action();
        };
        timer.Start();
    }

    /// <summary>Escape to Claude (FR-2.3), and what is still queued to type goes: an answer typed out a key at a time must not land after it.</summary>
    private void Interrupt()
    {
        ClearWrites();
        Write("\x1b");
    }
}
