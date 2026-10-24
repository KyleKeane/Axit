using Axit.Core;

namespace Axit.Forms;

/// <summary>
/// The bundle's list (docs/axit/SPEC.md AX-8.6, 2026-10-24): a <see cref="ListBox"/> with what every list the user moves
/// through has, built in, so that a window gets it by making an <c>AxitList</c> and nothing else (<c>ListTests</c> fails
/// on a plain list box in an app):
/// <list type="bullet">
/// <item>Type-to-find, File Explorer's way: letters typed within a second of each other build one prefix that
/// <see cref="TypeAhead"/> finds among the names; a run of one letter cycles through the names starting with it; Shift
/// with a letter searches backwards; a new prefix starts after a second's pause; the edge earcon when no name starts
/// with it. The list box's own one-letter jump gives way to it.</item>
/// <item>The edge earcon (<see cref="Sounds.Edge"/>) for Up or Home on the first item and Down or End on the last,
/// where the key moves nothing and NVDA would say nothing.</item>
/// </list>
/// The names are the items' texts unless the window gives others (<see cref="Names"/>: fewer when the list ends in a
/// line that is no item, such as axFile's "Not shown"). A window's own keys come first: what it handles in
/// ProcessCmdKey (Enter, Space, Delete, its letters with Ctrl) never reaches the list, and nothing is said here; NVDA
/// reads the selection.
/// </summary>
public class AxitList : ListBox
{
    /// <summary>The pause after which typing starts a new prefix.</summary>
    public const int PauseMs = 1000;

    private string _typed = string.Empty;
    private long _typedAt;

    public AxitList()
    {
        IntegralHeight = false;
    }

    /// <summary>The names type-to-find searches, in the list's order; null for the items' texts. An empty list of names turns it off.</summary>
    [System.ComponentModel.Browsable(false)]
    [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
    public Func<IReadOnlyList<string>>? Names { get; set; }

    /// <summary>The next letter starts a new prefix: a window calls it when it fills the list anew (axFile's next folder).</summary>
    public void ResetTypeAhead()
    {
        _typed = string.Empty;
        _typedAt = 0;
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (e.Handled || e.Modifiers != Keys.None || Items.Count == 0 || SelectedIndex < 0)
        {
            return;
        }

        // The key moves nothing at an end: the earcon instead of silence (axFile's AF-3.6, every list since 2026-10-24).
        if ((e.KeyCode is Keys.Up or Keys.Home && SelectedIndex == 0) || (e.KeyCode is Keys.Down or Keys.End && SelectedIndex == Items.Count - 1))
        {
            Sounds.Edge();
        }
    }

    protected override void OnKeyPress(KeyPressEventArgs e)
    {
        base.OnKeyPress(e);
        if (e.Handled || char.IsControl(e.KeyChar))
        {
            return;
        }

        var names = Names?.Invoke() ?? Items.Cast<object>().Select(item => item.ToString() ?? string.Empty).ToList();
        if (names.Count == 0)
        {
            return;
        }

        e.Handled = true;
        var now = Environment.TickCount64;
        _typed = now - _typedAt > PauseMs ? e.KeyChar.ToString() : _typed + e.KeyChar;
        _typedAt = now;
        // Shift with a letter searches backwards; with a digit or a sign it only makes another character.
        var backward = char.IsLetter(e.KeyChar) && (ModifierKeys & Keys.Shift) != 0;
        var index = TypeAhead.Find(names, _typed, SelectedIndex, backward);
        if (index >= 0)
        {
            SelectedIndex = index;
        }
        else
        {
            Sounds.Edge();
        }
    }
}
