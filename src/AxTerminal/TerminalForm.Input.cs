using AxTerminal.Core;
using Axit.Forms;

namespace AxTerminal;

internal sealed partial class TerminalForm
{
    // ---- the command field and the sending (AT-3); the paced write queue is the console window's ----

    private void OnInputKeyDown(object? sender, KeyEventArgs e)
    {
        switch (e.KeyData)
        {
            case AxTerminalKeys.Enter:
                // Enter sends; Shift+Enter is the field's own new line.
                e.Handled = e.SuppressKeyPress = true;
                SendInput(record: true);
                break;
            case AxTerminalKeys.SendWithoutRecording:
                e.Handled = e.SuppressKeyPress = true;
                SendInput(record: false);
                break;
            case AxTerminalKeys.Escape:
                // The guard (AT-3.5): a reflex key, and the shell would take it as an edit of its line.
                e.SuppressKeyPress = true;
                Announce(EscapeGuardText, true);
                break;
            case AxTerminalKeys.OlderCommand:
                e.Handled = e.SuppressKeyPress = true;
                Recall(_history.Older(FieldText()));
                break;
            case AxTerminalKeys.NewerCommand:
                e.Handled = e.SuppressKeyPress = true;
                Recall(_history.Newer(FieldText()));
                break;
            default:
                // The bundle's page and paragraph keys (AX-8.2), after the field's own: Ctrl+Up and Ctrl+Down are the
                // history here, Shift with them still selects by paragraph.
                EditKeys.Handle(sender, e);
                break;
        }
    }

    private string FieldText() => Input.Text.Replace("\r\n", "\n");

    /// <summary>
    /// Ctrl+Up and Ctrl+Down (AT-3.9): a command of the recent list in the field, or the draft past the newest, with
    /// the recall earcon and the command said once, the caret at its end; the edge earcon when there is none further.
    /// Plain Up and Down stay the caret's.
    /// </summary>
    private void Recall(string? command)
    {
        if (command is null)
        {
            Sounds.Edge();
            return;
        }

        Input.Text = command.Replace("\n", "\r\n");
        Input.Select(Input.TextLength, 0);
        Sounds.Recall();
        Announce(command.Length == 0 ? "blank" : command.Replace("\n", " "), true);
    }

    /// <summary>
    /// Enter (and Ctrl+Enter without a record): the field's text goes to the shell line by line, each with its
    /// Enter (AT-3.1); the model prints the exchange block above the prompt row (AT-3.2) and the command joins the
    /// recent list; an empty field sends a bare Enter. Nothing is sent once the shell ended.
    /// </summary>
    private void SendInput(bool record)
    {
        if (!ProcessRunning)
        {
            Announce("The shell ended. Ctrl+Shift+R starts it again", true);
            return;
        }

        var text = Input.Text.Replace("\r\n", "\n").TrimEnd('\n', ' ');
        Input.Clear();
        if (text.Trim().Length == 0)
        {
            Write("\r");
            return;
        }

        _model.Send(text, record, CurrentDirectory);
        if (record)
        {
            _history.Add(text);
        }

        QueueWrites(text);
        Announce(record ? "Sent" : "Sent, not recorded", false);
        if (_settings.SoundOnBell)
        {
            Sounds.Sent();
        }
    }

    /// <summary>
    /// Queues a command for the shell: each line as its own write and Enter as a separate write after it, with
    /// axClaude's pacing, so the shell reads a multi-line command as it would from the keyboard (AT-3.1). PowerShell's
    /// console host, which runs without PSReadLine while a screen reader is on, ends a block only on an empty line,
    /// so a command of several lines gets one more Enter; with PSReadLine on it is an empty Enter at the next prompt.
    /// </summary>
    private void QueueWrites(string text)
    {
        var lines = text.Split('\n');
        foreach (var line in lines)
        {
            Enqueue(line, 60);
            Enqueue("\r", 120);
        }

        if (lines.Length > 1 && _shell is ShellKind.PowerShell7 or ShellKind.WindowsPowerShell)
        {
            Enqueue("\r", 120);
        }

        StartWrites();
    }

    /// <summary>Ctrl+Shift+C (AT-3.5): Ctrl+C to the shell, and what is still queued to type goes.</summary>
    private void Interrupt()
    {
        if (!ProcessRunning)
        {
            Announce("The shell ended", true);
            return;
        }

        ClearWrites();
        Write("\x03");
        Announce("Interrupt sent", true);
    }

    /// <summary>Shift+Escape (AT-3.5): Escape to the shell.</summary>
    private void SendEscape() => SendKey("\x1b", "Escape sent");

    /// <summary>Shell → Send key: a key the field cannot type, for a program that wants it.</summary>
    private void SendKey(string sequence, string said)
    {
        if (!ProcessRunning)
        {
            Announce("The shell ended", true);
            return;
        }

        Write(sequence);
        Announce(said, true);
    }

    /// <summary>
    /// Enter on a terminal line (AT-3.7): the line's text goes into the field, after a new line when the field holds
    /// text, and the focus goes there with the caret at the end, to run or edit it. No quote markers (AT-D4).
    /// </summary>
    private void CopyLineToField(int number, int count, string text)
    {
        if (Area.Current is not null)
        {
            CloseArea();
        }

        var existing = Input.Text.TrimEnd('\r', '\n');
        Input.Text = existing.Length == 0 ? text : existing + "\r\n" + text;
        Input.Select(Input.TextLength, 0);
        Announce("Line copied", true);
        GoTo(Input);
    }

    /// <summary>
    /// Ctrl+R (AT-3.6): the window's recent commands as a list in the field's place, the newest first; Enter puts the
    /// chosen one in the field and focuses it, Escape brings the field back.
    /// </summary>
    private void ShowRecentCommands()
    {
        if (_history.Items.Count == 0)
        {
            Announce("No recent commands", true);
            return;
        }

        var items = _history.Items.ToList();
        Area.ShowList("Recent commands", items, 0, null, focus: true, hold: false, (index, _) =>
        {
            CloseArea();
            if (index >= 0 && index < items.Count)
            {
                Input.Text = items[index];
                Input.Select(Input.TextLength, 0);
            }

            Input.Focus();
        }, () =>
        {
            CloseArea();
            Input.Focus();
        });
        Input.Visible = false;
        Area.Visible = !NoticeOpen;
    }
}
