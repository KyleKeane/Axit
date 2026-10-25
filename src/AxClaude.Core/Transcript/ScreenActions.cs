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

    // ---- information screens, shown as read-only text (SPEC.md FR-2.11) ----

    /// <summary>
    /// A screen that only informs (/btw's side answer, /goal, /mobile): its hint row names no Enter and it has no
    /// tabs. The window shows it as a read-only notice rather than a list; a screen with Enter (/tasks with tasks,
    /// a review) or with tabs (/status) stays a list.
    /// </summary>
    public static bool IsInformation(ClaudeScreen screen) => !screen.Tabbed && !screen.Keys.Any(key => key.Send == "\r");

    /// <summary>Up or Down: the notice's own arrows move in its text, and the scroll buttons send them (<see cref="Scrolls"/>).</summary>
    public static bool IsScrollKey(ScreenKey key) => key.Send is "\x1b[A" or "\x1b[B";

    /// <summary>The keys that get a button of their own besides Close: every key of the hint row but Up, Down and the leaving key ("Copy (c)", "Fork (f)").</summary>
    public static IReadOnlyList<ScreenKey> NoticeKeys(ClaudeScreen screen)
    {
        var leave = LeaveKey(screen);
        return screen.Keys.Where(key => !IsScrollKey(key) && !ReferenceEquals(key, leave)).ToList();
    }

    /// <summary>The leaving key's button: "&amp;Close" for a key that closes the screen, else its action ("&amp;Go back").</summary>
    public static string LeaveButton(ScreenKey leave) => Closes(leave) ? "&Close" : $"&{char.ToUpperInvariant(leave.Action[0])}{leave.Action[1..]}";

    /// <summary>
    /// Whether the notice offers Scroll down and Scroll up: the hint row names Up and Down, and the screen reaches the
    /// top of the console (its first row is row 0), so that it may hold more than the console shows.
    /// </summary>
    public static bool Scrolls(ClaudeScreen screen) => screen.FirstRow == 0 && screen.Keys.Count(IsScrollKey) == 2;

    /// <summary>The notice's text: the screen's lines, one per line.</summary>
    public static string NoticeText(ClaudeScreen screen) => string.Join("\n", screen.Lines);

    /// <summary>
    /// The lines of an information screen's notice when the screen shows again (FR-2.11, 2026-10-25): Claude's screen
    /// holds only as many rows as the console has, so a long answer pushes its first rows off the top as it arrives or
    /// is scrolled down, and Scroll up brings them back at the cost of the last ones. When the new rows overlap the
    /// lines shown (the new screen's first rows are the last ones shown, or its last rows the first ones shown, two
    /// lines at least with text in them), the two are joined, so that nothing read is lost; otherwise the screen's
    /// rows replace them. The window starts afresh after a key that changes the answer (Browse).
    /// </summary>
    public static IReadOnlyList<string> Merge(IReadOnlyList<string> shown, IReadOnlyList<string> now)
    {
        if (shown.Count == 0 || now.SequenceEqual(shown))
        {
            return now;
        }

        // Scrolled down, or more arrived: the lines shown end with what the screen begins with.
        for (var overlap = Math.Min(shown.Count, now.Count); overlap >= 2; overlap--)
        {
            if (Overlaps(shown, shown.Count - overlap, now, 0, overlap))
            {
                return [.. shown, .. now.Skip(overlap)];
            }
        }

        // Scrolled up: the screen ends with what the lines shown begin with.
        for (var overlap = Math.Min(shown.Count, now.Count); overlap >= 2; overlap--)
        {
            if (Overlaps(now, now.Count - overlap, shown, 0, overlap))
            {
                return [.. now, .. shown.Skip(overlap)];
            }
        }

        return now;
    }

    /// <summary>Whether <paramref name="count"/> lines of <paramref name="a"/> from <paramref name="aStart"/> are those of <paramref name="b"/> from <paramref name="bStart"/>, and at least two of them have text: blank rows alone prove nothing.</summary>
    private static bool Overlaps(IReadOnlyList<string> a, int aStart, IReadOnlyList<string> b, int bStart, int count)
    {
        var withText = 0;
        for (var i = 0; i < count; i++)
        {
            if (a[aStart + i] != b[bStart + i])
            {
                return false;
            }

            if (a[aStart + i].Trim().Length > 0)
            {
                withText++;
            }
        }

        return withText >= 2;
    }

    /// <summary>What the status bar says the screen is after "Claude: showing": "a side answer" for /btw, else its title.</summary>
    public static string StatusName(ClaudeScreen screen) =>
        screen.Title.StartsWith("/btw", StringComparison.Ordinal) ? "a side answer" : screen.Title;
}
