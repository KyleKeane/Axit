using System.Text;
using AxClaude.Core.Pty;
using AxClaude.Core.Transcript;
using Axit.Forms;

namespace AxClaude;

internal sealed partial class MainForm
{
    // ---- the control area: what Claude waits on, in the message field's place (D33) ----

    /// <summary>Claude waits on something the control area reads, so its rows are not read as reply lines too.</summary>
    private bool QuestionShown => _settings.QuestionNotices && _model.PromptPending;

    /// <summary>The control at the bottom of the window: the message field, or the control area's list or field in its place.</summary>
    private Control BottomControl => _area.Current ?? _input;

    /// <summary>
    /// Shows what Claude waits on in the control area, in place of the message field (D33). <paramref name="show"/>
    /// puts the control there, given whether it takes the focus and whether it holds its answering keys for a moment
    /// (when the user did not cause it, since they may be typing). Such a control also plays the notice chime. With
    /// Go to them at once, interrupting speech (the default) it takes the focus wherever it was, and the question
    /// with the answer the focus is on is spoken as an interrupting notification 300 ms after the focus moved: it cuts off
    /// what NVDA was reading, a reply included, and comes last (raised before the focus, or at once after it, it was
    /// cut off). Without the option, the focus moves at once from the bottom of the window and after a moment from
    /// the conversation, and nothing interrupts: NVDA reads the question when the focus reaches it, after what it was
    /// reading (a focus change alone does not cut NVDA off).
    /// </summary>
    private void ShowInArea(string signature, string name, string text, bool unprompted, Action<bool, bool> show)
    {
        _areaText = text;
        var interrupt = unprompted && !NoticeOpen && _settings.InterruptForQuestions;
        var focus = !NoticeOpen && (interrupt || _input.ContainsFocus || _area.ContainsFocus);
        if (unprompted && _settings.SoundOnBell)
        {
            Sounds.Notice();
        }

        show(focus, unprompted);
        _input.Visible = false;
        _area.Visible = !NoticeOpen;
        _shownWait = signature;
        if (interrupt)
        {
            // After NVDA has taken the focus change, so that the question comes last: raised at once, it was cut off.
            var question = (_area.CurrentItem.Length == 0 ? name : name + Stop(name) + _area.CurrentItem).TrimEnd(' ', '.');
            Later(FocusSettleMs, () =>
            {
                if (_shownWait == signature)
                {
                    Announce(question, true);
                }
            });
        }
        else if (!focus && !NoticeOpen && (_transcript.ContainsFocus || !ContainsFocus))
        {
            // From the conversation, or with the window not the active one, without the interrupting kind, or within
            // three seconds of the user's own answer: the focus goes to the question after a moment, so that it is
            // not found by Tab, and NVDA reads it there after what it is reading (asked 2026-10-21; the three-second
            // case was found by the off-screen replay). Not from an open menu, which the move would close (2026-10-23).
            Later(QuestionFocusMs, () =>
            {
                if (_shownWait == signature && !NoticeOpen && _area.Current is { } current && !current.ContainsFocus)
                {
                    current.Focus();
                }
            });
        }
    }

    /// <summary>The message field comes back in the control area's place, with the focus when the area had it.</summary>
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
        _shownWait = null;
        _areaText = null;
    }

    /// <summary>
    /// Ctrl+Q (D33): the question or screen the control area shows, spoken again on demand from anywhere, the title
    /// and all its lines and then the answer the focus is on, as when it opened; the focus stays. NVDA+Tab repeats a
    /// question too, since its list is named with it, but a screen's lines are items and its name only the title.
    /// </summary>
    private void ReadQuestionAgain()
    {
        if (_area.Current is null || _areaText is not { } text)
        {
            Announce("Claude is not asking anything", true);
            return;
        }

        Announce((text + Stop(text) + _area.CurrentItem).TrimEnd(' ', '.'), true);
    }

    /// <summary>
    /// A question as a list: its answers as items ("2. Green"), the current one marked and the start, or check boxes
    /// when it takes several, ticked where Claude marks them. Enter hands <paramref name="choose"/> the chosen
    /// answer, or the ticked ones; <paramref name="startKey"/> starts on another answer (the setting just changed).
    /// </summary>
    private void ShowQuestionList(string signature, string name, string text, Question question, string? startKey, bool unprompted,
        Action<IReadOnlyList<QuestionOption>> choose, Action escape)
    {
        var options = question.Options;
        var several = question.AllowsSeveral;
        var items = options.Select(option => $"{option.Key}. {option.Text}" + (option.Current && !several ? " (current)" : string.Empty)).ToList();
        var start = Math.Max(0, options.ToList().FindIndex(option => startKey is null ? option.Current : option.Key == startKey));
        IReadOnlyList<int>? ticked = several ? Enumerable.Range(0, options.Count).Where(i => options[i].Current).ToList() : null;
        ShowInArea(signature, name, text, unprompted, (focus, hold) => _area.ShowList(name, items, start, ticked, focus, hold, (focused, ticks) =>
        {
            if (several && ticks.Count == 0)
            {
                Announce("Tick one with Space", true);
                return;
            }

            choose(several ? ticks.Select(i => options[i]).ToList() : [options[focused]]);
        }, escape));
    }

    /// <summary>
    /// What the list of a question is called, read each time the focus comes to it: the title (with its place among
    /// several) and the question's lines while they stay short, so that the review of several answers says the
    /// answers and "Ready to submit your answers?", and a long plan is not read out whole. With no
    /// <paramref name="limit"/>, the whole question, which Ctrl+Q speaks (<see cref="ReadQuestionAgain"/>).
    /// </summary>
    private static string QuestionName(Question question, int limit = 1000)
    {
        var name = new StringBuilder(question.Step is (int number, int count) ? $"Question {number} of {count}: {question.Title}" : question.Title);
        foreach (var line in question.Text)
        {
            if (name.Length + line.Length > limit)
            {
                break;
            }

            name.Append(Stop(name.ToString())).Append(line);
        }

        return name.ToString();
    }

    /// <summary>What goes between two sentences: a space after a text that ends in its own punctuation, else a full stop and a space; nothing after an empty text.</summary>
    private static string Stop(string before) => before.Length == 0 ? string.Empty : before[^1] is '.' or '?' or '!' or ':' ? " " : ". ";

    /// <summary>
    /// One of Claude's questions (D33): the answers as a list in the control area. Enter answers, Escape cancels the
    /// question. Other, like any answer, is sent as its key; Claude then asks for the words, and
    /// <see cref="ShowOwnAnswer"/> follows.
    /// </summary>
    private void ShowClaudeQuestion(Question question)
    {
        if (question.IsSettings)
        {
            ShowSettings(question);
            return;
        }

        if (SubmitsReview(question))
        {
            // The last answer's "Sent" said nothing about a review; y goes as the user's own answer would.
            SendAnswer("y");
            return;
        }

        _askedQuestion = question;
        ShowQuestionList(question.Signature, QuestionName(question), QuestionName(question, int.MaxValue), question, null, !UserActed(_answerAt), AnswerQuestion, () =>
        {
            SendEscape();
            Announce("Cancelled", false);
        });
    }

    /// <summary>
    /// /config's list of settings (docs/axclaude/claude-screens.md), read off Claude's screen each time it shows. Enter on a
    /// true-or-false setting asks for its value (<see cref="ShowSwitch"/>), since Claude flips it when its number is
    /// sent; on any other setting it sends the number and Claude's list of its values follows. Either way the list comes
    /// back on the setting just chosen. Escape saves and closes.
    /// </summary>
    private void ShowSettings(Question settings, bool back = false) =>
        ShowQuestionList(settings.Signature, "Claude's settings", "Claude's settings", settings, _settingKey, !back && !UserActed(_answerAt), chosen =>
        {
            var setting = chosen[0];
            _settingKey = setting.Key;
            if (setting.Switch is { } on)
            {
                ShowSwitch(settings, setting, on);
            }
            else
            {
                SendAnswer(setting.Key);
            }
        }, () =>
        {
            _settingKey = null;
            SendEscape();
            Announce("Saved", false);
        });

    /// <summary>A true-or-false setting of /config: True and False, the current one first in focus. Only a change sends the setting's number.</summary>
    private void ShowSwitch(Question settings, QuestionOption setting, bool on)
    {
        var name = setting.Setting?.Name ?? setting.Text;
        var values = new Question(name, [], [new QuestionOption("1", "True", on), new QuestionOption("2", "False", !on)]);
        ShowQuestionList(settings.Signature, name, name, values, null, false, chosen =>
        {
            if (chosen[0].Current)
            {
                // Unchanged: nothing to send, and the settings come back on this one.
                ShowSettings(settings, back: true);
            }
            else
            {
                SendAnswer(setting.Key);
            }
        }, () => ShowSettings(settings, back: true));
    }

    /// <summary>Sends the chosen answer's key, or the ticked keys joined with commas, and says what comes next.</summary>
    private void AnswerQuestion(IReadOnlyList<QuestionOption> chosen)
    {
        SendAnswer(string.Join(",", chosen.Select(option => option.Key)));
        // Each answer's name without Claude's explanation after the dash.
        Announce("Sent: " + string.Join(", ", chosen.Select(option =>
        {
            var dash = option.Text.IndexOf(" — ", StringComparison.Ordinal);
            return $"{option.Key}, {(dash > 0 ? option.Text[..dash] : option.Text)}";
        })) + (chosen.Any(option => option.Text.Equals("Other", StringComparison.OrdinalIgnoreCase))
            // Claude asks for the words of Other next (ShowOwnAnswer).
            ? ". Your words next"
            : NextStep()), false);
    }

    /// <summary>After an answer to one of several questions, what comes next, so that the gap before it does not sound like the end.</summary>
    private string NextStep() => _askedQuestion?.Step is (int number, int count)
        ? number < count ? ". Next question" : _settings.ReviewAnswers ? ". Review next" : string.Empty
        : string.Empty;

    /// <summary>
    /// Claude's "Review your answers" right after the user answered the last of its questions, which the app answers y
    /// itself unless Review answers is on (D33). Not when it warns that a question has no answer, and not a review the
    /// user came back to by some other way.
    /// </summary>
    private bool SubmitsReview(Question question) =>
        !_settings.ReviewAnswers
        && question.Title == "Review your answers"
        && question.OptionFor("y") is not null
        && !question.Text.Any(line => line.StartsWith("warning:", StringComparison.Ordinal))
        && _askedQuestion?.Step is (int number, int count) && number == count
        && UserActed(_answerAt);

    /// <summary>
    /// The follow-up of a question when Claude asks for the user's own words ("Enter text for option 4 (Other), or
    /// Escape for the list:", also plan approval's "No, keep planning"): a field named with the question and the
    /// answer chosen. Enter sends the words, a keystroke each, and Escape goes back to Claude's list.
    /// </summary>
    private void ShowOwnAnswer(string prompt)
    {
        var chosen = LineClassifier.TextPromptAnswer(prompt) ?? "an answer that asks for your own words";
        var asked = _askedQuestion is { } question ? QuestionName(question) : null;
        var name = (asked is null ? string.Empty : asked + Stop(asked)) + $"You chose {chosen}. Your answer";
        ShowInArea(prompt, name, name, !UserActed(_answerAt), (focus, hold) => _area.ShowField(name, focus, hold, typed =>
        {
            var words = typed.Trim();
            if (words.Length == 0)
            {
                Announce("Type an answer", true);
                return;
            }

            SendAnswer(words);
            Announce("Sent: " + words + NextStep(), false);
        }, SendEscape));
    }

    /// <summary>What Claude waits on now, as a signature: the question, the screen, or the row for the user's own words; null when it waits on nothing.</summary>
    private string? WaitSignature => _model.PendingQuestion?.Signature ?? _model.PendingScreen?.Signature ?? _model.TextPrompt;

    /// <summary>
    /// A screen of Claude's that waits on its hint row (/status, /tasks, /help …; D33) as a list: its lines, which the
    /// conversation does not show for a tab screen, then an item for every key the hint row names ("View (Enter)",
    /// "Close (Escape)"). Enter on a key's item sends the key, Escape sends Escape; the screen Claude shows next takes
    /// the list's place, without the chime since the user caused it.
    /// </summary>
    private void ShowClaudeScreen(ClaudeScreen screen)
    {
        var items = screen.Lines.ToList();
        var first = items.Count;
        items.AddRange(screen.Keys.Select(key => $"{char.ToUpperInvariant(key.Action[0])}{key.Action[1..]} ({key.Name})"));
        // The key that leaves the screen: Escape, or the one key of "Any key to continue".
        var escape = screen.Keys.FirstOrDefault(key => key.Send == "\x1b") ?? screen.Keys[0];
        var closes = escape.Action.ToLowerInvariant() is "close" or "dismiss" or "cancel" or "exit";
        var leaves = closes || escape.Action == "continue";
        void Leave()
        {
            SendScreenKey(escape.Send);
            if (closes)
            {
                Announce("Closed", false);
            }
        }

        // The screen's keys work from anywhere in the list, as in Claude, where only its highlight counts (D33). Enter
        // on one of the screen's lines does what the hint row says Enter does (send on the review of a draft, next on
        // one of its switches, view in /tasks); a screen whose hint names no Enter is left when Escape closes it, and
        // otherwise the actions are named, since Escape would put the screen off. Until 2.11.0 Enter on a line meant
        // Escape, here Later, so Enter on the line "❯  Send feedback", taken for the button, put the draft off.
        var enter = screen.Keys.FirstOrDefault(key => key.Send == "\r");
        void EnterOnLine()
        {
            if (enter is not null)
            {
                SendScreenKey(enter.Send);
            }
            else if (leaves)
            {
                Leave();
            }
            else
            {
                Announce("Actions at the end of the list: " + string.Join(", ", items.Skip(first)), true);
            }
        }

        // A letter the hint row names (d to discard, x to stop) goes to Claude from anywhere in the list; any other
        // letter is the list's own jump to the item that starts with it.
        bool Letter(char letter)
        {
            if (screen.Keys.FirstOrDefault(key => key.Send.Length == 1 && key.Send[0] == letter) is not { } key)
            {
                return false;
            }

            SendScreenKey(key.Send);
            return true;
        }

        var text = new StringBuilder(screen.Title);
        foreach (var line in screen.Lines)
        {
            text.Append(Stop(text.ToString())).Append(line);
        }

        if (screen.Editing)
        {
            ShowEditedRow(screen, text.ToString());
            return;
        }

        ShowInArea(screen.Signature, screen.Title, text.ToString(), !UserActed(_screenKeyAt), (focus, hold) =>
            _area.ShowList(screen.Title, items, 0, null, focus, hold, (index, _) =>
            {
                if (index < first)
                {
                    EnterOnLine();
                    return;
                }

                SendScreenKey(screen.Keys[index - first].Send);
            }, Leave, Letter));
    }

    /// <summary>
    /// A feedback draft Claude has queued in the turn that just ended, counted on its mode line ("1 feedback draft",
    /// D33, <see cref="FeedbackDraftWatch"/>): /feedback opens for it once Claude is idle, after a short settle, and
    /// Claude's list of drafts is answered with the draft's title (<see cref="AnswerDraftsList"/>), so that its
    /// review comes straight up as a screen in the control area, like a question. The card Claude prints for the
    /// draft has keys that do nothing in screen reader mode; its rows are chrome. Drafts counted at startup or left
    /// by other sessions, and a draft put off with Later, wait for /feedback by hand. Not with Answer from a list off,
    /// when the card stays in the conversation and /feedback is typed.
    /// </summary>
    private void OpenDraftsWhenIdle()
    {
        if (!_readyAnnounced || _host is null)
        {
            return;
        }

        var idle = ClaudeIdle;
        if (idle && !UserActed(Math.Max(_screenKeyAt, _draftOpenedAt)))
        {
            // The list never came, or the review ended without the list: nothing of the draft's is on its way.
            _draftToReview = null;
            _reviewing = false;
        }

        if (!_drafts.Drafted(_model.Working, idle, _model.FeedbackDrafts))
        {
            return;
        }

        if (!_settings.QuestionNotices)
        {
            _drafts.Opened(_model.FeedbackDrafts);
        }
        else if (!_openingDrafts)
        {
            _openingDrafts = true;
            Later(DraftSettleMs, OpenFeedbackDrafts);
        }
    }

    /// <summary>Claude runs, waits on nothing, and no write of the user's is on its way to it.</summary>
    private bool ClaudeIdle => _host is not null && !_model.Working && !_model.PromptPending && !_writing && _writes.Count == 0;

    /// <summary>Sends /feedback as the user's own command would go, without "Sent": the review of the draft announces itself. Skipped when Claude is no longer idle.</summary>
    private void OpenFeedbackDrafts()
    {
        _openingDrafts = false;
        if (!ClaudeIdle || _area.Current is not null || !_drafts.Drafted(_model.Working, idle: true, _model.FeedbackDrafts))
        {
            return;
        }

        _drafts.Opened(_model.FeedbackDrafts);
        _draftToReview = _model.DraftTitle;
        _draftOpenedAt = Environment.TickCount64;
        _lastCommand = SlashCommands.Find("/feedback");
        QueueWrites("/feedback");
    }

    /// <summary>
    /// Claude's list of drafts when the app opened /feedback itself (D33): answered with the draft's number, so that
    /// the review comes straight up, or, when the review is over (Send, Discard or Later brings the list back),
    /// closed with Escape, back to the message field. False when the list is the user's own, or the draft is not on
    /// it: the list is shown.
    /// </summary>
    private bool AnswerDraftsList(Question question)
    {
        if (question.Title != FeedbackDraftWatch.ListTitle)
        {
            return false;
        }

        if (_reviewing)
        {
            _reviewing = false;
            MarkHandled(question.Signature, mark: false);
            Write("\x1b");
            return true;
        }

        if (_draftToReview is not { } title)
        {
            return false;
        }

        _draftToReview = null;
        if (FeedbackDraftWatch.Find(question, title) is not { } draft)
        {
            return false;
        }

        _reviewing = true;
        MarkHandled(question.Signature, mark: false);
        TypeAnswer(draft.Key);
        return true;
    }

    /// <summary>
    /// Escape from the control area: the question closes, or goes back a step. What Claude shows next is the user's
    /// doing, and the area holds its keys until then, so that a second Escape does not reach Claude as an interrupt.
    /// </summary>
    private void SendEscape()
    {
        _answerAt = Environment.TickCount64;
        _area.Hold(UserActionMs);
        Write("\x1b");
    }

    /// <summary>
    /// A text row of a draft's review that Claude has opened for editing (D33, <see cref="ClaudeScreen.Editing"/>):
    /// a field in the control area named with the row. Enter sends what was typed to the end of Claude's text, a
    /// keystroke at a time, then Enter, which keeps it and moves the highlight on; an empty field only moves on;
    /// Escape cancels the edit. Ctrl+Q reads the review with the row's text as it stands. Nothing is deleted from
    /// here: a draft is reworded by discarding it and asking Claude again.
    /// </summary>
    private void ShowEditedRow(ClaudeScreen screen, string text)
    {
        var row = screen.Highlighted?.TrimEnd(':') ?? "Text";
        var name = $"{screen.Title}. {row}, add at the end";
        ShowInArea(screen.Signature, name, text, !UserActed(_screenKeyAt), (focus, hold) => _area.ShowField(name, focus, hold, typed =>
        {
            ScreenKeySent();
            TypeAnswer(typed);
        }, () => SendScreenKey("\x1b")));
    }

    /// <summary>Sends a screen's key. The screen that shows next opens again even when its title and keys are the same (a moved selection, another tab).</summary>
    private void SendScreenKey(string key)
    {
        ScreenKeySent();
        Write(key);
    }

    /// <summary>The bookkeeping of a key sent from a screen, or of text typed into one of its rows: what Claude shows next is the user's doing, and the area holds its keys meanwhile.</summary>
    private void ScreenKeySent()
    {
        _screenKeyAt = Environment.TickCount64;
        _handledWait = null;
        _area.Hold(UserActionMs);
        // A key that changes nothing draws no frame: the timer brings the screen back all the same.
        _attention.Stop();
        _attention.Start();
    }

    /// <summary>
    /// An answer through the send queue: each character as a keystroke of its own, then Enter. The control area holds
    /// its keys meanwhile, so that a second Enter does not send it again.
    /// </summary>
    private void SendAnswer(string key)
    {
        if (_host is null)
        {
            return;
        }

        TypeAnswer(key);
        _answerAt = Environment.TickCount64;
        _area.Hold(UserActionMs);
        if (_settings.SoundOnBell)
        {
            Sounds.Sent();
        }
    }

    /// <summary>
    /// Types an answer: one keystroke per character (per rune, so an emoji stays whole), since Claude's answer field
    /// takes typed keys and several characters in one write are a paste, which it ignores ("12" in a long list, "1,3"
    /// for several answers). Then Enter. Without the sound and the hold of <see cref="SendAnswer"/>: an answer the app
    /// gives on the user's behalf, which what follows announces as new.
    /// </summary>
    private void TypeAnswer(string key)
    {
        var keys = key.EnumerateRunes().Select(rune => rune.ToString()).ToList();
        for (var i = 0; i < keys.Count; i++)
        {
            _writes.Enqueue((keys[i], i == keys.Count - 1 ? 100 : 60));
        }

        _writes.Enqueue(("\r", 50));
        StartWrites();
    }

    /// <summary>
    /// FR-1.7: a close while Claude is in the middle of a turn, or runs work in the background, is confirmed first.
    /// Enter closes, Escape keeps the window.
    /// </summary>
    private void ConfirmClose()
    {
        var background = ClaudeBackground;
        string state;
        if (ClaudeBusy)
        {
            state = _model.PromptPending ? "Claude is waiting for your answer" : "Claude is still working";
            if (_model.QueuedMessages > 0)
            {
                state += _model.QueuedMessages == 1
                    ? " and a message you sent is still waiting for it"
                    : $" and {_model.QueuedMessages} messages you sent are still waiting for it";
            }

            if (background is not null)
            {
                state += $", and it has work running in the background ({background})";
            }
        }
        else
        {
            state = $"Claude has work running in the background ({background})";
        }

        var stops = ClaudeBusy ? "Claude stops in the middle of its work" : "the background work stops";
        var consequence = !RestartPending
            ? $"If you close now, {stops}."
            : $"If you close now, {stops} and the updated AxClaude starts again.";
        ShowNotice(new Notice("Close AxClaude?",
            $"{state}. {consequence}\n" +
            "What is done so far is saved. Start AxClaude again to carry on.",
        [
            new OverlayChoice("&Close anyway", () =>
            {
                _closeConfirmed = true;
                Close();
            }, IsDefault: true),
            new OverlayChoice("&Keep working", KeepWorkingAfterUpdate, IsCancel: true),
        ]));
    }

    /// <summary>FR-1.9: where the app looked, the install command with a button that copies it, the install page, and Locate claude.exe.</summary>
    private void ShowClaudeNotFound(IReadOnlyList<string> candidates)
    {
        ShowNotice(new Notice("Claude Code was not found", HelpText.ClaudeNotFound(candidates),
        [
            new OverlayChoice("Copy install &command", () =>
            {
                if (CopyToClipboard(ClaudeLauncher.InstallCommand))
                {
                    Announce("Copied. Paste it into PowerShell", true);
                }
            }, StaysOpen: true),
            new OverlayChoice("&Open install instructions", () => OpenUrl(ClaudeLauncher.InstallUrl), StaysOpen: true),
            new OverlayChoice("&Locate claude.exe...", LocateClaude, StaysOpen: true),
            OverlayChoice.Close,
        ])
        {
            Unprompted = true,
        });
    }

    /// <summary>The standard file picker; a choice closes the notice, is saved as claudePath and started at once. Cancel keeps the notice.</summary>
    private void LocateClaude()
    {
        using var dialog = new OpenFileDialog
        {
            Title = "Locate claude.exe",
            Filter = "Claude Code (claude.exe;claude.cmd)|claude.exe;claude.cmd|Programs (*.exe;*.cmd)|*.exe;*.cmd|All files (*.*)|*.*",
            CheckFileExists = true,
        };
        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        CloseNotice(null);
        _settings.ClaudePath = dialog.FileName;
        SaveSettings();
        StartClaude();
    }

    /// <summary>Runs the action on the UI thread; false when the window is gone.</summary>
    private bool Post(Action action)
    {
        if (IsDisposed || !IsHandleCreated)
        {
            return false;
        }

        try
        {
            BeginInvoke(action);
            return true;
        }
        catch (InvalidOperationException)
        {
            // The window is closing.
            return false;
        }
    }
}
