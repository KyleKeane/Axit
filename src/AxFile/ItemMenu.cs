namespace AxFile;

/// <summary>
/// The entry's menu (SPEC.md AF-4.6): a context menu whose items the window fills, made twice, once as the list's
/// context menu and once as the drop-down of the menu bar's Item menu, so that both are the same menu. While it is
/// open, the Information key says the entry without closing it (AF-4.10): WinForms routes the keys of an open menu
/// to the menu's own <see cref="ProcessCmdKey"/>, not to the window's, so the key is taken here.
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
