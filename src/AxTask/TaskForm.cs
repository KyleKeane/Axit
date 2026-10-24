using AxTask.Core;
using Axit.Core;
using Axit.Forms;

namespace AxTask;

/// <summary>
/// axTask's window (docs/axit/item-apps.md IA-2): a menu bar, the navigator on the left, the detail pane on the right,
/// a status bar. The navigator is one level at a time, as axFile shows a folder: the top level with All tasks, the
/// smart lists, the lists and the items; a list's level; a smart list's items. The detail pane is filled silently for
/// the navigator's selection: an item's fields, a list's details, what a smart list shows. This file holds the panes,
/// the levels and the keys; the commands and the one way a file changes are in <c>TaskForm.Commands.cs</c>.
/// </summary>
public sealed partial class TaskForm : AxitForm
{
    private enum RowKind
    {
        Smart,
        List,
        Item,
    }

    /// <summary>A line of the navigator: what it is, its path (a folder, an item file, a filter file or none for All tasks), its name for type-to-find, its words, and what it holds.</summary>
    private sealed record Row(RowKind Kind, string? Path, string Name, string Label, SmartList? Smart = null, ItemFile? Item = null);

    /// <summary>A line of the detail pane: a field of the document shown (its index), the template line of a list (-1), or a line that only says something (-2).</summary>
    private sealed record DetailRow(int Field, string Label);

    private const int TemplateLine = -1;
    private const int InfoLine = -2;

    private readonly TaskSettings _settings;
    private readonly MenuStrip _menu = new();
    private readonly AxitList _nav = new();
    private readonly AxitList _detail = new();
    private readonly SplitContainer _split = new();
    private readonly StatusStrip _status = new();
    private readonly ToolStripStatusLabel _nameLabel;
    private readonly ToolStripStatusLabel _folderLabel;
    private readonly ToolStripMenuItem _listMenu = new("&List");
    private readonly ToolStripMenuItem _itemMenu = new("&Item");
    private readonly ToolStripMenuItem _fieldMenu = new("Fiel&d");
    private readonly ToolStripMenuItem _hideDoneItem;

    /// <summary>The level shown: a folder (the top or a list), or a smart list's items.</summary>
    private string _folder = string.Empty;

    private SmartList? _smart;
    private List<Row> _rows = [];

    /// <summary>What the detail pane shows: the document, its file (an item, a list's <c>list.json</c>, which may not exist yet), the text it was read from (null when there is no file), and its lines.</summary>
    private ItemDocument? _shown;

    private string? _shownPath;
    private string? _shownText;
    private List<DetailRow> _details = [];

    public TaskForm(string? path, TaskSettings settings, string? settingsError)
        : base(AxTaskApp.Name, settings, TaskSettings.DefaultPath, settingsError)
    {
        _settings = settings;

        // ---- menus; the keys are handled in ProcessWindowKey from the table, the items only show them ----
        // The first menu holds what every pane has; List and Item act on the navigator's selection, Field on the
        // detail pane's, and only the menu of where the focus is and what is selected is enabled (UpdateMenus).
        var file = new ToolStripMenuItem("&Folder");
        file.DropDownItems.Add(new ToolStripMenuItem("Top &folder...", null, (_, _) => ChooseTopFolder()));
        file.DropDownItems.Add(Item("&All tasks", ShowAllTasks, AxTaskKeys.AllTasks));
        file.DropDownItems.Add(Item("&Refresh", () => Refresh(announce: true), AxTaskKeys.Refresh));
        file.DropDownItems.Add(Item("&Undo", Undo, AxTaskKeys.Undo));
        file.DropDownItems.Add(new ToolStripSeparator());
        file.DropDownItems.Add(CreateExitItem());

        _listMenu.DropDownItems.Add(Item("&Open", OpenSelected, AxTaskKeys.Open));
        _listMenu.DropDownItems.Add(Item("New &item...", NewItem, AxTaskKeys.New));
        _listMenu.DropDownItems.Add(Item("New &list...", NewList, AxTaskKeys.NewList));
        _listMenu.DropDownItems.Add(new ToolStripMenuItem("New &smart list...", null, (_, _) => NewSmartList()));
        _listMenu.DropDownItems.Add(Item("&Rename...", Rename, AxTaskKeys.Rename));
        _listMenu.DropDownItems.Add(Item("Ar&chive", Archive, AxTaskKeys.Delete));
        _listMenu.DropDownItems.Add(new ToolStripSeparator());
        _listMenu.DropDownItems.Add(new ToolStripMenuItem("Add &Done to every item", null, (_, _) => SwitchList(done: true)));
        _listMenu.DropDownItems.Add(new ToolStripMenuItem("Remove Do&ne from every item", null, (_, _) => SwitchList(done: false)));
        _listMenu.DropDownItems.Add(new ToolStripSeparator());
        _listMenu.DropDownItems.Add(Item("Open in ax&JSON", OpenInAxJson, AxTaskKeys.OpenInAxJson));
        _listMenu.DropDownItems.Add(Item("Show in ax&File", ShowInAxFile, AxTaskKeys.ShowInAxFile));

        _itemMenu.DropDownItems.Add(Item("&Open its fields", OpenSelected, AxTaskKeys.Open));
        _itemMenu.DropDownItems.Add(Item("&Done or not done", CheckSelected, AxTaskKeys.Check));
        _itemMenu.DropDownItems.Add(Item("&Read in full", ReadAll, AxTaskKeys.ReadAll));
        _itemMenu.DropDownItems.Add(Item("New &item...", NewItem, AxTaskKeys.New));
        _itemMenu.DropDownItems.Add(Item("Rena&me...", Rename, AxTaskKeys.Rename));
        _itemMenu.DropDownItems.Add(Item("Ar&chive", Archive, AxTaskKeys.Delete));
        _itemMenu.DropDownItems.Add(new ToolStripMenuItem("Use these fields for &new items", null, (_, _) => UseAsTemplate()));
        _itemMenu.DropDownItems.Add(new ToolStripSeparator());
        _itemMenu.DropDownItems.Add(Item("Open in ax&JSON", OpenInAxJson, AxTaskKeys.OpenInAxJson));
        _itemMenu.DropDownItems.Add(Item("Show in ax&File", ShowInAxFile, AxTaskKeys.ShowInAxFile));

        _fieldMenu.DropDownItems.Add(Item("&Change...", ChangeField, AxTaskKeys.Open));
        _fieldMenu.DropDownItems.Add(Item("&Done or not done", CheckSelected, AxTaskKeys.Check));
        _fieldMenu.DropDownItems.Add(Item("&Read in full", ReadAll, AxTaskKeys.ReadAll));
        _fieldMenu.DropDownItems.Add(Item("&New field...", NewField, AxTaskKeys.New));
        _fieldMenu.DropDownItems.Add(Item("Ren&ame field...", RenameField, AxTaskKeys.Rename));
        _fieldMenu.DropDownItems.Add(Item("Re&move field", RemoveField, AxTaskKeys.Delete));
        _fieldMenu.DropDownItems.Add(Item("Move &up", () => MoveField(-1), AxTaskKeys.MoveFieldUp));
        _fieldMenu.DropDownItems.Add(Item("Move do&wn", () => MoveField(1), AxTaskKeys.MoveFieldDown));

        var view = new ToolStripMenuItem("&View");
        _hideDoneItem = Item("&Hide done tasks", ToggleHideDone, AxTaskKeys.HideDone);
        _hideDoneItem.Checked = settings.HideDone;
        view.DropDownItems.Add(_hideDoneItem);

        var options = new ToolStripMenuItem("&Options");
        options.DropDownItems.Add(CreateTextMenu());
        options.DropDownItems.Add(CreateGeneralMenu());
        options.DropDownItems.Add(CreateDeveloperMenu());

        var help = CreateHelpMenu(HelpText.UserGuide, "Item &resources", [("&JSON", "https://www.json.org/json-en.html")]);

        _menu.Items.AddRange([file, _listMenu, _itemMenu, _fieldMenu, view, options, CreateAxitMenu(), help]);
        _menu.MenuActivate += (_, _) => UpdateMenus();
        MainMenuStrip = _menu;

        // The two panes (IA-2.1), each the bundle's list: type-to-find with Shift backwards and the edge earcon.
        _nav.Dock = DockStyle.Fill;
        _nav.TabIndex = 0;
        _nav.Names = () => _rows.Select(row => row.Name).ToList();
        _nav.SelectedIndexChanged += (_, _) => FillDetail();
        _detail.Dock = DockStyle.Fill;
        _detail.TabIndex = 1;
        _detail.Names = () => _details.Select(row => row.Field >= 0 && _shown is not null && row.Field < _shown.Fields.Count ? _shown.Fields[row.Field].Title : row.Label).ToList();
        foreach (var list in new[] { _nav, _detail })
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
        _split.Panel1.Controls.Add(_nav);
        _split.Panel2.Controls.Add(_detail);

        (_nameLabel, _folderLabel) = StatusLayout.Pair(_status);
        Controls.AddRange([Overlay, _split, _status, _menu]);

        ApplyTextFont();
        RestoreWindow();
        Start(path);
    }

    // ---- what the window base asks for ----

    protected override string Shortcuts => HelpText.Shortcuts;

    protected override void HideForNotice() => _split.Visible = false;

    protected override void ShowAfterNotice() => _split.Visible = true;

    protected override Control FocusTarget => _detail.Focused ? _detail : _nav;

    protected override void ApplyFont(Font font)
    {
        _nav.Font = font;
        _detail.Font = font;
    }

    protected override IReadOnlyList<string> RestartArguments() => [Dispatch.AxTask, TopFolder];

    protected override IEnumerable<string> DiagnosticsLines => [$"Level: {(_smart is not null ? "smart list" : _folder == TopFolder ? "top" : "list")}, {_rows.Count} rows, {_details.Count} detail lines"];

    /// <summary>Ctrl+S: every change is written at once.</summary>
    protected override void SaveCommand() => Announce("Nothing to save here: every change is written at once", true);

    protected override void OnShown(EventArgs e)
    {
        _split.SplitterDistance = Math.Max(_split.Panel1MinSize, _split.Width * 45 / 100);
        if (!NoticeOpen)
        {
            _nav.Select();
        }

        base.OnShown(e);
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        // AX-9.4: the folder joins the bundle's Recently closed list.
        RecentlyClosed.Remember(AxTaskApp.Name, TopFolder);
        base.OnFormClosing(e);
    }

    // ---- keys (IA-3, TK-7): every key comes from BundleKeys or AxTaskKeys, and is handled here only ----

    protected override bool ProcessWindowKey(Keys keyData)
    {
        switch (keyData)
        {
            case BundleKeys.Find:
                ShowFindInList(_nav, NavNames);
                return true;
            case BundleKeys.FindNext:
                FindInList(_nav, NavNames, 1);
                return true;
            case BundleKeys.FindPrevious:
                FindInList(_nav, NavNames, -1);
                return true;
            case AxTaskKeys.Undo:
                Undo();
                return true;
            case AxTaskKeys.Refresh:
                Refresh(announce: true);
                return true;
            case AxTaskKeys.OpenInAxJson:
                OpenInAxJson();
                return true;
            case AxTaskKeys.ShowInAxFile:
                ShowInAxFile();
                return true;
            case AxTaskKeys.HideDone:
                ToggleHideDone();
                return true;
            case AxTaskKeys.AllTasks:
                ShowAllTasks();
                return true;
            case AxTaskKeys.NewList:
                NewList();
                return true;
            case AxTaskKeys.ReadAll:
                ReadAll();
                return true;
        }

        if (_nav.Focused)
        {
            // The navigator's keys; returning true keeps the list box from its own move and its ding.
            switch (keyData)
            {
                case AxTaskKeys.Into:
                    Into();
                    return true;
                case AxTaskKeys.Up:
                case AxTaskKeys.UpAlso:
                    UpLevel();
                    return true;
                case AxTaskKeys.Open:
                    OpenSelected();
                    return true;
                case AxTaskKeys.OtherPane:
                case AxTaskKeys.OtherPaneBack:
                    _detail.Select();
                    return true;
                case AxTaskKeys.Check:
                    CheckSelected();
                    return true;
                case AxTaskKeys.New:
                    NewItem();
                    return true;
                case AxTaskKeys.Rename:
                    Rename();
                    return true;
                case AxTaskKeys.Delete:
                    Archive();
                    return true;
                case AxTaskKeys.ItemMenu:
                case AxTaskKeys.ItemMenuAlso:
                    OpenPaneMenu();
                    return true;
            }
        }
        else if (_detail.Focused)
        {
            switch (keyData)
            {
                case AxTaskKeys.Open:
                    ChangeField();
                    return true;
                case AxTaskKeys.OtherPane:
                case AxTaskKeys.OtherPaneBack:
                case AxTaskKeys.Back:
                    _nav.Select();
                    return true;
                case AxTaskKeys.Check:
                    CheckSelected();
                    return true;
                case AxTaskKeys.New:
                    NewField();
                    return true;
                case AxTaskKeys.Rename:
                    RenameField();
                    return true;
                case AxTaskKeys.Delete:
                    RemoveField();
                    return true;
                case AxTaskKeys.MoveFieldUp:
                    MoveField(-1);
                    return true;
                case AxTaskKeys.MoveFieldDown:
                    MoveField(1);
                    return true;
                case AxTaskKeys.Into:
                case AxTaskKeys.Up:
                    // Nothing moves sideways in the field list: the edge sound, not the list box's own scroll.
                    Sounds.Edge();
                    return true;
                case AxTaskKeys.ItemMenu:
                case AxTaskKeys.ItemMenuAlso:
                    OpenPaneMenu();
                    return true;
            }
        }

        return false;
    }

    /// <summary>Shift+F10, the Applications key, a right click: the menu of the selection, List, Item or Field.</summary>
    private void OpenPaneMenu()
    {
        UpdateMenus();
        OpenBarMenu(_detail.Focused ? 'D' : SelectedRow()?.Kind == RowKind.Item ? 'I' : 'L');
    }

    /// <summary>The menus that act on the selection enabled, the others not: Field in the detail pane, Item on an item, List otherwise.</summary>
    private void UpdateMenus()
    {
        var inDetail = _detail.Focused && _shown is not null;
        var onItem = SelectedRow()?.Kind == RowKind.Item;
        _fieldMenu.Enabled = inDetail;
        _itemMenu.Enabled = !inDetail && onItem;
        _listMenu.Enabled = !inDetail && !onItem;
    }

    // ---- the levels (IA-2.2) ----

    private string TopFolder => _settings.TopFolder;

    private bool AtTop => _smart is null && _folder.Equals(TopFolder, StringComparison.OrdinalIgnoreCase);

    private static DateOnly Today => DateOnly.FromDateTime(DateTime.Now);

    /// <summary>A folder opens as the top; an item's file opens its list with the item selected; nothing opens the last run's list.</summary>
    private void Start(string? path)
    {
        string? select = null;
        if (path is not null && Directory.Exists(path))
        {
            _settings.Folder = Path.GetFullPath(path);
            _folder = TopFolder;
        }
        else if (path is not null && File.Exists(path))
        {
            var full = Path.GetFullPath(path);
            var folder = Path.GetDirectoryName(full)!;
            if (!folder.Equals(TopFolder, StringComparison.OrdinalIgnoreCase) && !folder.StartsWith(TopFolder + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            {
                _settings.Folder = folder;
            }

            _folder = folder;
            select = full;
        }
        else
        {
            _folder = _settings.LastList is { } last && Directory.Exists(Path.Combine(TopFolder, last)) ? Path.Combine(TopFolder, last) : TopFolder;
        }

        SaveSettings();
        Fill(select);
    }

    /// <summary>Right: into a list or a smart list; on an item, nothing but the edge sound (IA-3, axFile's Right on a file).</summary>
    private void Into()
    {
        if (SelectedRow() is { Kind: RowKind.List or RowKind.Smart } row)
        {
            OpenRow(row);
        }
        else
        {
            Sounds.Edge();
        }
    }

    /// <summary>Enter (IA-D2): a list or a smart list opens, an item's fields take the focus.</summary>
    private void OpenSelected()
    {
        switch (SelectedRow())
        {
            case null:
                Announce(_rows.Count == 0 ? "Nothing here: Ctrl+N adds an item" : "Nothing selected", true);
                return;
            case { Kind: RowKind.Item }:
                _detail.Select();
                return;
            case var row:
                OpenRow(row);
                return;
        }
    }

    private void OpenRow(Row row)
    {
        if (row.Kind == RowKind.Smart)
        {
            _smart = row.Smart;
        }
        else
        {
            _smart = null;
            _folder = row.Path!;
        }

        RememberLevel();
        Fill(select: null);
        AnnounceLevel();
    }

    /// <summary>Left, Backspace: up a level, the list or smart list left selected; at the top, the edge sound.</summary>
    private void UpLevel()
    {
        if (AtTop)
        {
            Sounds.Edge();
            return;
        }

        string? select;
        if (_smart is not null)
        {
            select = _smart.Path ?? SmartList.AllTasksName;
            _smart = null;
            _folder = TopFolder;
        }
        else
        {
            select = _folder;
            _folder = Path.GetDirectoryName(_folder) ?? TopFolder;
        }

        RememberLevel();
        Fill(select);
        AnnounceLevel();
    }

    /// <summary>Ctrl+1: All tasks from anywhere.</summary>
    private void ShowAllTasks()
    {
        _smart = SmartList.AllTasks;
        _folder = TopFolder;
        Fill(select: null);
        _nav.Select();
        AnnounceLevel();
    }

    /// <summary>IA-2.4: "Home, 4 items, Fix the tap".</summary>
    private void AnnounceLevel()
    {
        var name = _nav.AccessibleName ?? string.Empty;
        Announce(SelectedRow() is { } row ? $"{name}, {row.Label}" : name, true);
    }

    private void RememberLevel()
    {
        _settings.LastList = _smart is null && !AtTop && _folder.StartsWith(TopFolder + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)
            ? Path.GetRelativePath(TopFolder, _folder)
            : null;
        SaveSettings();
    }

    /// <summary>F5: the level read again, the selection kept.</summary>
    private void Refresh(bool announce)
    {
        if (_smart?.Path is { } filter && File.Exists(filter))
        {
            _smart = SmartList.Read(filter);
        }

        Fill(SelectedKey());
        if (announce)
        {
            Announce("Refreshed: " + _nav.AccessibleName, true);
        }
    }

    /// <summary>
    /// The navigator filled for the level, its name, the title and the status bar; the selection on the row whose
    /// path (or All tasks' name) is <paramref name="select"/>, otherwise the first; then the detail pane.
    /// </summary>
    private void Fill(string? select)
    {
        var today = Today;
        var rows = new List<Row>();
        string name;
        if (!Directory.Exists(_folder) && !_folder.Equals(TopFolder, StringComparison.OrdinalIgnoreCase))
        {
            // A list gone from disk: back to the top.
            _folder = TopFolder;
            _smart = null;
        }

        if (_smart is { } smart)
        {
            var items = smart.Items(TopFolder, today);
            foreach (var item in items)
            {
                var itemName = ItemFolder.NameOf(item);
                rows.Add(new Row(RowKind.Item, item.Path, itemName, itemName + TaskRules.Summary(item.Document, today) + ", in " + ItemFolder.ListName(TopFolder, Path.GetDirectoryName(item.Path)!), Item: item));
            }

            name = smart.NotUnderstood is { } why ? $"{smart.Name}, a condition not understood: {why}" : $"{smart.Name}, {Count(items.Count, "item")}";
        }
        else
        {
            var top = AtTop;
            if (top)
            {
                var all = ItemFolder.AllItems(TopFolder);
                rows.Add(SmartRow(SmartList.AllTasks, all, today));
                foreach (var filter in SmartList.InFolder(TopFolder))
                {
                    rows.Add(SmartRow(filter, all, today));
                }
            }

            var level = ItemFolder.Read(_folder, top);
            foreach (var list in level.Lists)
            {
                var inside = ItemFolder.Read(list, isTop: false);
                var listName = Path.GetFileName(list);
                rows.Add(new Row(RowKind.List, list, listName, $"{listName}, list, {Count(inside.Lists.Count + inside.Items.Count, "item")}"));
            }

            var hidden = 0;
            foreach (var item in level.Items)
            {
                if (_settings.HideDone && TaskRules.IsDone(item.Document))
                {
                    hidden++;
                    continue;
                }

                var itemName = ItemFolder.NameOf(item);
                rows.Add(new Row(RowKind.Item, item.Path, itemName, itemName + TaskRules.Summary(item.Document, today), Item: item));
            }

            var shownCount = rows.Count(row => row.Kind != RowKind.Smart);
            name = (top ? "Lists" : Path.GetFileName(_folder)) + ", " + (shownCount == 0 && !top ? "empty: Ctrl+N adds an item" : Count(shownCount, "item"));
            if (hidden > 0)
            {
                name += $", {hidden} done hidden";
            }

            if (level.NotShown > 0)
            {
                name += $", {Count(level.NotShown, "file")} not shown";
            }
        }

        _rows = rows;
        _nav.BeginUpdate();
        _nav.Items.Clear();
        _nav.Items.AddRange(rows.Select(row => (object)row.Label).ToArray());
        _nav.EndUpdate();
        _nav.AccessibleName = name;
        _nav.ResetTypeAhead();
        if (rows.Count > 0)
        {
            var index = select is null ? 0 : rows.FindIndex(row => string.Equals(row.Path ?? row.Name, select, StringComparison.OrdinalIgnoreCase));
            _nav.SelectedIndex = Math.Max(0, index);
        }

        var levelName = _smart?.Name ?? (AtTop ? "Lists" : ItemFolder.ListName(TopFolder, _folder));
        Text = levelName + " - " + AxTaskApp.Name;
        _nameLabel.Text = name;
        _folderLabel.Text = TopFolder;
        StatusLayout.Fit(_status, _nameLabel, _folderLabel);
        FillDetail();
    }

    private Row SmartRow(SmartList smart, IReadOnlyList<ItemFile> all, DateOnly today)
    {
        var count = smart.NotUnderstood is null ? all.Count(item => smart.Matches(item, TopFolder, today)) : 0;
        var label = smart.Path is null
            ? $"{smart.Name}, {count} not done"
            : smart.NotUnderstood is null ? $"{smart.Name}, smart list, {count}" : $"{smart.Name}, smart list, a condition not understood";
        return new Row(RowKind.Smart, smart.Path, smart.Name, label, Smart: smart);
    }

    /// <summary>The selected row's key for <see cref="Fill"/>: its path, or All tasks' name.</summary>
    private string? SelectedKey() => SelectedRow() is { } row ? row.Path ?? row.Name : null;

    private Row? SelectedRow() => _nav.SelectedIndex is var i && i >= 0 && i < _rows.Count ? _rows[i] : null;

    private IReadOnlyList<string> NavNames() => _rows.Select(row => row.Label).ToList();

    private static string Count(int count, string noun) => $"{count} {(count == 1 ? noun : noun + "s")}";

    // ---- the detail pane (IA-2.3) ----

    /// <summary>The detail pane filled for the navigator's selection, silently; the field selected before kept when the same document shows.</summary>
    private void FillDetail()
    {
        var row = SelectedRow();
        var keep = _detail.SelectedIndex;
        var samePath = row is not null && _shownPath is not null && string.Equals(DetailPathOf(row), _shownPath, StringComparison.OrdinalIgnoreCase);
        _shown = null;
        _shownPath = null;
        _shownText = null;
        var lines = new List<DetailRow>();
        string name;
        switch (row)
        {
            case null:
                name = "Details, nothing selected";
                lines.Add(new DetailRow(InfoLine, _rows.Count == 0 ? "Nothing in this list: Ctrl+N in the navigator adds an item" : "Nothing selected"));
                break;
            case { Kind: RowKind.Item, Item: { } item }:
                _shown = item.Document;
                _shownPath = item.Path;
                _shownText = item.Text;
                name = "Fields of " + row.Name;
                AddFieldLines(lines);
                break;
            case { Kind: RowKind.List }:
                var listFile = Path.Combine(row.Path!, ItemDocument.ListFile);
                var read = ItemFolder.ReadItem(listFile);
                _shown = read?.Document ?? ItemDocument.NewList(TaskRules.DefaultTemplate);
                _shownPath = listFile;
                _shownText = read?.Text;
                name = "Details of " + row.Name;
                AddFieldLines(lines);
                var template = TaskRules.TemplateOf(read?.Document);
                lines.Add(new DetailRow(TemplateLine, template.Count == 0
                    ? "Fields of a new item: only its title, a plain list"
                    : "Fields of a new item: " + string.Join(", ", template.Select(field => field.Title))));
                break;
            default:
                var smart = row.Smart!;
                name = "Details of " + smart.Name;
                lines.Add(new DetailRow(InfoLine, smart.Path is null ? "Every task not done, in every list but the Archive, the soonest due first" : "A smart list: the items its conditions hold for"));
                if (smart.NotUnderstood is { } why)
                {
                    lines.Add(new DetailRow(InfoLine, "A condition not understood: " + why));
                }

                lines.Add(new DetailRow(InfoLine, smart.Path is null ? "Built in; a smart list of your own is List, New smart list" : $"Its file: {Path.GetFileName(smart.Path)}; Ctrl+J changes it in axJSON"));
                break;
        }

        _details = lines;
        _detail.BeginUpdate();
        _detail.Items.Clear();
        _detail.Items.AddRange(lines.Select(line => (object)line.Label).ToArray());
        _detail.EndUpdate();
        _detail.AccessibleName = name;
        _detail.ResetTypeAhead();
        if (lines.Count > 0)
        {
            _detail.SelectedIndex = samePath ? Math.Clamp(keep, 0, lines.Count - 1) : 0;
        }
    }

    private void AddFieldLines(List<DetailRow> lines)
    {
        var fields = _shown!.Fields;
        for (var i = 0; i < fields.Count; i++)
        {
            lines.Add(new DetailRow(i, FieldLabel(fields[i])));
        }
    }

    /// <summary>"Due, date, 30 October 2026, Friday" (IA-3.1).</summary>
    private string FieldLabel(ItemField field)
    {
        var value = FieldTypes.Spoken(field, link => _shownPath is { } from ? Links.Spoken(from, link) : link);
        return $"{(field.Title.Length == 0 ? "untitled" : field.Title)}, {FieldTypes.Spoken(field.Type)}, {value}";
    }

    /// <summary>The file the detail pane shows for a row: an item's own, a list's <c>list.json</c>, none for a smart list.</summary>
    private static string? DetailPathOf(Row row) => row.Kind switch
    {
        RowKind.Item => row.Path,
        RowKind.List => Path.Combine(row.Path!, ItemDocument.ListFile),
        _ => null,
    };

    private DetailRow? SelectedDetail() => _detail.SelectedIndex is var i && i >= 0 && i < _details.Count ? _details[i] : null;

    /// <summary>The field the detail pane's selection is on; null on another line.</summary>
    private int? SelectedField() => SelectedDetail() is { Field: >= 0 } line && _shown is not null && line.Field < _shown.Fields.Count ? line.Field : null;
}
