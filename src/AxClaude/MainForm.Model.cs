using System.Text;
using AxClaude.Core.Transcript;
using Axit.Core;
using Axit.Core.Transcript;
using Axit.Forms;

namespace AxClaude;

internal sealed partial class MainForm
{
    // ---- what the session model reports (FR-7): the console window has synced the view, the status bar and the music first ----

    protected override void OnTranscriptChanged()
    {
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

        // Claude draws a question's rows before its prompt row, so reply speech waits a moment (_speechHold) and goes
        // only when Claude has not turned out to be waiting; once it has, the question's rows are dropped from what
        // waits (the control area reads the question whole, D33) and the rest, a reply line finalized just before the
        // question, is spoken at once (until 2026-10-23 all of it was dropped).
        if (arrival.Speech is not null)
        {
            _heldLines.AddRange(arrival.SpokenLines);
            _speechHold.Stop();
            _speechHold.Start();
        }

        if (QuestionShown && _heldLines.Count > 0)
        {
            _heldLines.RemoveAll(_model.IsWaitRow);
            SpeakHeld();
        }

        // What Claude waits on is acted on once the screen has settled for the attention timer (OnFrameSettled):
        // shown in the control area, and the message field put back when it went away, since Claude redraws a
        // question in passing.
        var waiting = WaitSignature;
        if (waiting is null)
        {
            if (_wait is { Shown: false })
            {
                // Announced and gone: the same question asked again later is new. The control area's own wait is
                // left to the settle, since a frame can end with the cursor off the prompt row (a repaint from the
                // top, the quiet timer cutting a redraw); forgetting it here rebuilt the list, replayed the chime and
                // announced the same question again (the refiring of 2026-10-02).
                _wait = null;
            }
            else if (_wait is not null && !_attention.Enabled)
            {
                _attention.Start();
            }
        }
        else
        {
            if (_model.HideReplay)
            {
                // A question before Claude is ready must be seen, replay or not.
                _model.ShowHiddenLines();
            }

            // A new question or screen, or the next step of one, waits for the screen to settle like the first.
            if (_wait is null)
            {
                _attention.Stop();
                _attention.Start();
            }
            else if (waiting != _wait.Signature && !_attention.Enabled)
            {
                _attention.Start();
            }
        }

        if (!_readyAnnounced && ProcessRunning && _model.Mode is not null && !_model.Working && !_model.PromptPending)
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

    /// <summary>The held reply lines go out as one notification (D32), and the hold ends.</summary>
    private void SpeakHeld()
    {
        _speechHold.Stop();
        if (_heldLines.Count > 0)
        {
            var text = new StringBuilder();
            foreach (var line in _heldLines)
            {
                Utterance.Append(text, Arrivals.SpokenText(line), line.JoinedToPrevious);
            }

            Announce(text.ToString(), false);
        }

        _heldLines.Clear();
    }

    /// <summary>
    /// The screen has settled (the attention timer, 400 ms after the bell or the last change of what waits): what
    /// the model says Claude waits on is compared with the one record of what the window did about it, and exactly
    /// one thing happens. Something new waits: it is shown in the control area, replacing what is there, or, when the
    /// area cannot show it, "Claude asks" is announced. Nothing waits any more: the area closes and the message field
    /// comes back, "Question closed" when Claude closed it by itself, and a bell that rang is "Done".
    /// </summary>
    private void OnFrameSettled()
    {
        var waiting = WaitSignature;
        if (waiting is not null)
        {
            if (_wait is null || _wait.Stale || _wait.Signature != waiting)
            {
                if (!(_settings.QuestionNotices && ShowWaiting()))
                {
                    // Claude moved on from what the control area shows to something it cannot show (D33).
                    if (_wait is { Shown: true })
                    {
                        CloseArea();
                    }

                    Handled(waiting);
                    Announce("Claude asks", false);
                    Signal(question: true);
                }
            }
        }
        else
        {
            if (_wait is { Shown: true })
            {
                // Nothing waits any more: the message field comes back. Said only when Claude closed it by itself, not
                // right after the user's answer or key.
                var byUser = UserActed();
                CloseArea();
                if (!byUser)
                {
                    Announce("Question closed", false);
                }
            }

            // The question is over: the same one asked again later is new.
            _wait = null;
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
    /// after Other. True when it was shown, or answered by the app itself (a review it submits, its own list of drafts),
    /// which signals nothing; false when the area cannot show it.
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

            Handled(question.Signature);
            if (!ShowClaudeQuestion(question))
            {
                return true;
            }
        }
        else if (_model.PendingScreen is { } screen)
        {
            Handled(screen.Signature);
            ShowClaudeScreen(screen);
        }
        else if (_model.TextPrompt is { } prompt)
        {
            Handled(prompt);
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
    /// What Claude waits on is handled: announced, shown or answered once, and recorded for q (nothing for the text
    /// row after Other, which belongs to the question before); without <paramref name="mark"/> it is handled by the
    /// app on the user's behalf and not recorded. <see cref="ShowInArea"/> then says that the area shows it.
    /// </summary>
    private void Handled(string signature, bool mark = true)
    {
        _wait = new Wait(signature, Shown: false, Text: null);
        if (mark)
        {
            _model.MarkQuestion();
        }
    }

    /// <summary>The status bar (FR-10.2): Claude's state with what waits and runs, the mode and the session name on the left; the folder on the right.</summary>
    protected override void UpdateStatus()
    {
        var state = !ProcessRunning ? "stopped"
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

        SetStatus(string.Join(" | ", parts), _folder ?? "No project folder");
    }

    private void UpdateFolderUi()
    {
        // FR-8.4: the folder's name, then its full path in parentheses, so that the name comes first in speech and
        // two folders of one name are still told apart in Alt+Tab and in the Axit menu; announcements use the name.
        Text = _folder is null ? "AxClaude" : $"{PathName.Of(_folder)} ({_folder}) - AxClaude";
        SetStatus(StateLabel.Text ?? string.Empty, _folder ?? "No project folder");
    }
}
