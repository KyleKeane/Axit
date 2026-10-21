namespace AxFile;

/// <summary>
/// The entry's context menu (SPEC.md AF-4.6), whose items the window fills the same way as the menu bar's Item menu.
/// While it is open, the Information key says the entry without closing it (AF-4.10): WinForms routes the keys of
/// an open menu to the menu's own <see cref="ProcessCmdKey"/>, not to the window's, so the key is taken here. It is
/// the list's context menu only: assigned as the bar item's drop-down it lost its owner item on closing and the bar
/// then opened its neighbours' menus on Left and Right (AX-6.6).
/// </summary>
internal sealed class ItemMenu(Action sayEntry) : ContextMenuStrip
{
    protected override bool ProcessCmdKey(ref Message m, Keys keyData)
    {
        if (keyData == AxFileKeys.Information)
        {
            sayEntry();
            return true;
        }

        return base.ProcessCmdKey(ref m, keyData);
    }
}
