using System.Text.Json.Nodes;
using AxTask.Core;
using Axit.Core;
using Axit.Forms;

namespace AxTask;

/// <summary>
/// The window's commands (IA-3, IA-5, TK-4, TK-5) and the one way a file changes (<see cref="Commit"/>): every change
/// is a <see cref="Step"/> of writes and moves, checked against the file as it was shown (IA-5.5), applied, kept for
/// Ctrl+Z, the panes filled again, its words said once (IA-5.4).
/// </summary>
public sealed partial class TaskForm
{
    private const int MaxUndo = 20;

    private readonly List<Step> _undo = [];

    // ---- the navigator's commands ----

    /// <summary>Space: done or not done (TK-4.4): the task in the navigator, the check in the detail pane.</summary>
    private void CheckSelected()
    {
        if (_detail.Focused)
        {
            if (SelectedField() is { } index && _shown!.Fields[index].Type == FieldTypes.Check)
            {
                var field = _shown.Fields[index];
                var value = !FieldTypes.IsTrue(field.Value);
                ChangeShown(document => (document.WithValue(index, JsonValue.Create(value)), $"{field.Title}: {(value ? "done" : "not done")}"), index);
            }
            else
            {
                Announce("Not a check: Enter changes the field", true);
            }

            return;
        }

        if (SelectedRow() is not { Kind: RowKind.Item, Item: { } item } row)
        {
            Sounds.Edge();
            return;
        }

        var done = TaskRules.DoneIndex(item.Document);
        if (done < 0)
        {
            Sounds.Edge();
            Announce("Not a task", true);
            return;
        }

        var now = !TaskRules.IsDone(item.Document);
        var leaves = now && (_smart is not null || _settings.HideDone);
        var select = leaves ? NeighbourKey() : row.Path;
        ChangeItemFile(item, item.Document.WithValue(done, JsonValue.Create(now)), (now ? "Done: " : "Not done: ") + row.Name, select);
    }

    /// <summary>Ctrl+N in the navigator: a new item in the list shown (IA-4.3), its fields from the list's template.</summary>
    private void NewItem()
    {
        if (_detail.Focused)
        {
            NewField();
            return;
        }

        if (_smart is not null)
        {
            Announce("A smart list gathers items from the lists: open a list to add one", true);
            return;
        }

        var folder = _folder;
        AskText("New item", $"In {(AtTop ? "the top folder" : Path.GetFileName(folder))}.", "Title:", "Title", string.Empty, "&Add", title =>
        {
            var template = TaskRules.TemplateOf(ItemFolder.ReadList(folder));
            var path = ItemFolder.FreePath(folder, ItemFolder.FileStem(title), ".json");
            Commit(new Step([FileChange.Write(path, null, ItemDocument.NewItem(title, template).ToText())], "Added " + title), path);
        }, typed => typed.Trim().Length == 0 ? "Type the item's title" : null);
    }

    /// <summary>Ctrl+Shift+N: a new list in the folder shown, with the template of the list it is in.</summary>
    private void NewList()
    {
        var folder = _smart is not null ? TopFolder : _folder;
        AskText("New list", $"A folder in {(folder.Equals(TopFolder, StringComparison.OrdinalIgnoreCase) ? "the top folder" : Path.GetFileName(folder))}.", "Name:", "Name", string.Empty, "&Make", name =>
        {
            var trimmed = name.Trim();
            var listFile = Path.Combine(folder, trimmed, ItemDocument.ListFile);
            var template = TaskRules.TemplateOf(ItemFolder.ReadList(folder));
            if (_smart is not null)
            {
                _smart = null;
                _folder = TopFolder;
            }

            Commit(new Step([FileChange.WriteInNewFolder(listFile, ItemDocument.NewList(template).ToText())], "Made " + trimmed), Path.Combine(folder, trimmed));
        }, typed => ItemFolder.RefusedListName(folder, typed));
    }

    /// <summary>List, New smart list (TK-5.3): a filter file with All tasks' conditions, opened in axJSON.</summary>
    private void NewSmartList()
    {
        AskText("New smart list", "Its conditions start as All tasks', and open in axJSON to change.", "Name:", "Name", string.Empty, "&Make", name =>
        {
            var path = Path.Combine(TopFolder, name.Trim() + ItemFolder.FilterExtension);
            _smart = null;
            _folder = TopFolder;
            if (Commit(new Step([FileChange.Write(path, null, SmartList.NewFileText())], "Made the smart list " + name.Trim()), path))
            {
                AxitMenu.Start(Dispatch.AxJSON, path);
            }
        }, typed =>
        {
            var trimmed = typed.Trim();
            if (ItemFolder.RefusedListName(TopFolder, trimmed) is { } why)
            {
                return why;
            }

            return File.Exists(Path.Combine(TopFolder, trimmed + ItemFolder.FilterExtension)) ? $"There is a smart list called {trimmed} already" : null;
        });
    }

    /// <summary>F2 in the navigator: an item's Title (its file renamed with it, IA-4.5) or a list's name; the links to them kept (IA-D3).</summary>
    private void Rename()
    {
        if (_detail.Focused)
        {
            RenameField();
            return;
        }

        switch (SelectedRow())
        {
            case { Kind: RowKind.Item, Item: { } item } row:
                AskText("Rename " + row.Name, string.Empty, "Title:", "Title", item.Document.Title, "&Rename", title => RenameItem(item, title.Trim()),
                    typed => typed.Trim().Length == 0 ? "Type the item's title" : null);
                return;
            case { Kind: RowKind.List } row when !IsArchiveFolder(row.Path!):
                var parent = Path.GetDirectoryName(row.Path!)!;
                AskText("Rename " + row.Name, string.Empty, "Name:", "Name", row.Name, "&Rename", name =>
                {
                    var to = Path.Combine(parent, name.Trim());
                    var parts = new List<FileChange> { FileChange.Move(row.Path!, to) };
                    parts.AddRange(Links.AfterMove(ItemFolder.AllItems(TopFolder), row.Path!, to));
                    Commit(new Step(parts, $"Renamed {row.Name} to {name.Trim()}"), to);
                }, typed => typed.Trim().Equals(row.Name, StringComparison.Ordinal) ? "That is its name already" : ItemFolder.RefusedListName(parent, typed) is { } why && !typed.Trim().Equals(row.Name, StringComparison.OrdinalIgnoreCase) ? why : null);
                return;
            case { Kind: RowKind.List }:
                Announce("The Archive keeps its name", true);
                return;
            case { Kind: RowKind.Smart, Smart.Path: null }:
                Announce("All tasks is built in and keeps its name", true);
                return;
            case { Kind: RowKind.Smart }:
                Announce("Rename a smart list's file in axFile: Ctrl+E", true);
                return;
            default:
                Announce("Nothing selected", true);
                return;
        }
    }

    private void RenameItem(ItemFile item, string title)
    {
        var renamed = item.Document.WithTitle(title);
        var folder = Path.GetDirectoryName(item.Path)!;
        var to = ItemFolder.FreePath(folder, ItemFolder.FileStem(title), ".json", own: item.Path);
        var parts = new List<FileChange> { FileChange.Write(item.Path, item.Text, renamed.ToText()) };
        if (!to.Equals(item.Path, StringComparison.Ordinal))
        {
            parts.Add(FileChange.Move(item.Path, to));
            parts.AddRange(Links.AfterMove(ItemFolder.AllItems(TopFolder), item.Path, to, skip: item.Path));
        }

        if (CheckFresh(item.Path, item.Text))
        {
            Commit(new Step(parts, "Renamed " + title), to);
        }
    }

    /// <summary>Delete in the navigator (IA-D4): the item or list into the Archive list, the links to it kept; Ctrl+Z brings it back.</summary>
    private void Archive()
    {
        if (_detail.Focused)
        {
            RemoveField();
            return;
        }

        if (SelectedRow() is not { Kind: RowKind.Item or RowKind.List } row)
        {
            Announce(SelectedRow() is null ? "Nothing selected" : "A smart list's file is deleted in axFile: Ctrl+E", true);
            return;
        }

        if (IsArchiveFolder(row.Path!) || ItemFolder.IsArchive(TopFolder, Path.GetDirectoryName(row.Path!)!))
        {
            Announce(IsArchiveFolder(row.Path!) ? "The Archive stays" : "Already in the Archive: Ctrl+E shows it in axFile, which deletes it for good", true);
            return;
        }

        var archive = ItemFolder.ArchiveFolder(TopFolder);
        var to = row.Kind == RowKind.Item
            ? ItemFolder.FreePath(archive, Path.GetFileNameWithoutExtension(row.Path!), ".json")
            : ItemFolder.FreePath(archive, Path.GetFileName(row.Path!), string.Empty);
        var parts = new List<FileChange> { FileChange.Move(row.Path!, to) };
        parts.AddRange(Links.AfterMove(ItemFolder.AllItems(TopFolder), row.Path!, to));
        if (row.Item is not { } item || CheckFresh(item.Path, item.Text))
        {
            Commit(new Step(parts, $"Archived {row.Name}"), NeighbourKey());
        }
    }

    /// <summary>TK-4.2: the list in hand made a checklist (Done on every item and in its template) or a plain list.</summary>
    private void SwitchList(bool done)
    {
        var folder = SelectedRow() is { Kind: RowKind.List } row && !_detail.Focused ? row.Path! : _smart is null ? _folder : null;
        if (folder is null)
        {
            Announce("Open a list first", true);
            return;
        }

        var parts = new List<FileChange>();
        var changed = 0;
        foreach (var item in ItemFolder.Read(folder, folder.Equals(TopFolder, StringComparison.OrdinalIgnoreCase)).Items)
        {
            var switched = done ? TaskRules.WithDone(item.Document) : TaskRules.WithoutDone(item.Document);
            if (!ReferenceEquals(switched, item.Document))
            {
                parts.Add(FileChange.Write(item.Path, item.Text, switched.ToText()));
                changed++;
            }
        }

        var listFile = Path.Combine(folder, ItemDocument.ListFile);
        var list = ItemFolder.ReadItem(listFile);
        var details = list?.Document ?? ItemDocument.NewList(TaskRules.DefaultTemplate);
        parts.Add(FileChange.Write(listFile, list?.Text, details.WithTemplate(TaskRules.TemplateWithDone(TaskRules.TemplateOf(list?.Document), done)).ToText()));
        var name = folder.Equals(TopFolder, StringComparison.OrdinalIgnoreCase) ? "The top folder" : Path.GetFileName(folder);
        Commit(new Step(parts, done ? $"{name} is a checklist: Done added to {Count(changed, "item")}" : $"{name} is a plain list: Done removed from {Count(changed, "item")}"), SelectedKey());
    }

    /// <summary>Item, Use these fields for new items (IA-4.3): the item's fields, emptied, as its list's template.</summary>
    private void UseAsTemplate()
    {
        if (SelectedRow() is not { Kind: RowKind.Item, Item: { } item })
        {
            Announce("Select an item whose fields new items should get", true);
            return;
        }

        var folder = Path.GetDirectoryName(item.Path)!;
        var listFile = Path.Combine(folder, ItemDocument.ListFile);
        var list = ItemFolder.ReadItem(listFile);
        var details = list?.Document ?? ItemDocument.NewList(TaskRules.DefaultTemplate);
        var template = item.Document.Fields.Where(field => !field.Title.Equals(ItemDocument.TitleField, StringComparison.OrdinalIgnoreCase)).ToList();
        var name = folder.Equals(TopFolder, StringComparison.OrdinalIgnoreCase) ? "the top folder" : Path.GetFileName(folder);
        Commit(new Step([FileChange.Write(listFile, list?.Text, details.WithTemplate(template).ToText())],
            template.Count == 0 ? $"New items in {name} get only a title" : $"New items in {name} get: {string.Join(", ", template.Select(field => field.Title))}"), SelectedKey());
    }

    /// <summary>Ctrl+Q: every field of the item in the navigator, or the whole value of the field in the detail pane.</summary>
    private void ReadAll()
    {
        if (_detail.Focused)
        {
            if (SelectedField() is { } index)
            {
                var field = _shown!.Fields[index];
                Announce(field.Type == FieldTypes.LongText && FieldTypes.TextOf(field.Value) is { Length: > 0 } text
                    ? $"{field.Title}: {text.ReplaceLineEndings(". ")}"
                    : FieldLabel(field), true);
            }
            else if (SelectedDetail() is { } line)
            {
                Announce(line.Label, true);
            }

            return;
        }

        switch (SelectedRow())
        {
            case { Kind: RowKind.Item, Item: { } item } row:
                Announce(row.Name + ". " + string.Join(". ", item.Document.Fields.Where(field => !field.Title.Equals(ItemDocument.TitleField, StringComparison.OrdinalIgnoreCase))
                    .Select(field => $"{field.Title}: {(field.Type == FieldTypes.LongText ? FieldTypes.TextOf(field.Value).ReplaceLineEndings(". ") : FieldTypes.Spoken(field, link => Links.Spoken(item.Path, link)))}")), true);
                return;
            case { } other:
                Announce(other.Label, true);
                return;
            default:
                Announce("Nothing selected", true);
                return;
        }
    }

    // ---- the detail pane's commands (IA-5) ----

    /// <summary>Enter in the detail pane (IA-5.1): the field's notice, by its type.</summary>
    private void ChangeField()
    {
        if (SelectedDetail() is { Field: TemplateLine } template)
        {
            ShowNotice(Notice.Plain("Fields of a new item", template.Label + ".\r\n\r\nTo change them: select an item with the fields new items should get, then Item, Use these fields for new items. List, Add Done to every item or Remove Done from every item makes the list a checklist or a plain list. Ctrl+J opens the list's details in axJSON."));
            return;
        }

        if (SelectedField() is not { } index)
        {
            Announce(SelectedDetail()?.Label ?? "Nothing selected", true);
            return;
        }

        var field = _shown!.Fields[index];
        var isItem = SelectedRow() is { Kind: RowKind.Item };
        if (isItem && field.Title.Equals(ItemDocument.TitleField, StringComparison.OrdinalIgnoreCase) && SelectedRow()!.Item is { } item)
        {
            AskText("Change Title", string.Empty, "Title:", "Title", FieldTypes.TextOf(field.Value), "&Change", title => RenameItem(item, title.Trim()),
                typed => typed.Trim().Length == 0 ? "Type the item's title" : null);
            return;
        }

        switch (field.Type)
        {
            case FieldTypes.Check:
                CheckSelected();
                return;
            case FieldTypes.Choice:
                ChooseOption(index, field);
                return;
            case FieldTypes.LongText:
                ChangeLongText(index, field);
                return;
            case FieldTypes.Link:
                ChangeLink(index, field);
                return;
        }

        if (!FieldTypes.Known(field.Type))
        {
            Announce($"A field of type {field.Type}: change it in axJSON, Ctrl+J", true);
            return;
        }

        var initial = field.Value is JsonValue value && value.GetValueKind() == System.Text.Json.JsonValueKind.Number ? value.ToJsonString() : FieldTypes.TextOf(field.Value).Replace('T', ' ');
        if (field.Type == FieldTypes.Text)
        {
            initial = FieldTypes.TextOf(field.Value);
        }

        var hint = field.Type switch
        {
            FieldTypes.Date => "A date: 2026-10-30, 30 October 2026, today, tomorrow, +3 for three days on, or a weekday. Empty for none.",
            FieldTypes.DateTime => "A date, a space and a time: tomorrow 14:00. Empty for none.",
            FieldTypes.Number => "A number. Empty for none.",
            _ => string.Empty,
        };
        Parsed? parsed = null;
        AskText("Change " + field.Title, hint, field.Title + ":", field.Title, initial, "&Change",
            _ => SetValue(index, field, parsed!.Value),
            typed =>
            {
                parsed = FieldTypes.Parse(field.Type, typed, field.Options, Today);
                return parsed.Refused;
            }, required: false);
    }

    private void SetValue(int index, ItemField field, JsonNode? value)
    {
        var shown = new ItemField(field.Title, field.Type, value, field.Options);
        ChangeShown(document => (document.WithValue(index, value), $"{field.Title}: {FieldTypes.Spoken(shown, link => _shownPath is { } from ? Links.Spoken(from, link) : link)}"), index);
    }

    /// <summary>A choice (IA-4.2): one button per option, the current one the default.</summary>
    private void ChooseOption(int index, ItemField field)
    {
        if (field.Options.Count == 0)
        {
            Announce("This choice has no options: add them in axJSON, Ctrl+J", true);
            return;
        }

        var current = FieldTypes.TextOf(field.Value);
        var start = Math.Max(0, field.Options.ToList().FindIndex(option => option.Equals(current, StringComparison.OrdinalIgnoreCase)));
        ChooseFromList("Change " + field.Title, $"Now {(current.Length == 0 ? "empty" : current)}.", field.Title, field.Options, start,
            chosen => SetValue(index, field, JsonValue.Create(field.Options[chosen])));
    }

    /// <summary>
    /// A notice with a list to choose from (a choice's options, a new field's type): the bundle's list, type-to-find
    /// included, holds the focus; Enter chooses the selected line, Escape leaves it.
    /// </summary>
    private void ChooseFromList(string title, string text, string name, IReadOnlyList<string> lines, int start, Action<int> chosen)
    {
        var list = new AxitList { AccessibleName = name, Font = _nav.Font };
        list.Items.AddRange([.. lines]);
        list.SelectedIndex = Math.Clamp(start, 0, lines.Count - 1);
        ShowNotice(new Notice(title, text + " Up and Down move through the list, Enter chooses.",
        [
            new OverlayChoice("C&hoose", () => chosen(list.SelectedIndex), IsDefault: true),
            new OverlayChoice("&Cancel", IsCancel: true),
        ])
        {
            Custom = list,
        });
        list.Select();
    }

    /// <summary>A long text (IA-4.2): a field of several lines; Enter makes a new line, Tab then Enter on Save keeps it.</summary>
    private void ChangeLongText(int index, ItemField field)
    {
        var box = new TextBox
        {
            Multiline = true,
            AcceptsReturn = true,
            AcceptsTab = false,
            ScrollBars = ScrollBars.Vertical,
            WordWrap = true,
            AccessibleName = field.Title,
            Text = FieldTypes.TextOf(field.Value).ReplaceLineEndings("\r\n"),
            Font = _nav.Font,
        };
        box.KeyDown += EditKeys.Handle;
        ShowNotice(new Notice("Change " + field.Title, "Enter makes a new line. Tab, then Enter on Save keeps the text.",
        [
            new OverlayChoice("&Save", () => SetValue(index, field, FieldTypes.Parse(FieldTypes.LongText, box.Text, [], Today).Value), IsDefault: true),
            new OverlayChoice("&Cancel", IsCancel: true),
        ])
        {
            Custom = box,
        });
        box.Select();
        box.Select(box.TextLength, 0);
    }

    /// <summary>A link (IA-D3): the other item's title typed, found among every item under the top folder.</summary>
    private void ChangeLink(int index, ItemField field)
    {
        var from = _shownPath!;
        var current = FieldTypes.TextOf(field.Value) is { Length: > 0 } link && ItemFolder.ReadItem(ItemFolder.Resolve(from, link)) is { } linked ? ItemFolder.NameOf(linked) : string.Empty;
        ItemFile? target = null;
        AskText("Change " + field.Title, "The title of the item to link to, in any list. Empty for none.", "Title:", "Title", current, "&Link",
            _ => SetValue(index, field, JsonValue.Create(target is null ? string.Empty : ItemFolder.LinkTo(from, target.Path))),
            typed =>
            {
                var title = typed.Trim();
                if (title.Length == 0)
                {
                    target = null;
                    return null;
                }

                var matches = ItemFolder.AllItems(TopFolder).Where(item => !item.Path.Equals(from, StringComparison.OrdinalIgnoreCase)
                    && ItemFolder.NameOf(item).Equals(title, StringComparison.OrdinalIgnoreCase)).ToList();
                target = matches.FirstOrDefault();
                return matches.Count switch
                {
                    0 => $"No item is called {title}",
                    1 => null,
                    _ => $"{matches.Count} items are called {title}: rename one first",
                };
            }, required: false);
    }

    /// <summary>Ctrl+N in the detail pane (IA-5.3): the field's title, then its type, then a choice's options; after the selected field.</summary>
    private void NewField()
    {
        if (_shown is null)
        {
            Announce("Select an item or a list first", true);
            return;
        }

        var after = SelectedField() ?? _shown.Fields.Count - 1;
        AskText("New field", string.Empty, "Title:", "Field title", string.Empty, "&Next", title =>
        {
            var trimmed = title.Trim();
            ChooseFromList("Type of " + trimmed, "What the field holds.", "Type", FieldTypes.All.Select(FieldTypes.Spoken).ToList(), 0,
                chosen => AddField(after, trimmed, FieldTypes.All[chosen]));
        }, typed => typed.Trim().Length == 0 ? "Type the field's title" : _shown.IndexOf(typed.Trim()) >= 0 ? $"There is a field called {typed.Trim()} already" : null);
    }

    private void AddField(int after, string title, string type)
    {
        if (type != FieldTypes.Choice)
        {
            ChangeShown(document => (document.WithField(after, title, type, []), $"Added {title}, {FieldTypes.Spoken(type)}"), after + 1);
            return;
        }

        AskText("Options of " + title, "The options, with a comma between them: High, Normal, Low.", "Options:", "Options", string.Empty, "&Add",
            typed => ChangeShown(document => (document.WithField(after, title, type, Options(typed)), $"Added {title}, choice of {Options(typed).Count}"), after + 1),
            typed => Options(typed).Count == 0 ? "Type at least one option" : null);
    }

    private static IReadOnlyList<string> Options(string typed) =>
        typed.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).Distinct(StringComparer.OrdinalIgnoreCase).ToList();

    /// <summary>F2 in the detail pane: the field's title; the Title field keeps its own.</summary>
    private void RenameField()
    {
        if (SelectedField() is not { } index)
        {
            Announce("Select a field first", true);
            return;
        }

        var field = _shown!.Fields[index];
        if (field.Title.Equals(ItemDocument.TitleField, StringComparison.OrdinalIgnoreCase))
        {
            Announce("The Title field keeps its name: F2 in the navigator renames the item", true);
            return;
        }

        AskText("Rename " + field.Title, string.Empty, "Field title:", "Field title", field.Title, "&Rename",
            title => ChangeShown(document => (document.WithFieldTitle(index, title.Trim()), $"Renamed {field.Title} to {title.Trim()}"), index),
            typed => typed.Trim().Length == 0 ? "Type the field's title" : _shown.IndexOf(typed.Trim()) is var i && i >= 0 && i != index ? $"There is a field called {typed.Trim()} already" : null);
    }

    /// <summary>Delete in the detail pane: the field removed; never the Title (IA-3).</summary>
    private void RemoveField()
    {
        if (SelectedField() is not { } index)
        {
            Announce("Select a field first", true);
            return;
        }

        var field = _shown!.Fields[index];
        if (field.Title.Equals(ItemDocument.TitleField, StringComparison.OrdinalIgnoreCase))
        {
            Announce("The Title field stays", true);
            return;
        }

        ChangeShown(document => (document.WithoutField(index), "Removed " + field.Title), Math.Min(index, _shown.Fields.Count - 2));
    }

    /// <summary>Alt+Up, Alt+Down in the detail pane: the field moved; the edge sound at an end.</summary>
    private void MoveField(int direction)
    {
        if (SelectedField() is not { } index)
        {
            Announce("Select a field first", true);
            return;
        }

        if (_shown!.WithFieldMoved(index, direction) is null)
        {
            Sounds.Edge();
            Announce(direction < 0 ? "Top of the fields" : "Bottom of the fields", true);
            return;
        }

        var field = _shown.Fields[index];
        ChangeShown(document => document.WithFieldMoved(index, direction) is { } moved ? (moved, (direction < 0 ? "Moved up: " : "Moved down: ") + field.Title) : null, index + direction);
    }

    // ---- everywhere ----

    /// <summary>Ctrl+Z (IA-5.4): the last step taken back, when nothing changed what it left since.</summary>
    private void Undo()
    {
        if (_undo.Count == 0)
        {
            Announce("Nothing to undo", true);
            return;
        }

        var step = _undo[^1];
        _undo.RemoveAt(_undo.Count - 1);
        try
        {
            if (Changes.CheckUndo(step) is { } why)
            {
                Announce($"Cannot undo {step.Said}: {why}", true);
                return;
            }

            Changes.Undo(step);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidOperationException)
        {
            Fail($"{step.Said} could not be undone: {ex.Message}");
            Refresh(announce: false);
            return;
        }

        Log.Info("Undone a step");
        // The selection on what the step touched first, where it is again.
        var first = step.Parts[0];
        Fill(first.IsMove ? first.Path : SelectedKey() ?? first.Path);
        Announce("Undone: " + step.Said, true);
    }

    private void ToggleHideDone()
    {
        _settings.HideDone = !_settings.HideDone;
        _hideDoneItem.Checked = _settings.HideDone;
        SaveSettings();
        Fill(SelectedKey());
        Announce(_settings.HideDone ? "Done tasks hidden" : "Done tasks shown", true);
    }

    /// <summary>Ctrl+J: the item, the list's details or the smart list in axJSON (G3).</summary>
    private void OpenInAxJson()
    {
        switch (SelectedRow())
        {
            case { Kind: RowKind.Item } row:
                AxitMenu.Start(Dispatch.AxJSON, row.Path);
                return;
            case { Kind: RowKind.List } row:
                var listFile = Path.Combine(row.Path!, ItemDocument.ListFile);
                if (File.Exists(listFile))
                {
                    AxitMenu.Start(Dispatch.AxJSON, listFile);
                }
                else
                {
                    Announce($"{row.Name} has no details file yet: change one of its details first", true);
                }

                return;
            case { Kind: RowKind.Smart, Smart.Path: { } filter }:
                AxitMenu.Start(Dispatch.AxJSON, filter);
                return;
            case { Kind: RowKind.Smart }:
                Announce("All tasks is built in: List, New smart list makes one to change", true);
                return;
            default:
                Announce("Nothing selected", true);
                return;
        }
    }

    /// <summary>Ctrl+E: the selection's file or folder in axFile; the folder shown when nothing is selected.</summary>
    private void ShowInAxFile() => AxitMenu.Start(Dispatch.AxFile, SelectedRow()?.Path ?? (_smart is null ? _folder : TopFolder));

    private void ChooseTopFolder()
    {
        if (ChooseFolder("Choose the top folder of lists", Directory.Exists(TopFolder) ? TopFolder : null) is not { } folder)
        {
            return;
        }

        _settings.Folder = folder;
        _settings.LastList = null;
        SaveSettings();
        _undo.Clear();
        _smart = null;
        _folder = TopFolder;
        Fill(select: null);
        Announce("Folder " + PathName.Of(folder) + ". " + _nav.AccessibleName, true);
    }

    // ---- the change itself ----

    /// <summary>A change to the document the detail pane shows (an item or a list's details), its field <paramref name="selectField"/> selected after.</summary>
    private void ChangeShown(Func<ItemDocument, (ItemDocument Document, string Said)?> change, int selectField)
    {
        if (_shown is null || _shownPath is null)
        {
            Announce("Nothing to change here", true);
            return;
        }

        var path = _shownPath;
        if (!CheckFresh(path, _shownText))
        {
            return;
        }

        if (change(_shown) is not { } result)
        {
            return;
        }

        var parts = new List<FileChange> { FileChange.Write(path, _shownText, result.Document.ToText()) };
        if (Commit(new Step(parts, result.Said), SelectedKey()))
        {
            var line = _details.FindIndex(row => row.Field == selectField);
            if (line >= 0)
            {
                _detail.SelectedIndex = line;
            }
        }
    }

    /// <summary>A whole new document for an item in the navigator (Space on a task).</summary>
    private void ChangeItemFile(ItemFile item, ItemDocument changed, string said, string? select)
    {
        if (CheckFresh(item.Path, item.Text))
        {
            Commit(new Step([FileChange.Write(item.Path, item.Text, changed.ToText())], said), select);
        }
    }

    /// <summary>IA-5.5: the file still as it was shown; otherwise the panes read again, that said, and false.</summary>
    private bool CheckFresh(string path, string? shownText)
    {
        string? now;
        try
        {
            now = Changes.ReadOrNull(path);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidOperationException)
        {
            Fail($"{Path.GetFileNameWithoutExtension(path)} could not be read: {ex.Message}");
            return false;
        }

        if (now == shownText)
        {
            return true;
        }

        Fill(SelectedKey());
        Announce("The item changed on disk; it has been read again", true);
        return false;
    }

    /// <summary>The one way the files change: the step applied, kept for Ctrl+Z, the panes filled on <paramref name="select"/>, its words said. False after the error notice.</summary>
    private bool Commit(Step step, string? select)
    {
        try
        {
            Changes.Apply(step.Parts);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidOperationException)
        {
            Fail($"{step.Said} could not be done: {ex.Message}");
            Refresh(announce: false);
            return false;
        }

        Log.Info("A change written");
        _undo.Add(step);
        if (_undo.Count > MaxUndo)
        {
            _undo.RemoveAt(0);
        }

        Fill(select);
        Announce(step.Said, true);
        return true;
    }

    /// <summary>The row to select when the selected one leaves the level: the next, or the one before at the end.</summary>
    private string? NeighbourKey()
    {
        var i = _nav.SelectedIndex;
        var neighbour = i + 1 < _rows.Count ? _rows[i + 1] : i - 1 >= 0 ? _rows[i - 1] : null;
        return neighbour is null ? null : neighbour.Path ?? neighbour.Name;
    }

    private bool IsArchiveFolder(string path) => path.Equals(ItemFolder.ArchiveFolder(TopFolder), StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// A notice with one field (IA-5.1): <paramref name="refused"/> asked before the button acts, its reason said and
    /// the notice kept; then <paramref name="act"/> with the text. A field that may be left empty passes
    /// <paramref name="required"/> false.
    /// </summary>
    private void AskText(string title, string text, string label, string name, string initial, string button, Action<string> act, Func<string, string?> refused, bool required = true)
    {
        ShowNotice(new Notice(title, text,
        [
            new OverlayChoice(button, () => act(Overlay.InputText), IsDefault: true),
            new OverlayChoice("&Cancel", IsCancel: true),
        ])
        {
            Input = new OverlayInput(label, name, initial, "Type something first", Required: required),
            Accepts = () =>
            {
                if (refused(Overlay.InputText) is { } why)
                {
                    Announce(why, true);
                    return false;
                }

                return true;
            },
        });
    }
}
