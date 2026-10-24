using Axit.Core;

namespace Axit.Forms;

/// <summary>
/// Type-to-find in a list box (docs/axit/SPEC.md AX-8.6; axFile's AF-3.6 since 2026-10-22, every list of the bundle that
/// the user types in since 2026-10-24): letters typed within a second of each other build one prefix, "cha", that
/// <see cref="TypeAhead"/> finds among the list's names; a run of one letter cycles through the names starting with it;
/// Shift with a letter searches backwards; a new prefix starts after a second's pause; the edge earcon when no name
/// starts with it. The list box's own one-letter jump gives way to this. NVDA reads the item selected; nothing is said.
/// Enter, Escape, Backspace, Space and the Ctrl letters stay the window's keys: a window that handles them in
/// ProcessCmdKey never lets them reach the list's key press.
/// </summary>
public sealed class ListTypeAhead
{
    /// <summary>The pause after which typing starts a new prefix.</summary>
    public const int PauseMs = 1000;

    private readonly ListBox _list;
    private readonly Func<IReadOnlyList<string>> _names;
    private string _typed = string.Empty;
    private long _typedAt;

    private ListTypeAhead(ListBox list, Func<IReadOnlyList<string>> names)
    {
        _list = list;
        _names = names;
    }

    /// <summary>
    /// The type-ahead on <paramref name="list"/>, over <paramref name="names"/>: the names in the list's order, which may
    /// be fewer than its items (a last line that is no entry); nothing happens while it gives none.
    /// </summary>
    public static ListTypeAhead Attach(ListBox list, Func<IReadOnlyList<string>> names)
    {
        var typeAhead = new ListTypeAhead(list, names);
        list.KeyPress += typeAhead.OnKeyPress;
        return typeAhead;
    }

    /// <summary>The next letter starts a new prefix: a window calls it when it fills its list anew (axFile's next folder).</summary>
    public void Reset()
    {
        _typed = string.Empty;
        _typedAt = 0;
    }

    private void OnKeyPress(object? sender, KeyPressEventArgs e)
    {
        if (char.IsControl(e.KeyChar))
        {
            return;
        }

        var names = _names();
        if (names.Count == 0)
        {
            return;
        }

        e.Handled = true;
        var now = Environment.TickCount64;
        _typed = now - _typedAt > PauseMs ? e.KeyChar.ToString() : _typed + e.KeyChar;
        _typedAt = now;
        // Shift with a letter searches backwards; with a digit or a sign it only makes another character.
        var backward = char.IsLetter(e.KeyChar) && (Control.ModifierKeys & Keys.Shift) != 0;
        var index = TypeAhead.Find(names, _typed, _list.SelectedIndex, backward);
        if (index >= 0)
        {
            _list.SelectedIndex = index;
        }
        else
        {
            Sounds.Edge();
        }
    }
}
