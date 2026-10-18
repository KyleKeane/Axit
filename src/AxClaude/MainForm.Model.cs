using System.Runtime.InteropServices;
using AxClaude.Core.Transcript;
using Axit.Forms;

namespace AxClaude;

internal sealed partial class MainForm
{
    // ---- model events ----

    private void OnModelChanged()
    {
        _transcript.Sync(_model.Lines);
        UpdateStatus();
        UpdateMusic();

        // One pass over the transcript for the tick (FR-7.8) and the spoken reply and tool calls (FR-7.4). Nothing is
        // spoken before Claude is ready in this run: what it prints until then is the replayed conversation, there for
        // reading. The chimes hang on the send, the first reply line and the bell (FR-7.3), never on the working status.
        var arrival = _arrivals.Update(_model.Lines, _model.Working, _readyAnnounced ? _settings.ReplySpeech : ReplySpeechMode.None, _readyAnnounced && _settings.SpeakToolCalls);
        if (_settings.ClickOnNewLine)
        {
            // A frame that brings a tool call gets the tool tick in place of the line's (FR-7.8).
            if (arrival.ToolCall)
            {
                Sounds.Tool();
            }
            else if (arrival.NewLine)
            {
                Sounds.Click();
            }
        }

        // The rows of a question are not read as reply lines while the control area reads the question (D33): new
        // rows only, repeats left out, they gave "→ Tea. → Dinner" before the review was read whole. Claude draws a
        // question's rows before its prompt row, so reply speech waits a moment (_speechHold) and goes only when
        // Claude has not turned out to be waiting.
        if (arrival.Speech is { } speech)
        {
            _heldSpeech = _heldSpeech is null ? speech : _heldSpeech + " " + speech;
            _speechHold.Stop();
            _speechHold.Start();
        }

        if (QuestionShown)
        {
            _heldSpeech = null;
        }

        // What Claude waits on is acted on once the screen has settled for the attention timer (AnnounceAttention):
        // showing it in the control area, and putting the message field back when it went away, since Claude redraws
        // a question in passing.
        var waiting = WaitSignature;
        if (_shownWait is { } shown && waiting != shown && !_attention.Enabled)
        {
            _attention.Start();
        }

        if (_model.PromptPending)
        {
            if (_model.HideReplay)
            {
                // A question before Claude is ready must be seen, replay or not.
                _model.ShowHiddenLines();
            }

            // A new question or screen, or the next step of one, waits for the screen to settle like the first.
            if (!_promptAnnounced || (waiting != _handledWait && !_attention.Enabled && _shownWait is null))
            {
                _attention.Stop();
                _attention.Start();
            }
        }
        else if (_shownWait is null)
        {
            // Claude redraws a question in passing, and a frame can end with the cursor off the prompt row (a repaint
            // from the top, the quiet timer cutting a redraw). While the control area shows the question it stays
            // handled; AnnounceAttention forgets it when nothing waits any more. Forgetting it here made the next frame
            // rebuild the list, replay the chime and announce the same question again (the refiring of 2026-10-02).
            _promptAnnounced = false;
            _handledWait = null;
        }

        if (!_readyAnnounced && _host is not null && _model.Mode is not null && !_model.Working && !_model.PromptPending)
        {
            _readyAnnounced = true;
            _model.HideReplay = false;
            // Speech starts here: the replayed conversation gets its markers and is not read out.
            _arrivals.Skip(_model.Lines);
            _model.MarkReplayedExchanges();
            Announce("Ready", false);
            if (_settings.SoundOnBell)
            {
                Sounds.Ready();
            }
        }

        OpenDraftsWhenIdle();
    }

    private void AnnounceAttention()
    {
        var waiting = WaitSignature;
        if (_model.PromptPending && _settings.QuestionNotices && waiting != _handledWait && ShowWaiting())
        {
            // In the control area, which replaced what it showed before (the next question of a set) in place.
        }
        else if (_model.PromptPending)
        {
            // Claude moved on from what the control area shows to something it cannot show (D33).
            if (_shownWait is { } shown && waiting != shown)
            {
                CloseArea();
            }

            if (!_promptAnnounced || waiting != _handledWait)
            {
                MarkHandled(waiting);
                Announce("Claude asks", false);
                Signal(question: true);
            }
        }
        else
        {
            // Nothing waits any more: the message field comes back. Said only when Claude closed it by itself, not
            // right after the user's answer or key.
            if (_area.Current is not null)
            {
                var byUser = UserActed(Math.Max(_answerAt, _screenKeyAt));
                CloseArea();
                // The question is over: the same one asked again later is new.
                _promptAnnounced = false;
                _handledWait = null;
                if (!byUser)
                {
                    Announce("Question closed", false);
                }
            }

            if (_bellPending)
            {
                if (_settings.AnnounceBell)
                {
                    Announce("Done", false);
                }

                Signal(question: false);
            }
        }

        _bellPending = false;
    }

    /// <summary>
    /// Shows what Claude waits on in the control area (D33): a list of answers unless the slash command sent last
    /// leaves its lists to the message field, a screen waiting on its hint row, or the row for the user's own words
    /// after Other. False when it is none of these.
    /// </summary>
    private bool ShowWaiting()
    {
        if (_model.PendingQuestion is { } question)
        {
            if (QuestionRouter.DialogFor(_lastCommand) != QuestionDialog.AnswerNotice)
            {
                return false;
            }

            if (AnswerDraftsList(question))
            {
                return true;
            }

            MarkHandled(question.Signature);
            ShowClaudeQuestion(question);
        }
        else if (_model.PendingScreen is { } screen)
        {
            MarkHandled(screen.Signature);
            ShowClaudeScreen(screen);
        }
        else if (_model.TextPrompt is { } prompt)
        {
            MarkHandled(prompt);
            ShowOwnAnswer(prompt);
        }
        else
        {
            return false;
        }

        // The taskbar flash; ShowInArea plays the chime for what the area shows.
        Signal(question: true, chime: false);
        return true;
    }

    /// <summary>
    /// What Claude waits on is handled, announced or shown once, and recorded for q (nothing for the text row after
    /// Other, which belongs to the question before); without <paramref name="mark"/> it is handled by the app on the
    /// user's behalf and not recorded.
    /// </summary>
    private void MarkHandled(string? signature, bool mark = true)
    {
        _promptAnnounced = true;
        _handledWait = signature;
        if (mark)
        {
            _model.MarkQuestion();
        }
    }

    /// <summary>
    /// The chime (two equal notes for a question, three falling notes when Claude is done, FR-7.3) and the taskbar
    /// flash that go with an attention announcement; without <paramref name="chime"/> only the flash (ShowInArea
    /// plays the chime for what the control area shows). While the window is in the background the alert, when it is on, plays in
    /// place of any chime, the area's included: it is the one sound meant to carry into another window.
    /// </summary>
    private void Signal(bool question, bool chime = true)
    {
        var away = !ContainsFocus;
        if (_settings.BackgroundAlert && away)
        {
            Sounds.Alert();
        }
        else if (_settings.SoundOnBell && chime)
        {
            if (question)
            {
                Sounds.Question();
            }
            else
            {
                Sounds.Done();
            }
        }

        if (_settings.FlashTaskbar && away && IsHandleCreated)
        {
            var info = new FLASHWINFO
            {
                cbSize = (uint)Marshal.SizeOf<FLASHWINFO>(),
                hwnd = Handle,
                dwFlags = FLASHW_ALL | FLASHW_TIMERNOFG,
                uCount = 0,
                dwTimeout = 0,
            };
            FlashWindowEx(ref info);
        }
    }

    /// <summary>
    /// The working music (FR-7.9) plays exactly while the status says working: it starts with the first working
    /// frame and stops with the frame in which Claude is done, asks, or was stopped, and at once when the option is
    /// turned off.
    /// </summary>
    private void UpdateMusic()
    {
        var play = _settings.WorkingMusic && _host is not null && _model.Working && !_model.PromptPending;
        if (play == _musicOn)
        {
            return;
        }

        _musicOn = play;
        if (play)
        {
            Sounds.StartMusic();
        }
        else
        {
            Sounds.StopMusic();
        }
    }

    /// <summary>
    /// Ctrl+Space anywhere in the window (FR-7.9): the music option flips through its menu item, which applies and
    /// saves it, and the result is said; when Claude is not working at that moment nothing is heard yet, which is said too.
    /// </summary>
    private void ToggleMusic()
    {
        _musicItem.Checked = !_musicItem.Checked;
        Announce(!_settings.WorkingMusic ? "Background music off" : _musicOn ? "Background music on" : "Background music on, nothing is running", true);
    }

    private void UpdateStatus()
    {
        var state = _host is null ? "stopped"
            : _model.TranscriptViewOpen ? "detailed view on, Ctrl+O turns it off"
            : _model.PromptPending ? "waiting for your answer"
            : _model.Working ? ("working " + (_model.Spinner ?? string.Empty)).Trim()
            : _model.Mode is not null ? "ready"
            : "starting";
        var parts = new List<string> { "Claude: " + state };
        if (_model.QueuedMessages > 0)
        {
            parts.Add(_model.QueuedMessages == 1 ? "1 message waiting" : $"{_model.QueuedMessages} messages waiting");
        }

        if (ClaudeBackground is { } background)
        {
            parts.Add(background + " in the background");
        }

        if (_model.Mode is { } mode)
        {
            parts.Add(mode);
        }

        if (_model.SessionName is { } name)
        {
            parts.Add(name);
        }

        var text = string.Join(" | ", parts);
        if (_state.Text != text)
        {
            _state.Text = text;
            StatusLayout.Fit(_status, _state, _folderLabel);
        }
    }

    private void UpdateFolderUi()
    {
        // FR-8.4: the folder's name, then its full path in parentheses, so that the name comes first in speech and
        // two folders of one name are still told apart in Alt+Tab and in the Axit menu; announcements use the name.
        Text = _folder is null ? "AxClaude" : $"{RecentMenu.FolderName(_folder)} ({_folder}) - AxClaude";
        _folderLabel.Text = _folder ?? "No project folder";
        StatusLayout.Fit(_status, _state, _folderLabel);
    }

    /// <summary>
    /// Ctrl+1, Ctrl+2 and the Navigate menu (FR-6.5): the control takes the focus, and NVDA announces the change. When it
    /// has the focus already, its name is spoken instead, so the key also answers "where am I".
    /// </summary>
    private void GoTo(Control control)
    {
        if (control.Focused)
        {
            Announce(control.AccessibleName ?? string.Empty, true);
            return;
        }

        control.Focus();
    }

    /// <summary>Speaks without moving the focus; from the notice while one shows, since the conversation is hidden then.</summary>
    protected override bool Announce(string text, bool interrupt) =>
        NoticeOpen ? Overlay.Announce(text, interrupt) : _transcript.Announce(text, interrupt);
}
