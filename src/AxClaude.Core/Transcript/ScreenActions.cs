namespace AxClaude.Core.Transcript;

/// <summary>
/// What the keys of a screen Claude waits on do in the control area (SPEC.md D33): the list's items (the screen's
/// lines, then one item per key of its hint row, "View (Enter)"), the key that leaves the screen, what Enter on one
/// of the lines sends, and the letters that act from anywhere in the list. Pure, so that the decisions are tested
/// without a window; the window (<c>MainForm.ShowClaudeScreen</c>) sends what they say.
/// </summary>
public static class ScreenActions
{
    /// <summary>The items of the screen's list: its lines, then an item per key, "View (Enter)", "Close (Escape)".</summary>
    public static IReadOnlyList<string> Items(ClaudeScreen screen) => [.. screen.Lines, .. screen.Keys.Select(KeyItem)];

    /// <summary>A key's item: its action with a capital, then its name in parentheses.</summary>
    public static string KeyItem(ScreenKey key) => $"{char.ToUpperInvariant(key.Action[0])}{key.Action[1..]} ({key.Name})";

    /// <summary>The key that leaves the screen: Escape, or the one key of "Any key to continue".</summary>
    public static ScreenKey LeaveKey(ClaudeScreen screen) => screen.Keys.FirstOrDefault(key => key.Send == "\x1b") ?? screen.Keys[0];

    /// <summary>Whether the leaving key closes the screen, which "Closed" then says: close, dismiss, cancel or exit.</summary>
    public static bool Closes(ScreenKey leave) => leave.Action.ToLowerInvariant() is "close" or "dismiss" or "cancel" or "exit";

    /// <summary>Whether the leaving key puts the screen off: a close, or Continue.</summary>
    public static bool Leaves(ScreenKey leave) => Closes(leave) || leave.Action == "continue";

    /// <summary>
    /// What Enter on one of the screen's lines sends: the key the hint row names for Enter (send on a review, next on
    /// one of its switches, view in /tasks); the leaving key when Enter is not named and leaving puts the screen off,
    /// since Enter is what a user presses to be done with an information screen; null when neither, and the actions
    /// at the end of the list are named instead (<see cref="ActionsText"/>), since an Enter that leaves would put the
    /// screen off. Until 2.11.0 Enter on a line meant Escape, here Later, so Enter on the line "Send feedback", taken
    /// for the button, put the draft off.
    /// </summary>
    public static ScreenKey? EnterOnLine(ClaudeScreen screen)
    {
        if (screen.Keys.FirstOrDefault(key => key.Send == "\r") is { } enter)
        {
            return enter;
        }

        var leave = LeaveKey(screen);
        return Leaves(leave) ? leave : null;
    }

    /// <summary>The key a plain letter stands for on the screen (d to discard, x to stop), which acts from anywhere in the list; null for a letter the hint row does not name, which is the list's own jump to the item starting with it.</summary>
    public static ScreenKey? LetterKey(ClaudeScreen screen, char letter) =>
        screen.Keys.FirstOrDefault(key => key.Send.Length == 1 && key.Send[0] == letter);

    /// <summary>What Enter on a line says when it sends nothing: "Actions at the end of the list: View (Enter), Close (Escape)".</summary>
    public static string ActionsText(ClaudeScreen screen) => "Actions at the end of the list: " + string.Join(", ", screen.Keys.Select(KeyItem));
}
