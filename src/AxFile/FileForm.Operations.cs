using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using AxFile.Core;
using Axit.Core;
using Axit.Forms;

namespace AxFile;

/// <summary>
/// The operations (SPEC.md AF-4), each one announcement or one notice: the folder, the rename in place, the delete,
/// the path, the clipboard, the last three on the selected entries when there are any (AF-3.7); and the ways out of
/// the window (AF-1.3, AF-3.2, AF-4.7, AF-4.9).
/// </summary>
public sealed partial class FileForm
{
    private const int ErrorNoAssociation = 1155;

    /// <summary>File Explorer's clipboard format beside the file list: a number saying whether the files were cut (move) or copied.</summary>
    private const string DropEffectFormat = "Preferred DropEffect";
    private const int DropEffectCopy = 1;
    private const int DropEffectMove = 2;

    /// <summary>The entry being renamed in place (AF-4.2) while the box over it shows; null otherwise.</summary>
    private Entry? _renaming;

    /// <summary>File operations → New folder… (Ctrl+Shift+N, AF-4.1): the name in a notice, the folder made and selected, "Created <name>".</summary>
    private void NewFolder()
    {
        if (_folder is not { } folder)
        {
            Announce("No folder", true);
            return;
        }

        ShowNotice(new Notice("New folder", string.Empty,
        [
            new OverlayChoice("&Create", () =>
            {
                var name = Overlay.InputText.Trim();
                if (FileOperations.CheckName(name) is { } problem)
                {
                    Announce(problem, true);
                    return;
                }

                Do(async () =>
                {
                    try
                    {
                        await Task.Run(() => FileOperations.CreateFolder(folder, name));
                    }
                    catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                    {
                        Failed("New folder failed", FileOperations.Message(ex));
                        return;
                    }

                    if (await ShowFolderAsync(folder, name, remember: false))
                    {
                        Announce($"Created {name}", true);
                    }
                });
            }, IsDefault: true),
            new OverlayChoice("C&ancel", IsCancel: true),
        ])
        {
            Input = new OverlayInput("Name:", "Name", string.Empty, "Type a name"),
        });
    }

    // ---- the rename in place (AF-4.2): a box over the entry, Enter renames, Escape or leaving the box keeps the name ----

    /// <summary>File operations → Rename (F2): the box over the entry with its name, the part before the extension selected.</summary>
    private void Rename()
    {
        if (_renaming is not null)
        {
            return;
        }

        if (_folder is null || CurrentEntry() is not { } entry)
        {
            SayNoEntry();
            return;
        }

        _renaming = entry;
        _renameBox.Bounds = _list.GetItemRectangle(_list.SelectedIndex);
        _renameBox.Text = entry.Name;
        // The selection goes before the focus: a text box that gets the focus selects everything unless one was set.
        _renameBox.Select(0, entry.IsFolder ? entry.Name.Length : Path.GetFileNameWithoutExtension(entry.Name).Length);
        _renameBox.Visible = true;
        _renameBox.BringToFront();
        _renameBox.Select();
    }

    /// <summary>Enter and Escape in the box are the box's own, as a notice's field's (AF-6.2).</summary>
    private void OnRenameKey(object? sender, KeyEventArgs e)
    {
        if (e.KeyData == Keys.Return)
        {
            e.SuppressKeyPress = true;
            CommitRename();
        }
        else if (e.KeyData == Keys.Escape)
        {
            e.SuppressKeyPress = true;
            CancelRename();
        }
    }

    /// <summary>The typed name checked and applied: "Renamed to <name>", the entry selected under its new name.</summary>
    private void CommitRename()
    {
        if (_renaming is not { } entry || _folder is not { } folder)
        {
            return;
        }

        var name = _renameBox.Text.Trim();
        if (FileOperations.CheckName(name) is { } problem)
        {
            Announce(problem, true);
            return;
        }

        EndRename();
        if (string.Equals(name, entry.Name, StringComparison.Ordinal))
        {
            Announce("Name unchanged", true);
            return;
        }

        Do(async () =>
        {
            try
            {
                await Task.Run(() => FileOperations.Rename(entry.Path, name));
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                Failed("Rename failed", FileOperations.Message(ex));
                return;
            }

            if (await ShowFolderAsync(folder, name, remember: false))
            {
                Announce($"Renamed to {name}", true);
            }
        });
    }

    /// <summary>The box closed with the name kept; the list's reading of the entry is the feedback.</summary>
    private void CancelRename()
    {
        if (_renaming is null)
        {
            return;
        }

        EndRename();
    }

    private void EndRename()
    {
        _renaming = null;
        // The focus goes back first, so that hiding the box does not send it somewhere of WinForms' choosing.
        if (!IsDisposed && _list.Visible && !NoticeOpen && !_list.Focused)
        {
            _list.Select();
        }

        _renameBox.Visible = false;
    }

    /// <summary>
    /// File operations → Delete… (Delete, AF-4.3): a question, then the shell sends the selected entries, or the
    /// current one, to the Recycle Bin in one operation; "Deleted <name>" or "Deleted n items", the entry after them
    /// selected.
    /// </summary>
    private void Delete()
    {
        if (_folder is not { } folder)
        {
            SayNoEntry();
            return;
        }

        var targets = Targets();
        if (targets.Count == 0)
        {
            return;
        }

        var next = NameAfter(targets);
        var one = targets.Count == 1 ? targets[0] : null;
        var title = one is null ? $"Delete {targets.Count} items?" : $"Delete {one.Name}?";
        var text = one is null
            ? targets.Any(target => target.IsFolder) ? "They go to the Recycle Bin, the folders with everything in them." : "They go to the Recycle Bin."
            : one.IsFolder ? "It goes to the Recycle Bin with everything in it." : "It goes to the Recycle Bin.";
        var done = one is null ? $"Deleted {targets.Count} items" : $"Deleted {one.Name}";
        ShowNotice(new Notice(title, text,
        [
            new OverlayChoice("&Delete", () => Do(async () =>
            {
                if (await FileOperations.DeleteToRecycleBinAsync(targets.Select(target => target.Path).ToList()) is { } reason)
                {
                    Failed("Delete failed", reason);
                    return;
                }

                if (await ShowFolderAsync(folder, next, remember: false))
                {
                    Announce(done, true);
                }
            }), IsDefault: true),
            new OverlayChoice("&Cancel", IsCancel: true),
        ]));
    }

    /// <summary>The name to select once <paramref name="gone"/> are deleted: the first entry after the current one that stays, else the last one before it; null when none stays.</summary>
    private string? NameAfter(List<Entry> gone)
    {
        var index = _list.SelectedIndex;
        for (var i = index + 1; i < _entries.Count; i++)
        {
            if (!gone.Contains(_entries[i]))
            {
                return _entries[i].Name;
            }
        }

        for (var i = Math.Min(index, _entries.Count - 1); i >= 0; i--)
        {
            if (!gone.Contains(_entries[i]))
            {
                return _entries[i].Name;
            }
        }

        return null;
    }

    /// <summary>File operations → Copy path (Ctrl+Shift+C, AF-4.4): the full path of the entry, or of each selected entry on a line, to the clipboard; "Copied path" or "Copied n paths".</summary>
    private void CopyPath()
    {
        var targets = Targets();
        if (targets.Count == 0)
        {
            return;
        }

        try
        {
            Clipboard.SetText(string.Join("\r\n", targets.Select(target => target.Path)));
            Announce(targets.Count == 1 ? "Copied path" : $"Copied {targets.Count} paths", true);
        }
        catch (Exception ex) when (ex is ExternalException or InvalidOperationException)
        {
            Failed("Copy failed", ex.Message);
        }
    }

    // ---- the clipboard (AF-4.8): the entries as files on it, in File Explorer's formats, and its files pasted into the folder ----

    /// <summary>File operations → Cut or Copy (Ctrl+X, Ctrl+C): the selected entries, or the current one, on the clipboard as files, marked to move or to copy; "Cut <name>", "Copied n items".</summary>
    private void CopyOrCut(bool cut)
    {
        var targets = Targets();
        if (targets.Count == 0)
        {
            return;
        }

        var files = new StringCollection();
        files.AddRange(targets.Select(target => target.Path).ToArray());
        var data = new DataObject();
        data.SetFileDropList(files);
        data.SetData(DropEffectFormat, new MemoryStream(BitConverter.GetBytes(cut ? DropEffectMove : DropEffectCopy)));
        try
        {
            Clipboard.SetDataObject(data, true);
        }
        catch (ExternalException ex)
        {
            Failed(cut ? "Cut failed" : "Copy failed", ex.Message);
            return;
        }

        Announce((cut ? "Cut " : "Copied ") + (targets.Count == 1 ? targets[0].Name : $"{targets.Count} items"), true);
    }

    /// <summary>
    /// File operations → Paste (Ctrl+V): the clipboard's files copied, or moved after a cut, into the folder shown
    /// by the shell; "Pasted <name>" or "Pasted n items", with "Pasting, please wait" when it takes longer than a second.
    /// </summary>
    private void Paste()
    {
        if (_folder is not { } folder)
        {
            Announce("No folder", true);
            return;
        }

        List<string> paths;
        bool move;
        try
        {
            var data = Clipboard.GetDataObject();
            if (data is null || !data.GetDataPresent(DataFormats.FileDrop))
            {
                Announce("Nothing to paste", true);
                return;
            }

            paths = (data.GetData(DataFormats.FileDrop) as string[] ?? []).Where(path => path.Length > 0).ToList();
            move = data.GetData(DropEffectFormat) is Stream effect && (ReadDropEffect(effect) & DropEffectMove) != 0;
        }
        catch (ExternalException ex)
        {
            Failed("Paste failed", ex.Message);
            return;
        }

        if (move)
        {
            // A cut entry already in this folder has nowhere to go.
            paths.RemoveAll(path => string.Equals(Path.GetDirectoryName(path), folder, StringComparison.OrdinalIgnoreCase));
        }

        if (paths.Count == 0)
        {
            Announce(move ? "Already in this folder" : "Nothing to paste", true);
            return;
        }

        Do(async () =>
        {
            var operation = FileOperations.CopyOrMoveAsync(paths, folder, move);
            if (await Task.WhenAny(operation, Task.Delay(1000)) != operation)
            {
                Announce("Pasting, please wait", true);
            }

            if (await operation is { } reason)
            {
                Failed("Paste failed", reason);
                return;
            }

            if (move)
            {
                // The files are gone from where they were; a second paste would find nothing, as in File Explorer.
                try
                {
                    Clipboard.Clear();
                }
                catch (ExternalException)
                {
                }
            }

            var first = Path.GetFileName(paths[0]);
            if (await ShowFolderAsync(folder, first, remember: false))
            {
                Announce(paths.Count == 1 ? $"Pasted {first}" : $"Pasted {paths.Count} items", true);
            }
        });
    }

    private static int ReadDropEffect(Stream stream)
    {
        var bytes = new byte[4];
        stream.Position = 0;
        return stream.Read(bytes, 0, bytes.Length) == bytes.Length ? BitConverter.ToInt32(bytes, 0) : 0;
    }

    // ---- the ways out (AF-1.3, AF-3.2, AF-4.9) ----

    /// <summary>Folder → Show current folder in → File Explorer (Ctrl+W): the folder opened in File Explorer, one of the system's own windows.</summary>
    private void ShowFolderInExplorer()
    {
        if (_folder is null)
        {
            Announce("No folder", true);
            return;
        }

        OpenInExplorer(_folder);
    }

    /// <summary>Item → Show in File Explorer (Ctrl+Shift+W): File Explorer on the folder with the entry selected, a folder entry too.</summary>
    private void ShowItemInExplorer()
    {
        if (CurrentEntry() is not { } entry)
        {
            SayNoEntry();
            return;
        }

        try
        {
            Process.Start(new ProcessStartInfo("explorer.exe", $"/select,\"{entry.Path}\"") { UseShellExecute = false });
        }
        catch (Exception ex) when (ex is Win32Exception or InvalidOperationException)
        {
            Fail("File Explorer could not be started: " + ex.Message);
        }
    }

    /// <summary>Folder → New window (Ctrl+N, AF-3.3): another AxFile window on the folder shown, as File Explorer's Ctrl+N; without a folder, one that starts as this one did.</summary>
    private void NewWindow() => AxitMenu.Start(Dispatch.AxFile, _folder);

    /// <summary>Folder → Show current folder in → AxClaude, AxGit, AxTerminal (AF-1.3): a new window of that app on the folder shown.</summary>
    private void OpenIn(string verb)
    {
        if (_folder is null)
        {
            Announce("No folder", true);
            return;
        }

        AxitMenu.Start(verb, _folder);
    }

    /// <summary>
    /// Item → Open in Axit → one app (AF-4.9): a new window of it on the entry: the editors on the file, AxGit on the
    /// repository that holds it, AxFile on the folder or the file in its folder, AxClaude and AxTerminal on the folder,
    /// or on the entry's folder for a file.
    /// </summary>
    private void OpenInAxit(string verb)
    {
        if (CurrentEntry() is not { } entry)
        {
            SayNoEntry();
            return;
        }

        switch (verb)
        {
            case Dispatch.AxDown or Dispatch.AxJSON when entry.IsFolder:
                Announce("Not a file", true);
                return;
            case Dispatch.AxClaude or Dispatch.AxTerminal when !entry.IsFolder:
                AxitMenu.Start(verb, _folder ?? entry.Path);
                return;
            default:
                AxitMenu.Start(verb, entry.Path);
                return;
        }
    }

    /// <summary>Item → Open in editor (Ctrl+E): the file in AxJSON for .json and AxDown for anything else, whatever Enter would do with it.</summary>
    private void OpenInEditor()
    {
        if (CurrentEntry() is not { } entry)
        {
            SayNoEntry();
            return;
        }

        if (entry.IsFolder)
        {
            Announce("Not a file", true);
            return;
        }

        var verb = FolderListing.WayOf(entry) == OpenWay.AxJSON ? Dispatch.AxJSON : Dispatch.AxDown;
        AxitMenu.Start(verb, entry.Path);
    }

    /// <summary>Item → Open with Windows (AF-4.7): the program Windows has for the entry, a folder in File Explorer, or the dialog that picks one.</summary>
    private void OpenSelectedWithWindows()
    {
        if (CurrentEntry() is not { } entry)
        {
            SayNoEntry();
            return;
        }

        OpenWithWindows(entry);
    }

    /// <summary>Enter on any other file (AF-3.2), and Open with Windows: the program Windows has for it; when there is none, the Windows dialog that picks one, as File Explorer does.</summary>
    private void OpenWithWindows(Entry entry)
    {
        try
        {
            Process.Start(new ProcessStartInfo(entry.Path) { UseShellExecute = true })?.Dispose();
        }
        catch (Win32Exception ex) when (ex.NativeErrorCode == ErrorNoAssociation)
        {
            if (Shell.OpenWith(Handle, entry.Path) is { } reason)
            {
                Failed("Open with failed", reason);
            }
        }
        catch (Exception ex) when (ex is Win32Exception or InvalidOperationException or IOException)
        {
            Failed("Open failed", ex.Message);
        }
    }

    // ---- Windows' own ways with the entry (AF-4.7): the system's dialogs and menu, like the folder picker (AX-7.2) ----

    /// <summary>Item → Open with another program…: the Windows dialog that picks a program for the file, and runs it.</summary>
    private void OpenWith()
    {
        if (CurrentEntry() is not { } entry)
        {
            SayNoEntry();
            return;
        }

        if (entry.IsFolder)
        {
            Announce("Not a file", true);
            return;
        }

        if (Shell.OpenWith(Handle, entry.Path) is { } reason)
        {
            Failed("Open with failed", reason);
        }
    }

    /// <summary>Item → Properties (Alt+Enter): the Windows Properties sheet of the entry, a file or a folder.</summary>
    private void ShowProperties()
    {
        if (CurrentEntry() is not { } entry)
        {
            SayNoEntry();
            return;
        }

        if (Shell.Properties(Handle, entry.Path) is { } reason)
        {
            Failed("Properties failed", reason);
        }
    }

    /// <summary>Item → More from Windows…: the shell's own menu for the entry, under it in the list, once the Item menu has closed.</summary>
    private void ShowWindowsMenu()
    {
        if (CurrentEntry() is not { } entry)
        {
            SayNoEntry();
            return;
        }

        var bounds = _list.GetItemRectangle(_list.SelectedIndex);
        var at = _list.PointToScreen(new Point(bounds.Left, bounds.Bottom));
        // Posted, so that the Item menu has closed and the list has the focus back before the shell's menu takes over.
        BeginInvoke(() =>
        {
            if (IsDisposed)
            {
                return;
            }

            try
            {
                if (Shell.ShowMenu(Handle, entry.Path, at) is { } reason)
                {
                    Failed("Windows menu failed", reason);
                }
            }
            catch (Exception ex) when (ex is COMException or InvalidCastException or Win32Exception)
            {
                Log.Error("The Windows menu failed", ex);
                Failed("Windows menu failed", ex.Message);
            }
        });
    }
}
