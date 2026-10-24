using AxList.Core;
using Axit.Core;
using Axit.Forms;

namespace AxList;

/// <summary>
/// axList's window (SPEC.md LI-2): a menu bar, one list box with three views (Lists, Items, Tasks) and a status bar.
/// Every change reads the list's file again, checks that the line is still the one shown (LI-6.2), makes the change
/// through <see cref="ListDocument"/>, writes the file at once (LI-D4) and keeps the step for Ctrl+Z. The notices, the
/// fonts, the window's place, find and the update flow are the bundle's window base (<see cref="AxitForm"/>).
/// </summary>
public sealed class ListForm : AxitForm
{
    private enum View
    {
        Lists,
        Items,
        Tasks,
    }

    /// <summary>One change that Ctrl+Z can take back (LI-4.9): the file, its text before and after, and what was said.</summary>
    private sealed record Step(string Path, string Before, string After, string Said);

    private const int MaxUndo = 10;

    private readonly ListSettings _settings;
    private readonly MenuStrip _menu = new();
    private readonly AxitList _list = new();
    private readonly ToolStripMenuItem _listMenu = new("&List");
    private readonly ToolStripMenuItem _itemMenu = new("&Item");
    private readonly ToolStripMenuItem _taskMenu = new("&Task");
    private readonly StatusStrip _status = new();
    private readonly ToolStripStatusLabel _nameLabel;
    private readonly ToolStripStatusLabel _folderLabel;
    private readonly ToolStripMenuItem _hideDoneItem;
    private readonly List<Step> _undo = [];

    private View _view = View.Lists;
    private IReadOnlyList<ListSummary> _lists = [];
    private IReadOnlyList<OpenTask> _tasks = [];

    /// <summary>The open list: its file, its lines, and the lines the Items view shows.</summary>
    private string? _openPath;

    private ListDocument? _document;
    private IReadOnlyList<int> _shown = [];

    /// <summary>The file line the Items view was on when another view took its place; null after another list opens.</summary>
    private int? _itemsLine;

    public ListForm(string? path, ListSettings settings, string? settingsError)
        : base(AxListApp.Name, settings, ListSettings.DefaultPath, settingsError)
    {
        _settings = settings;

        // ---- menus (LI-6.4); the keys are handled in ProcessWindowKey from the tables, the items only show them ----
        // The first menu holds what every view has; each view has a menu of its own, and only the menu of the view
        // shown is enabled, so that an item's command is never at hand among the lists (axProcess's lesson, the user's
        // ask of 2026-10-24; UpdateViewMenus).
        var folder = new ToolStripMenuItem("&Folder");
        folder.DropDownItems.Add(new ToolStripMenuItem("Lists &folder...", null, (_, _) => ChooseListsFolder()));
        folder.DropDownItems.Add(Item("&Refresh", () => Refresh(announce: true), AxListKeys.Refresh));
        folder.DropDownItems.Add(new ToolStripSeparator());
        folder.DropDownItems.Add(CreateExitItem());

        _listMenu.DropDownItems.Add(Item("&Open", Act, AxListKeys.Act));
        _listMenu.DropDownItems.Add(Item("New &list...", NewList, AxListKeys.New));
        _listMenu.DropDownItems.Add(Item("Show in ax&File", ShowInAxFile, AxListKeys.ShowInAxFile));
        _listMenu.DropDownItems.Add(new ToolStripMenuItem("Open in ax&Down", null, (_, _) => OpenInAxDown()));
        _listMenu.DropDownItems.Add(Item("&Copy name", CopyText, AxListKeys.CopyText));

        _itemMenu.DropDownItems.Add(Item("&Check or uncheck", Check, AxListKeys.Check));
        _itemMenu.DropDownItems.Add(Item("C&hange...", ChangeItem, AxListKeys.Act));
        _itemMenu.DropDownItems.Add(Item("Ne&w item...", NewItem, AxListKeys.New));
        _itemMenu.DropDownItems.Add(Item("Make a &task or not", MakeTask, AxListKeys.MakeTask));
        _itemMenu.DropDownItems.Add(Item("&Remove", RemoveItem, AxListKeys.Remove));
        _itemMenu.DropDownItems.Add(new ToolStripSeparator());
        _itemMenu.DropDownItems.Add(Item("Move &up", () => MoveItem(-1), AxListKeys.MoveUp));
        _itemMenu.DropDownItems.Add(Item("Move &down", () => MoveItem(1), AxListKeys.MoveDown));
        _itemMenu.DropDownItems.Add(Item("&Indent", () => IndentItem(1), AxListKeys.Indent));
        _itemMenu.DropDownItems.Add(Item("&Outdent", () => IndentItem(-1), AxListKeys.Outdent));
        _itemMenu.DropDownItems.Add(new ToolStripSeparator());
        _itemMenu.DropDownItems.Add(Item("U&ndo", Undo, AxListKeys.Undo));
        _itemMenu.DropDownItems.Add(Item("Co&py text", CopyText, AxListKeys.CopyText));
        _itemMenu.DropDownItems.Add(Item("Show in ax&File", ShowInAxFile, AxListKeys.ShowInAxFile));
        _itemMenu.DropDownItems.Add(new ToolStripMenuItem("&Edit in axDown", null, (_, _) => OpenInAxDown()));

        _taskMenu.DropDownItems.Add(Item("&Check off", Check, AxListKeys.Check));
        _taskMenu.DropDownItems.Add(Item("&Go to its list", Act, AxListKeys.Act));
        _taskMenu.DropDownItems.Add(new ToolStripSeparator());
        _taskMenu.DropDownItems.Add(Item("U&ndo", Undo, AxListKeys.Undo));
        _taskMenu.DropDownItems.Add(Item("Co&py text", CopyText, AxListKeys.CopyText));

        var view = new ToolStripMenuItem("&View");
        view.DropDownItems.Add(Item("&Lists", () => ShowView(View.Lists), AxListKeys.ViewLists));
        view.DropDownItems.Add(Item("&Items", () => ShowView(View.Items), AxListKeys.ViewItems));
        view.DropDownItems.Add(Item("&Tasks", () => ShowView(View.Tasks), AxListKeys.ViewTasks));
        view.DropDownItems.Add(new ToolStripSeparator());
        _hideDoneItem = Item("&Hide done tasks", ToggleHideDone, AxListKeys.HideDone);
        _hideDoneItem.Checked = settings.HideDone;
        view.DropDownItems.Add(_hideDoneItem);

        // Options, as the other windows' (AX-6).
        var options = new ToolStripMenuItem("&Options");
        options.DropDownItems.Add(CreateTextMenu());
        options.DropDownItems.Add(CreateGeneralMenu());
        options.DropDownItems.Add(CreateDeveloperMenu());

        var help = CreateHelpMenu(HelpText.UserGuide, "List &resources",
            [("&Markdown task lists", "https://docs.github.com/en/get-started/writing-on-github/working-with-advanced-formatting/about-tasklists")]);

        _menu.Items.AddRange([folder, _listMenu, _itemMenu, _taskMenu, view, options, CreateAxitMenu(), help]);
        MainMenuStrip = _menu;

        // The bundle's list (AX-8.6): type-to-find with Shift backwards and the edge earcon, built in.
        _list.Dock = DockStyle.Fill;
        _list.MouseDown += (_, e) =>
        {
            if (e.Button == MouseButtons.Right && _list.IndexFromPoint(e.Location) is var index && index >= 0)
            {
                _list.SelectedIndex = index;
            }
        };
        _list.MouseUp += (_, e) =>
        {
            if (e.Button == MouseButtons.Right)
            {
                OpenViewMenu();
            }
        };
        _list.TabIndex = 0;

        (_nameLabel, _folderLabel) = StatusLayout.Pair(_status);
        Controls.AddRange([Overlay, _list, _status, _menu]);

        ApplyTextFont();
        RestoreWindow();
        Start(path);
    }

    // ---- what the window base asks for ----

    protected override string Shortcuts => HelpText.Shortcuts;

    protected override void HideForNotice() => _list.Visible = false;

    protected override void ShowAfterNotice() => _list.Visible = true;

    protected override Control FocusTarget => _list;

    protected override void ApplyFont(Font font) => _list.Font = font;

    protected override IReadOnlyList<string> RestartArguments() => [Dispatch.AxList, _openPath ?? Folder];

    protected override IEnumerable<string> DiagnosticsLines => [$"View: {_view}, {_list.Items.Count} items, a list open: {_openPath is not null}"];

    /// <summary>Ctrl+S: every change is written at once (LI-D4).</summary>
    protected override void SaveCommand() => Announce("Nothing to save here: every change is written at once", true);

    protected override void OnShown(EventArgs e)
    {
        if (!NoticeOpen)
        {
            _list.Select();
        }

        base.OnShown(e);
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (_openPath is { } open)
        {
            // AX-9.4: the list joins the bundle's Recently closed list.
            RecentlyClosed.Remember(AxListApp.Name, open);
        }

        base.OnFormClosing(e);
    }

    // ---- keys (LI-6.5): every key comes from BundleKeys or AxListKeys, and is handled here only ----

    protected override bool ProcessWindowKey(Keys keyData)
    {
        switch (keyData)
        {
            case BundleKeys.Find:
                ShowFindInList(_list, Labels);
                return true;
            case BundleKeys.FindNext:
                FindInList(_list, Labels, 1);
                return true;
            case BundleKeys.FindPrevious:
                FindInList(_list, Labels, -1);
                return true;
            case AxListKeys.ViewLists:
                ShowView(View.Lists);
                return true;
            case AxListKeys.ViewItems:
                ShowView(View.Items);
                return true;
            case AxListKeys.ViewTasks:
                ShowView(View.Tasks);
                return true;
            case AxListKeys.New:
                NewItem();
                return true;
            case AxListKeys.MoveUp:
                MoveItem(-1);
                return true;
            case AxListKeys.MoveDown:
                MoveItem(1);
                return true;
            case AxListKeys.Indent:
                IndentItem(1);
                return true;
            case AxListKeys.Outdent:
                IndentItem(-1);
                return true;
            case AxListKeys.MakeTask:
                MakeTask();
                return true;
            case AxListKeys.HideDone:
                ToggleHideDone();
                return true;
            case AxListKeys.Undo:
                Undo();
                return true;
            case AxListKeys.CopyText:
                CopyText();
                return true;
            case AxListKeys.ShowInAxFile:
                ShowInAxFile();
                return true;
            case AxListKeys.Refresh:
                Refresh(announce: true);
                return true;
        }

        if (_list.Focused)
        {
            // Enter, Space, Delete, Escape and Backspace in the list; returning true keeps it from dinging.
            switch (keyData)
            {
                case AxListKeys.Act:
                    Act();
                    return true;
                case AxListKeys.Check:
                    Check();
                    return true;
                case AxListKeys.Remove:
                    RemoveItem();
                    return true;
                case AxListKeys.NextView:
                    ShowView(Cycled(1));
                    return true;
                case AxListKeys.PreviousView:
                    ShowView(Cycled(-1));
                    return true;
                case AxListKeys.ItemMenu:
                case AxListKeys.ItemMenuAlso:
                    OpenViewMenu();
                    return true;
                case AxListKeys.Back:
                case AxListKeys.BackAlso:
                    if (_view == View.Items)
                    {
                        ShowView(View.Lists);
                        return true;
                    }

                    return false;
            }
        }

        return false;
    }

    // ---- the folder and the views (LI-1, LI-2) ----

    /// <summary>The view after or before the one shown, round from Tasks to Lists (Tab, Shift+Tab; LI-2.2); the Items view is passed over while no list is open.</summary>
    private View Cycled(int step)
    {
        var next = _view;
        do
        {
            next = (View)(((int)next + step + 3) % 3);
        }
        while (next == View.Items && _openPath is null);

        return next;
    }

    /// <summary>Shift+F10, the Applications key, a right click: the view's menu from the bar, List, Item or Task.</summary>
    private void OpenViewMenu() => OpenBarMenu(_view switch { View.Items => 'I', View.Tasks => 'T', _ => 'L' });

    /// <summary>The menu of the view shown enabled, the other two not (LI-6.4): the bar's arrows pass over a disabled menu and the palette leaves its items out.</summary>
    private void UpdateViewMenus()
    {
        _listMenu.Enabled = _view == View.Lists;
        _itemMenu.Enabled = _view == View.Items;
        _taskMenu.Enabled = _view == View.Tasks;
    }

    private string Folder => _settings.ListsFolder;

    /// <summary>LI-1.1: a .md file opens its folder and itself, a folder becomes the folder of lists, nothing opens the last run's.</summary>
    private void Start(string? path)
    {
        string? open = null;
        if (path is not null && File.Exists(path) && path.EndsWith(".md", StringComparison.OrdinalIgnoreCase))
        {
            _settings.Folder = Path.GetDirectoryName(path);
            open = path;
        }
        else if (path is not null && Directory.Exists(path))
        {
            _settings.Folder = path;
        }
        else if (_settings.LastList is { } last && File.Exists(Path.Combine(Folder, last)))
        {
            open = Path.Combine(Folder, last);
        }

        ReadLists();
        if (open is not null && OpenList(open, selectLine: null))
        {
            _view = View.Items;
        }

        Fill(select: null);
    }

    private void ReadLists() => _lists = ListFolder.Lists(Folder);

    /// <summary>Reads a list into the window; false, after the error notice, when it cannot be read.</summary>
    private bool OpenList(string path, int? selectLine)
    {
        if (!ReadOpen(path))
        {
            return false;
        }

        _itemsLine = null;
        _settings.LastList = Path.GetFileName(path);
        SaveSettings();
        _view = View.Items;
        Fill(selectLine);
        return true;
    }

    /// <summary>The open list read again from its file (LI-6.1).</summary>
    private bool ReadOpen(string path)
    {
        try
        {
            var file = TextDocument.Read(path);
            _document = ListDocument.Parse(file.Text);
            _openPath = path;
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidOperationException)
        {
            Fail($"{ListFolder.NameOf(path)} could not be read: {ex.Message}");
            return false;
        }
    }

    /// <summary>Ctrl+1, 2, 3 (LI-2.2): the view shown, read again, with its name and the selected item said.</summary>
    private void ShowView(View view)
    {
        if (view == View.Items && (_openPath is null || !File.Exists(_openPath)))
        {
            Announce("No list open: Ctrl+1, then Enter", true);
            return;
        }

        if (_view == View.Items)
        {
            // The line the Items view was on, for the way back to it (Escape, then Ctrl+2 or Tab).
            _itemsLine = SelectedLine();
        }

        _view = view;
        switch (view)
        {
            case View.Lists:
                ReadLists();
                Fill(_openPath);
                break;
            case View.Items:
                ReadOpen(_openPath!);
                Fill(_itemsLine);
                break;
            default:
                _tasks = ListFolder.OpenTasks(Folder);
                Fill(select: null);
                break;
        }

        var name = _list.AccessibleName ?? string.Empty;
        Announce(_list.SelectedItem is string item ? $"{name}. {item}" : name, true);
    }

    /// <summary>F5 (LI-2.4): the folder, the open list and the tasks read again, the selection kept.</summary>
    private void Refresh(bool announce)
    {
        var keep = _view switch
        {
            View.Items => (object?)SelectedLine(),
            View.Lists => SelectedList()?.Path,
            _ => _list.SelectedIndex,
        };
        ReadLists();
        if (_openPath is { } open && File.Exists(open))
        {
            ReadOpen(open);
        }

        if (_view == View.Tasks)
        {
            _tasks = ListFolder.OpenTasks(Folder);
        }

        Fill(keep);
        if (announce)
        {
            Announce("Refreshed: " + _list.AccessibleName, true);
        }
    }

    /// <summary>
    /// The list box filled for the view shown, its name, the title and the status bar; the selection on
    /// <paramref name="select"/>: a list's path in the Lists view, a line of the file in the Items view, an index in the
    /// Tasks view; otherwise the first item.
    /// </summary>
    private void Fill(object? select)
    {
        IReadOnlyList<string> labels;
        var index = 0;
        string name;
        switch (_view)
        {
            case View.Lists:
                labels = _lists.Select(list => list.Label).ToList();
                name = _lists.Count == 0 ? "Lists, none yet: Ctrl+N makes one" : $"Lists, {_lists.Count}";
                if (select is string path)
                {
                    index = Math.Max(0, _lists.ToList().FindIndex(list => list.Path.Equals(path, StringComparison.OrdinalIgnoreCase)));
                }

                break;
            case View.Items:
                _shown = _document!.Shown(_settings.HideDone);
                labels = _shown.Select(_document.Label).ToList();
                var hidden = _settings.HideDone ? _document.Lines.Count(line => line.IsTask && line.Done) : 0;
                name = $"{ListFolder.NameOf(_openPath!)}, {(_shown.Count == 0 ? "no items" : $"{_shown.Count} {(_shown.Count == 1 ? "item" : "items")}")}{(hidden > 0 ? $", {hidden} done hidden" : string.Empty)}";
                if (select is int line)
                {
                    // The line itself, or the first shown after it (a removed or hidden line), or the last.
                    index = _shown.Count - 1;
                    for (var i = 0; i < _shown.Count; i++)
                    {
                        if (_shown[i] >= line)
                        {
                            index = i;
                            break;
                        }
                    }
                }

                break;
            default:
                labels = _tasks.Select(task => task.Label).ToList();
                name = _tasks.Count == 0 ? "Tasks, none open" : $"Tasks, {_tasks.Count} open";
                if (select is int position)
                {
                    index = Math.Clamp(position, 0, Math.Max(0, _tasks.Count - 1));
                }

                break;
        }

        _list.BeginUpdate();
        _list.Items.Clear();
        _list.Items.AddRange(labels.Select(label => (object)label).ToArray());
        _list.EndUpdate();
        _list.AccessibleName = name;
        UpdateViewMenus();
        if (_list.Items.Count > 0)
        {
            _list.SelectedIndex = Math.Clamp(index, 0, _list.Items.Count - 1);
        }

        Text = (_view == View.Lists || _openPath is null ? "Lists" : ListFolder.NameOf(_openPath)) + " - " + AxListApp.Name;
        _nameLabel.Text = name;
        _folderLabel.Text = Folder;
        StatusLayout.Fit(_status, _nameLabel, _folderLabel);
    }

    private IReadOnlyList<string> Labels() => _list.Items.Cast<string>().ToList();

    private ListSummary? SelectedList() =>
        _view == View.Lists && _list.SelectedIndex is var i && i >= 0 && i < _lists.Count ? _lists[i] : null;

    /// <summary>The file line of the item selected in the Items view; null with none.</summary>
    private int? SelectedLine() =>
        _view == View.Items && _list.SelectedIndex is var i && i >= 0 && i < _shown.Count ? _shown[i] : null;

    private OpenTask? SelectedTask() =>
        _view == View.Tasks && _list.SelectedIndex is var i && i >= 0 && i < _tasks.Count ? _tasks[i] : null;

    // ---- the commands ----

    /// <summary>Enter: a list opened, an item changed, a task shown in its list.</summary>
    private void Act()
    {
        switch (_view)
        {
            case View.Lists:
                if (SelectedList() is { } list)
                {
                    if (OpenList(list.Path, selectLine: null))
                    {
                        Announce(_list.SelectedItem is string first ? $"{_list.AccessibleName}. {first}" : _list.AccessibleName ?? string.Empty, true);
                    }
                }
                else
                {
                    Announce("Nothing selected", true);
                }

                return;
            case View.Items:
                ChangeItem();
                return;
            default:
                if (SelectedTask() is { } task && OpenList(task.Path, task.Index))
                {
                    Announce(_list.SelectedItem is string item ? $"{_list.AccessibleName}. {item}" : _list.AccessibleName ?? string.Empty, true);
                }

                return;
        }
    }

    /// <summary>Space (LI-4.2, LI-5.2).</summary>
    private void Check()
    {
        if (_view == View.Tasks)
        {
            if (SelectedTask() is not { } task)
            {
                Announce("Nothing selected", true);
                return;
            }

            var position = _list.SelectedIndex;
            Change(task.Path, task.Index, task.Line, (document, line) => document.Toggle(line) is { } toggled ? (toggled.Document, toggled.Said, line) : null, "Not a task: Ctrl+T makes it one");
            _tasks = ListFolder.OpenTasks(Folder);
            Fill(position);
            return;
        }

        ChangeSelected((document, line) => document.Toggle(line) is { } toggled ? (toggled.Document, toggled.Said, line) : null, "Not a task: Ctrl+T makes it one");
    }

    /// <summary>Enter in the Items view (LI-4.3): a notice with the text to change.</summary>
    private void ChangeItem()
    {
        if (!ItemsFirst() || SelectedLine() is not { } line)
        {
            return;
        }

        var shown = _document!.Lines[line];
        ShowNotice(new Notice("Change item", string.Empty,
        [
            new OverlayChoice("&Change", () =>
            {
                var text = Overlay.InputText.Trim();
                ChangeSelected((document, at) => (document.SetText(at, text), "Changed " + text, at), string.Empty, line, shown);
            }, IsDefault: true),
            new OverlayChoice("&Cancel", IsCancel: true),
        ])
        {
            Input = new OverlayInput("Text:", "Text", shown.Text, "Type the item's text"),
        });
    }

    /// <summary>Ctrl+N (LI-3.2, LI-4.4): a new list in the Lists view, a new item in the Items view.</summary>
    private void NewItem()
    {
        if (_view == View.Lists)
        {
            NewList();
            return;
        }

        if (!ItemsFirst())
        {
            return;
        }

        var after = SelectedLine();
        var shown = after is { } line ? _document!.Lines[line] : null;
        ShowNotice(new Notice("New item", string.Empty,
        [
            new OverlayChoice("&Add", () =>
            {
                var text = Overlay.InputText.Trim();
                if (after is { } at && shown is not null)
                {
                    ChangeSelected((document, line) => Added(document.Insert(line, text), text), string.Empty, at, shown);
                }
                else
                {
                    Change(_openPath!, -1, null, (document, _) => Added(document.Insert(null, text), text), string.Empty);
                }
            }, IsDefault: true),
            new OverlayChoice("&Cancel", IsCancel: true),
        ])
        {
            Input = new OverlayInput("Text:", "Text", string.Empty, "Type the item's text"),
        });
    }

    private void NewList()
    {
        ShowNotice(new Notice("New list", $"The list is a Markdown file in {Folder}.",
        [
            new OverlayChoice("&Make", () => MakeList(Overlay.InputText.Trim()), IsDefault: true),
            new OverlayChoice("&Cancel", IsCancel: true),
        ])
        {
            Input = new OverlayInput("Name:", "Name", string.Empty, "Type the list's name"),
            Accepts = () =>
            {
                if (ListFolder.RefusedName(Folder, Overlay.InputText) is { } reason)
                {
                    Announce(reason, true);
                    return false;
                }

                return true;
            },
        });
    }

    private void MakeList(string name)
    {
        var path = Path.Combine(Folder, name + ".md");
        try
        {
            Directory.CreateDirectory(Folder);
            TextDocument.Empty().Write(path, $"# {name}\r\n");
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            Fail($"{name} could not be made: {ex.Message}");
            return;
        }

        Log.Info("Made a list");
        ReadLists();
        if (OpenList(path, selectLine: null))
        {
            Announce("Made " + name, true);
        }
    }

    /// <summary>What an added item gives the change: the document, the words, and the new line to select.</summary>
    private static (ListDocument Document, string Said, int Select) Added((ListDocument Document, int Index) inserted, string text) =>
        (inserted.Document, "Added " + text, inserted.Index);

    private void MakeTask() =>
        ChangeSelected((document, line) => document.ToggleTask(line) is { } made ? (made.Document, made.Said, line) : null, "Only list items become tasks");

    /// <summary>Delete (LI-4.5).</summary>
    private void RemoveItem()
    {
        if (_view != View.Items)
        {
            Announce(_view == View.Lists ? "Delete a list in axFile: Ctrl+E" : "Open a list first: Enter", true);
            return;
        }

        ChangeSelected((document, line) =>
        {
            var name = document.NameOf(line);
            var (removed, nested) = document.Remove(line);
            return (removed, nested == 0 ? "Removed " + name : $"Removed {name} and {nested} nested {(nested == 1 ? "item" : "items")}", line);
        }, string.Empty);
    }

    /// <summary>Alt+Up, Alt+Down (LI-4.6).</summary>
    private void MoveItem(int direction)
    {
        if (!ItemsFirst() || SelectedLine() is not { } line)
        {
            return;
        }

        if (_document!.Lines[line].Kind != LineKind.Item)
        {
            Announce("Only list items move", true);
            return;
        }

        var outcome = ChangeSelected((document, at) => document.Move(at, direction) is { } result
            ? (result.Document, (direction < 0 ? "Moved up: " : "Moved down: ") + document.NameOf(at), result.Index)
            : null, direction < 0 ? "Top of the list" : "Bottom of the list");
        if (outcome == Outcome.Refused)
        {
            Sounds.Edge();
        }
    }

    /// <summary>Alt+Right, Alt+Left (LI-4.6).</summary>
    private void IndentItem(int direction)
    {
        if (!ItemsFirst() || SelectedLine() is not { } line)
        {
            return;
        }

        if (_document!.Lines[line].Kind != LineKind.Item)
        {
            Announce("Only list items move", true);
            return;
        }

        ChangeSelected((document, at) =>
        {
            if (document.Indent(at, direction) is not { } indented)
            {
                return null;
            }

            var level = indented.Levels()[at];
            return (indented, direction > 0 ? $"Indented: {document.NameOf(at)}, level {level}" : $"Outdented: {document.NameOf(at)}", at);
        }, direction > 0 ? "Nothing before it to indent under" : "Already at the top level");
    }

    private void ToggleHideDone()
    {
        _settings.HideDone = !_settings.HideDone;
        _hideDoneItem.Checked = _settings.HideDone;
        SaveSettings();
        if (_view == View.Items)
        {
            Fill(SelectedLine());
        }

        Announce(_settings.HideDone ? "Done tasks hidden" : "Done tasks shown", true);
    }

    /// <summary>Ctrl+Z (LI-4.9): the last step's file written back as it was, when nothing else changed it since.</summary>
    private void Undo()
    {
        if (_undo.Count == 0)
        {
            Announce("Nothing to undo", true);
            return;
        }

        var step = _undo[^1];
        try
        {
            var file = TextDocument.Read(step.Path);
            if (file.Text != step.After)
            {
                _undo.Clear();
                Announce("The list changed on disk, so the last step cannot be undone", true);
                return;
            }

            file.Write(step.Path, step.Before);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidOperationException)
        {
            Fail($"{ListFolder.NameOf(step.Path)} could not be written: {ex.Message}");
            return;
        }

        _undo.RemoveAt(_undo.Count - 1);
        var keep = _view == View.Tasks ? (object?)_list.SelectedIndex : SelectedLine();
        Refresh(announce: false);
        Fill(keep);
        Announce("Undone: " + step.Said, true);
    }

    private void CopyText()
    {
        var text = _view switch
        {
            View.Lists => SelectedList()?.Name,
            View.Items => SelectedLine() is { } line ? _document!.Lines[line].Text : null,
            _ => SelectedTask()?.Line.Text,
        };
        if (text is null)
        {
            Announce("Nothing selected", true);
            return;
        }

        try
        {
            Clipboard.SetText(text.Length == 0 ? " " : text);
            Announce("Copied", true);
        }
        catch (Exception ex) when (ex is System.Runtime.InteropServices.ExternalException or InvalidOperationException)
        {
            Fail("The clipboard could not be written: " + ex.Message);
        }
    }

    /// <summary>The file the view is about: the selected list, the open list, the task's list.</summary>
    private string? FileInHand() => _view switch
    {
        View.Lists => SelectedList()?.Path,
        View.Items => _openPath,
        _ => SelectedTask()?.Path,
    };

    private void ShowInAxFile()
    {
        if (FileInHand() is { } path)
        {
            AxitMenu.Start(Dispatch.AxFile, path);
        }
        else
        {
            Announce("Nothing selected", true);
        }
    }

    private void OpenInAxDown()
    {
        if (FileInHand() is { } path)
        {
            AxitMenu.Start(Dispatch.AxDown, path);
        }
        else
        {
            Announce("Nothing selected", true);
        }
    }

    private void ChooseListsFolder()
    {
        if (ChooseFolder("Choose the folder of lists", Directory.Exists(Folder) ? Folder : null) is not { } folder)
        {
            return;
        }

        _settings.Folder = folder;
        _settings.LastList = null;
        SaveSettings();
        _openPath = null;
        _document = null;
        _undo.Clear();
        _view = View.Lists;
        ReadLists();
        Fill(select: null);
        Announce("Folder " + PathName.Of(folder) + ". " + _list.AccessibleName, true);
    }

    /// <summary>Whether the Items view shows, for an item command; otherwise what to do first is said.</summary>
    private bool ItemsFirst()
    {
        if (_view == View.Items && _document is not null)
        {
            return true;
        }

        Announce(_view == View.Lists ? "Open a list first: Enter" : "Items view first: Ctrl+2", true);
        return false;
    }

    // ---- the change itself (LI-6) ----

    /// <summary>What came of a change: done, refused by the document (the refusal said), or stopped before (an error, a line that moved, nothing selected).</summary>
    private enum Outcome
    {
        Done,
        Refused,
        Stopped,
    }

    /// <summary>A change to the line selected in the Items view (or to <paramref name="line"/> as <paramref name="shown"/>, for a notice's action).</summary>
    private Outcome ChangeSelected(Func<ListDocument, int, (ListDocument Document, string Said, int Select)?> change, string refused, int? line = null, ListLine? shown = null)
    {
        if (line is null)
        {
            if (!ItemsFirst())
            {
                return Outcome.Stopped;
            }

            if (SelectedLine() is not { } selected)
            {
                Announce(_shown.Count == 0 ? "No items: Ctrl+N adds one" : "Nothing selected", true);
                return Outcome.Stopped;
            }

            line = selected;
            shown = _document!.Lines[selected];
        }

        return Change(_openPath!, line.Value, shown, change, refused);
    }

    /// <summary>
    /// The one way a file changes (LI-6.1, LI-6.2): read again; the line still the one shown, or the list read again and
    /// that said; the change; null from it is the refusal said; the file written, the step kept, the view filled again
    /// on the line the change gives, and its words said. A line of -1 is a change to the whole list (an item added to an
    /// empty one).
    /// </summary>
    private Outcome Change(string path, int line, ListLine? shown, Func<ListDocument, int, (ListDocument Document, string Said, int Select)?> change, string refused)
    {
        TextDocument file;
        ListDocument fresh;
        try
        {
            file = TextDocument.Read(path);
            fresh = ListDocument.Parse(file.Text);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidOperationException)
        {
            Fail($"{ListFolder.NameOf(path)} could not be read: {ex.Message}");
            return Outcome.Stopped;
        }

        var isOpen = path.Equals(_openPath, StringComparison.OrdinalIgnoreCase);
        if (shown is not null && !fresh.Holds(line, shown))
        {
            if (isOpen)
            {
                _document = fresh;
                Fill(line);
            }

            Announce("The list changed on disk; it has been read again", true);
            return Outcome.Stopped;
        }

        if (change(fresh, line) is not { } result)
        {
            if (refused.Length > 0)
            {
                Announce(refused, true);
            }

            return Outcome.Refused;
        }

        var before = file.Text;
        var after = result.Document.ToText();
        try
        {
            file.Write(path, after);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            Fail($"{ListFolder.NameOf(path)} could not be written: {ex.Message}");
            return Outcome.Stopped;
        }

        _undo.Add(new Step(path, before, after, result.Said));
        if (_undo.Count > MaxUndo)
        {
            _undo.RemoveAt(0);
        }

        if (isOpen)
        {
            _document = result.Document;
        }

        ReadLists();
        if (_view == View.Items)
        {
            Fill(result.Select);
        }

        Announce(result.Said, true);
        return Outcome.Done;
    }
}
