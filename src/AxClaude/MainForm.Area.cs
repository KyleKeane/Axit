using System.Text;
using AxClaude.Core.Transcript;
using Axit.Forms;

namespace AxClaude;

internal sealed partial class MainForm
{
    // ---- the control area: what Claude waits on, in the message field's place (D33) ----
    //
    // The state is one record, _wait: what the window did about what Claude waits on. OnFrameSettled
    // (MainForm.Model.cs), 400 ms after the screen last changed, compares it with what the model says Claude waits
    // on and does one thing: shows the wait in the area, replaces what the area shows, closes the area, or announces
    // "Claude asks". One stamp, _userActedAt, says whether what Claude shows is the user's doing. The timings of the
    // flow are the constants here. (Sixteen fields and three timers until 2026-10-23.)

    /// <summary>What Claude's screen must settle for before what it waits on is acted on (FR-7.2): a question's rows can be blank for a frame while Claude redraws it.</summary>
    private const int SettleMs = 400;

    /// <summary>How long reply speech waits for Claude's prompt row, which comes after a question's rows (D33).</summary>
    private const int SpeechHoldMs = 250;

    /// <summary>D33: the interrupting speech of a question waits this long after the focus moved, so that NVDA's reading of the new control does not cut it off.</summary>
    private const int FocusSettleMs = 300;

    /// <summary>The moment before a question takes the focus from the conversation without the interrupting kind (D33).</summary>
    private const int QuestionFocusMs = 1000;

    /// <summary>
    /// How long after the user's answer or key what Claude shows next counts as the user's doing: no chime, no
    /// "Question closed", and the control area holds its keys that long so that a second Escape does not reach Claude.
    /// </summary>
    private const int UserActionMs = 3000;

    /// <summary>The settle before /feedback is opened for a draft Claude queued (D33).</summary>
    private const int DraftSettleMs = 500;

    /// <summary>
    /// What the window did about what Claude waits on (D33): the signature of the question, screen or text prompt;
    /// whether the control area shows it (false: "Claude asks" was announced, or the app answered it itself); the
    /// text Ctrl+Q speaks while it is shown; and <see cref="Stale"/> once a key of the user's went to a screen, so
    /// that the screen shown next replaces it even when it looks the same (a moved selection, another tab).
    /// </summary>
    private sealed record Wait(string Signature, bool Shown, string? Text)
    {
        public bool Stale { get; init; }
    }

    /// <summary>Where the app is in bringing up a feedback draft Claude queued (D33): about to send /feedback, waiting for Claude's list of drafts to answer it with the draft, or reviewing, until the list Claude shows after the review is closed for the user.</summary>
    private sealed record Draft(DraftStep Step, string? Title, long At);

    private enum DraftStep
    {
        Opening,
        Listing,
        Reviewing,
    }

    private Wait? _wait;
    private Draft? _draft;

    /// <summary>When the user last answered, pressed a screen key or dismissed a question, or the app did so for them: what Claude shows within <see cref="UserActionMs"/> is the user's doing.</summary>
    private long _userActedAt;

    private bool UserActed() => Environment.TickCount64 - _userActedAt < UserActionMs;

    private void UserActedNow() => _userActedAt = Environment.TickCount64;

    /// <summary>Claude waits on something the control area reads, so its rows are not read as reply lines too.</summary>
    private bool QuestionShown => _settings.QuestionNotices && _model.PromptPending;

    /// <summary>What Claude waits on now, as a signature: the question, the screen, or the row for the user's own words; null when it waits on nothing.</summary>
    private string? WaitSignature => _model.PendingQuestion?.Signature ?? _model.PendingScreen?.Signature ?? _model.TextPrompt;

    /// <summary>Whether the control area still shows the wait with this signature.</summary>
    private bool AreaShows(string signature) => _wait is { Shown: true } wait && wait.Signature == signature;

    /// <summary>
    /// Shows what Claude waits on in the control area, in place of the message field (D33). <paramref name="show"/>
    /// puts the control there, given whether it takes the focus and whether it holds its answering keys for a moment
    /// (when the user did not cause it, since they may be typing). Such a control also plays the notice chime, unless
    /// the window is in the background with the louder alert on, which the signal then plays in its place (FR-7.3).
    /// With Go to them at once, interrupting speech (the default) it takes the focus wherever it was, and the question
    /// with the answer the focus is on is spoken as an interrupting notification 300 ms after the focus moved: it cuts
    /// off what NVDA was reading, a reply included, and comes last (raised before the focus, or at once after it, it
    /// was cut off); not when the user answered or dismissed it in those 300 ms. Without the option, the focus moves
    /// at once from the bottom of the window and after a moment from the conversation, and nothing interrupts: NVDA
    /// reads the question when the focus reaches it, after what it was reading (a focus change alone does not cut
    /// NVDA off).
    /// </summary>
    private void ShowInArea(string signature, string name, string text, bool unprompted, Action<bool, bool> show)
    {
        var interrupt = unprompted && !NoticeOpen && _settings.InterruptForQuestions;
        var focus = !NoticeOpen && (interrupt || Input.ContainsFocus || Area.ContainsFocus);
        if (unprompted && _settings.SoundOnBell && !(_settings.BackgroundAlert && !ContainsFocus))
        {
            Sounds.Notice();
        }

        show(focus, unprompted);
        Input.Visible = false;
        Area.Visible = !NoticeOpen;
        _wait = new Wait(signature, Shown: true, text);
        var actedBefore = _userActedAt;
        if (interrupt)
        {
            // After NVDA has taken the focus change, so that the question comes last: raised at once, it was cut off.
            var question = (Area.CurrentItem.Length == 0 ? name : name + Stop(name) + Area.CurrentItem).TrimEnd(' ', '.');
            Later(FocusSettleMs, () =>
            {
                if (AreaShows(signature) && _userActedAt == actedBefore)
                {
                    Announce(question, true);
                }
            });
        }
        else if (!focus && !NoticeOpen && (Transcript.ContainsFocus || !ContainsFocus))
        {
            // From the conversation, or with the window not the active one, without the interrupting kind, or within
            // three seconds of the user's own answer: the focus goes to the question after a moment, so that it is
            // not found by Tab, and NVDA reads it there after what it is reading (asked 2026-10-21; the three-second
            // case was found by the off-screen replay). Not from an open menu, which the move would close (2026-10-23).
            Later(QuestionFocusMs, () =>
            {
                if (AreaShows(signature) && _userActedAt == actedBefore && !NoticeOpen && Area.Current is { } current && !current.ContainsFocus)
                {
                    current.Focus();
                }
            });
        }
    }

    /// <summary>The message field comes back in the control area's place (the console window's); what was shown stays handled, so that it is not shown again while Claude waits on it.</summary>
    protected override void CloseArea()
    {
        base.CloseArea();
        if (_wait is { Shown: true } wait)
        {
            _wait = wait with { Shown = false, Text = null };
        }
    }

    /// <summary>
    /// Ctrl+Q (D33): the question or screen the control area shows, spoken again on demand from anywhere, the title
    /// and all its lines and then the answer the focus is on, as when it opened; the focus stays. NVDA+Tab repeats a
    /// question too, since its list is named with it, but a screen's lines are items and its name only the title.
    /// </summary>
    private void ReadQuestionAgain()
    {
        if (_wait is not { Shown: true, Text: { } text })
        {
            Announce("Claude is not asking anything", true);
            return;
        }

        Announce((text + Stop(text) + Area.CurrentItem).TrimEnd(' ', '.'), true);
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
        ShowInArea(signature, name, text, unprompted, (focus, hold) => Area.ShowList(name, items, start, ticked, focus, hold, (focused, ticks) =>
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
    /// <see cref="ShowOwnAnswer"/> follows. False when the app answered the question itself (a review it submits).
    /// </summary>
    private bool ShowClaudeQuestion(Question question)
    {
        if (question.IsSettings)
        {
            ShowSettings(question);
            return true;
        }

        if (SubmitsReview(question))
        {
            // The last answer's "Sent" said nothing about a review; y goes as the user's own answer would.
            SendAnswer("y");
            return false;
        }

        _askedQuestion = question;
        ShowQuestionList(question.Signature, QuestionName(question), QuestionName(question, int.MaxValue), question, null, !UserActed(), AnswerQuestion, () =>
        {
            SendEscape();
            Announce("Cancelled", false);
        });
        return true;
    }

    /// <summary>
    /// /config's list of settings (docs/axclaude/claude-screens.md), read off Claude's screen each time it shows. Enter on a
    /// true-or-false setting asks for its value (<see cref="ShowSwitch"/>), since Claude flips it when its number is
    /// sent; on any other setting it sends the number and Claude's list of its values follows. Either way the list comes
    /// back on the setting just chosen. Escape saves and closes.
    /// </summary>
    private void ShowSettings(Question settings, bool back = false) =>
        ShowQuestionList(settings.Signature, "Claude's settings", "Claude's settings", settings, _settingKey, !back && !UserActed(), chosen =>
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
        && UserActed();

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
        ShowInArea(prompt, name, name, !UserActed(), (focus, hold) => Area.ShowField(name, focus, hold, typed =>
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

    /// <summary>
    /// A screen of Claude's that waits on its hint row (/status, /tasks, /help …; D33) as a list: its lines, which the
    /// conversation does not show for a tab screen, then an item for every key the hint row names ("View (Enter)",
    /// "Close (Escape)"). Enter on a key's item sends the key, Escape sends Escape; the screen Claude shows next takes
    /// the list's place, without the chime since the user caused it. What the keys do is <see cref="ScreenActions"/>,
    /// tested: the screen's keys work from anywhere in the list, as in Claude, where only its highlight counts; Enter
    /// on one of the screen's lines does what the hint row says Enter does, or leaves a screen whose Escape closes
    /// it, or names the actions; a letter the hint row names (d to discard) goes to Claude from any item.
    /// </summary>
    private void ShowClaudeScreen(ClaudeScreen screen)
    {
        var items = ScreenActions.Items(screen);
        var first = screen.Lines.Count;
        var leave = ScreenActions.LeaveKey(screen);
        void Leave()
        {
            SendScreenKey(leave.Send);
            if (ScreenActions.Closes(leave))
            {
                Announce("Closed", false);
            }
        }

        void EnterOnLine()
        {
            var key = ScreenActions.EnterOnLine(screen);
            if (key is null)
            {
                Announce(ScreenActions.ActionsText(screen), true);
            }
            else if (ReferenceEquals(key, leave))
            {
                Leave();
            }
            else
            {
                SendScreenKey(key.Send);
            }
        }

        bool Letter(char letter)
        {
            if (ScreenActions.LetterKey(screen, letter) is not { } key)
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

        ShowInArea(screen.Signature, screen.Title, text.ToString(), !UserActed(), (focus, hold) =>
            Area.ShowList(screen.Title, items, 0, null, focus, hold, (index, _) =>
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
        ShowInArea(screen.Signature, name, text, !UserActed(), (focus, hold) => Area.ShowField(name, focus, hold, typed =>
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

    /// <summary>The bookkeeping of a key sent from a screen, or of text typed into one of its rows: what Claude shows next is the user's doing, the area holds its keys meanwhile, and the screen shown next replaces this one even when it looks the same.</summary>
    private void ScreenKeySent()
    {
        UserActedNow();
        if (_wait is { } wait)
        {
            _wait = wait with { Stale = true };
        }

        Area.Hold(UserActionMs);
        // A key that changes nothing draws no frame: the timer brings the screen back all the same.
        _attention.Stop();
        _attention.Start();
    }

    /// <summary>
    /// Escape from the control area: the question closes, or goes back a step. What Claude shows next is the user's
    /// doing, and the area holds its keys until then, so that a second Escape does not reach Claude as an interrupt.
    /// </summary>
    private void SendEscape()
    {
        UserActedNow();
        Area.Hold(UserActionMs);
        Write("\x1b");
    }

    /// <summary>
    /// An answer through the send queue: each character as a keystroke of its own, then Enter. The control area holds
    /// its keys meanwhile, so that a second Enter does not send it again.
    /// </summary>
    private void SendAnswer(string key)
    {
        if (!ProcessRunning)
        {
            return;
        }

        TypeAnswer(key);
        UserActedNow();
        Area.Hold(UserActionMs);
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
            Enqueue(keys[i], i == keys.Count - 1 ? 100 : 60);
        }

        Enqueue("\r", 50);
        StartWrites();
    }

    // ---- a feedback draft Claude queued (D33) ----

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
        if (!_readyAnnounced || !ProcessRunning)
        {
            return;
        }

        var idle = ClaudeIdle;
        if (idle && !UserActed() && _draft is { Step: not DraftStep.Opening } draft && Environment.TickCount64 - draft.At >= UserActionMs)
        {
            // The list never came, or the review ended without the list: nothing of the draft's is on its way.
            _draft = null;
        }

        if (!_drafts.Drafted(_model.Working, idle, _model.FeedbackDrafts))
        {
            return;
        }

        if (!_settings.QuestionNotices)
        {
            _drafts.Opened(_model.FeedbackDrafts);
        }
        else if (_draft is null)
        {
            _draft = new Draft(DraftStep.Opening, null, Environment.TickCount64);
            Later(DraftSettleMs, OpenFeedbackDrafts);
        }
    }

    /// <summary>Claude runs, waits on nothing, and no write of the user's is on its way to it.</summary>
    private bool ClaudeIdle => ProcessRunning && !_model.Working && !_model.PromptPending && !WritesPending;

    /// <summary>Sends /feedback as the user's own command would go, without "Sent": the review of the draft announces itself. Skipped when Claude is no longer idle.</summary>
    private void OpenFeedbackDrafts()
    {
        if (!ClaudeIdle || Area.Current is not null || !_drafts.Drafted(_model.Working, idle: true, _model.FeedbackDrafts))
        {
            _draft = null;
            return;
        }

        _drafts.Opened(_model.FeedbackDrafts);
        _draft = new Draft(DraftStep.Listing, _model.DraftTitle, Environment.TickCount64);
        _lastCommand = SlashCommands.Find("/feedback");
        QueueWrites("/feedback");
    }

    /// <summary>
    /// Claude's list of drafts when the app opened /feedback itself (D33): answered with the draft's number, so that
    /// the review comes straight up, or, when the review is over (Send, Discard or Later brings the list back),
    /// closed with Escape, back to the message field; neither flashes the taskbar nor is followed by "Question
    /// closed", being the app's doing. False when the list is the user's own, or the draft is not on it: the list is shown.
    /// </summary>
    private bool AnswerDraftsList(Question question)
    {
        if (question.Title != FeedbackDraftWatch.ListTitle || _draft is null)
        {
            return false;
        }

        if (_draft.Step == DraftStep.Reviewing)
        {
            _draft = null;
            Handled(question.Signature, mark: false);
            UserActedNow();
            Write("\x1b");
            return true;
        }

        if (_draft.Step != DraftStep.Listing)
        {
            return false;
        }

        var title = _draft.Title;
        _draft = null;
        if (title is null || FeedbackDraftWatch.Find(question, title) is not { } draft)
        {
            return false;
        }

        // The review that follows is new to the user: it comes with the trill, so the stamp is not touched here.
        _draft = new Draft(DraftStep.Reviewing, title, Environment.TickCount64);
        Handled(question.Signature, mark: false);
        TypeAnswer(draft.Key);
        return true;
    }
}
