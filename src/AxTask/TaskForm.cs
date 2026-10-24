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

        // ---- menus and keys: one command list (docs/axit/app-kit.md AK-2), each key from the tables (TK-7) ----
        // The shared keys (ItemKeys) are ItemForm's commands, registered after these by PlaceControls; the items here
        // for them only show them. A key in a second menu is an item showing it too: its command is the first menu's.
        // The first menu holds what every pane has; List and Item act on the navigator's selection, Field on the
        // detail pane's, and only the menu of where the focus is and what is selected is enabled (UpdateRowMenus).
        var file = new ToolStripMenuItem("&Folder");
        file.DropDownItems.Add(new ToolStripMenuItem("Top &folder...", null, (_, _) => ChooseTopFolder()));
        Add(file, new Command("&All tasks", AxTaskKeys.AllTasks, ShowAllTasks) { When = NotRenaming, Mark = CommandMark.Open });
        file.DropDownItems.Add(Item("&Refresh", () => Refresh(announce: true), ItemKeys.Refresh));
        file.DropDownItems.Add(Item("&Undo", Undo, ItemKeys.Undo));
        file.DropDownItems.Add(new ToolStripSeparator());
        file.DropDownItems.Add(CreateExitItem());

        _listMenu.DropDownItems.Add(Item("&Open", OpenSelected, ItemKeys.Open));
        _listMenu.DropDownItems.Add(Item("New &item...", NewItem, ItemKeys.New));
        _listMenu.DropDownItems.Add(Item("New &list...", NewList, ItemKeys.NewList));
        _listMenu.DropDownItems.Add(new ToolStripMenuItem("New &smart list...", null, (_, _) => NewSmartList()));
        _listMenu.DropDownItems.Add(Item("&Rename", RenameSelected, ItemKeys.Rename));
        _listMenu.DropDownItems.Add(Item("Ar&chive", () => WithRow(DeleteRow), ItemKeys.Delete));
        Add(_listMenu, new Command("Move to s&ection...", AxTaskKeys.MoveToSection, MoveToSection) { When = InNavigator });
        _listMenu.DropDownItems.Add(Item("Pre&vious section", () => JumpSection(-1), ItemKeys.PreviousSection));
        _listMenu.DropDownItems.Add(Item("Ne&xt section", () => JumpSection(1), ItemKeys.NextSection));
        _listMenu.DropDownItems.Add(new ToolStripSeparator());
        _listMenu.DropDownItems.Add(new ToolStripMenuItem("Add &Done to every item", null, (_, _) => SwitchList(done: true)));
        _listMenu.DropDownItems.Add(new ToolStripMenuItem("Remove Do&ne from every item", null, (_, _) => SwitchList(done: false)));
        _listMenu.DropDownItems.Add(new ToolStripSeparator());
        Add(_listMenu, new Command("Open in ax&JSON", AxTaskKeys.OpenInAxJson, OpenInAxJson) { When = NotRenaming, Mark = CommandMark.Open });
        Add(_listMenu, new Command("Show in ax&File", AxTaskKeys.ShowInAxFile, ShowInAxFile) { When = NotRenaming, Mark = CommandMark.Open });

        _itemMenu.DropDownItems.Add(Item("&Open its fields", OpenSelected, ItemKeys.Open));
        _itemMenu.DropDownItems.Add(Item("&Done or not done", () => CheckInNavigator(SelectedRow()), ItemKeys.Check));
        _itemMenu.DropDownItems.Add(Item("&Read in full", ReadAll, ItemKeys.ReadAll));
        _itemMenu.DropDownItems.Add(Item("New &item...", NewItem, ItemKeys.New));
        _itemMenu.DropDownItems.Add(Item("Rena&me", RenameSelected, ItemKeys.Rename));
        _itemMenu.DropDownItems.Add(Item("Ar&chive", () => WithRow(DeleteRow), ItemKeys.Delete));
        _itemMenu.DropDownItems.Add(new ToolStripMenuItem("Use these fields for &new items", null, (_, _) => UseAsTemplate()));
        _itemMenu.DropDownItems.Add(new ToolStripSeparator());
        _itemMenu.DropDownItems.Add(Item("Open in ax&JSON", OpenInAxJson, AxTaskKeys.OpenInAxJson));
        _itemMenu.DropDownItems.Add(Item("Show in ax&File", ShowInAxFile, AxTaskKeys.ShowInAxFile));

        _fieldMenu.DropDownItems.Add(Item("&Change...", ChangeField, ItemKeys.Open));
        _fieldMenu.DropDownItems.Add(Item("&Done or not done", CheckField, ItemKeys.Check));
        _fieldMenu.DropDownItems.Add(Item("&Read in full", ReadAll, ItemKeys.ReadAll));
        _fieldMenu.DropDownItems.Add(Item("&New field...", NewField, ItemKeys.New));
        _fieldMenu.DropDownItems.Add(Item("Ren&ame field", RenameField, ItemKeys.Rename));
        _fieldMenu.DropDownItems.Add(Item("Re&move field", RemoveField, ItemKeys.Delete));
        _fieldMenu.DropDownItems.Add(Item("Move &up", () => MoveField(-1), ItemKeys.MoveFieldUp));
        _fieldMenu.DropDownItems.Add(Item("Move do&wn", () => MoveField(1), ItemKeys.MoveFieldDown));

        var view = new ToolStripMenuItem("&View");
        _hideDoneItem = Add(view, new Command("&Hide done tasks", AxTaskKeys.HideDone, ToggleHideDone) { When = NotRenaming });
        _hideDoneItem.Checked = settings.HideDone;

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

    /// <summary>The folder of lists, for Recently closed (AX-9.4).</summary>
    protected override string? PathInHand => TopFolder;

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
            // One level of lists (TK-D4): out of any list straight to the sections.
            select = _folder;
            _folder = TopFolder;
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
        else if (AtTop)
        {
            name = ReadSections(rows, today);
        }
        else
        {
            // A list's level: its items only (TK-D4).
            var level = ItemFolder.Read(_folder);
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

            name = ListTitle() + ", " + (rows.Count == 0 && hidden == 0 ? "empty: Ctrl+N adds an item" : Count(rows.Count, "item"));
            if (hidden > 0)
            {
                name += $", {hidden} done hidden";
            }

            if (level.NotShown > 0)
            {
                name += $", {Count(level.NotShown, "file")} not shown";
            }
        }

        var title = _smart?.Name ?? (AtTop ? "Lists" : ListTitle());
        return (rows, name, title, TopFolder);
    }

    /// <summary>"Home"; the Archive's own items are "Archived items".</summary>
    private string ListTitle() => IsArchiveFolder(_folder) ? ArchivedItemsName : ItemFolder.ListName(TopFolder, _folder);

    private const string ArchivedItemsName = "Archived items";

    /// <summary>A section's header row (TK-3a.1): its name, and whether it is one of the user's own.</summary>
    private sealed record SectionTag(string Name, bool Custom);

    /// <summary>The Tag of the "Archived items" row, which opens the Archive's own items.</summary>
    private static readonly object ArchivedItemsTag = new();

    /// <summary>
    /// The top level in sections (TK-3a.1): Smart lists, User lists, the user's own sections by name, Archived lists;
    /// each under a header row. The navigator's name is returned.
    /// </summary>
    private string ReadSections(List<ItemRow> rows, DateOnly today)
    {
        var top = ItemFolder.ReadTop(TopFolder);
        var all = top.Lists.SelectMany(list => ItemFolder.Read(list).Items).ToList();
        var smart = new List<ItemRow> { SmartRow(SmartList.AllTasks, all, today) };
        smart.AddRange(SmartList.InFolder(TopFolder).Select(filter => SmartRow(filter, all, today)));
        AddSection(rows, "Smart lists", custom: false, smart, "smart list");

        var bySection = top.Lists.GroupBy(ItemFolder.SectionOf, StringComparer.OrdinalIgnoreCase).ToDictionary(group => group.Key, group => group.ToList(), StringComparer.OrdinalIgnoreCase);
        var plain = bySection.GetValueOrDefault(string.Empty) ?? [];
        if (plain.Count == 0)
        {
            rows.Add(new ItemRow(false, SectionKey("User lists"), "User lists", "User lists, section, none: Ctrl+Shift+N makes one", new SectionTag("User lists", false), IsSection: true));
        }
        else
        {
            AddSection(rows, "User lists", custom: false, plain.Select(ListRow).ToList(), "list");
        }

        foreach (var section in bySection.Keys.Where(key => key.Length > 0).OrderBy(key => key, Axit.Core.NaturalOrder.Instance))
        {
            AddSection(rows, section, custom: true, bySection[section].Select(ListRow).ToList(), "list");
        }

        if (top.ArchivedLists.Count > 0 || top.ArchivedItems > 0)
        {
            var archived = top.ArchivedLists.Select(ListRow).ToList();
            if (top.ArchivedItems > 0)
            {
                archived.Add(new ItemRow(true, ItemFolder.ArchiveFolder(TopFolder), ArchivedItemsName, $"{ArchivedItemsName}, {top.ArchivedItems}", ArchivedItemsTag));
            }

            AddSection(rows, "Archived lists", custom: false, archived, "list");
        }

        var name = $"Lists, {Count(top.Lists.Count, "list")}";
        return top.NotShown > 0 ? name + $", {Count(top.NotShown, "file")} not shown" : name;
    }

    private static void AddSection(List<ItemRow> rows, string name, bool custom, IReadOnlyList<ItemRow> inside, string noun)
    {
        rows.Add(new ItemRow(false, SectionKey(name), name, $"{name}, section, {Count(inside.Count, noun)}", new SectionTag(name, custom), IsSection: true));
        rows.AddRange(inside);
    }

    private static string SectionKey(string name) => "section:" + name;

    /// <summary>"Home, list, 2 items".</summary>
    private static ItemRow ListRow(string list)
    {
        var listName = Path.GetFileName(list);
        return new ItemRow(true, list, listName, $"{listName}, list, {Count(ItemFolder.Read(list).Items.Count, "item")}");
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
            case { Tag: SectionTag section }:
                return new ItemDetail("Details of " + section.Name, row.Key, null,
                [
                    new ItemDetailLine(row.Label),
                    new ItemDetailLine(section.Name switch
                    {
                        "Smart lists" => "All tasks and your saved filters; List, New smart list makes one",
                        "Archived lists" => "What Delete moved away; Ctrl+Z brings back the last, axFile deletes for good",
                        "User lists" => "Lists in no section of your own; Ctrl+M on a list moves it to a section",
                        _ => "A section of your own: a list joins it by its Section field, or Ctrl+M",
                    }),
                ]);
            case { Tag: var tag } when ReferenceEquals(tag, ArchivedItemsTag):
                return new ItemDetail("Details of " + ArchivedItemsName, row.Key, null,
                    [new ItemDetailLine(row.Label), new ItemDetailLine("Items Delete moved out of their lists; Right shows them")]);
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
                return new ItemDetail("Details of " + row.Name, listFile, WithSectionField(read?.Document ?? ItemDocument.NewList(TaskRules.DefaultTemplate)), [templateLine], new FileSource(listFile, read?.Text));
        }
    }

    /// <summary>TK-3a.3: a list's details with the Section field first, made empty when the file has none.</summary>
    private static ItemDocument WithSectionField(ItemDocument details)
    {
        if (details.IndexOf(ItemFolder.SectionField) >= 0)
        {
            return details;
        }

        var withField = details.WithField(-1, ItemFolder.SectionField, FieldTypes.Text, []);
        for (var i = withField.Fields.Count - 1; i > 0; i--)
        {
            withField = withField.WithFieldMoved(i, -1)!;
        }

        return withField;
    }

    /// <summary>Enter on a list's "Fields of a new item" line: how they change.</summary>
    private void ExplainTemplate() =>
        ShowNotice(Notice.Plain("Fields of a new item", (ShownDetail.Lines.FirstOrDefault()?.Label ?? string.Empty) + ".\r\n\r\nTo change them: select an item with the fields new items should get, then Item, Use these fields for new items. List, Add Done to every item or Remove Done from every item makes the list a checklist or a plain list. Ctrl+J opens the list's details in axJSON."));
}
