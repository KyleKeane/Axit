using AxClaude.Core.Transcript;
using static AxClaude.Tests.TestHelpers;

namespace AxClaude.Tests;

/// <summary>
/// The trust question on a fresh folder (Claude Code 2.1.309): after the answer Claude erases the question's rows and
/// draws its banner from the second row without clearing the screen, and the first message follows. Found by
/// tools/AxAutomate on 2026-10-23: the banner showed twice, the second copy inside the first reply.
/// </summary>
public class TrustBannerTests
{
    private const string Fixture = "trust-banner-claude2.1.309-240x50.vt";

    /// <summary>The message sent the moment the mode line shows: Claude strips an invisible character from it, asks for Enter again, and draws its screen again under the prompt, banner and all.</summary>
    private const string Fast = "trust-banner-fast-claude2.1.309-240x50.vt";

    private const string Message = "Reply with the single word hello.";

    /// <summary>A two-line message sent the moment the mode line shows (2.1.309): Claude's warning row stays under the mode line for the rest of the session.</summary>
    private const string EarlyTwoLines = "early-two-lines-claude2.1.309-240x50.vt";

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void An_early_two_line_message_leaves_no_draft_warning_or_mode_line_in_the_conversation(bool quietFrameDuringDraft)
    {
        var (columns, rows) = FixtureSize(EarlyTwoLines);
        var model = new SessionModel(columns, rows);
        Replay(model, EarlyTwoLines, ["What is 2 plus 2?\nAnswer in one line."], quietFrameDuringDraft);
        var texts = Visible(model);

        Assert.Single(texts, text => text == "Claude Code v2.1.309");
        Assert.True(texts.Count(text => text == "Answer in one line.") == 1, string.Join(" | ", texts));
        var output = texts.IndexOf("# Output 1 Reply from Claude");
        Assert.Equal("claude: 4", texts[output + 2]);
        Assert.DoesNotContain(texts, text => text.StartsWith('$') || text.StartsWith("you:", StringComparison.Ordinal) || text.Contains("invisible character", StringComparison.Ordinal) || text.StartsWith("auto mode on", StringComparison.Ordinal) || text.Contains("ctrl+g", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData(Fixture, false)]
    [InlineData(Fixture, true)]
    [InlineData(Fast, false)]
    [InlineData(Fast, true)]
    public void The_banner_after_the_trust_question_shows_once_above_the_first_exchange(string fixture, bool quietFrameDuringDraft)
    {
        var (columns, rows) = FixtureSize(fixture);
        var model = new SessionModel(columns, rows);
        // The window records the question it shows as a "Question:" line (MarkQuestion), as it does at each frame end.
        model.Changed += () =>
        {
            if (model.PendingQuestion is not null)
            {
                model.MarkQuestion();
            }
        };
        Replay(model, fixture, [Message], quietFrameDuringDraft);
        var texts = Visible(model);

        Assert.Single(texts, text => text == "Claude Code v2.1.309");
        var banner = texts.IndexOf("Claude Code v2.1.309");
        var input = texts.IndexOf("# Input 1");
        var output = texts.IndexOf("# Output 1 Reply from Claude");
        Assert.True(banner < input, string.Join(" | ", texts));
        Assert.Equal("claude: hello", texts[output + 2]);
        Assert.DoesNotContain(texts, text => text.StartsWith('$') || text.StartsWith("you:", StringComparison.Ordinal) || text.Contains("invisible character", StringComparison.Ordinal) || text.Contains("ctrl+g", StringComparison.Ordinal) || text.StartsWith("auto mode on", StringComparison.Ordinal) || text.StartsWith("effort:", StringComparison.Ordinal));
    }
}
