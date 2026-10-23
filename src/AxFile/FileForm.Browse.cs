using System.Diagnostics.CodeAnalysis;
using AxFile.Core;
using Axit.Core;
using Axit.Forms;

namespace AxFile;

/// <summary>The folder and the list (SPEC.md AF-3): the reads, the navigation, the ways to another folder, find, information, the selection.</summary>
public sealed partial class FileForm
{
    private string? _folder;

    /// <summary>Every entry of the folder as read; <see cref="_shown"/> is what the list holds (AF-3.8).</summary>
    private IReadOnlyList<Entry> _entries = [];

    /// <summary>The entries the list shows, in its order: those that pass the filter, sorted as the setting says; the list's indices are into this.</summary>
    private IReadOnlyList<Entry> _shown = [];

    /// <summary>The filter text (AF-3.8), empty for none; dropped with the folder.</summary>
    private string _filter = string.Empty;

    /// <summary>How many entries the read left out (AF-2.2): hidden ones, and ones starting with a period; the list's last line says so.</summary>
    private int _hidden;
    private int _dot;

    /// <summary>The marks on the shown entries (AF-3.7), which the operations act on, and the Shift range being made; a marked item reads ", selected". Kept while the same folder shows.</summary>
    private readonly Marks _marks = new();

    /// <summary>The folder's last write time at the last read, so that coming back to the window knows whether to read again (AF-3.4).</summary>
    private DateTime _folderStamp;

    /// <summary>The number of the latest read: a slow read of a folder left behind is dropped when it arrives.</summary>
    private int _reads;

    /// <summary>Type-ahead (AF-3.6): the letters typed within a second of each other, gathered into one prefix as File Explorer gathers them, and when the last came.</summary>
    private string _typed = string.Empty;
    private long _typedAt;
    private const int TypeAheadMs = 1000;

    /// <summary>The folder shown when it is one on disk; null before one is shown and at This PC, which is no path a window can start on (AF-3.10).</summary>
    private string? RealFolder => _folder is { } folder && !FolderListing.IsThisPC(folder) ? folder : null;

    // ---- the guards of a command (AF-7) ----

    /// <summary>The folder a command needs, or why there is none said: "No folder" before any is shown, "Choose a drive first" at This PC (AF-3.10).</summary>
    private bool TryFolder([NotNullWhen(true)] out string? folder)
    {
        folder = RealFolder;
        if (folder is null)
        {
            Announce(_folder is null ? "No folder" : "Choose a drive first", true);
        }

        return folder is not null;
    }

    /// <summary>Whether a command that needs a real folder has none, which is said (<see cref="TryFolder"/>).</summary>
    private bool NoFolderHere() => !TryFolder(out _);

    /// <summary>The entry under the selection, or the line the selection is on said: an empty folder's, the filter's, the not-shown line, "Nothing selected", or "No folder".</summary>
    private bool TryEntry([NotNullWhen(true)] out Entry? entry)
    {
        entry = CurrentEntry();
        if (entry is null)
        {
            SayNoEntry();
        }

        return entry is not null;
    }

    /// <summary>The guard of every command on an entry (AF-7): "No folder", the line the selection is on when it is not an entry (the empty folder's, the filter's, the not-shown line), or "Nothing selected".</summary>
    private void SayNoEntry()
    {
        var index = _list.SelectedIndex;
        var line = index >= _shown.Count && index < _list.Items.Count ? _list.Items[index].ToString() : null;
        Announce(_folder is null ? "No folder" : line ?? "Nothing selected", true);
    }

    /// <summary>The entry under the selection; null on the lines after the entries (an empty list's, the not-shown line), which are not entries.</summary>
    private Entry? CurrentEntry() =>
        _list.SelectedIndex is var i && i >= 0 && i < _shown.Count ? _shown[i] : null;

    // ---- the folder (AF-2.2, AF-3.1) ----

    /// <summary>The start (AF-1.1): the path given, or the last folder, or the user's profile folder; a file selects itself in its folder.</summary>
    private async Task StartAsync(string? path)
    {
        string? folder = null;
        string? select = null;
        if (path is not null)
        {
            if (Directory.Exists(path))
            {
                folder = path;
            }
            else if (File.Exists(path))
            {
                folder = Path.GetDirectoryName(path);
                select = Path.GetFileName(path);
            }
            else
            {
                ShowNotice(Notice.Plain("Folder not found", $"{path} does not exist.") with { Unprompted = true });
            }
        }

        folder ??= _settings.LastFolder is { } last && Directory.Exists(last) ? last : Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        // NVDA reads the window, the list and its entry as the window opens; nothing is added (AF-3.1).
        await ShowFolderAsync(folder, select, remember: true, announce: false);
    }

    /// <summary>
    /// Shows a folder (AF-2.2): read on a background task, the list refilled with <paramref name="select"/> selected
    /// by name (or the first entry), the title and the status bar following. A folder that is gone gives the "Folder
    /// not found" notice and the nearest parent that exists (AF-3.5); any other failure is one announcement and the
    /// list stays. With <paramref name="remember"/> the folder joins the recent list (AF-3.3). Another folder than the
    /// one shown is announced with its count and the entry landed on, "docs, 5 items, plan.md" (AF-3.1), unless
    /// <paramref name="announce"/> is off: the start, which NVDA reads itself, an undo, which says what it undid, and
    /// the Actions list's Enter, which moves the focus first.
    /// </summary>
    private async Task<bool> ShowFolderAsync(string folder, string? select, bool remember, bool announce = true)
    {
        // A rename box left open over the list that is about to change renames the wrong entry (AF-4.2).
        CancelRename();
        var read = ++_reads;
        var changed = !string.Equals(folder, _folder, StringComparison.OrdinalIgnoreCase);
        Listing listing;
        try
        {
            // AF-3.10: This PC is the drives, read the same way.
            listing = await Task.Run(() => FolderListing.IsThisPC(folder) ? new Listing(FolderListing.Drives(), 0, 0) : FolderListing.Read(folder, _settings.ShowHidden, _settings.ShowDotFiles));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException)
        {
            if (read != _reads || IsDisposed)
            {
                return false;
            }

            if (!Directory.Exists(folder))
            {
                ShowNotice(Notice.Plain("Folder not found", $"{folder} does not exist any more.") with { Unprompted = true });
                if (NearestExisting(folder) is { } parent)
                {
                    await ShowFolderAsync(parent, null, remember);
                }

                return false;
            }

            Failed("Folder not read", FileOperations.Message(ex));
            return false;
        }

        if (read != _reads || IsDisposed)
        {
            return false;
        }

        // AF-3.7, AF-3.8: the marks and the filter live in one folder: another folder starts with none; the same one
        // keeps the filter, and the marks on the entries still shown (Rearrange).
        if (changed)
        {
            _marks.Clear();
            DropSelectionLines();
            _filter = string.Empty;
        }

        _folder = folder;
        _entries = listing.Entries;
        _hidden = listing.Hidden;
        _dot = listing.Dot;
        _folderStamp = Stamp(folder);
        Text = $"{PathName.Of(folder)} - {AxFileApp.Name}";
        Rearrange(select);
        if (FolderListing.IsThisPC(folder))
        {
            // Not a folder to come back to: the last folder and the recent list keep the one before.
        }
        else if (remember)
        {
            _settings.RememberFolder(folder);
            SaveSettings();
        }
        else
        {
            _settings.LastFolder = folder;
        }

        UpdateStatusBar();
        if (announce && changed)
        {
            Announce(FolderAnnouncement(), true);
        }

        return true;
    }

    /// <summary>
    /// What a change of folder says (AF-3.1): the folder's name, the list's count and the entry landed on, "docs, 5
    /// items, plan.md"; "temp, No items in this folder" for an empty one. NVDA alone would read the new entry only,
    /// which does not tell the folder landed in from the folder under the focus.
    /// </summary>
    private string FolderAnnouncement()
    {
        var name = FolderListing.IsThisPC(_folder) ? FolderListing.ThisPC : PathName.Of(_folder ?? string.Empty);
        var item = _list.SelectedItem?.ToString() ?? string.Empty;
        return _shown.Count == 0 ? $"{name}, {item}" : $"{name}, {_list.AccessibleName}, {item}";
    }

    /// <summary>
    /// Letters typed in the folder list (AF-3.6), as File Explorer takes them: typed within a second of each other
    /// they build one prefix, "cha", which <see cref="FolderListing.TypeAhead"/> finds among the shown entries (a run
    /// of one letter cycles through the names starting with it); a new prefix starts after a second's pause. The edge
    /// earcon when no name starts with it. The list's own one-letter jump gives way to this. NVDA reads the entry selected.
    /// </summary>
    private void OnListKeyPress(object? sender, KeyPressEventArgs e)
    {
        if (char.IsControl(e.KeyChar) || _shown.Count == 0)
        {
            // Enter, Escape, Backspace and the Ctrl letters are keys of their own (ProcessWindowKey), not type-ahead.
            return;
        }

        e.Handled = true;
        var now = Environment.TickCount64;
        _typed = now - _typedAt > TypeAheadMs ? e.KeyChar.ToString() : _typed + e.KeyChar;
        _typedAt = now;
        var index = FolderListing.TypeAhead(_shown, _typed, _list.SelectedIndex);
        if (index >= 0)
        {
            _list.SelectedIndex = index;
        }
        else
        {
            // No name starts with it: the edge earcon, not a sentence (AF-3.6).
            Sounds.Edge();
        }
    }

    /// <summary>
    /// Up or Home at the first entry, Down or End at the last (AF-3.6), in either list: the key moves nothing, so the
    /// edge earcon sounds instead of silence; false when the key moves and stays the list's own.
    /// </summary>
    private bool AtEdge(Keys key)
    {
        var list = _pane.Focused ? _pane : _list;
        var index = list.SelectedIndex;
        if (index < 0 || list.Items.Count == 0)
        {
            return false;
        }

        var first = key is AxFileKeys.FirstItem or AxFileKeys.FirstItemAlso && index == 0;
        var last = key is AxFileKeys.LastItem or AxFileKeys.LastItemAlso && index == list.Items.Count - 1;
        if (!first && !last)
        {
            return false;
        }

        Sounds.Edge();
        return true;
    }

    /// <summary>
    /// An operation's end (AF-4.5): in the folder it ran in, when that is still the one shown, the list read again with
    /// the result selected and <paramref name="said"/> announced (unless <paramref name="sayInView"/> is off: a caller
    /// that goes on with the result, New text file's rename box, says its own); when the user has gone to another
    /// folder meanwhile (the window never waits for an operation), the result is said and the list is left where the
    /// user is, since a list that jumps back is a move the user did not make (AX-7.3; 2026-10-23). True when the
    /// result is in view.
    /// </summary>
    private async Task<bool> ShowResultAsync(string folder, string? select, string said, bool sayInView = true)
    {
        if (!string.Equals(folder, _folder, StringComparison.OrdinalIgnoreCase))
        {
            Announce(said, true);
            return false;
        }

        if (!await ShowFolderAsync(folder, select, remember: false))
        {
            return false;
        }

        if (sayInView)
        {
            Announce(said, true);
        }

        return true;
    }

    /// <summary>The one item of an empty folder (AF-2.2), so that the list is never empty and the reason is read (AX-9.5).</summary>
    private const string EmptyFolderText = "No items in this folder";

    /// <summary>The one item of a folder whose entries all fail the filter (AF-3.8).</summary>
    private string NoMatchText => "No names contain " + _filter;

    /// <summary>The shown entries made again from the folder's entries, the order and the filter (AF-3.8), and the list refilled; the marks on entries no longer shown go with them (AF-3.7).</summary>
    private void Rearrange(string? select)
    {
        // The drives of This PC stay in letter order (AF-3.10).
        _shown = FolderListing.IsThisPC(_folder) ? FolderListing.Filter(_entries, _filter) : FolderListing.Arrange(_entries, _settings.SortBy, _filter);
        // A Shift range is rows of the shown entries: a new order or filter, or a read, ends it; the items are made anew below.
        _marks.Keep(_shown);
        FillList(select);
    }

    /// <summary>The list's items for the shown entries, named with the folder and the count; the selection by name, or the first entry.</summary>
    private void FillList(string? select)
    {
        // A new list starts the type-ahead afresh (AF-3.6).
        _typed = string.Empty;
        _list.BeginUpdate();
        _list.Items.Clear();
        _list.Items.AddRange(_shown.Select(entry => (object)ItemText(entry)).ToArray());
        if (_shown.Count == 0)
        {
            _list.Items.Add(_filter.Length > 0 ? NoMatchText : EmptyFolderText);
        }

        if (_hidden > 0 || _dot > 0)
        {
            // AF-2.2: what the read left out, as the last line, so that the count is there when wanted.
            _list.Items.Add(FolderListing.NotShownText(_hidden, _dot));
        }

        _list.EndUpdate();
        _list.AccessibleName = FolderListing.ListName(_shown.Count, _entries.Count, _filter);
        var index = select is null ? -1 : IndexOfName(select);
        _list.SelectedIndex = Math.Max(0, index);
    }

    // ---- the order and the filter (AF-3.8) ----

    /// <summary>Folder → Sort by → one order: the setting, the checked item, the list made again with the entry kept; "Sorted by date".</summary>
    private void SetSort(SortBy by)
    {
        _settings.SortBy = by;
        SaveSettings();
        foreach (var (sortBy, item) in _sortItems)
        {
            item.Checked = sortBy == by;
        }

        Rearrange(CurrentEntry()?.Name);
        Announce("Sorted by " + FolderListing.SortName(by), true);
    }

    /// <summary>Folder → Filter… (Ctrl+Shift+F): the notice "Filter" with the field "Name contains"; an empty field clears the filter.</summary>
    private void ShowFilterNotice()
    {
        if (_folder is null)
        {
            Announce("No folder", true);
            return;
        }

        ShowNotice(new Notice("Filter", "Only the entries whose name contains the text are listed; empty clears the filter.",
        [
            new OverlayChoice("&Filter", () => ApplyFilter(Overlay.InputText.Trim()), IsDefault: true),
            new OverlayChoice("&Cancel", IsCancel: true),
        ])
        {
            Input = new OverlayInput("Name contains:", "Name contains", _filter, string.Empty, Required: false),
        });
    }

    /// <summary>The filter set and the list made again, the entry kept when it still shows; the list's new name and the entry are said, "3 of 24 items, filter chap, Chapter 1.mp3", or "Filter cleared" when it is emptied.</summary>
    private void ApplyFilter(string text)
    {
        var had = _filter.Length > 0;
        _filter = text;
        Rearrange(CurrentEntry()?.Name);
        if (text.Length == 0)
        {
            if (had)
            {
                Announce("Filter cleared", true);
            }

            return;
        }

        Announce($"{_list.AccessibleName}, {_list.SelectedItem}", true);
    }

    /// <summary>Escape in the list: the selection cleared, as Ctrl+Shift+A, while entries are selected (AF-3.7); otherwise the filter cleared; false, the key left alone, when there is neither.</summary>
    private bool ClearSelectionOrFilter()
    {
        if (_marks.Count > 0)
        {
            SelectNone();
            return true;
        }

        if (_filter.Length == 0)
        {
            return false;
        }

        ApplyFilter(string.Empty);
        return true;
    }

    /// <summary>The list item of an entry (AF-2.2): its label, with ", selected" while it is (AF-3.7).</summary>
    private string ItemText(Entry entry) => _marks.Contains(entry.Path) ? entry.Label + ", selected" : entry.Label;

    // ---- the selection (AF-3.7): the marks, one pure piece (Marks), applied here to the items and the Actions list ----
    // Every change of the marks is a line of the Actions list's Selection section (AF-9.2), which Ctrl+Z on the line
    // takes back (AF-9.3); a Shift range is one line, rewritten while the range goes on.

    /// <summary>The line made last for a change of the marks, which is the Shift range's while one is being made (<see cref="RangeLine"/>).</summary>
    private ActionEntry? _rangeLine;

    /// <summary>The line of the Shift range being made, rewritten as it goes on; null once the range has ended (AF-9.2).</summary>
    private ActionEntry? RangeLine => _marks.RangeOn ? _rangeLine : null;

    /// <summary>
    /// One change of the marks applied (AF-3.7): the items whose mark flipped rewritten, so that the list is not made
    /// again under NVDA; its line recorded in the Actions list, a Shift range's rewritten while the range goes on; and
    /// what the key says said.
    /// </summary>
    private void ApplyMarks(MarkChange change)
    {
        if (change.Changed.Count > 1)
        {
            _list.BeginUpdate();
        }

        foreach (var row in change.Changed)
        {
            _list.Items[row] = ItemText(_shown[row]);
        }

        if (change.Changed.Count > 1)
        {
            _list.EndUpdate();
        }

        if (change.Line is { } text)
        {
            if (!change.RangeBegins && RangeLine is { } line)
            {
                _actions.Rewrite(line, text);
                line.Select = change.Select;
            }
            else
            {
                _rangeLine = _actions.Mark(text, _folder ?? string.Empty, change.Select, change.Before);
            }

            FillPane();
        }

        if (change.Said.Length > 0)
        {
            Announce(change.Said, true);
        }
    }

    /// <summary>A Selection menu operation (AF-3.7): on the marked entries alone, "None selected" when there are none, never the current entry.</summary>
    private void OnMarks(Action operation)
    {
        if (_marks.Count == 0)
        {
            Announce("None selected", true);
            return;
        }

        operation();
    }

    /// <summary>Another folder shown: the marks are gone (<see cref="Marks.Clear"/>), and their lines with them (AF-9.2).</summary>
    private void DropSelectionLines()
    {
        _actions.DropSelection();
        FillPane();
    }

    /// <summary>Ctrl+Z on a selection line (AF-9.3): the marks as they were before it, entries gone since skipped; "Undone: 3 selected".</summary>
    private void UndoMarks(ActionEntry entry)
    {
        _actions.Undone(entry);
        FillPane();
        ApplyMarks(_marks.Restore(entry.Before ?? [], _shown));
    }

    /// <summary>Space in the list: the entry selected, or deselected again, its item reading ", selected" while it is; "Selected" or "Deselected", with the count when more are.</summary>
    private void ToggleSelection()
    {
        if (!TryEntry(out _))
        {
            return;
        }

        ApplyMarks(_marks.Toggle(_shown, _list.SelectedIndex));
    }

    /// <summary>Ctrl+A in the list, Selection → Select all: every entry shown, "n selected".</summary>
    private void SelectAll()
    {
        if (_shown.Count == 0)
        {
            SayNoEntry();
            return;
        }

        ApplyMarks(_marks.All(_shown, _list.SelectedIndex));
    }

    /// <summary>Ctrl+Shift+A in the list, Selection → Deselect all: the marks cleared, "None selected".</summary>
    private void SelectNone() => ApplyMarks(_marks.None(_shown, _list.SelectedIndex));

    /// <summary>Shift+Space in the list (AF-3.7): every entry from the nearest marked entry to the current one marked, "Selected, n selected"; with no other entry marked, Space.</summary>
    private void SelectToHere()
    {
        if (!TryEntry(out _))
        {
            return;
        }

        ApplyMarks(_marks.ToHere(_shown, _list.SelectedIndex));
    }

    /// <summary>
    /// Shift with Up, Down, Home or End in the list (AF-3.7): the current entry moves to <paramref name="target"/>
    /// and every entry between where the range began and there is marked, as in File Explorer (<see cref="Marks.Extend"/>).
    /// Each entry passed reads ", selected" on its own; nothing else is said. On a line after the entries (an empty
    /// folder's, the not-shown line) the keys say that line.
    /// </summary>
    private void ExtendSelection(int target)
    {
        if (!TryEntry(out _))
        {
            return;
        }

        target = Math.Clamp(target, 0, _shown.Count - 1);
        // The items first, the move after: the entry reached is read once, with its mark.
        ApplyMarks(_marks.Extend(_shown, _list.SelectedIndex, target));
        _list.SelectedIndex = target;
    }

    /// <summary>The selected entries among those shown, in the list's order; none when nothing is selected.</summary>
    private List<Entry> SelectedEntries() => _shown.Where(entry => _marks.Contains(entry.Path)).ToList();

    /// <summary>What an operation acts on (AF-3.7): the selected entries, or the current entry alone when none is selected; empty, and said, when there is no entry.</summary>
    private List<Entry> Targets()
    {
        var targets = SelectedEntries();
        if (targets.Count == 0 && CurrentEntry() is { } entry)
        {
            targets.Add(entry);
        }

        if (targets.Count == 0)
        {
            SayNoEntry();
        }

        return targets;
    }

    /// <summary>The shown entry with that name, or that path (a drive of This PC is selected by its root); -1 when none.</summary>
    private int IndexOfName(string name)
    {
        for (var i = 0; i < _shown.Count; i++)
        {
            if (string.Equals(_shown[i].Name, name, StringComparison.OrdinalIgnoreCase) || string.Equals(_shown[i].Path, name, StringComparison.OrdinalIgnoreCase))
            {
                return i;
            }
        }

        return -1;
    }

    // ---- the Drives menu (AF-3.11) ----

    /// <summary>Drives ▸: the ready drives as "1. Local Disk (C:)", This PC, then Eject and Properties for the drive in hand.</summary>
    private void FillDrives()
    {
        var items = _drivesMenu.DropDownItems;
        items.Clear();
        var drives = FolderListing.Drives();
        for (var i = 0; i < drives.Count; i++)
        {
            var drive = drives[i];
            items.Add(new ToolStripMenuItem(AxitWindows.Label(i, drive.Name), null, (_, _) => Do(() => ShowFolderAsync(drive.Path, null, remember: true))));
        }

        items.Add(new ToolStripSeparator());
        items.Add(new ToolStripMenuItem("This &PC", null, (_, _) => Do(() => ShowFolderAsync(FolderListing.ThisPC, CurrentDrive()?.Path, remember: false))));
        items.Add(new ToolStripSeparator());
        items.Add(new ToolStripMenuItem("&Eject this drive", null, (_, _) => EjectDrive()));
        items.Add(new ToolStripMenuItem("Drive p&roperties", null, (_, _) => ShowDriveProperties()));
    }

    /// <summary>The drive in hand: the entry at This PC, the folder's own drive otherwise; null without a folder.</summary>
    private Entry? CurrentDrive()
    {
        if (_folder is null)
        {
            return null;
        }

        if (FolderListing.IsThisPC(_folder))
        {
            return CurrentEntry();
        }

        var root = Path.GetPathRoot(_folder);
        return FolderListing.Drives().FirstOrDefault(drive => string.Equals(drive.Path, root, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>Drives → Eject this drive: the shell's own Eject for a stick or a disc, "Ejecting <name>", and This PC when the folder was on it; "Failed: …" when the drive has no Eject or Windows refuses.</summary>
    private void EjectDrive()
    {
        if (CurrentDrive() is not { } drive)
        {
            Announce(_folder is null ? "No folder" : "Choose a drive first", true);
            return;
        }

        if (Shell.Eject(Handle, drive.Path) is { } reason)
        {
            Failed("Eject failed", reason);
            return;
        }

        Announce($"Ejecting {drive.Name}", true);
        if (RealFolder is not null)
        {
            Do(() => ShowFolderAsync(FolderListing.ThisPC, null, remember: false));
        }
    }

    /// <summary>Drives → Drive properties: the Windows Properties sheet of the drive in hand, with its space and tools.</summary>
    private void ShowDriveProperties()
    {
        if (CurrentDrive() is not { } drive)
        {
            Announce(_folder is null ? "No folder" : "Choose a drive first", true);
            return;
        }

        if (Shell.Properties(Handle, drive.Path) is { } reason)
        {
            Failed("Properties failed", reason);
        }
    }

    // ---- favorites (AF-3.9) ----

    /// <summary>Folder → Favorites ▸: the pinned folders that still exist as "1. name (path)", or "No favorites yet", then the key's item.</summary>
    private void FillFavorites()
    {
        RecentMenu.FillFolders(_favoritesItem, _settings.Favorites, "&No favorites yet", target => Do(() => ShowFolderAsync(target, null, remember: true)));
        _favoritesItem.DropDownItems.Add(new ToolStripSeparator());
        _favoritesItem.DropDownItems.Add(Item("&Add or remove this folder", ToggleFavorite, AxFileKeys.Favorite));
    }

    /// <summary>Ctrl+D: the folder shown pinned, "Added to Favorites", or unpinned again, "Removed from Favorites".</summary>
    private void ToggleFavorite()
    {
        if (!TryFolder(out var folder))
        {
            return;
        }

        var added = _settings.ToggleFavorite(folder);
        SaveSettings();
        Announce(added ? "Added to Favorites" : "Removed from Favorites", true);
    }

    /// <summary>The folder above a folder that is gone, the first one that exists; null when none is.</summary>
    private static string? NearestExisting(string folder)
    {
        for (var parent = FolderListing.Parent(folder); parent is not null; parent = FolderListing.Parent(parent))
        {
            if (Directory.Exists(parent))
            {
                return parent;
            }
        }

        return null;
    }

    private static DateTime Stamp(string folder)
    {
        try
        {
            return Directory.GetLastWriteTimeUtc(folder);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return DateTime.MinValue;
        }
    }

    /// <summary>
    /// The shown entries in the command palette, after the commands (AF-3.12, bundle AX-10.5): Enter on one selects
    /// it in the folder list and opens it as Enter there does.
    /// </summary>
    protected override IEnumerable<PaletteEntry> PaletteEntries()
    {
        var kind = FolderListing.IsThisPC(_folder) ? "drive" : null;
        return _shown.Select(entry => new PaletteEntry($"{entry.Name}, {kind ?? (entry.IsFolder ? "folder" : "file")}", () =>
        {
            var index = IndexOfName(entry.Path);
            if (index < 0)
            {
                // The folder changed between the palette's opening and Enter.
                SayNoEntry();
                return;
            }

            _list.Select();
            _list.SelectedIndex = index;
            OpenSelected();
        }));
    }

    // ---- browsing (AF-3.1) ----

    /// <summary>Enter in the list (G2): a folder opens here, a file in the program the user has made its default in Windows, as in File Explorer (AF-3.2, AF-D2).</summary>
    private void OpenSelected()
    {
        if (!TryEntry(out var entry))
        {
            return;
        }

        if (entry.IsFolder)
        {
            Do(() => ShowFolderAsync(entry.Path, null, remember: false));
        }
        else
        {
            OpenWithWindows(entry);
        }
    }

    /// <summary>Shift+Enter in the list, Item → Preview (AF-3.2): a file read in axPreview, which hands it to its open window itself; a folder in a new axFile window, File Explorer's Shift+Enter.</summary>
    private void OpenInPreview()
    {
        if (!TryEntry(out var entry))
        {
            return;
        }

        AxitMenu.Start(entry.IsFolder ? Dispatch.AxFile : Dispatch.AxPreview, entry.Path);
    }

    /// <summary>Backspace, Alt+Up, Folder → Up one level: the parent with the folder just left selected; This PC with the drive selected from a drive's root (AF-3.10); "Top" at This PC.</summary>
    private void GoUp()
    {
        if (_folder is null)
        {
            Announce("No folder", true);
            return;
        }

        if (FolderListing.IsThisPC(_folder))
        {
            Announce("Top", true);
            return;
        }

        var from = _folder;
        if (FolderListing.Parent(from) is not { } parent)
        {
            Do(() => ShowFolderAsync(FolderListing.ThisPC, from, remember: false));
            return;
        }

        var left = PathName.Of(from);
        Do(() => ShowFolderAsync(parent, left, remember: false));
    }

    /// <summary>Right Arrow in the list (AF-3.1, AF-D12): a folder or a drive opened, as Enter; the edge earcon on a file or a line that is not an entry, so that an arrow key never starts a program.</summary>
    private void IntoFolder()
    {
        if (CurrentEntry() is { IsFolder: true } entry)
        {
            Do(() => ShowFolderAsync(entry.Path, null, remember: false));
            return;
        }

        Sounds.Edge();
    }

    /// <summary>Left Arrow in the list (AF-3.1, AF-D12): up one level, as Backspace; the edge earcon at This PC.</summary>
    private void OutOfFolder()
    {
        if (FolderListing.IsThisPC(_folder))
        {
            Sounds.Edge();
            return;
        }

        GoUp();
    }

    /// <summary>Folder → Open folder… (Ctrl+O): the bundle's folder picker, one of the system's own windows, on the folder shown.</summary>
    private void OpenFolderDialog()
    {
        var chosen = ChooseFolder("Choose the folder to show", _folder);
        _list.Select();
        if (chosen is not null)
        {
            Do(() => ShowFolderAsync(chosen, null, remember: true));
        }
    }

    /// <summary>Folder → Open recent folder (AF-3.3): the last ten opened by command, those that still exist, "1. name (path)" (AX-9.5).</summary>
    private void FillRecent() =>
        RecentMenu.FillFolders(_recentItem, _settings.RecentFolders, "&No recent folders", target => Do(() => ShowFolderAsync(target, null, remember: true)));

    // ---- completion in the Go to folder field (AF-3.3, AF-D12) ----

    /// <summary>The completions of the text last completed from, in the list's order.</summary>
    private IReadOnlyList<Completion> _completions = [];

    /// <summary>The completion the field holds; -1 before the first.</summary>
    private int _completionAt = -1;

    /// <summary>The text the last completion put in the field: while the field still holds it, Down and Up move among the same completions; anything typed starts afresh.</summary>
    private string? _completed;

    /// <summary>Counts the folder reads, so that a slow read that comes back after a newer one is dropped.</summary>
    private int _completionReads;

    /// <summary>The Go to folder field's keys: Down and Up the next and the previous completion, Right at the end of a completed folder its first entry.</summary>
    private bool OnPathKey(Keys key)
    {
        switch (key)
        {
            case AxFileKeys.CompleteNext:
                Do(() => CompleteAsync(1));
                return true;
            case AxFileKeys.CompletePrevious:
                Do(() => CompleteAsync(-1));
                return true;
            case AxFileKeys.IntoFolder when _completed is { } done && done.EndsWith('\\') && Overlay.InputText == done && Overlay.InputCaret == done.Length:
                // The folder's own entries next: the text is now what the completion starts from.
                _completed = null;
                Do(() => CompleteAsync(1));
                return true;
        }

        return false;
    }

    /// <summary>
    /// The next or the previous completion in the field, "Documents, folder, 2 of 5"; the folder the text names is read
    /// on a task when the text is new; past either end the edge earcon and then the completion again, as the palette
    /// and axStart do at an end (AX-10.4); the earcon alone when no name starts with the text.
    /// </summary>
    private async Task CompleteAsync(int step)
    {
        var typed = Overlay.InputText;
        if (typed != _completed)
        {
            var (folder, start) = PathCompletion.Split(typed, _folder);
            var read = ++_completionReads;
            var (hidden, dot) = (_settings.ShowHidden, _settings.ShowDotFiles);
            var found = await Task.Run(() => PathCompletion.Candidates(folder, start, hidden, dot));
            if (read != _completionReads || !NoticeOpen || Overlay.InputText != typed)
            {
                return;
            }

            _completions = found;
            _completionAt = step > 0 ? -1 : found.Count;
        }

        var next = _completionAt + step;
        if (next < 0 || next >= _completions.Count)
        {
            Sounds.Edge();
            if (_completionAt >= 0 && _completionAt < _completions.Count)
            {
                Announce(PathCompletion.Spoken(_completions[_completionAt], _completionAt, _completions.Count), true);
            }

            return;
        }

        _completionAt = next;
        var completion = _completions[next];
        _completed = completion.Text;
        Overlay.SetInputText(completion.Text);
        Announce(PathCompletion.Spoken(completion, next, _completions.Count), true);
    }

    /// <summary>The field's first text: the folder shown with a backslash at its end, so that the first Down completes among its own entries; empty at This PC.</summary>
    private string PathToComplete() =>
        RealFolder is not { } folder ? string.Empty : folder.EndsWith('\\') ? folder : folder + "\\";

    /// <summary>Folder → Go to folder… (Ctrl+G): a typed path, environment variables allowed, a file's path selecting the file, with completion on Down and Up (AF-3.3).</summary>
    private void GoToFolder()
    {
        _completed = null;
        _completions = [];
        ShowNotice(new Notice("Go to folder", "A folder's path, or a file's to select it in its folder. %USERPROFILE%\\Downloads and the other variables work. Down and Up complete the path from what is typed; Right goes into a completed folder.",
        [
            new OverlayChoice("&Go", () =>
            {
                var typed = Overlay.InputText;
                if (FolderListing.Resolve(typed) is not { } target)
                {
                    Announce("Not found: " + typed.Trim(), true);
                    return;
                }

                Do(() => ShowFolderAsync(target.Folder, target.File is { } file ? Path.GetFileName(file) : null, remember: true));
            }, IsDefault: true),
            new OverlayChoice("&Cancel", IsCancel: true),
        ])
        {
            Input = new OverlayInput("Path:", "Path", PathToComplete(), "Type a path", KeyHook: OnPathKey),
        });
    }

    /// <summary>Folder → Refresh (F5): the folder read again, the selection kept by name (AF-3.4).</summary>
    private async Task RefreshAsync(bool announce)
    {
        if (_folder is null)
        {
            Announce("No folder", true);
            return;
        }

        if (await ShowFolderAsync(_folder, CurrentEntry()?.Name, remember: false) && announce)
        {
            Announce("Refreshed", true);
        }
    }

    /// <summary>Coming back to the window (AF-3.4): the folder read again, silently, when its write time moved; nothing otherwise.</summary>
    private async Task RefreshIfChangedAsync()
    {
        if (_folder is { } folder && !NoticeOpen && Stamp(folder) != _folderStamp)
        {
            await ShowFolderAsync(folder, CurrentEntry()?.Name, remember: false);
        }
    }

    // ---- find (AF-3.6) and information (AF-2.4) ----

    /// <summary>Ctrl+F: the bundle's find in a list over the shown entries' names (AF-3.6); the lines after the entries are not searched.</summary>
    private void ShowFindNotice() => ShowFindInList(_list, EntryNames);

    /// <summary>F3 and Shift+F3: the next (or previous) entry whose name contains the text, wrapping once; "Not found" otherwise.</summary>
    private void FindInList(int direction) => FindInList(_list, EntryNames, direction);

    private IReadOnlyList<string> EntryNames() => _shown.Select(entry => entry.Name).ToList();

    /// <summary>Item → Information (Ctrl+I): the entry's size, or that it is a folder, and its date, on demand (AF-D7); and how many entries are selected, when any are (AF-3.7).</summary>
    private void SayInformation()
    {
        if (!TryEntry(out var entry))
        {
            return;
        }

        var count = _marks.Count;
        Announce(FolderListing.Information(entry) + (count > 0 ? $", {count} selected" : string.Empty), true);
    }
}
