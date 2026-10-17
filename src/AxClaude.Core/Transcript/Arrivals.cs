using System.Text;

namespace AxClaude.Core.Transcript;

/// <summary>How much of Claude's replies is spoken as it arrives (FR-7.4): nothing, the first line of each message, or every line.</summary>
public enum ReplySpeechMode
{
    None,

    /// <summary>The <c>claude:</c> row of each message, where Claude says what it is doing or what it found, and an <c>error:</c> row.</summary>
    FirstLines,

    /// <summary>The <c>claude:</c> or <c>error:</c> row and its unlabelled continuation rows; tool output and thinking never.</summary>
    All,
}

/// <summary>What a frame brought from Claude: a new visible line (the tick, FR-7.8), a tool call row that is new or rewritten (the tool tick, FR-7.8), and the reply and tool call text that became final (speech, FR-7.4).</summary>
public readonly record struct Arrival(bool NewLine, bool ToolCall, string? Speech);

/// <summary>
/// One pass over the transcript at the end of every frame, shared by the tick and by speaking replies. The tick
/// wants to know whether a visible line from Claude arrived: any line that is not one of the app's own. Speech
/// wants the reply lines after the newest output marker that became final in this frame, as one utterance: a line
/// is final when it is committed, followed by another visible line, or Claude is idle. An <c>error:</c> row is a
/// reply line too: when the login has lapsed it is Claude's whole answer, and a turn that said only Sent,
/// Responding and Done left the user without a word of why. Claude prints a whole message at once in screen
/// reader mode, so a frame often finishes many lines; one notification per line was a
/// burst that NVDA cut short after the first few, and one utterance per frame also reads as prose (D32). A line
/// counts as spoken by identity and text, because Claude redraws rows in place: the row that held the prompt a
/// moment ago holds the first reply line next. The <c>tool:</c> row of each tool call joins the utterance when
/// asked for, whatever the reply mode; it is printed whole and the tool then runs under it, so it is final at once.
/// The same rows, new or rewritten in place for the next call, are reported for the tool tick whatever is spoken.
/// </summary>
public sealed class Arrivals
{
    private static readonly char[] PauseEndings = ['.', ',', ';', ':', '!', '?', '…'];

    /// <summary>The hint Claude appends to a collapsed tool row (<c>tool: Reading 1 file… (ctrl+o to expand)</c>); shown, not spoken.</summary>
    private const string ExpandHint = "(ctrl+o to expand)";

    private readonly Dictionary<int, string> _spoken = [];
    private readonly Dictionary<int, string> _toolRows = [];
    private readonly HashSet<int> _present = [];
    private Line? _first;
    private readonly List<Line> _candidates = [];
    private readonly StringBuilder _text = new();
    private int _contentLines;

    /// <summary>
    /// The texts spoken since the newest output marker. In a long turn Claude can print lines it has already printed
    /// again, as new rows (the spoken reply was read out a second time at the end of the turn, over "Claude is done"):
    /// a line whose text was spoken in the same exchange is not spoken again. The next exchange starts empty.
    /// </summary>
    private readonly HashSet<string> _spokenInExchange = [];

    private int _exchangeMarker = -1;

    /// <summary>The row that names a tool call, <c>tool: Read (a.txt)</c>; the rows of its output are Tool lines too and are never spoken.</summary>
    public static bool IsToolCall(Line line) => line.Kind == LineKind.Tool && line.Text.StartsWith("tool:", StringComparison.Ordinal);

    /// <summary>Looks at the transcript after a frame: whether a line from Claude arrived, and what to speak: the reply in <paramref name="mode"/>, and each tool call when <paramref name="toolCalls"/> is set.</summary>
    public Arrival Update(IReadOnlyList<Line> lines, bool working, ReplySpeechMode mode, bool toolCalls)
    {
        _candidates.Clear();
        var content = 0;
        var toolCall = false;
        var lastVisible = -1;
        var afterMarker = false;
        var inReply = false;
        var marker = -1;
        for (var i = 0; i < lines.Count; i++)
        {
            var line = lines[i];
            if (line.Hidden)
            {
                continue;
            }

            lastVisible = i;
            if (line.IsMarker)
            {
                if (line.Kind == LineKind.OutputMarker)
                {
                    // A new exchange: what came before it was final, and spoken, in earlier frames.
                    _candidates.Clear();
                    afterMarker = true;
                    inReply = false;
                    marker = line.Id;
                }

                continue;
            }

            content++;
            if (IsToolCall(line) && (!_toolRows.TryGetValue(line.Id, out var known) || known != line.Text))
            {
                _toolRows[line.Id] = line.Text;
                toolCall = true;
            }

            if (!afterMarker || (mode == ReplySpeechMode.None && !toolCalls))
            {
                continue;
            }

            // An error: row opens a reply like a claude: row: it is Claude's whole answer when the login has lapsed.
            if (line.Kind is LineKind.ClaudeReply or LineKind.Error)
            {
                inReply = true;
            }
            else if (line.Kind != LineKind.Plain)
            {
                inReply = false;
            }

            var wanted = inReply
                ? mode == ReplySpeechMode.All || (mode == ReplySpeechMode.FirstLines && line.Kind is (LineKind.ClaudeReply or LineKind.Error))
                : toolCalls && IsToolCall(line);
            if (wanted && line.Text.Length > 0)
            {
                _candidates.Add(line);
            }
        }

        if (marker != _exchangeMarker)
        {
            _exchangeMarker = marker;
            _spokenInExchange.Clear();
        }

        if (lines.Count > 0 && !ReferenceEquals(lines[0], _first))
        {
            // The transcript was trimmed from the front (FR-3.6) or cleared: what was remembered about lines that are
            // gone goes too. By membership, since the ids are not in list order (a marker inserted later is newer).
            _first = lines[0];
            _present.Clear();
            for (var i = 0; i < lines.Count; i++)
            {
                _present.Add(lines[i].Id);
            }

            Forget(_spoken, _present);
            Forget(_toolRows, _present);
        }

        var newLine = content > _contentLines;
        _contentLines = content;
        return new Arrival(newLine, toolCall, Speech(lines, lastVisible, working));
    }

    /// <summary>The lines remembered as spoken or seen; for the test that keeps the memory bounded.</summary>
    public int Remembered => _spoken.Count + _toolRows.Count;

    private static void Forget(Dictionary<int, string> known, HashSet<int> present)
    {
        foreach (var id in known.Keys.Where(id => !present.Contains(id)).ToList())
        {
            known.Remove(id);
        }
    }

    /// <summary>
    /// Treats every line now in the transcript as spoken: a conversation replayed at startup is not read out, and a
    /// mode chosen in the menu starts with what arrives next.
    /// </summary>
    public void Skip(IReadOnlyList<Line> lines)
    {
        foreach (var line in lines)
        {
            _spoken[line.Id] = line.Text;
            if (IsToolCall(line))
            {
                _toolRows[line.Id] = line.Text;
            }
        }
    }

    private string? Speech(IReadOnlyList<Line> lines, int lastVisible, bool working)
    {
        if (_candidates.Count == 0)
        {
            return null;
        }

        _text.Clear();
        foreach (var line in _candidates)
        {
            // Every candidate but the last visible line has a visible line after it. A tool row is complete when it
            // is printed, and waiting for the next line would hold it until the tool has finished.
            var final = line.Committed || !working || line.Kind == LineKind.Tool || !ReferenceEquals(line, lines[lastVisible]);
            if (final && (!_spoken.TryGetValue(line.Id, out var spoken) || spoken != line.Text))
            {
                _spoken[line.Id] = line.Text;
                // A reprint of a line already spoken in this exchange is not spoken again.
                if (_spokenInExchange.Add(line.Text))
                {
                    Append(line);
                }
            }
        }

        return _text.Length == 0 ? null : _text.ToString();
    }

    /// <summary>A wrapped continuation joins with a space; other lines with a full stop unless they end with a pause of their own, so a heading or a list item is heard as one. A tool row loses its expand hint.</summary>
    private void Append(Line line)
    {
        var text = line.Text;
        if (line.Kind == LineKind.Tool && text.EndsWith(ExpandHint, StringComparison.Ordinal))
        {
            text = text[..^ExpandHint.Length].TrimEnd();
        }

        if (_text.Length > 0)
        {
            if (line.JoinedToPrevious)
            {
                if (!text.StartsWith(' '))
                {
                    _text.Append(' ');
                }
            }
            else
            {
                _text.Append(Array.IndexOf(PauseEndings, _text[^1]) >= 0 ? " " : ". ");
            }
        }

        _text.Append(text);
    }
}
