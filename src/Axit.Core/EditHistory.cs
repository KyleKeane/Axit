namespace Axit.Core;

/// <summary>
/// One change to a text (docs/axdown/SPEC.md AD-2.5): at <paramref name="Start"/>, <paramref name="Removed"/> went
/// out and <paramref name="Inserted"/> came in. Undoing it replaces the span of the inserted text with the removed
/// text; redoing it the other way round. The window applies a step through the edit control's selection.
/// </summary>
public sealed record EditStep(int Start, string Removed, string Inserted)
{
    /// <summary>The text with this step taken back.</summary>
    public string Undo(string text) => string.Concat(text.AsSpan(0, Start), Removed, text.AsSpan(Start + Inserted.Length));

    /// <summary>The text with this step made again.</summary>
    public string Redo(string text) => string.Concat(text.AsSpan(0, Start), Inserted, text.AsSpan(Start + Removed.Length));

    /// <summary>The characters this step keeps, for the bound of the history.</summary>
    public int Size => Removed.Length + Inserted.Length;
}

/// <summary>
/// The history of a window's changes to its text, for Undo and Redo (docs/axdown/SPEC.md AD-2.5, docs/axjson/SPEC.md
/// AJ-2.5). The window records each change as the text before and after it (<see cref="Record"/>); the step is found
/// by the common prefix and suffix, with the caret telling which of the equal places it was when the texts allow
/// several. A run of typing, or of deleting backwards or forwards, is one step while each change comes from a key
/// typed at <see cref="RunCaret"/> and nothing broke the run (<see cref="Break"/> after a pause of a second). A new
/// change empties the redo side. The history keeps a few megabytes of deltas and drops its oldest steps beyond
/// that. Pure code, no clock: the window tells where a key was typed, and when the second has passed.
/// </summary>
public sealed class EditHistory
{
    /// <summary>The characters of deltas kept at most, the oldest steps dropped first (a few megabytes).</summary>
    public const int Limit = 4_000_000;

    private readonly List<EditStep> _undo = [];
    private readonly List<EditStep> _redo = [];
    private bool _runOpen;
    private int _size;

    public bool CanUndo => _undo.Count > 0;

    public bool CanRedo => _redo.Count > 0;

    /// <summary>Where the caret sits when the open run of typing or deleting goes on, or -1 when no run is open.</summary>
    public int RunCaret => _runOpen && _undo.Count > 0 ? _undo[^1].Start + _undo[^1].Inserted.Length : -1;

    /// <summary>
    /// The one step from <paramref name="before"/> to <paramref name="after"/>, or null when they are the same. The
    /// changed span lies between the common prefix and the common suffix; when it could sit at several places (an "a"
    /// typed next to an "a"), it is slid left until the inserted text ends at <paramref name="caret"/>, which is
    /// where a typed or pasted text ends and where a deletion happened.
    /// </summary>
    public static EditStep? Diff(string before, string after, int caret)
    {
        if (before == after)
        {
            return null;
        }

        var prefix = before.AsSpan().CommonPrefixLength(after);
        var bound = Math.Min(before.Length, after.Length) - prefix;
        var suffix = 0;
        while (suffix < bound && before[before.Length - 1 - suffix] == after[after.Length - 1 - suffix])
        {
            suffix++;
        }

        var removedLength = before.Length - prefix - suffix;
        var insertedLength = after.Length - prefix - suffix;
        while (prefix > 0 && prefix + insertedLength > caret && before[prefix - 1 + removedLength] == after[prefix - 1 + insertedLength])
        {
            prefix--;
        }

        return new EditStep(prefix, before.Substring(prefix, removedLength), after.Substring(prefix, insertedLength));
    }

    /// <summary>
    /// A change recorded: the step from <paramref name="before"/> to <paramref name="after"/>, with the caret after
    /// it. <paramref name="typedAt"/> is where the caret was when the key that made it was typed (a character,
    /// Backspace or Delete), or -1 for a change made another way (a paste, a cut, a command, a reload). A typed
    /// change joins the open run when it was typed at the run's caret and is adjacent to it (more typed after the
    /// run, a Backspace at its end, a Delete at its caret); any other change is a step of its own, and only a typed
    /// one leaves a run open behind it. Nothing is recorded when the texts are the same.
    /// </summary>
    public void Record(string before, string after, int caret, int typedAt)
    {
        if (Diff(before, after, caret) is not { } step)
        {
            return;
        }

        foreach (var old in _redo)
        {
            _size -= old.Size;
        }

        _redo.Clear();
        if (typedAt >= 0 && typedAt == RunCaret && Merge(_undo[^1], step) is { } merged)
        {
            _size += merged.Size - _undo[^1].Size;
            _undo[^1] = merged;
        }
        else
        {
            _undo.Add(step);
            _size += step.Size;
        }

        _runOpen = typedAt >= 0;
        while (_size > Limit && _undo.Count > 1)
        {
            _size -= _undo[0].Size;
            _undo.RemoveAt(0);
        }
    }

    /// <summary>The open run ends: the next change starts a step of its own (a pause of a second, AD-2.5).</summary>
    public void Break() => _runOpen = false;

    /// <summary>The last step, to be taken back by the window, moved to the redo side; null with nothing to undo.</summary>
    public EditStep? Undo()
    {
        _runOpen = false;
        if (_undo.Count == 0)
        {
            return null;
        }

        var step = _undo[^1];
        _undo.RemoveAt(_undo.Count - 1);
        _redo.Add(step);
        return step;
    }

    /// <summary>The last undone step, to be made again by the window, back on the undo side; null with nothing to redo.</summary>
    public EditStep? Redo()
    {
        _runOpen = false;
        if (_redo.Count == 0)
        {
            return null;
        }

        var step = _redo[^1];
        _redo.RemoveAt(_redo.Count - 1);
        _undo.Add(step);
        return step;
    }

    /// <summary>Everything forgotten: another file, or a new document (AD-2.5).</summary>
    public void Clear()
    {
        _undo.Clear();
        _redo.Clear();
        _runOpen = false;
        _size = 0;
    }

    /// <summary>
    /// The run's step with the next one folded in, or null when the next one is not a continuation: typing goes on
    /// where the run's inserted text ends; a Backspace takes the run's last typed characters back, or reaches before
    /// them into the text that was there; a Delete removes what follows the run.
    /// </summary>
    private static EditStep? Merge(EditStep run, EditStep next)
    {
        var end = run.Start + run.Inserted.Length;
        if (next.Removed.Length == 0 && next.Start == end)
        {
            return run with { Inserted = run.Inserted + next.Inserted };
        }

        if (next.Inserted.Length == 0 && next.Start + next.Removed.Length == end)
        {
            if (next.Start >= run.Start)
            {
                return run with { Inserted = run.Inserted[..(next.Start - run.Start)] };
            }

            return new EditStep(next.Start, next.Removed[..(run.Start - next.Start)] + run.Removed, string.Empty);
        }

        if (next.Inserted.Length == 0 && next.Start == end)
        {
            return run with { Removed = run.Removed + next.Removed };
        }

        return null;
    }
}
