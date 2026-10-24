using AxTask.Core;
using Axit.Core;
using Axit.Forms;

namespace AxTask;

/// <summary>
/// axTask's window (docs/axit/item-apps.md IA-2), on the item apps' shared window <see cref="ItemForm"/>, which holds
/// the panes, the moves between levels, the shared keys and every field command. This class holds what is axTask's:
/// the levels of a folder of lists (the top with All tasks and the smart lists, a list, a smart list's items), what
/// the detail pane shows for each, its menus and its own keys. The commands and the one way a file changes are in
/// <c>TaskForm.Commands.cs</c>.
/// </summary>
public sealed partial class TaskForm : ItemForm
{
    /// <summary>What the detail pane's document is saved to: a file and the text it was read from (null when it does not exist yet, a list's first <c>list.json</c>).</summary>
    private sealed record FileSource(string Path, string? Text);

    private readonly TaskSettings _settings;
    private readonly MenuStrip _menu = new();
    private readonly ToolStripMenuItem _listMenu = new("&List");
    private readonly ToolStripMenuItem _itemMenu = new("&Item");
    private readonly ToolStripMenuItem _fieldMenu = new("Fiel&d");
    private readonly ToolStripMenuItem _hideDoneItem;

    /// <summary>The level shown: a folder (the top or a list), or a smart list's items.</summary>
    private string _folder = string.Empty;

    private SmartList? _smart;

    public TaskForm(string? path, TaskSettings settings, string? settingsError)
        : base(AxTaskApp.Name, settings, TaskSettings.DefaultPath, settingsError)
    {
        _settings = settings;

        // ---- menus; the keys are handled in ProcessWindowKey from the tables, the items only show them ----
        // The first menu holds what every pane has; List and Item act on the navigator's selection, Field on the
        // detail pane's, and only the menu of where the focus is and what is selected is enabled (UpdateRowMenus).
        var file = new ToolStripMenuItem("&Folder");
        file.DropDownItems.Add(new ToolStripMenuItem("Top &folder...", null, (_, _) => ChooseTopFolder()));
        file.DropDownItems.Add(Item("&All tasks", ShowAllTasks, AxTaskKeys.AllTasks));
        file.DropDownItems.Add(Item("&Refresh", () => Refresh(announce: true), ItemKeys.Refresh));
        file.DropDownItems.Add(Item("&Undo", Undo, ItemKeys.Undo));
        file.DropDownItems.Add(new ToolStripSeparator());
        file.DropDownItems.Add(CreateExitItem());

        _listMenu.DropDownItems.Add(Item("&Open", OpenSelected, ItemKeys.Open));
        _listMenu.DropDownItems.Add(Item("New &item...", NewItem, ItemKeys.New));
        _listMenu.DropDownItems.Add(Item("New &list...", NewList, ItemKeys.NewList));
        _listMenu.DropDownItems.Add(new ToolStripMenuItem("New &smart list...", null, (_, _) => NewSmartList()));
        _listMenu.DropDownItems.Add(Item("&Rename...", () => WithRow(RenameRow), ItemKeys.Rename));
        _listMenu.DropDownItems.Add(Item("Ar&chive", () => WithRow(DeleteRow), ItemKeys.Delete));
        _listMenu.DropDownItems.Add(new ToolStripSeparator());
        _listMenu.DropDownItems.Add(new ToolStripMenuItem("Add &Done to every item", null, (_, _) => SwitchList(done: true)));
        _listMenu.DropDownItems.Add(new ToolStripMenuItem("Remove Do&ne from every item", null, (_, _) => SwitchList(done: false)));
        _listMenu.DropDownItems.Add(new ToolStripSeparator());
        _listMenu.DropDownItems.Add(Item("Open in ax&JSON", OpenInAxJson, AxTaskKeys.OpenInAxJson));
        _listMenu.DropDownItems.Add(Item("Show in ax&File", ShowInAxFile, AxTaskKeys.ShowInAxFile));

        _itemMenu.DropDownItems.Add(Item("&Open its fields", OpenSelected, ItemKeys.Open));
        _itemMenu.DropDownItems.Add(Item("&Done or not done", () => CheckInNavigator(SelectedRow()), ItemKeys.Check));
        _itemMenu.DropDownItems.Add(Item("&Read in full", ReadAll, ItemKeys.ReadAll));
        _itemMenu.DropDownItems.Add(Item("New &item...", NewItem, ItemKeys.New));
        _itemMenu.DropDownItems.Add(Item("Rena&me...", () => WithRow(RenameRow), ItemKeys.Rename));
        _itemMenu.DropDownItems.Add(Item("Ar&chive", () => WithRow(DeleteRow), ItemKeys.Delete));
        _itemMenu.DropDownItems.Add(new ToolStripMenuItem("Use these fields for &new items", null, (_, _) => UseAsTemplate()));
        _itemMenu.DropDownItems.Add(new ToolStripSeparator());
        _itemMenu.DropDownItems.Add(Item("Open in ax&JSON", OpenInAxJson, AxTaskKeys.OpenInAxJson));
        _itemMenu.DropDownItems.Add(Item("Show in ax&File", ShowInAxFile, AxTaskKeys.ShowInAxFile));

        _fieldMenu.DropDownItems.Add(Item("&Change...", ChangeField, ItemKeys.Open));
        _fieldMenu.DropDownItems.Add(Item("&Done or not done", CheckField, ItemKeys.Check));
        _fieldMenu.DropDownItems.Add(Item("&Read in full", ReadAll, ItemKeys.ReadAll));
        _fieldMenu.DropDownItems.Add(Item("&New field...", NewField, ItemKeys.New));
        _fieldMenu.DropDownItems.Add(Item("Ren&ame field...", RenameField, ItemKeys.Rename));
        _fieldMenu.DropDownItems.Add(Item("Re&move field", RemoveField, ItemKeys.Delete));
        _fieldMenu.DropDownItems.Add(Item("Move &up", () => MoveField(-1), ItemKeys.MoveFieldUp));
        _fieldMenu.DropDownItems.Add(Item("Move do&wn", () => MoveField(1), ItemKeys.MoveFieldDown));

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
        PlaceControls(_menu);

        ApplyTextFont();
        RestoreWindow();
        Start(path);
    }

    // ---- what the window base asks for ----

    protected override string Shortcuts => HelpText.Shortcuts;

    protected override IReadOnlyList<string> RestartArguments() => [Dispatch.AxTask, TopFolder];

    protected override IEnumerable<string> DiagnosticsLines => [$"Level: {(_smart is not null ? "smart list" : AtTop ? "top" : "list")}, {Rows.Count} rows"];

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        // AX-9.4: the folder joins the bundle's Recently closed list.
        RecentlyClosed.Remember(AxTaskApp.Name, TopFolder);
        base.OnFormClosing(e);
    }

    /// <summary>axTask's own keys (TK-7), asked before the shared ones.</summary>
    protected override bool ProcessItemKey(Keys keyData)
    {
        switch (keyData)
        {
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
        }

        return false;
    }

    protected override char MenuLetter(bool inDetail, ItemRow? row) => inDetail ? 'D' : row is { IsList: false } ? 'I' : 'L';

    protected override void UpdateRowMenus(bool inDetail, ItemRow? row)
    {
        var onItem = row is { IsList: false };
        _fieldMenu.Enabled = inDetail;
        _itemMenu.Enabled = !inDetail && onItem;
        _listMenu.Enabled = !inDetail && !onItem;
    }

    /// <summary>A link reads the item it points to, or "missing: name" (IA-D3).</summary>
    protected override string SpokenValue(ItemRow? row, ItemField field) =>
        FieldTypes.Spoken(field, link => ShownDetail.Source is FileSource source ? Links.Spoken(source.Path, link) : link);

    protected override void BeforeRefresh()
    {
        if (_smart?.Path is { } filter && File.Exists(filter))
        {
            _smart = SmartList.Read(filter);
        }
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

    protected override void EnterList(ItemRow row)
    {
        if (row.Tag is SmartList smart)
        {
            _smart = smart;
        }
        else
        {
            _smart = null;
            _folder = row.Key;
        }

        RememberLevel();
    }

    protected override string? LeaveList()
    {
        if (AtTop)
        {
            return null;
        }

        string select;
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
        return select;
    }

    /// <summary>Ctrl+1: All tasks from anywhere.</summary>
    private void ShowAllTasks()
    {
        _smart = SmartList.AllTasks;
        _folder = TopFolder;
        Fill(select: null);
        Navigator.Select();
        AnnounceLevel();
    }

    private void RememberLevel()
    {
        _settings.LastList = _smart is null && !AtTop && _folder.StartsWith(TopFolder + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)
            ? Path.GetRelativePath(TopFolder, _folder)
            : null;
        SaveSettings();
    }

    protected override (IReadOnlyList<ItemRow> Rows, string Name, string Title, string Place) ReadLevel()
    {
        var today = Today;
        var rows = new List<ItemRow>();
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
                rows.Add(new ItemRow(false, item.Path, itemName, itemName + TaskRules.Summary(item.Document, today) + ", in " + ItemFolder.ListName(TopFolder, Path.GetDirectoryName(item.Path)!), item));
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
                rows.Add(new ItemRow(true, list, listName, $"{listName}, list, {Count(inside.Lists.Count + inside.Items.Count, "item")}"));
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
                rows.Add(new ItemRow(false, item.Path, itemName, itemName + TaskRules.Summary(item.Document, today), item));
            }

            var shownCount = rows.Count(row => row.Tag is not SmartList);
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

        var title = _smart?.Name ?? (AtTop ? "Lists" : ItemFolder.ListName(TopFolder, _folder));
        return (rows, name, title, TopFolder);
    }

    private ItemRow SmartRow(SmartList smart, IReadOnlyList<ItemFile> all, DateOnly today)
    {
        var count = smart.NotUnderstood is null ? all.Count(item => smart.Matches(item, TopFolder, today)) : 0;
        var label = smart.Path is null
            ? $"{smart.Name}, {count} not done"
            : smart.NotUnderstood is null ? $"{smart.Name}, smart list, {count}" : $"{smart.Name}, smart list, a condition not understood";
        return new ItemRow(true, smart.Path ?? smart.Name, smart.Name, label, smart);
    }

    // ---- the detail pane (IA-2.3) ----

    protected override ItemDetail DetailOf(ItemRow? row)
    {
        switch (row)
        {
            case null:
                return new ItemDetail("Details, nothing selected", null, null,
                    [new ItemDetailLine(Rows.Count == 0 ? "Nothing in this list: Ctrl+N in the navigator adds an item" : "Nothing selected")]);
            case { Tag: ItemFile item }:
                return new ItemDetail("Fields of " + row.Name, item.Path, item.Document, [], new FileSource(item.Path, item.Text));
            case { Tag: SmartList smart }:
                var lines = new List<ItemDetailLine>
                {
                    new(smart.Path is null ? "Every task not done, in every list but the Archive, the soonest due first" : "A smart list: the items its conditions hold for"),
                };
                if (smart.NotUnderstood is { } why)
                {
                    lines.Add(new ItemDetailLine("A condition not understood: " + why));
                }

                lines.Add(new ItemDetailLine(smart.Path is null ? "Built in; a smart list of your own is List, New smart list" : $"Its file: {Path.GetFileName(smart.Path)}; Ctrl+J changes it in axJSON"));
                return new ItemDetail("Details of " + smart.Name, null, null, lines);
            default:
                var listFile = Path.Combine(row.Key, ItemDocument.ListFile);
                var read = ItemFolder.ReadItem(listFile);
                var template = TaskRules.TemplateOf(read?.Document);
                var templateLine = new ItemDetailLine(template.Count == 0
                    ? "Fields of a new item: only its title, a plain list"
                    : "Fields of a new item: " + string.Join(", ", template.Select(field => field.Title)), ExplainTemplate);
                return new ItemDetail("Details of " + row.Name, listFile, read?.Document ?? ItemDocument.NewList(TaskRules.DefaultTemplate), [templateLine], new FileSource(listFile, read?.Text));
        }
    }

    /// <summary>Enter on a list's "Fields of a new item" line: how they change.</summary>
    private void ExplainTemplate() =>
        ShowNotice(Notice.Plain("Fields of a new item", (ShownDetail.Lines.FirstOrDefault()?.Label ?? string.Empty) + ".\r\n\r\nTo change them: select an item with the fields new items should get, then Item, Use these fields for new items. List, Add Done to every item or Remove Done from every item makes the list a checklist or a plain list. Ctrl+J opens the list's details in axJSON."));
}
