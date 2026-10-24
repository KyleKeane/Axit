using System.Text.Json.Nodes;
using AxTask.Core;
using Axit.Core;
using Axit.Forms;

namespace AxTask;

/// <summary>
/// axTask's commands (IA-3, IA-5, TK-4, TK-5) and the one way a file changes (<see cref="Commit"/>): every change is a
/// <see cref="Step"/> of writes and moves, checked against the file as it was shown (IA-5.5), applied, kept for Ctrl+Z,
/// the panes filled again, its words said once (IA-5.4). The field commands are the shared window's; they reach a file
/// through <see cref="SaveFields"/>.
/// </summary>
public sealed partial class TaskForm
{
    private const int MaxUndo = 20;

    private readonly List<Step> _undo = [];

    // ---- the navigator's commands ----

    /// <summary>Space in the navigator (TK-4.4): a task done or not done; the edge sound on a list or a plain item.</summary>
    protected override void CheckInNavigator(ItemRow? row)
    {
        if (row is not { Tag: ItemFile item })
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
        var select = leaves ? NeighbourKey() : row.Key;
        if (CheckFresh(item.Path, item.Text))
        {
            Commit(new Step([FileChange.Write(item.Path, item.Text, item.Document.WithValue(done, JsonValue.Create(now)).ToText())], (now ? "Done: " : "Not done: ") + row.Name), select);
        }
    }

    /// <summary>Ctrl+N in the navigator: a new item in the list shown (IA-4.3), its fields from the list's template.</summary>
    protected override void NewItem()
    {
        if (_smart is not null)
        {
            Announce("A smart list gathers items from the lists: open a list to add one", true);
            return;
        }

        if (AtTop || IsArchiveFolder(_folder))
        {
            // TK-D4: items live in lists; the Archive keeps what was moved there.
            Announce(AtTop ? "Items go in a list: Right on a list, then Ctrl+N" : "The Archive keeps what was archived: open a list to add an item", true);
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

    /// <summary>
    /// Ctrl+Shift+N: a new list in the top folder (TK-D4), in the section of the row selected (TK-3a.3) and with the
    /// template of the list selected or shown; the sections shown after, with the new list selected.
    /// </summary>
    protected override void NewList()
    {
        var folder = TopFolder;
        var section = SelectedRow() switch
        {
            { Tag: SectionTag { Custom: true } header } => header.Name,
            { IsList: true, Tag: null } list when !ItemFolder.IsArchive(TopFolder, list.Key) => ItemFolder.SectionOf(list.Key),
            _ => AtTop || _smart is not null || IsArchiveFolder(_folder) ? string.Empty : ItemFolder.SectionOf(_folder),
        };
        var templateFrom = SelectedRow() is { IsList: true, Tag: null } selected ? selected.Key : AtTop || _smart is not null ? null : _folder;
        AskText("New list", section.Length == 0 ? "In User lists." : $"In the section {section}.", "Name:", "Name", string.Empty, "&Make", name =>
        {
            var trimmed = name.Trim();
            var listFile = Path.Combine(folder, trimmed, ItemDocument.ListFile);
            var template = TaskRules.TemplateOf(templateFrom is null ? null : ItemFolder.ReadList(templateFrom));
            var details = WithSectionField(ItemDocument.NewList(template));
            details = details.WithValue(details.IndexOf(ItemFolder.SectionField), JsonValue.Create(section));
            _smart = null;
            _folder = TopFolder;
            Commit(new Step([FileChange.WriteInNewFolder(listFile, details.ToText())], "Made " + trimmed), Path.Combine(folder, trimmed));
        }, typed => ItemFolder.RefusedListName(folder, typed) ?? (typed.Trim().Equals(ItemFolder.ArchiveName, StringComparison.OrdinalIgnoreCase) ? "Archive is the archive's own name" : null));
    }

    /// <summary>
    /// List, Move to section (Ctrl+M, TK-3a.3): the list's Section field set from a list of the sections there are,
    /// User lists, and a new one.
    /// </summary>
    private void MoveToSection()
    {
        if (SelectedRow() is not { IsList: true, Tag: null } row || ItemFolder.IsArchive(TopFolder, row.Key) || !AtTop)
        {
            Announce("Select a list among the sections first", true);
            return;
        }

        var current = ItemFolder.SectionOf(row.Key);
        var sections = ItemFolder.ReadTop(TopFolder).Lists.Select(ItemFolder.SectionOf).Where(name => name.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(name => name, NaturalOrder.Instance).ToList();
        List<string> choices = ["User lists", .. sections, "A new section"];
        var start = current.Length == 0 ? 0 : Math.Max(0, choices.FindIndex(choice => choice.Equals(current, StringComparison.OrdinalIgnoreCase)));
        ChooseFromList("Move " + row.Name, $"Now in {(current.Length == 0 ? "User lists" : current)}.", "Section", choices, start, chosen =>
        {
            if (chosen == choices.Count - 1)
            {
                AskText("New section", $"The section {row.Name} moves to.", "Name:", "Section name", string.Empty, "&Move", typed => SetSection(row, typed.Trim()),
                    typed => typed.Trim().Length == 0 ? "Type the section's name" : choices.Contains(typed.Trim(), StringComparer.OrdinalIgnoreCase) ? $"There is a section called {typed.Trim()} already" : null);
                return;
            }

            SetSection(row, chosen == 0 ? string.Empty : choices[chosen]);
        });
    }

    /// <summary>The list's Section field written (TK-3a.3).</summary>
    private void SetSection(ItemRow row, string section)
    {
        var listFile = Path.Combine(row.Key, ItemDocument.ListFile);
        var read = ItemFolder.ReadItem(listFile);
        var details = WithSectionField(read?.Document ?? ItemDocument.NewList(TaskRules.DefaultTemplate));
        details = details.WithValue(details.IndexOf(ItemFolder.SectionField), JsonValue.Create(section));
        if (CheckFresh(listFile, read?.Text))
        {
            Commit(new Step([FileChange.Write(listFile, read?.Text, details.ToText())], $"Moved {row.Name} to {(section.Length == 0 ? "User lists" : section)}"), row.Key);
        }
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

    /// <summary>F2 in the navigator (AX-8.7): an item's Title (its file renamed with it, IA-4.5) or a list's name, in place; the links to them kept (IA-D3).</summary>
    protected override string? RenameRefused(ItemRow row) => row switch
    {
        { Tag: SectionTag { Custom: true } } => "A section is renamed by moving its lists: Ctrl+M on each",
        { Tag: SectionTag } => "This section keeps its name",
        { Tag: SmartList { Path: null } } => "All tasks is built in and keeps its name",
        { Tag: SmartList } => "Rename a smart list's file in axFile: Ctrl+E",
        { Tag: ItemFile } => null,
        _ when ReferenceEquals(row.Tag, ArchivedItemsTag) || IsArchiveFolder(row.Key) => "The Archive keeps its name",
        _ => null,
    };

    protected override string? RefusedName(ItemRow row, string name)
    {
        if (row.Tag is ItemFile)
        {
            return name.Length == 0 ? "Type the item's title" : null;
        }

        if (name.Equals(row.Name, StringComparison.OrdinalIgnoreCase))
        {
            // A change of case only: the same folder.
            return null;
        }

        return ItemFolder.RefusedListName(Path.GetDirectoryName(row.Key)!, name)
            ?? (name.Equals(ItemFolder.ArchiveName, StringComparison.OrdinalIgnoreCase) ? "Archive is the archive's own name" : null);
    }

    protected override void RenameTo(ItemRow row, string name)
    {
        if (row.Tag is ItemFile)
        {
            RenameItem(row, name);
            return;
        }

        var to = Path.Combine(Path.GetDirectoryName(row.Key)!, name);
        var parts = new List<FileChange> { FileChange.Move(row.Key, to) };
        parts.AddRange(Links.AfterMove(ItemFolder.AllItems(TopFolder), row.Key, to));
        Commit(new Step(parts, $"Renamed {row.Name} to {name}"), to);
    }
    protected override void RenameItem(ItemRow row, string title)
    {
        if (row.Tag is not ItemFile item)
        {
            return;
        }

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
    protected override void DeleteRow(ItemRow row)
    {
        if (row.Tag is SmartList)
        {
            Announce("A smart list's file is deleted in axFile: Ctrl+E", true);
            return;
        }

        if (row.Tag is SectionTag)
        {
            Announce("A section goes when its lists leave it: Ctrl+M on each", true);
            return;
        }

        if (IsArchiveFolder(row.Key) || ItemFolder.IsArchive(TopFolder, Path.GetDirectoryName(row.Key)!))
        {
            Announce(IsArchiveFolder(row.Key) ? "The Archive stays" : "Already in the Archive: Ctrl+E shows it in axFile, which deletes it for good", true);
            return;
        }

        var archive = ItemFolder.ArchiveFolder(TopFolder);
        var to = row.IsList
            ? ItemFolder.FreePath(archive, Path.GetFileName(row.Key), string.Empty)
            : ItemFolder.FreePath(archive, Path.GetFileNameWithoutExtension(row.Key), ".json");
        var parts = new List<FileChange> { FileChange.Move(row.Key, to) };
        parts.AddRange(Links.AfterMove(ItemFolder.AllItems(TopFolder), row.Key, to));
        if (row.Tag is not ItemFile item || CheckFresh(item.Path, item.Text))
        {
            Commit(new Step(parts, $"Archived {row.Name}"), NeighbourKey());
        }
    }

    /// <summary>TK-4.2: the list in hand made a checklist (Done on every item and in its template) or a plain list.</summary>
    private void SwitchList(bool done)
    {
        var folder = SelectedRow() is { IsList: true, Tag: null } row && !DetailPane.Focused ? row.Key : _smart is null && !AtTop ? _folder : null;
        if (folder is null)
        {
            Announce("Open a list first", true);
            return;
        }

        var parts = new List<FileChange>();
        var changed = 0;
        foreach (var item in ItemFolder.Read(folder).Items)
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
        if (SelectedRow() is not { Tag: ItemFile item })
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

    // ---- the detail pane's link (IA-D3) ----

    /// <summary>A link: the other item's title typed, found among every item under the top folder.</summary>
    protected override bool ChangeSpecial(ItemRow row, int index, ItemField field)
    {
        if (field.Type != FieldTypes.Link || ShownDetail.Source is not FileSource source)
        {
            return false;
        }

        var from = source.Path;
        var current = FieldTypes.TextOf(field.Value) is { Length: > 0 } link && ItemFolder.Resolve(from, link) is { } linkedPath && ItemFolder.ReadItem(linkedPath) is { } linked ? ItemFolder.NameOf(linked) : string.Empty;
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
        return true;
    }

    // ---- everywhere ----

    /// <summary>Ctrl+Z (IA-5.4): the last step taken back, when nothing changed what it left since.</summary>
    protected override void Undo()
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
            case { Tag: ItemFile item }:
                AxitMenu.Start(Dispatch.AxJSON, item.Path);
                return;
            case { Tag: SmartList { Path: { } filter } }:
                AxitMenu.Start(Dispatch.AxJSON, filter);
                return;
            case { Tag: SmartList }:
                Announce("All tasks is built in: List, New smart list makes one to change", true);
                return;
            case { } row:
                var listFile = Path.Combine(row.Key, ItemDocument.ListFile);
                if (File.Exists(listFile))
                {
                    AxitMenu.Start(Dispatch.AxJSON, listFile);
                }
                else
                {
                    Announce($"{row.Name} has no details file yet: change one of its details first", true);
                }

                return;
            default:
                Announce("Nothing selected", true);
                return;
        }
    }

    /// <summary>Ctrl+E: the selection's file or folder in axFile; the folder shown when nothing is selected.</summary>
    private void ShowInAxFile() =>
        AxitMenu.Start(Dispatch.AxFile, SelectedRow() is { } row && row.Tag is not SmartList { Path: null } ? row.Key : _smart is null ? _folder : TopFolder);

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
        Announce("Folder " + PathName.Of(folder) + ". " + Navigator.AccessibleName, true);
    }

    // ---- the change itself ----

    /// <summary>A change to the detail pane's document (an item or a list's details), written to its file.</summary>
    protected override void SaveFields(ItemDetail detail, ItemDocument changed, string said, int selectField)
    {
        if (detail.Source is not FileSource source || !CheckFresh(source.Path, source.Text))
        {
            return;
        }

        if (Commit(new Step([FileChange.Write(source.Path, source.Text, changed.ToText())], said), SelectedKey()))
        {
            SelectField(selectField);
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

    private bool IsArchiveFolder(string path) => path.Equals(ItemFolder.ArchiveFolder(TopFolder), StringComparison.OrdinalIgnoreCase);
}
