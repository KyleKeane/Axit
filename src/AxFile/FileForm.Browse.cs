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

    /// <summary>The paths selected with Space (AF-3.7), kept while the same folder shows; their items read ", selected".</summary>
    private readonly HashSet<string> _selected = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>The folder's last write time at the last read, so that coming back to the window knows whether to read again (AF-3.4).</summary>
    private DateTime _folderStamp;

    /// <summary>The number of the latest read: a slow read of a folder left behind is dropped when it arrives.</summary>
    private int _reads;

    private string _findText = string.Empty;

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
        await ShowFolderAsync(folder, select, remember: true);
    }

    /// <summary>
    /// Shows a folder (AF-2.2): read on a background task, the list refilled with <paramref name="select"/> selected
    /// by name (or the first entry), the title and the status bar following. A folder that is gone gives the "Folder
    /// not found" notice and the nearest parent that exists (AF-3.5); any other failure is one announcement and the
    /// list stays. With <paramref name="remember"/> the folder joins the recent list (AF-3.3).
    /// </summary>
    private async Task<bool> ShowFolderAsync(string folder, string? select, bool remember)
    {
        var read = ++_reads;
        IReadOnlyList<Entry> entries;
        try
        {
            entries = await Task.Run(() => FolderListing.Read(folder, _settings.ShowHidden));
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
            _filter = string.Empty;
        }
        else
        {
            _selected.IntersectWith(entries.Select(entry => entry.Path));
        }

        _folder = folder;
        _entries = entries;
        _folderStamp = Stamp(folder);
        Text = $"{FolderListing.FolderName(folder)} - {AxFileApp.Name}";
        Rearrange(select);
        if (remember)
        {
            _settings.RememberFolder(folder);
            SaveSettings();
        }
        else
        {
            _settings.LastFolder = folder;
        }

        UpdateStatusBar();
        return true;
    }

    /// <summary>The one item of an empty folder (AF-2.2), so that the list is never empty and the reason is read (AX-9.5).</summary>
    private const string EmptyFolderText = "No items in this folder";

    /// <summary>The one item of a folder whose entries all fail the filter (AF-3.8).</summary>
    private string NoMatchText => "No names contain " + _filter;

    /// <summary>The shown entries made again from the folder's entries, the order and the filter (AF-3.8), and the list refilled.</summary>
    private void Rearrange(string? select)
    {
        _shown = FolderListing.Arrange(_entries, _settings.SortBy, _filter);
        FillList(select);
    }

    /// <summary>The list's items for the shown entries, named with the folder and the count; the selection by name, or the first entry.</summary>
    private void FillList(string? select)
    {
        _list.BeginUpdate();
        _list.Items.Clear();
        _list.Items.AddRange(_shown.Select(entry => (object)ItemText(entry)).ToArray());
        if (_shown.Count == 0)
        {
            _list.Items.Add(_filter.Length > 0 ? NoMatchText : EmptyFolderText);
        }

        _list.EndUpdate();
        _list.AccessibleName = FolderListing.ListName(_folder ?? string.Empty, _shown.Count, _entries.Count, _filter);
        var index = select is null ? -1 : IndexOfName(select);
        _list.SelectedIndex = Math.Max(0, index);
    }

    /// <summary>The guard of every command on an entry (AF-7): why there is none.</summary>
    private void SayNoEntry() =>
        Announce(_folder is null ? "No folder" : _entries.Count == 0 ? EmptyFolderText : _shown.Count == 0 ? NoMatchText : "Nothing selected", true);

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

    /// <summary>The filter set and the list made again, the entry kept when it still shows; the list's name says the count, "Filter cleared" when it is emptied.</summary>
    private void ApplyFilter(string text)
    {
        var had = _filter.Length > 0;
        _filter = text;
        Rearrange(CurrentEntry()?.Name);
        if (text.Length == 0 && had)
        {
            Announce("Filter cleared", true);
        }
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

        FillList(CurrentEntry()?.Name);
        Announce($"{_selected.Count} selected", true);
    }

    /// <summary>Ctrl+Shift+A in the list, File operations → Select none: the marks cleared, "None selected".</summary>
    private void SelectNone()
    {
        if (_selected.Count > 0)
        {
            _selected.Clear();
            FillList(CurrentEntry()?.Name);
        }

        Announce("None selected", true);
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

    private int IndexOfName(string name)
    {
        for (var i = 0; i < _shown.Count; i++)
        {
            if (string.Equals(_shown[i].Name, name, StringComparison.OrdinalIgnoreCase))
            {
                return i;
            }
        }

        return -1;
    }

    /// <summary>The entry under the selection; null for an empty list, whose one item is not an entry.</summary>
    private Entry? CurrentEntry() =>
        _list.SelectedIndex is var i && i >= 0 && i < _shown.Count && _list.Items.Count == _shown.Count ? _shown[i] : null;

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

    /// <summary>Enter in the list (G2): a folder opens here, a file in its app, a media file in the player, the rest in the program Windows has for it (AF-3.2).</summary>
    private void OpenSelected()
    {
        if (CurrentEntry() is not { } entry)
        {
            SayNoEntry();
            return;
        }

        switch (FolderListing.WayOf(entry))
        {
            case OpenWay.Folder:
                Do(() => ShowFolderAsync(entry.Path, null, remember: false));
                break;
            case OpenWay.AxDown:
                AxitMenu.Start(Dispatch.AxDown, entry.Path);
                break;
            case OpenWay.AxJSON:
                AxitMenu.Start(Dispatch.AxJSON, entry.Path);
                break;
            case OpenWay.Play:
                PlayFromList(entry);
                break;
            default:
                OpenWithWindows(entry);
                break;
        }
    }

    /// <summary>Backspace, Alt+Up, Folder → Up one level: the parent with the folder just left selected; "Top of the drive" at a root.</summary>
    private void GoUp()
    {
        if (_folder is null)
        {
            Announce("No folder", true);
            return;
        }

        if (FolderListing.Parent(_folder) is not { } parent)
        {
            Announce("Top of the drive", true);
            return;
        }

        var left = FolderListing.FolderName(_folder);
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

    private void ShowFindNotice() =>
        ShowNotice(new Notice("Find", string.Empty,
        [
            new OverlayChoice("&Find next", () =>
            {
                _findText = Overlay.InputText;
                FindInList(1);
            }, IsDefault: true),
            new OverlayChoice("&Cancel", IsCancel: true),
        ])
        {
            Input = new OverlayInput("Name contains:", "Name contains", _findText, "Type the text to find"),
        });

    /// <summary>The next (or previous) entry whose name contains the text, wrapping once; "Not found" otherwise; the Find notice when nothing was searched for yet.</summary>
    private void FindInList(int direction)
    {
        if (_findText.Length == 0)
        {
            ShowFindNotice();
            return;
        }

        var count = _shown.Count;
        var from = Math.Max(0, _list.SelectedIndex);
        for (var step = 1; step <= count; step++)
        {
            var i = ((from + direction * step) % count + count) % count;
            if (_shown[i].Name.Contains(_findText, StringComparison.OrdinalIgnoreCase))
            {
                _list.SelectedIndex = i;
                return;
            }
        }

        Announce("Not found", true);
    }

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
