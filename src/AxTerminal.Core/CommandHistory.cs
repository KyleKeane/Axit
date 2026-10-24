namespace AxTerminal.Core;

/// <summary>
/// The window's recent commands for Ctrl+R (SPEC.md AT-3.6): the newest first, each once, fifty at most; not
/// persisted. A send without recording never gets in (AT-3.4).
/// </summary>
public sealed class CommandHistory
{
    public const int Limit = 50;

    private readonly List<string> _items = [];

    /// <summary>The commands, the newest first.</summary>
    public IReadOnlyList<string> Items => _items;

    /// <summary>Puts the command in front, once; an empty text is ignored.</summary>
    public void Add(string command)
    {
        var text = command.Trim();
        if (text.Length == 0)
        {
            return;
        }

        Reset();
        _items.RemoveAll(c => string.Equals(c, text, StringComparison.Ordinal));
        _items.Insert(0, text);
        if (_items.Count > Limit)
        {
            _items.RemoveRange(Limit, _items.Count - Limit);
        }
    }

    public void Clear()
    {
        _items.Clear();
        Reset();
    }

    // ---- Up and Down in the field (AT-3.9, 2026-10-23): PowerShell's own history keys over the same list ----

    /// <summary>The command the field holds from the list, 0 the newest; -1 while it holds the user's own text.</summary>
    private int _at = -1;

    /// <summary>What the field held before the first Up, given back by Down past the newest.</summary>
    private string _draft = string.Empty;

    /// <summary>The command before the one in hand, or null at the oldest; the first call keeps <paramref name="current"/> as the draft.</summary>
    public string? Older(string current)
    {
        if (_at + 1 >= _items.Count)
        {
            return null;
        }

        if (_at < 0)
        {
            _draft = current;
        }

        _at++;
        return _items[_at];
    }

    /// <summary>The command after the one in hand, the draft past the newest, or null while the field holds the user's own text.</summary>
    public string? Newer()
    {
        if (_at < 0)
        {
            return null;
        }

        _at--;
        return _at < 0 ? _draft : _items[_at];
    }

    /// <summary>The field holds the user's own text again (typed, sent, cleared): the next Up starts from the newest.</summary>
    public void Reset()
    {
        _at = -1;
        _draft = string.Empty;
    }
}
