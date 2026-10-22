using System.Text.RegularExpressions;
using Axit.Core.Transcript;

namespace AxClaude.Core.Transcript;

/// <summary>Recognises Claude Code's screen reader mode labels, chrome rows and heading-like lines.</summary>
public static partial class LineClassifier
{
    // "Forming…" while thinking; "Running…  (3s · timeout 1m)" while a Bash tool runs.
    [GeneratedRegex(@"^(\S+)…(\s+\(.*\))?$")]
    private static partial Regex SpinnerRegex();

    // The same timer on a row of its own under a running Bash tool, "(4s · timeout 3m)", redrawn every second; as a
    // conversation row it moved the caret and NVDA's highlight once a second while a command ran (2026-10-22).
    [GeneratedRegex(@"^\s*\((?:\d+[hms] ?)+ · timeout [\dhms ]+\)$")]
    private static partial Regex ToolTimerRegex();

    // A running subagent, one row each above the spinner, redrawn every second with its time and token count
    // (§4.4 item 21): "◯  general-purpose Checking callers 2m 15s · ↓ 1.2k tokens".
    [GeneratedRegex(@"^\s*◯\s+.*\d+[hms](?: \d+[ms])?(?: · [↓↑] [\d.]+[kM]? tokens)?$")]
    private static partial Regex AgentRowRegex();

    // The git branch Claude prints under its mode line in a repository, "  ● main" (2.1.300), rewritten with the
    // mode line and gone when the next rows are drawn.
    [GeneratedRegex(@"^\s*● \S+$")]
    private static partial Regex BranchRowRegex();

    // Shown under a message that was sent while Claude was busy; the message itself is drawn just above it.
    [GeneratedRegex(@"^ctrl\+x ctrl\+s to send now$")]
    private static partial Regex QueuedHintRegex();

    [GeneratedRegex(@"^\(ctrl\+b to run in background\)$")]
    private static partial Regex BackgroundHintRegex();

    // "auto mode on (shift+tab to cycle)", "auto mode on (shift+tab to cycle)  ·  esc to interrupt", "manual mode on"
    // (the default mode shows no cycle hint), "accept edits on (shift+tab to cycle)". While background work runs the
    // cycle hint gives way to a count and a hint: "auto mode on  ·  1 shell  ·  esc to interrupt · ↓ to manage".
    [GeneratedRegex(@"^((?:\w+ mode|accept edits|bypass permissions|dangerously skip permissions) (?:on|off))(?:\s+\(shift\+tab to cycle\))?((?:\s+·\s+(?:esc to interrupt|↓ to manage|\d+ [a-z][a-z -]*[a-z]))*)$")]
    private static partial Regex ModeLineRegex();

    // Claude Code's own screen reader announcements: a bracketed row under the prompt for a moment, "[accept edits on]".
    [GeneratedRegex(@"^\[[a-z][^\[\]]{0,120}\]$")]
    private static partial Regex AnnouncementRowRegex();

    // Ctrl+O replaces the prompt with this status row; typed messages go nowhere until Ctrl+O is pressed again.
    [GeneratedRegex(@"^Showing detailed transcript · ctrl\+o to toggle")]
    private static partial Regex TranscriptViewRowRegex();

    [GeneratedRegex(@"^Tip: ")]
    private static partial Regex TipRegex();

    [GeneratedRegex(@"^ctrl\+g to edit in Notepad$")]
    private static partial Regex NotepadHintRegex();

    [GeneratedRegex(@"^effort: .* · /effort$")]
    private static partial Regex EffortHintRegex();

    // The slash-command completion list: "/name    description" rows and their indented continuation rows.
    [GeneratedRegex(@"^/\S*\s{2,}\S")]
    private static partial Regex CompletionRowRegex();

    [GeneratedRegex(@"^\s{20,}\S")]
    private static partial Regex CompletionContinuationRegex();

    [GeneratedRegex(@"^\S+ for [\dhms ]+ · done ")]
    private static partial Regex TurnSummaryRegex();

    [GeneratedRegex(@"^(\d+|[yn])\. ")]
    private static partial Regex PromptOptionRegex();

    [GeneratedRegex(@"^([-*•]|\d+[.)])\s")]
    private static partial Regex ListItemRegex();

    // The last row of the card Claude prints above its prompt when it has drafted feedback for Anthropic
    // (docs/axclaude/claude-screens.md): "1 to review · 2 to send · 0 to dismiss". In screen reader mode those keys
    // do nothing (a typed digit lands in the prompt), so the row is interface, never conversation.
    [GeneratedRegex(@"^\d to review · \d to send · \d to dismiss$")]
    private static partial Regex FeedbackCardHintRegex();

    /// <summary>Rows that belong to the interface rather than the conversation.</summary>
    public static bool IsChrome(string text) =>
        ModeLineRegex().IsMatch(text) || SpinnerRegex().IsMatch(text) || TipRegex().IsMatch(text)
        || NotepadHintRegex().IsMatch(text) || EffortHintRegex().IsMatch(text) || BackgroundHintRegex().IsMatch(text)
        || QueuedHintRegex().IsMatch(text) || AnnouncementRowRegex().IsMatch(text) || AgentRowRegex().IsMatch(text)
        || BranchRowRegex().IsMatch(text) || FeedbackCardHintRegex().IsMatch(text) || ToolTimerRegex().IsMatch(text);

    /// <summary>The key-hint row of the feedback card, <c>1 to review · 2 to send · 0 to dismiss</c> (D33).</summary>
    public static bool IsFeedbackCardHint(string text) => FeedbackCardHintRegex().IsMatch(text);

    /// <summary>The first row of the feedback card, which names the draft: <c>Product feedback drafted: &lt;title&gt;</c>.</summary>
    public static bool IsFeedbackCardTitle(string text) => FeedbackCardTitle(text) is not null;

    /// <summary>A heading /feedback puts between its drafts: <c>This session</c>, <c>Other sessions</c> (2.1.302).</summary>
    public static bool IsDraftsHeading(string text) => text is "This session" or "Other sessions";

    /// <summary>The draft's title off the first row of the feedback card (<c>Product feedback drafted: &lt;title&gt;</c>); null for any other row.</summary>
    public static string? FeedbackCardTitle(string text)
    {
        const string marker = " drafted: ";
        var at = text.IndexOf(marker, StringComparison.Ordinal);
        return at < 0 ? null : text[(at + marker.Length)..];
    }

    /// <summary>A bracketed announcement row; the text inside the brackets is what Claude wants spoken.</summary>
    public static string? AnnouncementText(string text) =>
        AnnouncementRowRegex().IsMatch(text) ? text[1..^1] : null;

    /// <summary>The status row of Claude's detailed transcript view, which takes the place of the prompt.</summary>
    public static bool IsTranscriptViewRow(string text) => TranscriptViewRowRegex().IsMatch(text);

    /// <summary>A prompt row with nothing typed: <c>$</c>, <c>&gt;</c> or <c>!</c> alone.</summary>
    public static bool IsBarePrompt(string text) => text.Trim() is "$" or ">" or "!";

    /// <summary>The hint under a queued message; the rows above it up to the <c>you:</c> row are the queued message.</summary>
    public static bool IsQueuedHint(string text) => QueuedHintRegex().IsMatch(text);

    public static bool IsNotepadHint(string text) => NotepadHintRegex().IsMatch(text);

    public static string? SpinnerText(string text) => SpinnerRegex().Match(text) is { Success: true } m ? m.Groups[1].Value + "…" : null;

    /// <summary>The prompt row, or the draft being typed on it.</summary>
    public static bool IsPromptRow(string text) =>
        text.Length == 0 || (text[0] is '$' or '>' or '!' && (text.Length == 1 || char.IsWhiteSpace(text[1])));

    /// <summary>
    /// The echo of a draft on the prompt row. Claude prints the prompt as a dollar sign followed by a no-break space,
    /// which also tells it apart from a command line quoted in a reply ("$ printf ...").
    /// </summary>
    public static bool IsDraftRow(string text) => text.Length > 2 && text[0] == '$' && text[1] == ' ';

    public static bool IsCompletionRow(string text) => CompletionRowRegex().IsMatch(text);

    public static bool IsCompletionContinuation(string text) => CompletionContinuationRegex().IsMatch(text);

    public static string? ModeText(string text)
    {
        var m = ModeLineRegex().Match(text);
        return m.Success ? m.Groups[1].Value : null;
    }

    /// <summary>
    /// The background work a mode line counts ("1 shell", or "1 shell, 2 agents"), "see /tasks" when it only says
    /// "↓ to manage" (a background agent), or null when it shows none or is not a mode line. A feedback draft the
    /// mode line counts ("1 feedback draft") is no work running: it waits in /feedback and is left out.
    /// </summary>
    public static string? BackgroundText(string text)
    {
        var m = ModeLineRegex().Match(text);
        // Runs at every frame end: no allocation unless the line counts something.
        if (!m.Success)
        {
            return null;
        }

        if (!m.Groups[2].ValueSpan.ContainsAnyInRange('0', '9'))
        {
            return m.Groups[2].ValueSpan.Contains("↓ to manage", StringComparison.Ordinal) ? "see /tasks" : null;
        }

        var counts = m.Groups[2].Value.Split('·', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .Where(part => char.IsAsciiDigit(part[0]) && !part.Contains("feedback draft", StringComparison.Ordinal))
            .ToList();
        return counts.Count > 0 ? string.Join(", ", counts) : null;
    }

    /// <summary>
    /// The feedback drafts a mode line counts ("1 feedback draft", "2 feedback drafts"): queued by Claude Code's
    /// SendFeedback tool, waiting in /feedback (D33). Zero when it counts none or is not a mode line.
    /// </summary>
    public static int FeedbackDrafts(string text)
    {
        var m = ModeLineRegex().Match(text);
        if (!m.Success)
        {
            return 0;
        }

        var at = m.Groups[2].ValueSpan.IndexOf(" feedback draft", StringComparison.Ordinal);
        if (at < 0)
        {
            return 0;
        }

        var start = at;
        while (start > 0 && char.IsAsciiDigit(m.Groups[2].ValueSpan[start - 1]))
        {
            start--;
        }

        return int.TryParse(m.Groups[2].ValueSpan[start..at], out var count) ? count : 0;
    }

    /// <summary>A row Claude prints when it waits for an answer: a permission or trust question, a numbered menu, a yes-or-no question, the /config list (§4.4).</summary>
    public static bool IsPromptText(string text) =>
        text.StartsWith("Permission Required:", StringComparison.Ordinal)
        || text.StartsWith("Enter selection", StringComparison.Ordinal)
        || text.StartsWith("Enter y/n", StringComparison.Ordinal)
        || text.StartsWith("Select with numbers", StringComparison.Ordinal)
        || text.StartsWith("Enter a number to change", StringComparison.Ordinal)
        || IsTextPrompt(text);

    /// <summary>The row where Claude waits for the user's own words: "Enter text for option 4 (Other), or Escape for the list:".</summary>
    public static bool IsTextPrompt(string text) => text.StartsWith("Enter text for", StringComparison.Ordinal);

    /// <summary>The answer a text row asks the words for: "Other" in "Enter text for option 4 (Other), or Escape for the list:"; null when it names none.</summary>
    public static string? TextPromptAnswer(string text) =>
        TextPromptRegex().Match(text) is { Success: true } m ? m.Groups[1].Value : null;

    [GeneratedRegex(@"^Enter text for option \d+ \((.+?)\)[,:]")]
    private static partial Regex TextPromptRegex();

    public static LineKind Classify(string text, bool inPromptBlock)
    {
        if (text.StartsWith("you:", StringComparison.Ordinal))
        {
            return LineKind.UserEcho;
        }

        if (text.StartsWith("claude:", StringComparison.Ordinal))
        {
            return LineKind.ClaudeReply;
        }

        if (text.StartsWith("thinking:", StringComparison.Ordinal))
        {
            return LineKind.Thinking;
        }

        if (text.StartsWith("tool error:", StringComparison.Ordinal))
        {
            return LineKind.ToolError;
        }

        if (text.StartsWith("tool:", StringComparison.Ordinal) || text.EndsWith("(ctrl+o to expand)", StringComparison.Ordinal))
        {
            return LineKind.Tool;
        }

        if (text.StartsWith("error:", StringComparison.Ordinal))
        {
            return LineKind.Error;
        }

        if (text.StartsWith("warning:", StringComparison.Ordinal))
        {
            return LineKind.Warning;
        }

        if (IsPromptText(text))
        {
            return LineKind.Prompt;
        }

        if (inPromptBlock && PromptOptionRegex().IsMatch(text))
        {
            return LineKind.PromptOption;
        }

        if (TurnSummaryRegex().IsMatch(text))
        {
            return LineKind.TurnSummary;
        }

        if (text.StartsWith("[Screen Reader Mode:", StringComparison.Ordinal))
        {
            return LineKind.System;
        }

        return LineKind.Plain;
    }

    /// <summary>
    /// Heading level of a line, or 0. Screen reader mode prints markdown headings as plain rows, so a short row
    /// inside a reply, after a blank row or on the <c>claude:</c> label row, without terminal punctuation and
    /// followed by a blank row counts as level 2. Rows outside a reply never qualify: a short tool-output row
    /// followed by a blank row is not a heading, and neither is the last line of a code block. Raw markdown
    /// hashes count anywhere.
    /// </summary>
    public static int HeadingLevel(string text, LineKind kind, string? previousText, string? nextText, bool inReply)
    {
        if (kind is not (LineKind.Plain or LineKind.ClaudeReply))
        {
            return 0;
        }

        // Spans throughout: this runs for every row on screen at every frame end.
        var t = kind == LineKind.ClaudeReply ? text.AsSpan("claude:".Length).Trim() : text.AsSpan();

        // Raw markdown ("## Title", as inside a code block Claude quotes): the number of hashes is the level.
        var hashes = 0;
        while (hashes < t.Length && t[hashes] == '#')
        {
            hashes++;
        }

        if (hashes is >= 1 and <= 6 && hashes < t.Length && char.IsWhiteSpace(t[hashes]) && !t[hashes..].IsWhiteSpace())
        {
            return hashes;
        }

        if (kind == LineKind.Plain && (!inReply || previousText is null || previousText.Length > 0))
        {
            return 0;
        }

        if (nextText is null || nextText.Length > 0 || t.Length is 0 or > 80)
        {
            return 0;
        }

        if (".,;:?!".Contains(t[^1]) || ListItemRegex().IsMatch(t) || !ContainsLetter(t))
        {
            return 0;
        }

        return 2;
    }

    private static bool ContainsLetter(ReadOnlySpan<char> text)
    {
        foreach (var c in text)
        {
            if (char.IsLetter(c))
            {
                return true;
            }
        }

        return false;
    }
}
