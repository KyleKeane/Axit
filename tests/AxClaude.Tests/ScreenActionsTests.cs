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

    // ---- information screens as read-only text (SPEC.md FR-2.11) ----

    private static ClaudeScreen Read(params string[] rows) => ScreenReader.Read(rows, rows.Length - 1)!;

    [Fact]
    public void A_side_answer_is_an_information_screen_with_copy_and_fork_buttons()
    {
        // /btw's screen, as Claude Code 2.1.310 drew it on 2026-10-24.
        var btw = Read("you: /btw what is two plus two? answer in one word", "", "/btw what is two plus two? answer in one word", "Four",
            "↑/↓ to scroll · c to copy · f to fork · Esc to close");
        Assert.Equal("/btw what is two plus two? answer in one word", btw.Title);
        Assert.True(ScreenActions.IsInformation(btw));
        Assert.Equal("Four", ScreenActions.NoticeText(btw));
        Assert.Equal(["Copy (c)", "Fork (f)"], ScreenActions.NoticeKeys(btw).Select(ScreenActions.KeyItem));
        Assert.Equal("&Close", ScreenActions.LeaveButton(ScreenActions.LeaveKey(btw)));
        Assert.Equal("a side answer", ScreenActions.StatusName(btw));
        // It fits the console: no scroll buttons.
        Assert.False(ScreenActions.Scrolls(btw));
    }

    [Fact]
    public void A_second_side_answer_browses_with_shift_and_the_arrows()
    {
        // The hint row of a second /btw in the same session (2.1.310): Shift with both arrows, and a history to clear.
        var keys = ScreenReader.Keys("⇧←/→ to browse · c to copy · f to fork · x to clear history · Esc to close")!;
        Assert.Equal(["Shift+Left", "Shift+Right", "c", "f", "x", "Escape"], keys.Select(key => key.Name));
        Assert.Equal("\x1b[1;2D", keys[0].Send);
        var btw = new ClaudeScreen("/btw what is three plus three? answer in one word", ["Six"], keys);
        Assert.True(ScreenActions.IsInformation(btw));
        Assert.Equal(["Browse (Shift+Left)", "Browse (Shift+Right)", "Copy (c)", "Fork (f)", "Clear history (x)"],
            ScreenActions.NoticeKeys(btw).Select(ScreenActions.KeyItem));
    }

    [Fact]
    public void A_screen_that_reaches_the_top_of_the_console_scrolls()
    {
        var rows = new[] { "/btw count to eighty" }.Concat(Enumerable.Range(1, 40).Select(n => n.ToString())).Append("↑/↓ to scroll · c to copy · f to fork · Esc to close").ToArray();
        var screen = Read(rows);
        Assert.Equal(0, screen.FirstRow);
        Assert.True(ScreenActions.Scrolls(screen));
        Assert.Equal("1\n2", ScreenActions.NoticeText(screen)[..3]);
    }

    [Fact]
    public void Goal_is_an_information_screen_with_close_alone()
    {
        var goal = Read("you: /goal", "Goal", "No goal set", "/goal <condition> to set one", "Esc to dismiss");
        Assert.True(ScreenActions.IsInformation(goal));
        Assert.Empty(ScreenActions.NoticeKeys(goal));
        Assert.Equal("&Close", ScreenActions.LeaveButton(ScreenActions.LeaveKey(goal)));
        Assert.Equal("Goal", ScreenActions.StatusName(goal));
        Assert.Equal("No goal set\n/goal <condition> to set one", ScreenActions.NoticeText(goal));
    }

    [Fact]
    public void Screens_with_enter_or_tabs_stay_lists()
    {
        Assert.False(ScreenActions.IsInformation(Read("you: /tasks", "Background", "1 shell running", "↑/↓ to select · Enter to view · Esc to close")));
        Assert.False(ScreenActions.IsInformation(Read("you: /tasks", "Shell details", "Status: running", "← to go back · Esc/Enter/Space to close · x to stop")));
        var status = ScreenReader.Read(["you: /status", "Settings  Status   Config   Usage   Stats", "Version: 2.1.281", "Esc to cancel"], 1)!;
        Assert.True(status.Tabbed);
        Assert.False(ScreenActions.IsInformation(status));
        Assert.False(ScreenActions.IsInformation(Read("Feedback drafts", "done: Feedback sent", "Any key to continue")));
    }

    [Fact]
    public void A_leaving_key_that_goes_back_names_its_button()
    {
        var hook = Read("you: /hooks", "PreToolUse", "No hooks configured", "Esc to go back");
        Assert.True(ScreenActions.IsInformation(hook));
        Assert.Equal("&Go back", ScreenActions.LeaveButton(ScreenActions.LeaveKey(hook)));
    }
}
