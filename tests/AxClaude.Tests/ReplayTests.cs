using AxClaude.Core.Transcript;
using static AxClaude.Tests.TestHelpers;

namespace AxClaude.Tests;

/// <summary>
/// Replays recordings chunk by chunk with the app's own send timing: the exchange block is printed before the text
/// is written, the draft echo arrives without a frame bracket, and a quiet period of 100 ms ends a frame.
/// </summary>
public class ReplayTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Wrapped_echo_of_a_long_message_is_hidden_under_its_block(bool quietFrameDuringDraft)
    {
        var texts = ReplayedLines(LongMessage, LongMessages, quietFrameDuringDraft).Select(l => l.Text).ToList();
        AssertExchange(texts, 1, LongMessages[0], "claude: OK DONE");
        AssertExchange(texts, 2, LongMessages[1], "claude: OK DONE");
        Assert.DoesNotContain(texts, t => t.StartsWith("you:") || t.StartsWith("$"));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Message_sent_while_claude_works_is_marked_where_claude_takes_it_up(bool quietFrameDuringDraft)
    {
        var lines = ReplayedLines(Steering, SteeringMessages, quietFrameDuringDraft);
        var texts = lines.Select(l => l.Text).ToList();

        AssertExchange(texts, 1, SteeringMessages[0], "claude: Running the sleep command now.");
        AssertExchange(texts, 2, SteeringMessages[1], "claude: OK DONE");
        Assert.Equal("HELLO", texts[texts.IndexOf("# Output 2 Reply from Claude") + 3]);
        AssertExchange(texts, 3, SteeringMessages[2], "claude: The message has 2 lines. The last line reads:");

        // The queued copy of the second message and its hint never show; the tool result rows stay.
        Assert.DoesNotContain(texts, t => t.StartsWith("you:") || t.StartsWith("$") || t.Contains("ctrl+x ctrl+s") || t.Contains("ctrl+b to run"));
        Assert.Contains("(No output)", texts);
        Assert.Contains("tool: Bash (sleep 20)", texts);
        Assert.True(texts.IndexOf("tool: Bash (sleep 20)") < texts.IndexOf("# Input 2"), string.Join("\n", texts));
        Assert.Equal(3, lines.Count(l => l.Kind == LineKind.OutputMarker));
        Assert.Equal(3, lines.Count(l => l.Kind == LineKind.InputMarker));
    }

    /// <summary>The exchange block of message n, exactly as FR-4.1 prints it, directly followed by the first reply line.</summary>
    private static void AssertExchange(List<string> texts, int n, string sent, string firstReplyLine)
    {
        var input = texts.IndexOf($"# Input {n}");
        Assert.True(input >= 0, $"Missing input marker {n}:\n" + string.Join("\n", texts));
        var expected = new List<string> { $"# Input {n}" };
        expected.AddRange(sent.Split('\n'));
        expected.AddRange(["", $"# Output {n} Reply from Claude", "", firstReplyLine]);
        Assert.Equal(expected, texts.Skip(input).Take(expected.Count).ToList());
    }

    /// <summary>The visible lines after a replay; the steering recording alone queues a message, and nothing stays queued.</summary>
    private static List<Line> ReplayedLines(string fixture, string[] sent, bool quietFrameDuringDraft)
    {
        var model = new SessionModel(240, 50);
        var maxQueued = 0;
        model.Changed += () => maxQueued = Math.Max(maxQueued, model.QueuedMessages);
        Replay(model, fixture, sent, quietFrameDuringDraft);
        Assert.Equal(fixture == Steering ? 1 : 0, maxQueued);
        Assert.Equal(0, model.QueuedMessages);
        return model.Lines.Where(l => !l.Hidden).ToList();
    }
}
