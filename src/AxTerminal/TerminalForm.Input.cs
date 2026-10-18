using AxTerminal.Core;
using Axit.Core.Transcript;
using Axit.Forms;

namespace AxTerminal;

internal sealed partial class TerminalForm
{
    // ---- the command field and the sending (AT-3) ----

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
        }
    }

    /// <summary>
    /// Enter (and Ctrl+Enter without a record): the field's text goes to the shell line by line, each with its
    /// Enter (AT-3.1); the model prints the exchange block above the prompt row (AT-3.2) and the command joins the
    /// recent list; an empty field sends a bare Enter. Nothing is sent once the shell ended.
    /// </summary>
    private void SendInput(bool record)
    {
        if (_host is null)
        {
            Announce("The shell ended. Ctrl+Shift+R starts it again", true);
            return;
        }

        var text = _input.Text.Replace("\r\n", "\n").TrimEnd('\n', ' ');
        _input.Clear();
        if (text.Trim().Length == 0)
        {
            Write("\r");
            return;
        }

        _model.Send(text, record);
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
    /// AxClaude's pacing, so the shell reads a multi-line command as it would from the keyboard (AT-3.1). PowerShell's
    /// console host, which runs without PSReadLine while a screen reader is on, ends a block only on an empty line,
    /// so a command of several lines gets one more Enter; with PSReadLine on it is an empty Enter at the next prompt.
    /// </summary>
    private void QueueWrites(string text)
    {
        var lines = text.Split('\n');
        foreach (var line in lines)
        {
            _writes.Enqueue((line, 60));
            _writes.Enqueue(("\r", 120));
        }

        if (lines.Length > 1 && _shell is ShellKind.PowerShell7 or ShellKind.WindowsPowerShell)
        {
            _writes.Enqueue(("\r", 120));
        }

        StartWrites();
    }

    private void StartWrites()
    {
        if (!_writing)
        {
            PumpWrites();
        }
    }

    private void PumpWrites()
    {
        if (_writes.Count == 0)
        {
            _writing = false;
            return;
        }

        _writing = true;
        var (text, delay) = _writes.Dequeue();
        Write(text);
        Later(delay, PumpWrites);
    }

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

    private void Write(string text) => _host?.Write(text);

    /// <summary>Ctrl+Shift+C (AT-3.5): Ctrl+C to the shell, and what is still queued to type goes.</summary>
    private void Interrupt()
    {
        if (_host is null)
        {
            Announce("The shell ended", true);
            return;
        }

        _writes.Clear();
        Write("\x03");
        Announce("Interrupt sent", true);
    }

    /// <summary>Shift+Escape (AT-3.5): Escape to the shell.</summary>
    private void SendEscape() => SendKey("\x1b", "Escape sent");

    /// <summary>Shell → Send key: a key the field cannot type, for a program that wants it.</summary>
    private void SendKey(string sequence, string said)
    {
        if (_host is null)
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
        if (_area.Current is not null)
        {
            CloseArea();
        }

        var existing = _input.Text.TrimEnd('\r', '\n');
        _input.Text = existing.Length == 0 ? text : existing + "\r\n" + text;
        _input.Select(_input.TextLength, 0);
        Announce("Line copied", true);
        GoTo(_input);
    }

    /// <summary>m, or Navigate → Bookmark this line: the model adds or removes the bookmark line and the result is spoken.</summary>
    private void ToggleBookmark(Line line)
    {
        if (_model.ToggleBookmark(line) is not ({ } bookmark, var added))
        {
            Announce("Line gone", true);
            return;
        }

        Announce($"{bookmark.Text} {(added ? "added" : "removed")}", true);
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
        _area.ShowList("Recent commands", items, 0, null, focus: true, hold: false, (index, _) =>
        {
            CloseArea();
            if (index >= 0 && index < items.Count)
            {
                _input.Text = items[index];
                _input.Select(_input.TextLength, 0);
            }

            _input.Focus();
        }, () =>
        {
            CloseArea();
            _input.Focus();
        });
        _input.Visible = false;
        _area.Visible = !NoticeOpen;
    }

    private void CloseArea()
    {
        if (_area.Current is null)
        {
            return;
        }

        var focus = _area.ContainsFocus;
        _input.Visible = !NoticeOpen;
        if (focus)
        {
            _input.Focus();
        }

        _area.Clear();
    }
}
