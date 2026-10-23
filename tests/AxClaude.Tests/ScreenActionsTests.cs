using AxClaude.Core.Transcript;

namespace AxClaude.Tests;

/// <summary>What the keys of a screen do in the control area (docs/axclaude/SPEC.md D33): the items, the leaving key, Enter on a line, the letters.</summary>
public sealed class ScreenActionsTests
{
    private static readonly ScreenKey Up = new("\x1b[A", "Up", "select");
    private static readonly ScreenKey Enter = new("\r", "Enter", "view");
    private static readonly ScreenKey Escape = new("\x1b", "Escape", "close");
    private static readonly ScreenKey Discard = new("d", "d", "discard");
    private static readonly ScreenKey Later = new("\x1b", "Escape", "later");
    private static readonly ScreenKey Continue = new("\r", "Enter", "continue");

    [Fact]
    public void The_items_are_the_lines_then_the_keys()
    {
        var screen = new ClaudeScreen("Background", ["No tasks currently running"], [Up, Enter, Escape]);
        Assert.Equal(["No tasks currently running", "Select (Up)", "View (Enter)", "Close (Escape)"], ScreenActions.Items(screen));
        Assert.Equal("Actions at the end of the list: Select (Up), View (Enter), Close (Escape)", ScreenActions.ActionsText(screen));
    }

    [Fact]
    public void Escape_leaves_and_closes_when_its_action_says_so()
    {
        var tasks = new ClaudeScreen("Background", [], [Up, Enter, Escape]);
        Assert.Same(Escape, ScreenActions.LeaveKey(tasks));
        Assert.True(ScreenActions.Closes(Escape));
        Assert.True(ScreenActions.Leaves(Escape));

        // A review's Escape means Later: it leaves nothing behind closed, so "Closed" is not said.
        var review = new ClaudeScreen("Feedback drafts", ["Type: idea"], [Up, new("\r", "Enter", "send"), Discard, Later]);
        Assert.Same(Later, ScreenActions.LeaveKey(review));
        Assert.False(ScreenActions.Closes(Later));
        Assert.False(ScreenActions.Leaves(Later));

        // The receipt waits on Enter alone: it is the leaving key, and it continues.
        var receipt = new ClaudeScreen("Feedback drafts", ["done: Feedback sent"], [Continue]);
        Assert.Same(Continue, ScreenActions.LeaveKey(receipt));
        Assert.True(ScreenActions.Leaves(Continue));
        Assert.False(ScreenActions.Closes(Continue));
    }

    [Fact]
    public void Enter_on_a_line_does_what_the_hint_names_or_leaves_or_names_the_actions()
    {
        // The hint names Enter: that key (view in /tasks, send on a review).
        Assert.Same(Enter, ScreenActions.EnterOnLine(new ClaudeScreen("Background", [], [Up, Enter, Escape])));
        // No Enter named, Escape closes: Enter leaves.
        Assert.Same(Escape, ScreenActions.EnterOnLine(new ClaudeScreen("Goal", ["No goal set"], [Escape])));
        // No Enter named and Escape only puts the screen off for later: the actions are named instead.
        Assert.Null(ScreenActions.EnterOnLine(new ClaudeScreen("Feedback drafts", [], [Up, Discard, Later])));
    }

    [Fact]
    public void A_letter_the_hint_names_acts_and_another_is_the_lists_own()
    {
        var review = new ClaudeScreen("Feedback drafts", [], [Up, Discard, Later]);
        Assert.Same(Discard, ScreenActions.LetterKey(review, 'd'));
        Assert.Null(ScreenActions.LetterKey(review, 's'));
    }
}
