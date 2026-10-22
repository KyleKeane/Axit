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

    /// <summary>The paths selected with Space (AF-3.7), kept while the same folder shows; their items read ", selected".</summary>
    private readonly HashSet<string> _selected = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Where a Shift range began (AF-3.7), where it has reached, and the marks there were before it; the anchor is null while no range is being made.</summary>
    private int? _anchor;
    private int _anchorEnd = -1;
    private HashSet<string> _anchorBase = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>The folder's last write time at the last read, so that coming back to the window knows whether to read again (AF-3.4).</summary>
    private DateTime _folderStamp;

    /// <summary>The number of the latest read: a slow read of a folder left behind is dropped when it arrives.</summary>
    private int _reads;

    /// <summary>Type-ahead (AF-3.6): the letters typed within a second of each other, gathered into one prefix as File Explorer gathers them, and when the last came.</summary>
    private string _typed = string.Empty;
    private long _typedAt;
    private const int TypeAheadMs = 1000;

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
    /// <paramref name="announce"/> is off: the start, which NVDA reads itself, and an undo, which says what it undid.
    /// </summary>
    private async Task<bool> ShowFolderAsync(string folder, string? select, bool remember, bool announce = true)
    {
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

        // AF-3.7, AF-3.8: the selection and the filter live in one folder: another folder starts with none, the same
        // one keeps the marks still there and the filter.
        if (!string.Equals(folder, _folder, StringComparison.OrdinalIgnoreCase))
        {
            _selected.Clear();
            _anchor = null;
            _filter = string.Empty;
        }
        else
        {
            _selected.IntersectWith(listing.Entries.Select(entry => entry.Path));
        }

        _folder = folder;
        _entries = listing.Entries;
        _hidden = listing.Hidden;
        _dot = listing.Dot;
        _folderStamp = Stamp(folder);
        Text = $"{FolderListing.FolderName(folder)} - {AxFileApp.Name}";
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
        var name = FolderListing.IsThisPC(_folder) ? FolderListing.ThisPC : FolderListing.FolderName(_folder ?? string.Empty);
        var item = _list.SelectedItem?.ToString() ?? string.Empty;
        return _shown.Count == 0 ? $"{name}, {item}" : $"{name}, {_list.AccessibleName}, {item}";
    }

    /// <summary>
    /// Letters typed in the folder list (AF-3.6), as File Explorer takes them: typed within a second of each other
    /// they build one prefix, "cha", and the first entry whose name starts with it is selected, searched from the
    /// entry in hand and round the end; a new prefix starts after a second's pause and is searched from the next
    /// entry on, and the same letter typed again cycles through the names starting with it. "No names start with
    /// <prefix>" when none does. The list's own one-letter jump gives way to this. NVDA reads the entry selected.
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
        var letter = e.KeyChar.ToString();
        var current = Math.Max(0, Math.Min(_list.SelectedIndex, _shown.Count - 1));
        int from;
        if (now - _typedAt > TypeAheadMs || _typed.Length == 0 || _typed.All(c => char.ToUpperInvariant(c) == char.ToUpperInvariant(e.KeyChar)))
        {
            // A new prefix, or the same letter again: the next entry starting with it.
            _typed = letter;
            from = current + 1;
        }
        else
        {
            _typed += letter;
            from = current;
        }

        _typedAt = now;
        for (var step = 0; step < _shown.Count; step++)
        {
            var i = (from + step) % _shown.Count;
            if (_shown[i].Name.StartsWith(_typed, StringComparison.OrdinalIgnoreCase))
            {
                _list.SelectedIndex = i;
                return;
            }
        }

        Announce($"No names start with {_typed}", true);
    }

    /// <summary>
    /// Up or Home at the first entry, Down or End at the last (AF-3.6), in either list: the key moves nothing, so
    /// "First item, <name>" or "Last item, <name>" is said instead of silence; false when the key moves.
    /// </summary>
    private bool SayBoundary(Keys key)
    {
        var list = _pane.Focused ? _pane : _list;
        var index = list.SelectedIndex;
        if (index < 0 || list.Items.Count == 0)
        {
            return false;
        }

        var first = key is Keys.Up or Keys.Home && index == 0;
        var last = key is Keys.Down or Keys.End && index == list.Items.Count - 1;
        if (!first && !last)
        {
            return false;
        }

        Announce((first ? "First item, " : "Last item, ") + list.Items[index], true);
        return true;
    }

    /// <summary>The one item of an empty folder (AF-2.2), so that the list is never empty and the reason is read (AX-9.5).</summary>
    private const string EmptyFolderText = "No items in this folder";

    /// <summary>The one item of a folder whose entries all fail the filter (AF-3.8).</summary>
    private string NoMatchText => "No names contain " + _filter;

    /// <summary>The shown entries made again from the folder's entries, the order and the filter (AF-3.8), and the list refilled.</summary>
    private void Rearrange(string? select)
    {
        // A Shift range is indices into the shown entries: a new order or filter, or a read, ends it (AF-3.7).
        _anchor = null;
        // The drives of This PC stay in letter order (AF-3.10).
        _shown = FolderListing.IsThisPC(_folder) ? FolderListing.Filter(_entries, _filter) : FolderListing.Arrange(_entries, _settings.SortBy, _filter);
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

    /// <summary>The guard of every command on an entry (AF-7): "No folder", the line the selection is on when it is not an entry (the empty folder's, the filter's, the not-shown line), or "Nothing selected".</summary>
    private void SayNoEntry()
    {
        var index = _list.SelectedIndex;
        var line = index >= _shown.Count && index < _list.Items.Count ? _list.Items[index].ToString() : null;
        Announce(_folder is null ? "No folder" : line ?? "Nothing selected", true);
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

    /// <summary>Escape in the list: the filter cleared; false, the key left alone, when there is none.</summary>
    private bool ClearFilter()
    {
        if (_filter.Length == 0)
        {
            return false;
        }

        ApplyFilter(string.Empty);
        return true;
    }

    /// <summary>The list item of an entry (AF-2.2): its label, with ", selected" while it is (AF-3.7).</summary>
    private string ItemText(Entry entry) => _selected.Contains(entry.Path) ? entry.Label + ", selected" : entry.Label;

    // ---- the selection (AF-3.7): marks on entries, which the operations act on instead of the current entry ----

    /// <summary>Space in the list: the entry selected, or deselected again, its item reading ", selected" while it is; "Selected" or "Deselected", with the count when more are.</summary>
    private void ToggleSelection()
    {
        if (CurrentEntry() is not { } entry)
        {
            SayNoEntry();
            return;
        }

        var on = _selected.Add(entry.Path);
        if (!on)
        {
            _selected.Remove(entry.Path);
        }

        _anchor = null;
        _list.Items[_list.SelectedIndex] = ItemText(entry);
        var count = _selected.Count;
        var word = on ? "Selected" : "Deselected";
        Announce(count > (on ? 1 : 0) ? $"{word}, {count} selected" : word, true);
    }

    /// <summary>Ctrl+A in the list, File operations → Select all: every entry shown, "n selected".</summary>
    private void SelectAll()
    {
        if (_shown.Count == 0)
        {
            SayNoEntry();
            return;
        }

        foreach (var entry in _shown)
        {
            _selected.Add(entry.Path);
        }

        _anchor = null;
        FillList(CurrentEntry()?.Name);
        Announce($"{_selected.Count} selected", true);
    }

    /// <summary>Ctrl+Shift+A in the list, Item → Select none: the marks cleared, "None selected".</summary>
    private void SelectNone()
    {
        _anchor = null;
        if (_selected.Count > 0)
        {
            _selected.Clear();
            FillList(CurrentEntry()?.Name);
        }

        Announce("None selected", true);
    }

    /// <summary>
    /// Shift with Up, Down, Home or End in the list (AF-3.7): the current entry moves to <paramref name="target"/>
    /// and every entry between where the range began and there is marked, as in File Explorer; a range begins where
    /// the first Shift key finds the current entry and keeps the marks made before it, and moving back shrinks it.
    /// Each entry passed reads ", selected" on its own; nothing else is said.
    /// </summary>
    private void ExtendSelection(int target)
    {
        var current = _list.SelectedIndex;
        if (current < 0 || current >= _shown.Count)
        {
            // No entry, or one of the lines after the entries (the empty folder's, the not-shown line).
            SayNoEntry();
            return;
        }

        if (_anchor is null || current != _anchorEnd)
        {
            _anchor = current;
            _anchorBase = new HashSet<string>(_selected, StringComparer.OrdinalIgnoreCase);
        }

        target = Math.Clamp(target, 0, _shown.Count - 1);
        _anchorEnd = target;
        var before = new HashSet<string>(_selected, StringComparer.OrdinalIgnoreCase);
        _selected.Clear();
        _selected.UnionWith(_anchorBase);
        for (var i = Math.Min(_anchor.Value, target); i <= Math.Max(_anchor.Value, target); i++)
        {
            _selected.Add(_shown[i].Path);
        }

        // Only the items whose mark changed are rewritten, so that the list is not rebuilt under NVDA on every key.
        for (var i = 0; i < _shown.Count; i++)
        {
            if (before.Contains(_shown[i].Path) != _selected.Contains(_shown[i].Path))
            {
                _list.Items[i] = ItemText(_shown[i]);
            }
        }

        _list.SelectedIndex = target;
    }

    /// <summary>The selected entries among those shown, in the list's order; none when nothing is selected.</summary>
    private List<Entry> SelectedEntries() => _shown.Where(entry => _selected.Contains(entry.Path)).ToList();

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

    /// <summary>
    /// Whether a command that needs a real folder has none: "No folder" before any is shown, "Choose a drive first"
    /// at This PC (AF-3.10), each said; false when there is one.
    /// </summary>
    private bool NoFolderHere()
    {
        if (_folder is null)
        {
            Announce("No folder", true);
            return true;
        }

        if (FolderListing.IsThisPC(_folder))
        {
            Announce("Choose a drive first", true);
            return true;
        }

        return false;
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
        if (_folder is { } folder && !FolderListing.IsThisPC(folder))
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
        RecentMenu.Fill(_favoritesItem, _settings.Favorites, Directory.Exists, RecentMenu.FolderName, "&No favorites yet", target => Do(() => ShowFolderAsync(target, null, remember: true)));
        _favoritesItem.DropDownItems.Add(new ToolStripSeparator());
        _favoritesItem.DropDownItems.Add(Item("&Add or remove this folder", ToggleFavorite, AxFileKeys.Favorite));
    }

    /// <summary>Ctrl+D: the folder shown pinned, "Added to Favorites", or unpinned again, "Removed from Favorites".</summary>
    private void ToggleFavorite()
    {
        if (NoFolderHere() || _folder is not { } folder)
        {
            return;
        }

        var added = _settings.ToggleFavorite(folder);
        SaveSettings();
        Announce(added ? "Added to Favorites" : "Removed from Favorites", true);
    }

    /// <summary>The entry under the selection; null on the lines after the entries (an empty list's, the not-shown line), which are not entries.</summary>
    private Entry? CurrentEntry() =>
        _list.SelectedIndex is var i && i >= 0 && i < _shown.Count ? _shown[i] : null;

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

    // ---- browsing (AF-3.1) ----

    /// <summary>Enter in the list (G2): a folder opens here, a file in the program the user has made its default in Windows, as in File Explorer (AF-3.2, AF-D2).</summary>
    private void OpenSelected()
    {
        if (CurrentEntry() is not { } entry)
        {
            SayNoEntry();
            return;
        }

        if (FolderListing.WayOf(entry) == OpenWay.Folder)
        {
            Do(() => ShowFolderAsync(entry.Path, null, remember: false));
        }
        else
        {
            OpenWithWindows(entry);
        }
    }

    /// <summary>Shift+Enter in the list, Item → Preview (AF-3.2): a file read in AxPreview, which hands it to its open window itself; a folder in a new AxFile window, File Explorer's Shift+Enter.</summary>
    private void OpenInPreview()
    {
        if (CurrentEntry() is not { } entry)
        {
            SayNoEntry();
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

        var left = FolderListing.FolderName(from);
        Do(() => ShowFolderAsync(parent, left, remember: false));
    }

    /// <summary>Folder → Open folder… (Ctrl+O): the Windows folder picker, one of the system's own windows.</summary>
    private void OpenFolderDialog()
    {
        using var dialog = new FolderBrowserDialog { Description = "Choose the folder to show", UseDescriptionForTitle = true, ShowNewFolderButton = true };
        if (_folder is not null)
        {
            dialog.InitialDirectory = _folder;
        }

        var chosen = dialog.ShowDialog(this) == DialogResult.OK && dialog.SelectedPath.Length > 0 ? dialog.SelectedPath : null;
        _list.Select();
        if (chosen is not null)
        {
            Do(() => ShowFolderAsync(chosen, null, remember: true));
        }
    }

    /// <summary>Folder → Open recent folder (AF-3.3): the last ten opened by command, those that still exist, "1. name (path)" (AX-9.5).</summary>
    private void FillRecent() =>
        RecentMenu.Fill(_recentItem, _settings.RecentFolders, Directory.Exists, RecentMenu.FolderName, "&No recent folders", target => Do(() => ShowFolderAsync(target, null, remember: true)));

    /// <summary>Folder → Go to folder… (Ctrl+G): a typed path, environment variables allowed, a file's path selecting the file (AF-3.3).</summary>
    private void GoToFolder() =>
        ShowNotice(new Notice("Go to folder", "A folder's path, or a file's to select it in its folder. %USERPROFILE%\\Downloads and the other variables work.",
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
            Input = new OverlayInput("Path:", "Path", _folder ?? string.Empty, "Type a path"),
        });

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
        if (CurrentEntry() is not { } entry)
        {
            SayNoEntry();
            return;
        }

        var count = _selected.Count;
        Announce(FolderListing.Information(entry) + (count > 0 ? $", {count} selected" : string.Empty), true);
    }
}
