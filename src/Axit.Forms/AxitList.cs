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

    /// <summary>Set while type-to-find itself moves the selection, so that only another move starts a new search.</summary>
    private bool _jumping;

    public AxitList()
    {
        IntegralHeight = false;
    }

    /// <summary>The names type-to-find searches, in the list's order; null for the items' texts. An empty list of names turns it off.</summary>
    [System.ComponentModel.Browsable(false)]
    [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
    public Func<IReadOnlyList<string>>? Names { get; set; }

    /// <summary>
    /// The list's lines made again (from axMedia's queue and axFile's Actions list, inventory item 7, 2026-10-25): while
    /// the count is the same each changed line is rewritten in place, so that the selection stays and nothing but the
    /// changed line moves under NVDA, a line <paramref name="leave"/> names being left as it is (axFile's running line
    /// the user is on); otherwise the whole list is filled again, and the caller puts the selection back. True when the
    /// lines were changed in place.
    /// </summary>
    public bool SetLines(IReadOnlyList<string> lines, Func<int, bool>? leave = null)
    {
        if (lines.Count == Items.Count)
        {
            for (var i = 0; i < lines.Count; i++)
            {
                if (leave?.Invoke(i) != true && !string.Equals(Items[i].ToString(), lines[i], StringComparison.Ordinal))
                {
                    Items[i] = lines[i];
                }
            }

            return true;
        }

        BeginUpdate();
        Items.Clear();
        Items.AddRange(lines.Cast<object>().ToArray());
        EndUpdate();
        return false;
    }

    /// <summary>The next letter starts a new prefix: a window calls it when it fills the list anew (axFile's next folder).</summary>
    public void ResetTypeAhead()
    {
        _typed = string.Empty;
        _typedAt = 0;
    }

    /// <summary>Any move of the selection but the search's own (an arrow, a command, the list filled anew) starts a new search, as in File Explorer: a letter typed just after it never continues the letters before (found by axAutomate, 2026-10-24: "s", then keys, then "b" within the second searched "sb").</summary>
    protected override void OnSelectedIndexChanged(EventArgs e)
    {
        if (!_jumping)
        {
            ResetTypeAhead();
        }

        base.OnSelectedIndexChanged(e);
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

    // ---- the rename in place (AX-8.7; from axFile's AF-4.2 on its second use, axTask and axPass, 2026-10-24) ----

    private TextBox? _renameBox;
    private Action<string>? _renameCommit;
    private Action? _renameEnded;

    /// <summary>Whether a rename box shows over the selected item.</summary>
    public bool Renaming => _renameBox is { Visible: true };

    /// <summary>
    /// F2 in every list whose items can be renamed: a box named "New name" over the selected item with
    /// <paramref name="text"/>, its first <paramref name="selectLength"/> characters selected (a file's name before
    /// its extension). Enter calls <paramref name="commit"/> with the text, which checks it and calls
    /// <see cref="EndRename"/> when it is taken (it stays open on a refusal the window says); Escape, or the focus
    /// leaving the box, ends it with the name kept. <paramref name="ended"/> is called whenever the box closes.
    /// </summary>
    public void BeginRename(string text, int selectLength, Action<string> commit, Action? ended = null)
    {
        if (SelectedIndex < 0 || Renaming)
        {
            return;
        }

        if (_renameBox is null)
        {
            _renameBox = new TextBox { Visible = false, AccessibleName = "New name", BorderStyle = BorderStyle.FixedSingle, Font = Font };
            _renameBox.KeyDown += OnRenameKey;
            _renameBox.Leave += (_, _) => EndRename();
            Controls.Add(_renameBox);
        }

        _renameCommit = commit;
        _renameEnded = ended;
        _renameBox.Bounds = GetItemRectangle(SelectedIndex);
        _renameBox.Text = text;
        // The selection goes before the focus: a text box that gets the focus selects everything unless one was set.
        _renameBox.Select(0, Math.Clamp(selectLength, 0, text.Length));
        _renameBox.Visible = true;
        _renameBox.BringToFront();
        _renameBox.Select();
    }

    /// <summary>The rename box closed, the focus back on the list first, so that hiding the box does not send it somewhere of WinForms' choosing.</summary>
    public void EndRename()
    {
        if (_renameBox is not { Visible: true } box)
        {
            return;
        }

        _renameCommit = null;
        var ended = _renameEnded;
        _renameEnded = null;
        if (!IsDisposed && Visible && !Focused)
        {
            Select();
        }

        box.Visible = false;
        ended?.Invoke();
    }

    protected override void OnFontChanged(EventArgs e)
    {
        base.OnFontChanged(e);
        if (_renameBox is not null)
        {
            _renameBox.Font = Font;
        }
    }

    /// <summary>Enter and Escape in the box are the box's own, as a notice's field's.</summary>
    private void OnRenameKey(object? sender, KeyEventArgs e)
    {
        if (e.KeyData == Keys.Return)
        {
            e.SuppressKeyPress = true;
            _renameCommit?.Invoke(_renameBox!.Text.Trim());
        }
        else if (e.KeyData == Keys.Escape)
        {
            e.SuppressKeyPress = true;
            EndRename();
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

        var now = Environment.TickCount64;
        var fresh = now - _typedAt > PauseMs || _typed.Length == 0;
        if (fresh && e.KeyChar == ' ')
        {
            // A space continues a name ("windows event") but never starts a search: it is a window's key (axGIT's stage,
            // axList's check) that may still arrive as a character.
            return;
        }

        e.Handled = true;
        _typed = fresh ? e.KeyChar.ToString() : _typed + e.KeyChar;
        _typedAt = now;
        // Shift with a letter searches backwards; with a digit or a sign it only makes another character.
        var backward = char.IsLetter(e.KeyChar) && (ModifierKeys & Keys.Shift) != 0;
        var index = TypeAhead.Find(names, _typed, SelectedIndex, backward);
        if (index >= 0)
        {
            _jumping = true;
            SelectedIndex = index;
            _jumping = false;
        }
        else
        {
            Sounds.Edge();
        }
    }
}
