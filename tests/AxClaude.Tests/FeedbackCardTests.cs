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
        Assert.Equal("auto mode on", model.Mode);
        Assert.Null(model.Background);
        Assert.False(model.Working);
        Assert.False(model.PromptPending);
        Assert.Equal(["claude: finished", "Baked for 3s · done 5:38 AM"], model.Lines.Where(line => !line.Hidden && line.Text.Length > 0).Select(line => line.Text));
    }

    [Fact]
    public void The_branch_and_agent_rows_under_the_mode_line_are_chrome()
    {
        Assert.True(LineClassifier.IsChrome("  ● main"));
        Assert.True(LineClassifier.IsChrome("● feature/menus"));
        Assert.False(LineClassifier.IsChrome("● main and more"));
        Assert.True(LineClassifier.IsChrome("   ◯  general-purpose Checking callers 2m 15s · ↓ 1.2k tokens"));

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
        Assert.True(LineClassifier.IsFeedbackCardTitle("Product feedback drafted: Fixture card test"));
    }

    [Fact]
    public void Feedback_lists_its_drafts_as_answers_and_reviews_one_on_a_screen()
    {
        // The rows of /feedback, with a neutral folder in place of the account path.
        string[] queue =
        [
            "you: /feedback",
            "Feedback drafts",
            "Other sessions",
            "1. Fixture card test, idea · 1m · C:\\Temp\\axclaude-fixture · transcript available",
            "2. Fixture test draft, idea · 3m · C:\\Temp\\axclaude-fixture · transcript available",
            "3. + Write new feedback",
            "Select with numbers [1-3]. Then Enter to submit:",
            "Drafts live only on this machine until you send them.",
            "Esc to close",
        ];
        var question = QuestionReader.Read(queue, 6)!;
        Assert.Equal("Feedback drafts", question.Title);
        Assert.Equal(["1", "2", "3"], question.Options.Select(option => option.Key));
        Assert.Equal(QuestionDialog.AnswerNotice, QuestionRouter.DialogFor(SlashCommands.Find("/feedback")));

        string[] review =
        [
            "Feedback drafts",
            "Type:  idea",
            "Title:",
            "Fixture card test",
            "Details:",
            "> - **What happened:** a recording test of the feedback card.",
            "End of details.",
            "Send transcript:  yes  · sends this conversation to Anthropic",
            "❯  Send feedback",
            "↑/↓ to move · Enter to send · d to discard · Esc to later",
        ];
        var screen = ScreenReader.Read(review, 9)!;
        Assert.Equal("Feedback drafts", screen.Title);
        Assert.Equal(["Up", "Down", "Enter", "d", "Escape"], screen.Keys.Select(k => k.Name));
        Assert.Equal(["move", "move", "send", "discard", "later"], screen.Keys.Select(k => k.Action));
        Assert.Contains("❯  Send feedback", screen.Lines);
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
