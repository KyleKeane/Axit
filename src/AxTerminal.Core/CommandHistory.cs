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

        _items.RemoveAll(c => string.Equals(c, text, StringComparison.Ordinal));
        _items.Insert(0, text);
        if (_items.Count > Limit)
        {
            _items.RemoveRange(Limit, _items.Count - Limit);
        }
    }

    public void Clear() => _items.Clear();
}
