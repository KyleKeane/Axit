using System.Text;
using Axit.Core.Transcript;

namespace AxTerminal.Core;

/// <summary>What one frame brought (SPEC.md AT-5.2, AT-5.3): a visible line from the shell, and the lines to speak as one notification.</summary>
public readonly record struct TerminalArrival(bool NewLine, string? Speech);

/// <summary>
/// The per-frame pass behind the tick and the spoken output (SPEC.md AT-5.2, AT-5.3), the shape of axClaude's
/// Arrivals without Claude's kinds: a frame that added a visible line from the shell ticks once; with speech on, the
/// shell's lines that became final (committed, followed by another visible line, or the shell idle) and were not
/// spoken yet go out as one utterance (axClaude D32). Lines are told apart by id, which grows with creation.
/// </summary>
public sealed class TerminalArrivals
{
    /// <summary>How far back a frame is looked at: a screenful and the lines a big frame scrolled off it.</summary>
    private const int LookBack = 5000;

    private int _tickedId = -1;
    private int _spokenId = -1;
    private readonly StringBuilder _speech = new();
    private readonly List<Line> _candidates = [];

    public TerminalArrival Update(IReadOnlyList<Line> lines, bool running, bool speak)
    {
        _candidates.Clear();
        var threshold = Math.Min(_tickedId, speak ? _spokenId : int.MaxValue);
        for (var i = lines.Count - 1; i >= 0 && lines.Count - i <= LookBack; i--)
        {
            var line = lines[i];
            if (line.IsMarker)
            {
                continue;
            }

            if (line.Id <= threshold && line.Text.Length > 0 && line.Committed)
            {
                break;
            }

            if (!line.Hidden && line.Text.Length > 0 && line.Kind == LineKind.Plain)
            {
                _candidates.Add(line);
            }
        }

        _candidates.Reverse();
        var newLine = false;
        var maxTicked = _tickedId;
        foreach (var line in _candidates)
        {
            if (line.Id > _tickedId)
            {
                newLine = true;
                maxTicked = Math.Max(maxTicked, line.Id);
            }
        }

        _tickedId = maxTicked;
        if (!speak)
        {
            return new TerminalArrival(newLine, null);
        }

        _speech.Clear();
        var maxSpoken = _spokenId;
        for (var i = 0; i < _candidates.Count; i++)
        {
            var line = _candidates[i];
            if (line.Id <= _spokenId)
            {
                continue;
            }

            var final = line.Committed || i + 1 < _candidates.Count || !running;
            if (!final)
            {
                break;
            }

            Utterance.Append(_speech, line.Text, line.JoinedToPrevious);
            maxSpoken = Math.Max(maxSpoken, line.Id);
        }

        _spokenId = maxSpoken;
        return new TerminalArrival(newLine, _speech.Length == 0 ? null : _speech.ToString());
    }

    /// <summary>After Clear or a new shell: nothing of the old transcript is remembered.</summary>
    public void Reset()
    {
        _tickedId = -1;
        _spokenId = -1;
    }
}
