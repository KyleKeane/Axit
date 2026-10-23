using Axit.Core;

namespace Axit.Forms;

/// <summary>
/// The command palette's window side (docs/axit/SPEC.md AX-10): the window's menu bar read into entries, and the
/// marker of the submenus it leaves out. The notice and its keys are the window base's (<see cref="AxitForm"/>); the
/// lines and the search are <see cref="Palette"/>, tested.
/// </summary>
public static class CommandPalette
{
    /// <summary>The keys in the palette's field (AX-10.4), which act there only, as AxStart's do in its field.</summary>
    public const Keys NextMatch = Keys.Down;

    public const Keys PreviousMatch = Keys.Up;
    public const Keys FirstMatch = Keys.Control | Keys.Home;
    public const Keys LastMatch = Keys.Control | Keys.End;

    private const string FilledOnOpeningTag = "Axit.FilledOnOpening";

    /// <summary>
    /// Marks a submenu whose items are made each time it opens (Open recent, Open windows, the drives): the palette
    /// leaves it out, since what it holds is known only once it opens (AX-10.2). Returns the item.
    /// </summary>
    public static ToolStripMenuItem FilledOnOpening(ToolStripMenuItem item)
    {
        item.Tag = FilledOnOpeningTag;
        return item;
    }

    /// <summary>
    /// Every enabled item of the menu bar that does something, in menu order, as an entry that clicks it (AX-10.2).
    /// A checked setting says its new state after the click through <paramref name="say"/>, "Word wrap, checked".
    /// Read while the menu bar is enabled: an item of a disabled bar counts as disabled.
    /// </summary>
    public static List<PaletteEntry> Commands(MenuStrip? menu, Action<string> say)
    {
        var entries = new List<PaletteEntry>();
        if (menu is not null)
        {
            Collect(menu.Items, [], entries, say);
        }

        return entries;
    }

    private static void Collect(ToolStripItemCollection items, List<string> path, List<PaletteEntry> entries, Action<string> say)
    {
        foreach (ToolStripItem item in items)
        {
            if (item is not ToolStripMenuItem menuItem || !menuItem.Available || !menuItem.Enabled || Equals(menuItem.Tag, FilledOnOpeningTag))
            {
                continue;
            }

            var name = MenuText.Plain(menuItem.Text ?? string.Empty);
            if (menuItem.HasDropDownItems)
            {
                path.Add(name);
                Collect(menuItem.DropDownItems, path, entries, say);
                path.RemoveAt(path.Count - 1);
                continue;
            }

            // The bar's own items (File, Edit) only open their menus.
            if (path.Count == 0)
            {
                continue;
            }

            var key = menuItem.ShortcutKeyDisplayString ?? (menuItem.ShortcutKeys != Keys.None ? BundleKeys.Text(menuItem.ShortcutKeys) : null);
            var text = Palette.Text(name, path, key, menuItem.Checked);
            entries.Add(new PaletteEntry(text, () =>
            {
                menuItem.PerformClick();
                if (menuItem.CheckOnClick)
                {
                    say($"{name}, {(menuItem.Checked ? "checked" : "not checked")}");
                }
            }));
        }
    }
}
