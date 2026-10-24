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

    // ---- Ctrl+Up and Ctrl+Down in the field (AT-3.9, 2026-10-23): the same list, with the draft as its last item ----

    /// <summary>The command the field holds from the list, 0 the newest; -1 while it holds the draft.</summary>
    private int _at = -1;

    /// <summary>
    /// The last item, past the newest: what the user typed, or the last command the user changed and left. A field
    /// whose text is not the item in hand has been edited, and that text becomes the draft as the field moves on, so
    /// that Ctrl+Down to the end always gets the edit back; the commands themselves never change.
    /// </summary>
    private string _draft = string.Empty;

    /// <summary>The command before the one in hand, or null at the oldest; an edited field is kept as the draft first.</summary>
    public string? Older(string current)
    {
        Keep(current);
        if (_at + 1 >= _items.Count)
        {
            return null;
        }

        _at++;
        return _items[_at];
    }

    /// <summary>The command after the one in hand, the draft past the newest, or null at the draft; an edited field is kept as the draft first.</summary>
    public string? Newer(string current)
    {
        Keep(current);
        if (_at < 0)
        {
            return null;
        }

        _at--;
        return _at < 0 ? _draft : _items[_at];
    }

    /// <summary>A sent command: the next Ctrl+Up starts from the newest, and the draft is empty again.</summary>
    public void Reset()
    {
        _at = -1;
        _draft = string.Empty;
    }

    private void Keep(string current)
    {
        var inHand = _at < 0 ? _draft : _items[_at];
        if (!string.Equals(current, inHand, StringComparison.Ordinal))
        {
            _draft = current;
        }
    }
}
