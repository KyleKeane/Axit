using AxClaude.Core.Transcript;

namespace AxClaude.Tests;

/// <summary>
/// The feedback draft Claude Code queues by itself: the count on the mode line that opens /feedback, the card above the
/// prompt whose keys do nothing in screen reader mode, and the /feedback screens that act on the draft
/// (docs/axclaude/claude-screens.md, D33). The rows are the ones Claude Code 2.1.302 drew.
/// </summary>
public class FeedbackCardTests
{
    private const string Fixture = "feedback-card-claude2.1.302-240x50.vt";
    private const string Esc = "\x1b";

    [Fact]
    public void The_mode_line_counts_the_draft_and_the_card_is_kept_out_of_the_conversation()
    {
        // Claude counts the draft on its mode line and prints the card after the turn in which it drafted feedback,
        // with the cursor back on the prompt, and draws it again after the next turn (feedback-card.script: a typed
        // 0 lands in the prompt as text, so the card's own keys are out of reach from the message field).
        var model = new SessionModel(240, 50);
        var idleWithDraft = 0;
        var screens = new List<ClaudeScreen>();
        ReplayFrames(Fixture, model, () =>
        {
            Assert.Null(model.Background);
            if (model.FeedbackDrafts > 0 && !model.Working && !model.PromptPending)
            {
                idleWithDraft++;
                Assert.Equal(1, model.FeedbackDrafts);
                Assert.DoesNotContain(model.Lines, line => !line.Hidden && IsCardRow(line.Text));
            }

            if (model.PendingScreen is { } screen)
            {
                screens.Add(screen);
            }
        });

        Assert.True(idleWithDraft >= 2, $"Claude was idle with the draft counted at the end of {idleWithDraft} frames");
        Assert.Equal("Fixture card test", model.DraftTitle);

        // /exit with a draft waiting: Claude's own question, a screen that waits on its hint row.
        var exit = Assert.Single(screens.DistinctBy(screen => screen.Signature));
        Assert.Equal("You have 1 unsent feedback draft", exit.Title);
        Assert.Equal(["Enter", "Escape"], exit.Keys.Select(k => k.Name));
        Assert.Equal(["review & send", "discard and exit"], exit.Keys.Select(k => k.Action));
    }

    [Fact]
    public void Without_the_control_area_the_card_stays_in_the_conversation_less_its_hint_row()
    {
        var model = new SessionModel(240, 50) { DetectScreens = false };
        var shown = false;
        ReplayFrames(Fixture, model, () =>
        {
            if (model.Lines.Any(line => !line.Hidden && line.Text == "Product feedback drafted: Fixture card test"))
            {
                shown = true;
                Assert.Contains(model.Lines, line => !line.Hidden && line.Text == "End of preview. Review the full draft with /feedback.");
                Assert.DoesNotContain(model.Lines, line => !line.Hidden && line.Text.EndsWith("to dismiss", StringComparison.Ordinal));
            }
        });
        Assert.True(shown);
    }

    [Fact]
    public void The_card_is_chrome_in_a_git_folder_too()
    {
        // In a repository Claude prints the branch under the mode line ("  ● main", 2.1.300), between the card and
        // the prompt; the card's rows up to its title row are chrome all the same, and the count is read.
        var model = new SessionModel(240, 50);
        TestHelpers.Feed(model, $"{Esc}[?25l{Esc}[2J{Esc}[Hclaude: finished\r\nBaked for 3s · done 5:38 AM\r\n"
            + "Product feedback drafted: Fixture card test\r\nDraft preview, 1 lines:\r\n> - **What happened:** a test.\r\n"
            + "End of preview. Review the full draft with /feedback.\r\n1 to review · 2 to send · 0 to dismiss\r\n"
            + $"auto mode on (shift+tab to cycle)  ·  1 feedback draft\r\n  ● main\r\n${Esc}[?25h");
        model.EndFrame();

        Assert.Equal(1, model.FeedbackDrafts);
        Assert.Equal("Fixture card test", model.DraftTitle);
        Assert.Equal("auto mode on", model.Mode);
        Assert.Null(model.Background);
        Assert.False(model.Working);
        Assert.False(model.PromptPending);
        Assert.Equal(["claude: finished", "Baked for 3s · done 5:38 AM"], model.Lines.Where(line => !line.Hidden && line.Text.Length > 0).Select(line => line.Text));
    }

    [Fact]
    public void Only_a_draft_of_the_turn_that_ended_opens()
    {
        // Drafts counted when Claude is ready (five queued by other sessions, or left over) do not open /feedback:
        // they opened it on every start before 2.11.0.
        var watch = new FeedbackDraftWatch();
        Assert.False(watch.Drafted(working: false, idle: true, drafts: 5));
        Assert.False(watch.Drafted(working: false, idle: true, drafts: 5));

        // A turn that queues one: the count rises while Claude works and the draft opens once it is idle, and stays
        // due until /feedback was opened for it.
        Assert.False(watch.Drafted(working: true, idle: false, drafts: 5));
        Assert.False(watch.Drafted(working: true, idle: false, drafts: 6));
        Assert.False(watch.Drafted(working: false, idle: false, drafts: 6), "a write on its way to Claude");
        Assert.True(watch.Drafted(working: false, idle: true, drafts: 6));
        Assert.True(watch.Drafted(working: false, idle: true, drafts: 6));
        watch.Opened(6);
        Assert.False(watch.Drafted(working: false, idle: true, drafts: 6));

        // A turn that drafts nothing, and the count followed down after a draft was sent or discarded.
        Assert.False(watch.Drafted(working: true, idle: false, drafts: 6));
        Assert.False(watch.Drafted(working: false, idle: true, drafts: 6));
        Assert.False(watch.Drafted(working: false, idle: true, drafts: 5));
        Assert.False(watch.Drafted(working: true, idle: false, drafts: 5));
        Assert.True(watch.Drafted(working: false, idle: true, drafts: 6));
    }

    [Fact]
    public void The_branch_and_agent_rows_under_the_mode_line_are_chrome()
    {
        Assert.True(LineClassifier.IsChrome("  ● main"));
        Assert.True(LineClassifier.IsChrome("● feature/menus"));
        Assert.False(LineClassifier.IsChrome("● main and more"));
        Assert.True(LineClassifier.IsChrome("   ◯  general-purpose Checking callers 2m 15s · ↓ 1.2k tokens"));

        // The running command's timer on a row of its own, redrawn every second (2026-10-22): interface, not conversation.
        Assert.True(LineClassifier.IsChrome("(4s · timeout 3m)"));
        Assert.True(LineClassifier.IsChrome("  (1m 12s · timeout 10m)"));
        Assert.False(LineClassifier.IsChrome("(4s · timeout 3m) and then it failed"));
        Assert.False(LineClassifier.IsChrome("(see the timeout section)"));

        // While Claude works with an agent running, the rows above the prompt are the spinner, the mode line, the
        // branch and the agent: all chrome, so the mode line is read and Claude counts as working.
        var model = new SessionModel(240, 50);
        TestHelpers.Feed(model, $"{Esc}[?25l{Esc}[2J{Esc}[Hclaude: on it\r\nBeboppin'…\r\n"
            + "auto mode on (shift+tab to cycle)  ·  esc to interrupt · ↓ to manage\r\n  ● main\r\n"
            + $"   ◯  general-purpose Checking callers 2m 15s · ↓ 1.2k tokens\r\n${Esc}[?25h");
        model.EndFrame();

        Assert.True(model.Working);
        Assert.Equal("auto mode on", model.Mode);
        Assert.Equal(["claude: on it"], model.Lines.Where(line => !line.Hidden && line.Text.Length > 0).Select(line => line.Text));
    }

    [Fact]
    public void Mode_lines_count_their_drafts()
    {
        Assert.Equal(1, LineClassifier.FeedbackDrafts("auto mode on (shift+tab to cycle)  ·  1 feedback draft"));
        Assert.Equal(2, LineClassifier.FeedbackDrafts("manual mode on  ·  2 feedback drafts"));
        Assert.Equal(1, LineClassifier.FeedbackDrafts("auto mode on (shift+tab to cycle)  ·  esc to interrupt · 1 feedback draft"));
        Assert.Equal(0, LineClassifier.FeedbackDrafts("auto mode on (shift+tab to cycle)"));
        Assert.Equal(0, LineClassifier.FeedbackDrafts("claude: 1 feedback draft"));

        const string mixed = "auto mode on  ·  1 shell  ·  1 feedback draft  ·  ↓ to manage";
        Assert.Equal(1, LineClassifier.FeedbackDrafts(mixed));
        Assert.Equal("1 shell", LineClassifier.BackgroundText(mixed));
        Assert.Null(LineClassifier.BackgroundText("auto mode on (shift+tab to cycle)  ·  1 feedback draft"));

        const string hint = "1 to review · 2 to send · 0 to dismiss";
        Assert.True(LineClassifier.IsChrome(hint));
        Assert.True(LineClassifier.IsFeedbackCardHint(hint));
        Assert.Null(ScreenReader.Keys(hint));
        Assert.NotNull(LineClassifier.FeedbackCardTitle("Product feedback drafted: Fixture card test"));
    }

    [Fact]
    public void Feedback_lists_its_drafts_as_answers_and_reviews_one_on_a_screen()
    {
        // The rows of /feedback as Claude Code 2.1.302 drew them in a session with a draft of its own (recorded
        // 2026-10-18, not kept: the drafts of other sessions named their folders), with a neutral folder throughout.
        string[] queue =
        [
            "you: /feedback",
            "Feedback drafts",
            "This session",
            "1. Fixture review test, idea · 1m · C:\\Temp\\axclaude-fixture · transcript available",
            "Other sessions",
            "2. Fixture card test, idea · 1h · C:\\Temp\\axclaude-fixture · transcript available",
            "3. Fixture test draft, idea · 2h · C:\\Temp\\axclaude-fixture · transcript available",
            "4. + Write new feedback",
            "Select with numbers [1-4]. Then Enter to submit:",
            "Drafts live only on this machine and are never sent without you. Unsent drafts expire after 30 days.",
            "Esc to close",
        ];
        var question = QuestionReader.Read(queue, 8)!;
        Assert.Equal("Feedback drafts", question.Title);
        Assert.Equal(["1", "2", "3", "4"], question.Options.Select(option => option.Key));
        // The headings between the drafts are neither answers nor the wrapped tail of the answer above them.
        Assert.Equal("Fixture review test, idea · 1m · C:\\Temp\\axclaude-fixture · transcript available", question.Options[0].Text);
        Assert.Equal(QuestionDialog.AnswerNotice, QuestionRouter.DialogFor(SlashCommands.Find("/feedback")));

        // The draft the app opened /feedback for is found by the card's title, wherever Claude lists it.
        Assert.Equal("1", FeedbackDraftWatch.Find(question, "Fixture review test")!.Key);
        Assert.Equal("2", FeedbackDraftWatch.Find(question, "Fixture card test")!.Key);
        Assert.Null(FeedbackDraftWatch.Find(question, "Fixture"));
        Assert.Null(FeedbackDraftWatch.Find(question with { Title = "Select model" }, "Fixture card test"));

        string[] review =
        [
            "you: /feedback",
            "Feedback drafts",
            "   Type:  idea",
            "   Title:",
            "Fixture review test",
            "   Area:",
            "   Failure mode:  (none)",
            "   Task:  (none)",
            "   Details:",
            "3 lines:",
            "> - **What happened:** a recording test of the feedback review.",
            "> - **What the user said:** none.",
            "> - **Repro:** none.",
            "End of details.",
            "   Send transcript:  yes  · sends this conversation to Anthropic",
            "   Environment info (win32 x64, v2.1.302, claude-fable-5-1, effort xhigh, thinking adaptive, turn 0/14, drafted 1m ago) is always attached.",
            "❯  Send feedback",
            "We may use these reports to debug related issues and improve Claude Code. Turn off Claude-drafted feedback anytime in /config.",
            "↑/↓ to move · Enter to send · d to discard · Esc to later",
        ];
        var screen = ScreenReader.Read(review, 18)!;
        Assert.Equal("Feedback drafts", screen.Title);
        Assert.Equal(["Up", "Down", "Enter", "d", "Escape"], screen.Keys.Select(k => k.Name));
        Assert.Equal(["move", "move", "send", "discard", "later"], screen.Keys.Select(k => k.Action));
        Assert.Contains("❯  Send feedback", screen.Lines);
        Assert.Contains("Fixture review test", screen.Lines);

        // Up moves Claude's highlight to the transcript row, whose keys differ: Left and Right change it, Enter moves on.
        string[] moved =
        [
            "you: /feedback",
            "Feedback drafts",
            "   Type:  idea",
            "   Title:",
            "Fixture review test",
            "   Details:",
            "> - **What happened:** a recording test of the feedback review.",
            "End of details.",
            "❯  Send transcript:  ◀ yes ▶  · sends this conversation to Anthropic",
            "   Environment info (win32 x64, v2.1.302) is always attached.",
            "   Send feedback",
            "We may use these reports to debug related issues and improve Claude Code. Turn off Claude-drafted feedback anytime in /config.",
            "↑/↓ to move · ←/→ to change · Enter to next · d to discard · Esc to later",
        ];
        var transcript = ScreenReader.Read(moved, 12)!;
        Assert.Equal(["Up", "Down", "Left", "Right", "Enter", "d", "Escape"], transcript.Keys.Select(k => k.Name));
        Assert.Equal(["move", "move", "change", "change", "next", "discard", "later"], transcript.Keys.Select(k => k.Action));
        Assert.Contains("❯  Send transcript:  ◀ yes ▶  · sends this conversation to Anthropic", transcript.Lines);
        Assert.NotEqual(screen.Signature, transcript.Signature);

        // Up again puts the highlight on Details, which is at once a field being edited: the cursor sits at the end of
        // its text, above the rest of the review and a hint row with the edit's keys. Task, Failure mode, Area and
        // Title follow upwards; the choice rows take Left and Right, the text rows are edited the same way.
        string[] editing =
        [
            "you: /feedback",
            "Feedback drafts",
            "   Type:  idea",
            "   Title:",
            "Fixture edit test three",
            "   Area:",
            "   Failure mode:  (none)",
            "   Task:  (none)",
            "❯  Details:",
            "- **What happened:** a test of the rows of the /feedback review.",
            "- **What the user said:** none.",
            "- **Repro:** none.",
            "   Send transcript:  yes  · sends this conversation to Anthropic",
            "   Send feedback",
            "↑/↓ to move · Enter to next · ctrl+j to new line · Esc to cancel edit",
        ];
        var edit = ScreenReader.Read(editing, 11)!;
        Assert.Equal("Feedback drafts", edit.Title);
        Assert.Equal(["Up", "Down", "Enter", "Ctrl+J", "Escape"], edit.Keys.Select(k => k.Name));
        Assert.Equal(["move", "move", "next", "new line", "cancel edit"], edit.Keys.Select(k => k.Action));
        Assert.Equal("\n", edit.Keys[3].Send);
        Assert.Contains("❯  Details:", edit.Lines);
        Assert.Contains("- **Repro:** none.", edit.Lines);
        Assert.True(edit.Editing);
        Assert.Equal("Details:", edit.Highlighted);
        Assert.False(screen.Editing);
        Assert.Equal("Send feedback", screen.Highlighted);
        Assert.False(transcript.Editing);
        Assert.StartsWith("Send transcript:", transcript.Highlighted);
        // A review line with the cursor on it and no edit hint under it is no screen.
        Assert.Null(ScreenReader.Read(review, 4));

        // The same frame through the model: the cursor in the field's text, the screen waits all the same.
        var model = new SessionModel(240, 50);
        TestHelpers.Feed(model, $"{Esc}[?25l{Esc}[2J{Esc}[H" + string.Join("\r\n", editing) + $"{Esc}[12;19H{Esc}[?25h");
        model.EndFrame();
        Assert.True(model.PromptPending);
        Assert.Equal("cancel edit", model.PendingScreen!.Keys[^1].Action);

        // Enter on "Send feedback": "Sending feedback…" (a spinner row), then the receipt over "Any key to continue", a
        // screen with Enter for the key; the list comes back after it without the draft.
        var sent = ScreenReader.Read(["you: /feedback", "Feedback drafts", "done: Feedback sent (receipt abc123). Thanks!", "Any key to continue"], 3)!;
        Assert.Equal("Feedback drafts", sent.Title);
        Assert.Equal(["done: Feedback sent (receipt abc123). Thanks!"], sent.Lines);
        Assert.Equal([("Enter", "continue", "\r")], sent.Keys.Select(k => (k.Name, k.Action, k.Send)));
        var receipt = new SessionModel(240, 50);
        TestHelpers.Feed(receipt, $"{Esc}[?25l{Esc}[2J{Esc}[Hyou: /feedback\r\nFeedback drafts\r\ndone: Feedback sent (receipt abc123). Thanks!\r\nAny key to continue{Esc}[?25h");
        receipt.EndFrame();
        Assert.Equal("continue", receipt.PendingScreen!.Keys[0].Action);

        // Before the list, /feedback shows "Loading…" over the hint row for a moment: a spinner row, so no screen waits
        // and nothing is shown on the way to the review; without a draft the same hint row under the title is a screen.
        Assert.Null(ScreenReader.Read(["you: /feedback", "Feedback drafts", "Loading…", "Enter to write new feedback · Esc to close"], 3));
        Assert.Equal("Feedback drafts", ScreenReader.Read(["you: /feedback", "Feedback drafts", "Enter to write new feedback · Esc to close"], 2)!.Title);
    }

    private static bool IsCardRow(string text) =>
        text.StartsWith("Product feedback drafted", StringComparison.Ordinal) || text.StartsWith("Draft preview", StringComparison.Ordinal)
        || text.StartsWith("> - ", StringComparison.Ordinal) || text.StartsWith("End of preview", StringComparison.Ordinal)
        || text.EndsWith("to dismiss", StringComparison.Ordinal);

    /// <summary>Feeds a recording chunk by chunk with the app's quiet rule and calls <paramref name="atFrameEnd"/> after every frame end.</summary>
    private static void ReplayFrames(string fixture, SessionModel model, Action atFrameEnd)
    {
        var bytes = TestHelpers.Fixture(fixture);
        var previousMs = 0;
        foreach (var chunk in TestHelpers.Chunks(fixture))
        {
            if (chunk.Ms - previousMs >= 100)
            {
                model.EndFrame();
                atFrameEnd();
            }

            model.Feed(bytes.AsSpan(chunk.Offset, chunk.Length).ToArray());
            previousMs = chunk.Ms;
        }

        model.EndFrame();
        atFrameEnd();
    }
}
