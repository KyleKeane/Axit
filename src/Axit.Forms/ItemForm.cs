using Axit.Core;

namespace Axit.Forms;

/// <summary>
/// A line of an item app's navigator (docs/axit/item-apps.md IA-2.2): a list (or group, or smart list) Right goes
/// into, or an item; its key for the selection (a path), its name for type-to-find, its words, and what the app keeps
/// with it. A section's header (IA-2.3a) has <see cref="IsSection"/>: Ctrl+Up and Ctrl+Down go from one to the next.
/// </summary>
public sealed record ItemRow(bool IsList, string Key, string Name, string Label, object? Tag = null, bool IsSection = false);

/// <summary>A line of the detail pane after the fields: its words, and what Enter on it does (nothing when null).</summary>
public sealed record ItemDetailLine(string Label, Action? Enter = null);

/// <summary>
/// What the detail pane shows for the navigator's selection (IA-2.3): its name ("Fields of Gmail"), the key of what it
/// shows (so that the field selected stays when the same thing is shown again), the fields' document (null for a line
/// that holds none, such as a smart list), the lines after the fields, and what the app keeps to save it.
/// </summary>
public sealed record ItemDetail(string Name, string? Key, ItemDocument? Document, IReadOnlyList<ItemDetailLine> Lines, object? Source = null);

/// <summary>
/// The window of an item app (docs/axit/item-apps.md IA-2, IA-3, IA-5; from axTask on its second use, axPass,
/// 2026-10-24): the navigator on the left, the detail pane on the right, each the bundle's <see cref="AxitList"/>, a
/// status bar, and the window's menu bar. This class holds what both apps do the same way: the panes, Right, Left and
/// Enter through the levels, Tab between the panes, the keys of <see cref="ItemKeys"/> on the window's command list
/// (docs/axit/app-kit.md AK-2; registered after the app's own, so that the app's come first), the detail pane filled
/// silently for the selection, and every field command with its notices (<c>ItemForm.Fields.cs</c>). The app fills in
/// what a level holds (<see cref="ReadLevel"/>), how it goes in and out of a list, what the detail pane shows, and how
/// a change is saved (<see cref="SaveFields"/>): a file for axTask, the KeePassXC tool for axPass.
/// </summary>
public abstract partial class ItemForm : AxitForm
{
    private readonly string _appName;
    private readonly SplitContainer _split = new();
    private readonly StatusStrip _status = new();
    private readonly ToolStripStatusLabel _nameLabel;
    private readonly ToolStripStatusLabel _placeLabel;
    private List<ItemRow> _rows = [];
    private List<(int Field, ItemDetailLine Line)> _lines = [];

    /// <summary>The detail pane's content now; its document is what the field commands change.</summary>
    private ItemDetail _shown = new("Details", null, null, []);

    protected ItemForm(string appName, WindowSettings settings, string settingsPath, string? settingsError)
        : base(appName, settings, settingsPath, settingsError)
    {
        _appName = appName;
        Navigator.Dock = DockStyle.Fill;
        Navigator.TabIndex = 0;
        Navigator.Names = () => _rows.Select(row => row.Name).ToList();
        Navigator.SelectedIndexChanged += (_, _) => FillDetail();
        DetailPane.Dock = DockStyle.Fill;
        DetailPane.TabIndex = 1;
        DetailPane.Names = () => _lines.Select(line => line.Field >= 0 && _shown.Document is { } document && line.Field < document.Fields.Count ? document.Fields[line.Field].Title : line.Line.Label).ToList();
        foreach (var list in new[] { Navigator, DetailPane })
        {
            list.MouseDown += (_, e) =>
            {
                if (e.Button == MouseButtons.Right && list.IndexFromPoint(e.Location) is var index && index >= 0)
                {
                    list.SelectedIndex = index;
                }
            };
            list.MouseUp += (_, e) =>
            {
                if (e.Button == MouseButtons.Right)
                {
                    OpenPaneMenu();
                }
            };
        }

        _split.Dock = DockStyle.Fill;
        _split.TabStop = false;
        _split.Panel1.Controls.Add(Navigator);
        _split.Panel2.Controls.Add(DetailPane);
        (_nameLabel, _placeLabel) = StatusLayout.Pair(_status);
    }

    /// <summary>The navigator, on the left (IA-2.2).</summary>
    protected AxitList Navigator { get; } = new();

    /// <summary>The detail pane, on the right (IA-2.3).</summary>
    protected AxitList DetailPane { get; } = new();

    /// <summary>The navigator's rows as last filled.</summary>
    protected IReadOnlyList<ItemRow> Rows => _rows;

    /// <summary>The detail pane's content now.</summary>
    protected ItemDetail ShownDetail => _shown;

    // ---- what the app fills in ----

    /// <summary>The level shown: its rows, the navigator's name with its count ("Home, 4 items"), the window title's first part, and the status bar's second label (the folder or the database).</summary>
    protected abstract (IReadOnlyList<ItemRow> Rows, string Name, string Title, string Place) ReadLevel();

    /// <summary>The app's state set to the list's level, for Right and Enter on a list.</summary>
    protected abstract void EnterList(ItemRow row);

    /// <summary>The app's state set to the level above, and the key of the row to select there; null at the top, where nothing changes.</summary>
    protected abstract string? LeaveList();

    /// <summary>What the detail pane shows for a row, or for nothing selected.</summary>
    protected abstract ItemDetail DetailOf(ItemRow? row);

    /// <summary>A change to the detail pane's document, saved; the app fills the panes again and calls <see cref="SelectField"/> with <paramref name="selectField"/> when it is done, and says <paramref name="said"/>.</summary>
    protected abstract void SaveFields(ItemDetail detail, ItemDocument changed, string said, int selectField);

    /// <summary>An item's Title changed, from the detail pane's Title field: the app renames it as F2 does.</summary>
    protected abstract void RenameItem(ItemRow row, string title);

    /// <summary>F2 in the navigator (AX-8.7): why this row cannot be renamed, said instead of the box; null when it can.</summary>
    protected abstract string? RenameRefused(ItemRow row);

    /// <summary>Why a typed name is refused for the row, said with the box kept open; null when it is fine.</summary>
    protected abstract string? RefusedName(ItemRow row, string name);

    /// <summary>The row renamed to a name <see cref="RefusedName"/> took, once the box has closed.</summary>
    protected abstract void RenameTo(ItemRow row, string name);

    /// <summary>Delete in the navigator (IA-D4).</summary>
    protected abstract void DeleteRow(ItemRow row);

    /// <summary>Ctrl+N in the navigator.</summary>
    protected abstract void NewItem();

    /// <summary>Ctrl+Shift+N.</summary>
    protected abstract void NewList();

    /// <summary>Ctrl+Z (IA-5.4).</summary>
    protected abstract void Undo();

    /// <summary>The letter of the bar's menu Shift+F10 opens: the field menu in the detail pane, the item's or the list's in the navigator.</summary>
    protected abstract char MenuLetter(bool inDetail, ItemRow? row);

    /// <summary>The menus that act on the selection enabled, the others not.</summary>
    protected abstract void UpdateRowMenus(bool inDetail, ItemRow? row);

    /// <summary>A field's value read out; the app's own types (a link, a password) here.</summary>
    protected virtual string SpokenValue(ItemRow? row, ItemField field) => FieldTypes.Spoken(field);

    /// <summary>A type's name read out.</summary>
    private static string SpokenType(string type) => FieldTypes.Spoken(type);

    /// <summary>Enter on a field of the app's own type (a link, a password); true when the app took it.</summary>
    protected virtual bool ChangeSpecial(ItemRow row, int index, ItemField field) => false;

    /// <summary>Why the set of fields cannot change (axPass: KeePass's own fields only); null when New, Rename, Remove and Move field work.</summary>
    protected virtual string? FieldsFixed => null;

    /// <summary>The types New field offers.</summary>
    private static IReadOnlyList<string> NewFieldTypes => FieldTypes.All;

    /// <summary>Space in the navigator (axTask: a task done); the edge sound otherwise.</summary>
    protected virtual void CheckInNavigator(ItemRow? row) => Sounds.Edge();

    /// <summary>F5, before the level is read again.</summary>
    protected virtual void BeforeRefresh()
    {
    }

    // ---- what the window base asks for ----

    protected override void HideForNotice() => _split.Visible = false;

    protected override void ShowAfterNotice() => _split.Visible = true;

    protected override Control FocusTarget => DetailPane.Focused ? DetailPane : Navigator;

    protected override void ApplyFont(Font font)
    {
        Navigator.Font = font;
        DetailPane.Font = font;
    }

    /// <summary>Ctrl+S: every change is written at once.</summary>
    protected override void SaveCommand() => Announce("Nothing to save here: every change is written at once", true);

    protected override void OnShown(EventArgs e)
    {
        _split.SplitterDistance = Math.Max(_split.Panel1MinSize, _split.Width * 45 / 100);
        if (!NoticeOpen)
        {
            Navigator.Select();
        }

        base.OnShown(e);
    }

    /// <summary>
    /// The controls in their places with the app's menu bar, and the shared keys on the command list: called once at
    /// the end of the app's constructor, after its menus and its own commands are made, so that the app's keys come
    /// first (axPass's Enter unlocking a locked window before Enter opening).
    /// </summary>
    protected void PlaceControls(MenuStrip menu)
    {
        menu.MenuActivate += (_, _) => UpdateMenus();
        Controls.AddRange([Overlay, _split, _status, menu]);
        MainMenuStrip = menu;
        AddSharedKeys();
    }

    // ---- keys (IA-3): one command list (docs/axit/app-kit.md AK-2), the app's own first, then ItemKeys here ----

    /// <summary>
    /// The rename box has its own keys (AX-8.7): Enter, Escape, Ctrl+Z on the typing. While it is open no command
    /// runs: a command that acts anywhere in the window has this as its <see cref="Command.When"/>, and one of a
    /// pane needs the pane focused, which it is not while the box has the focus.
    /// </summary>
    protected bool NotRenaming() => !Navigator.Renaming && !DetailPane.Renaming;

    /// <summary>Where a navigator command's key acts.</summary>
    protected bool InNavigator() => Navigator.Focused;

    /// <summary>Where a field command's key acts.</summary>
    protected bool InDetail() => DetailPane.Focused;

    /// <summary>The keys of <see cref="ItemKeys"/>; the apps' menu items for them only show them. In a pane, taking a key keeps the list box from its own move and its ding.</summary>
    private void AddSharedKeys()
    {
        AddKey(new Command("Find in the list", BundleKeys.Find, () => ShowFindInList(Navigator, NavigatorLabels)) { When = NotRenaming, Mark = CommandMark.Read });
        AddKey(new Command("Find next", BundleKeys.FindNext, () => FindInList(Navigator, NavigatorLabels, 1)) { When = NotRenaming, Mark = CommandMark.Read });
        AddKey(new Command("Find previous", BundleKeys.FindPrevious, () => FindInList(Navigator, NavigatorLabels, -1)) { When = NotRenaming, Mark = CommandMark.Read });
        AddKey(new Command("Undo", ItemKeys.Undo, Undo) { When = NotRenaming });
        AddKey(new Command("Refresh", ItemKeys.Refresh, () => Refresh(announce: true)) { When = NotRenaming, Mark = CommandMark.Read });
        AddKey(new Command("New list", ItemKeys.NewList, NewList) { When = NotRenaming });
        AddKey(new Command("Read in full", ItemKeys.ReadAll, ReadAll) { When = NotRenaming, Mark = CommandMark.Read });

        // The navigator's keys.
        AddKey(new Command("Into the list", ItemKeys.Into, Into) { When = InNavigator, Mark = CommandMark.Open });
        AddKey(new Command("Up a level", ItemKeys.Up, UpLevel) { When = InNavigator, Mark = CommandMark.Open });
        AddKey(new Command("Up a level", ItemKeys.UpAlso, UpLevel) { When = InNavigator, Mark = CommandMark.Open });
        AddKey(new Command("Previous section", ItemKeys.PreviousSection, () => JumpSection(-1)) { When = InNavigator, Mark = CommandMark.Open });
        AddKey(new Command("Next section", ItemKeys.NextSection, () => JumpSection(1)) { When = InNavigator, Mark = CommandMark.Open });
        AddKey(new Command("Open", ItemKeys.Open, OpenSelected) { When = InNavigator, Mark = CommandMark.Open });
        AddKey(new Command("To the detail pane", ItemKeys.OtherPane, () => DetailPane.Select()) { When = InNavigator, Mark = CommandMark.Open });
        AddKey(new Command("To the detail pane", ItemKeys.OtherPaneBack, () => DetailPane.Select()) { When = InNavigator, Mark = CommandMark.Open });
        AddKey(new Command("Check", ItemKeys.Check, () => CheckInNavigator(SelectedRow())) { When = InNavigator });
        AddKey(new Command("New item", ItemKeys.New, NewItem) { When = InNavigator });
        AddKey(new Command("Rename", ItemKeys.Rename, RenameSelected) { When = InNavigator });
        AddKey(new Command("Delete", ItemKeys.Delete, () => WithRow(DeleteRow)) { When = InNavigator, Mark = CommandMark.Act });
        AddKey(new Command("The selection's menu", ItemKeys.ItemMenu, OpenPaneMenu) { When = InNavigator, Mark = CommandMark.Open });
        AddKey(new Command("The selection's menu", ItemKeys.ItemMenuAlso, OpenPaneMenu) { When = InNavigator, Mark = CommandMark.Open });

        // The detail pane's keys.
        AddKey(new Command("Change the field", ItemKeys.Open, ChangeField) { When = InDetail });
        AddKey(new Command("To the navigator", ItemKeys.OtherPane, () => Navigator.Select()) { When = InDetail, Mark = CommandMark.Open });
        AddKey(new Command("To the navigator", ItemKeys.OtherPaneBack, () => Navigator.Select()) { When = InDetail, Mark = CommandMark.Open });
        AddKey(new Command("To the navigator", ItemKeys.Back, () => Navigator.Select()) { When = InDetail, Mark = CommandMark.Open });
        AddKey(new Command("Check the field", ItemKeys.Check, CheckField) { When = InDetail });
        AddKey(new Command("New field", ItemKeys.New, NewField) { When = InDetail });
        AddKey(new Command("Rename the field", ItemKeys.Rename, RenameField) { When = InDetail });
        AddKey(new Command("Remove the field", ItemKeys.Delete, RemoveField) { When = InDetail });
        AddKey(new Command("Move the field up", ItemKeys.MoveFieldUp, () => MoveField(-1)) { When = InDetail });
        AddKey(new Command("Move the field down", ItemKeys.MoveFieldDown, () => MoveField(1)) { When = InDetail });

        // Nothing moves sideways in the field list: the edge sound, not the list box's own scroll.
        AddKey(new Command("Nothing sideways", ItemKeys.Into, Sounds.Edge) { When = InDetail, Mark = CommandMark.Read });
        AddKey(new Command("Nothing sideways", ItemKeys.Up, Sounds.Edge) { When = InDetail, Mark = CommandMark.Read });
        AddKey(new Command("The field's menu", ItemKeys.ItemMenu, OpenPaneMenu) { When = InDetail, Mark = CommandMark.Open });
        AddKey(new Command("The field's menu", ItemKeys.ItemMenuAlso, OpenPaneMenu) { When = InDetail, Mark = CommandMark.Open });
    }

    /// <summary>A navigator command on the selected row; "Nothing selected" without one.</summary>
    protected void WithRow(Action<ItemRow> command)
    {
        if (SelectedRow() is { } row)
        {
            command(row);
        }
        else
        {
            Announce("Nothing selected", true);
        }
    }

    /// <summary>
    /// F2 in the navigator (AX-8.7): the bundle's rename box over the row with its name selected; Enter takes a name
    /// the app does not refuse, Escape keeps the old one.
    /// </summary>
    protected void RenameSelected() => WithRow(row =>
    {
        if (RenameRefused(row) is { } why)
        {
            Announce(why, true);
            return;
        }

        Navigator.BeginRename(row.Name, row.Name.Length, name =>
        {
            if (name == row.Name)
            {
                Navigator.EndRename();
                Announce("Name unchanged", true);
                return;
            }

            if (RefusedName(row, name) is { } refused)
            {
                Announce(refused, true);
                return;
            }

            Navigator.EndRename();
            RenameTo(row, name);
        });
    });

    /// <summary>Shift+F10, the Applications key, a right click: the menu of the selection.</summary>
    protected void OpenPaneMenu()
    {
        UpdateMenus();
        OpenBarMenu(MenuLetter(DetailPane.Focused, SelectedRow()));
    }

    private void UpdateMenus() => UpdateRowMenus(DetailPane.Focused && _shown.Document is not null, SelectedRow());

    // ---- the levels (IA-2.2) ----

    /// <summary>Right: into a list; on an item, nothing but the edge sound (IA-3, axFile's Right on a file).</summary>
    protected void Into()
    {
        if (SelectedRow() is { IsSection: true })
        {
            OnSectionHeader();
        }
        else if (SelectedRow() is { IsList: true } row)
        {
            OpenList(row);
        }
        else
        {
            Sounds.Edge();
        }
    }

    /// <summary>Enter (IA-D2): a list opens, an item's fields take the focus.</summary>
    protected void OpenSelected()
    {
        switch (SelectedRow())
        {
            case null:
                Announce(_rows.Count == 0 ? "Nothing here: Ctrl+N adds an item" : "Nothing selected", true);
                return;
            case { IsSection: true }:
                OnSectionHeader();
                return;
            case { IsList: false }:
                DetailPane.Select();
                return;
            case var row:
                OpenList(row);
                return;
        }
    }

    /// <summary>Right or Enter on a section's header (IA-2.3a): nothing to go into.</summary>
    private void OnSectionHeader()
    {
        Sounds.Edge();
        Announce("A section: Down goes into it", true);
    }

    /// <summary>
    /// Ctrl+Up, Ctrl+Down (IA-2.3a): the section header before or after the selection, as a screen reader's heading
    /// keys go; the edge sound past the first or the last. NVDA reads the header as the selection moves to it.
    /// </summary>
    protected void JumpSection(int direction)
    {
        var at = Navigator.SelectedIndex;
        for (var i = at + direction; i >= 0 && i < _rows.Count; i += direction)
        {
            if (_rows[i].IsSection)
            {
                Navigator.SelectedIndex = i;
                return;
            }
        }

        Sounds.Edge();
    }

    private void OpenList(ItemRow row)
    {
        EnterList(row);
        Fill(select: null);
        AnnounceLevel();
    }

    /// <summary>Left, Backspace: up a level, the list left selected; at the top, the edge sound.</summary>
    protected void UpLevel()
    {
        if (LeaveList() is not { } select)
        {
            Sounds.Edge();
            return;
        }

        Fill(select);
        AnnounceLevel();
    }

    /// <summary>IA-2.4: "Home, 4 items, Fix the tap".</summary>
    protected void AnnounceLevel()
    {
        var name = Navigator.AccessibleName ?? string.Empty;
        Announce(SelectedRow() is { } row ? $"{name}, {row.Label}" : name, true);
    }

    /// <summary>F5: the level read again, the selection kept.</summary>
    protected void Refresh(bool announce)
    {
        BeforeRefresh();
        Fill(SelectedKey());
        if (announce)
        {
            Announce("Refreshed: " + Navigator.AccessibleName, true);
        }
    }

    /// <summary>
    /// The navigator filled for the level (<see cref="ReadLevel"/>), its name, the title and the status bar; the
    /// selection on the row whose key is <paramref name="select"/>, otherwise the first; then the detail pane.
    /// </summary>
    protected void Fill(string? select)
    {
        var (rows, name, title, place) = ReadLevel();
        _rows = [.. rows];
        Navigator.BeginUpdate();
        Navigator.Items.Clear();
        // S-42: titles from a shared folder or database with their hidden characters made visible.
        Navigator.Items.AddRange(_rows.Select(row => (object)DisplayText.Visible(row.Label)).ToArray());
        Navigator.EndUpdate();
        Navigator.AccessibleName = name;
        Navigator.ResetTypeAhead();
        if (_rows.Count > 0)
        {
            var index = select is null ? 0 : _rows.FindIndex(row => string.Equals(row.Key, select, StringComparison.OrdinalIgnoreCase));
            Navigator.SelectedIndex = Math.Max(0, index);
        }

        Text = title + " - " + _appName;
        _nameLabel.Text = name;
        _placeLabel.Text = place;
        StatusLayout.Fit(_status, _nameLabel, _placeLabel);
        FillDetail();
    }

    /// <summary>The selected row's key, for <see cref="Fill"/>.</summary>
    protected string? SelectedKey() => SelectedRow()?.Key;

    protected ItemRow? SelectedRow() => Navigator.SelectedIndex is var i && i >= 0 && i < _rows.Count ? _rows[i] : null;

    /// <summary>The row to select when the selected one leaves the level: the next, or the one before at the end.</summary>
    protected string? NeighbourKey()
    {
        var i = Navigator.SelectedIndex;
        return i + 1 < _rows.Count ? _rows[i + 1].Key : i - 1 >= 0 ? _rows[i - 1].Key : null;
    }

    private IReadOnlyList<string> NavigatorLabels() => _rows.Select(row => row.Label).ToList();

    /// <summary>"1 item", "3 items".</summary>
    protected static string Count(int count, string noun) => $"{count} {(count == 1 ? noun : noun + "s")}";

    // ---- the detail pane (IA-2.3) ----

    /// <summary>The detail pane filled for the navigator's selection, silently; the field selected before kept when the same thing shows.</summary>
    protected void FillDetail()
    {
        var keep = DetailPane.SelectedIndex;
        var detail = DetailOf(SelectedRow());
        var same = detail.Key is not null && string.Equals(detail.Key, _shown.Key, StringComparison.OrdinalIgnoreCase);
        _shown = detail;
        var lines = new List<(int Field, ItemDetailLine Line)>();
        if (detail.Document is { } document)
        {
            for (var i = 0; i < document.Fields.Count; i++)
            {
                lines.Add((i, new ItemDetailLine(FieldLabel(document.Fields[i]))));
            }
        }

        lines.AddRange(detail.Lines.Select(line => (-1, line)));
        _lines = lines;
        DetailPane.BeginUpdate();
        DetailPane.Items.Clear();
        DetailPane.Items.AddRange(lines.Select(line => (object)DisplayText.Visible(line.Line.Label)).ToArray());
        DetailPane.EndUpdate();
        DetailPane.AccessibleName = detail.Name;
        DetailPane.ResetTypeAhead();
        if (lines.Count > 0)
        {
            DetailPane.SelectedIndex = same ? Math.Clamp(keep, 0, lines.Count - 1) : 0;
        }
    }

    /// <summary>The detail pane's selection on field <paramref name="field"/>, after a change.</summary>
    protected void SelectField(int field)
    {
        var line = _lines.FindIndex(row => row.Field == field);
        if (line >= 0)
        {
            DetailPane.SelectedIndex = line;
        }
    }

    /// <summary>"Due, date, 30 October 2026, Friday" (IA-3.1).</summary>
    protected string FieldLabel(ItemField field) =>
        $"{(field.Title.Length == 0 ? "untitled" : field.Title)}, {SpokenType(field.Type)}, {SpokenValue(SelectedRow(), field)}";

    /// <summary>The field the detail pane's selection is on; null on another line.</summary>
    protected int? SelectedField() =>
        DetailPane.SelectedIndex is var i && i >= 0 && i < _lines.Count && _lines[i].Field >= 0 && _shown.Document is { } document && _lines[i].Field < document.Fields.Count
            ? _lines[i].Field
            : null;

    private ItemDetailLine? SelectedLine() => DetailPane.SelectedIndex is var i && i >= 0 && i < _lines.Count ? _lines[i].Line : null;
}
