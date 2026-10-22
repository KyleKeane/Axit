using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.IO.Compression;
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

    /// <summary>The operations of this window that can be taken back (AF-4.14), newest last; each operation pushes itself when it has succeeded.</summary>
    private readonly OperationHistory _history = new();

    /// <summary>Whether an undo is on its way: a second Ctrl+Z says so rather than racing it.</summary>
    private bool _undoing;

    /// <summary>
    /// Folder → Undo <operation> (Ctrl+Z, AF-4.14): the last operation of this window taken back, one per press, and
    /// the entry it touched selected in its folder: "Undone: …"; "Not undone: <name> has changed" when something
    /// else moved the entry since, "Nothing to undo" at the end. A delete comes back through the shell's own
    /// Restore on the Recycle Bin's item; the other kinds are the file operations' inverses. An undo that fails
    /// (a zip open elsewhere) leaves the operation in the history to try again.
    /// </summary>
    private void Undo()
    {
        if (_undoing)
        {
            Announce("Undo is still running", true);
            return;
        }

        if (_history.Pop() is not { } operation)
        {
            Announce("Nothing to undo", true);
            return;
        }

        _undoing = true;
        Do(async () =>
        {
            try
            {
                if (await Task.Run(() => FileOperations.CheckUndo(operation)) is { } problem)
                {
                    Announce(problem, true);
                    return;
                }

                // The Restore walks the whole Recycle Bin, so it runs on a shell thread of its own, as the other shell operations do.
                var owner = Handle;
                var reason = operation.Kind == OperationKind.Delete
                    ? await FileOperations.OnShellThreadAsync(() => Shell.Undelete(owner, operation.Before))
                    : await FileOperations.UndoAsync(operation);
                if (reason is not null)
                {
                    _history.Push(operation);
                    Failed("Undo failed", reason);
                    return;
                }

                if (await ShowFolderAsync(operation.Folder, operation.Select ?? CurrentEntry()?.Name, remember: false))
                {
                    Announce(operation.DoneText, true);
                }
            }
            finally
            {
                _undoing = false;
            }
        });
    }

    /// <summary>File operations → New folder… (Ctrl+Shift+N, AF-4.1): the name in a notice, the folder made and selected, "Created <name>".</summary>
    private void NewFolder()
    {
        if (NoFolderHere() || _folder is not { } folder)
        {
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

                if (Lookalike(name) is { } twin)
                {
                    Announce($"Reads the same as {twin}, which is already here", true);
                    return;
                }

                Do(async () =>
                {
                    try
                    {
                        var made = await Task.Run(() => FileOperations.CreateFolder(folder, name));
                        _history.Push(new Operation(OperationKind.NewFolder, [], [made]));
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

    /// <summary>
    /// File operations → New text file (Ctrl+Shift+T, AF-4.1): an empty "New file.txt" made and selected, and the
    /// rename box opened on it with the whole name selected, so that the name typed, extension included, replaces
    /// it; "Created <name>" when the box cannot open (the file hidden by the filter, the focus elsewhere).
    /// </summary>
    private void NewFile()
    {
        if (NoFolderHere() || _folder is not { } folder)
        {
            return;
        }

        Do(async () =>
        {
            string path;
            try
            {
                path = await Task.Run(() => FileOperations.CreateFile(folder));
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                Failed("New file failed", FileOperations.Message(ex));
                return;
            }

            _history.Push(new Operation(OperationKind.NewFile, [], [path]));
            var name = Path.GetFileName(path);
            if (!await ShowFolderAsync(folder, name, remember: false))
            {
                return;
            }

            if (_list.Focused && string.Equals(CurrentEntry()?.Path, path, StringComparison.OrdinalIgnoreCase))
            {
                Rename(wholeName: true);
            }
            else
            {
                Announce($"Created {name}", true);
            }
        });
    }

    /// <summary>
    /// Item → Move to new folder… (AF-4.13): the name asked in the notice "Move to new folder", the
    /// folder made here and the selected entries, or the current one, moved into it; "Moved <what> into <name>",
    /// the folder selected.
    /// </summary>
    private void MoveIntoNewFolder()
    {
        if (NoFolderHere() || _folder is not { } folder)
        {
            return;
        }

        var targets = Targets();
        if (targets.Count == 0)
        {
            return;
        }

        var what = targets.Count == 1 ? targets[0].Name : $"{targets.Count} items";
        ShowNotice(new Notice("Move to new folder", $"{what} will move into the new folder.",
        [
            new OverlayChoice("&Move", () =>
            {
                var name = Overlay.InputText.Trim();
                if (FileOperations.CheckName(name) is { } problem)
                {
                    Announce(problem, true);
                    return;
                }

                if (Lookalike(name) is { } twin)
                {
                    Announce($"Reads the same as {twin}, which is already here", true);
                    return;
                }

                Do(async () =>
                {
                    string made;
                    try
                    {
                        made = await Task.Run(() => FileOperations.CreateFolder(folder, name));
                    }
                    catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                    {
                        Failed("Move failed", FileOperations.Message(ex));
                        return;
                    }

                    var sources = targets.Select(target => target.Path).ToList();
                    var destinations = await Task.Run(() => FileOperations.Destinations(sources, made));
                    // AF-9.3: the operation's line in the pane, replaced when it ends.
                    var token = Report(-1, $"Moving {what} into {name}");
                    if (await FileOperations.CopyOrMoveAsync(sources, destinations, move: true) is { } reason)
                    {
                        Report(token, "Failed: " + reason);
                        Failed("Move failed", reason);
                        return;
                    }

                    Report(token, $"Moved {what} into {name}");
                    _history.Push(new Operation(OperationKind.Move, sources, destinations, made));
                    if (await ShowFolderAsync(folder, name, remember: false))
                    {
                        Announce($"Moved {what} into {name}", true);
                    }
                });
            }, IsDefault: true),
            new OverlayChoice("&Cancel", IsCancel: true),
        ])
        {
            Input = new OverlayInput("Name:", "Name", string.Empty, "Type a name"),
        });
    }

    /// <summary>File operations → Rename (F2): the box over the entry with its name, the part before the extension selected; the whole name for a folder, or when asked (a new file, AF-4.1).</summary>
    private void Rename(bool wholeName = false)
    {
        if (_renaming is not null || NoFolderHere())
        {
            return;
        }

        if (CurrentEntry() is not { } entry)
        {
            SayNoEntry();
            return;
        }

        _renaming = entry;
        _renameBox.Bounds = _list.GetItemRectangle(_list.SelectedIndex);
        _renameBox.Text = entry.Name;
        // The selection goes before the focus: a text box that gets the focus selects everything unless one was set.
        _renameBox.Select(0, wholeName || entry.IsFolder ? entry.Name.Length : Path.GetFileNameWithoutExtension(entry.Name).Length);
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

        if (Lookalike(name, entry) is { } twin)
        {
            Announce($"Reads the same as {twin}, which is already here", true);
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
                var renamed = await Task.Run(() => FileOperations.Rename(entry.Path, name));
                _history.Push(new Operation(OperationKind.Rename, [entry.Path], [renamed]));
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

    /// <summary>
    /// The entry of the folder whose name reads the same as <paramref name="name"/> without being it (AF-4.2), the
    /// entry being renamed left out; null when none. Every entry read is compared, the hidden ones too.
    /// </summary>
    private string? Lookalike(string name, Entry? self = null) =>
        FileOperations.Lookalike(name, _entries.Where(entry => entry != self).Select(entry => entry.Name));

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
        if (NoFolderHere() || _folder is not { } folder)
        {
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
                // AF-9.3: a delete of several has a line in the pane; one entry goes too fast to watch.
                var token = one is null ? Report(-1, $"Deleting {targets.Count} items") : -1;
                if (await FileOperations.DeleteToRecycleBinAsync(targets.Select(target => target.Path).ToList()) is { } reason)
                {
                    if (token >= 0)
                    {
                        Report(token, "Failed: " + reason);
                    }

                    Failed("Delete failed", reason);
                    return;
                }

                if (token >= 0)
                {
                    Report(token, done);
                }

                _history.Push(new Operation(OperationKind.Delete, targets.Select(target => target.Path).ToList(), []));
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
        for (var i = index + 1; i < _shown.Count; i++)
        {
            if (!gone.Contains(_shown[i]))
            {
                return _shown[i].Name;
            }
        }

        for (var i = Math.Min(index, _shown.Count - 1); i >= 0; i--)
        {
            if (!gone.Contains(_shown[i]))
            {
                return _shown[i].Name;
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
        if (NoFolderHere())
        {
            return;
        }

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
        if (NoFolderHere() || _folder is not { } folder)
        {
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
            // The names are chosen first: a name already in the folder becomes "name - 2" (AF-4.8).
            var destinations = await Task.Run(() => FileOperations.Destinations(paths, folder));
            var what = paths.Count == 1 ? Path.GetFileName(destinations[0]) : $"{paths.Count} items";
            // AF-9.3: the operation's line in the pane; the shell gives no count, so the line waits until it ends.
            var token = Report(-1, (move ? "Moving " : "Copying ") + what);
            var operation = FileOperations.CopyOrMoveAsync(paths, destinations, move);
            if (await Task.WhenAny(operation, Task.Delay(1000)) != operation)
            {
                Announce("Pasting, please wait", true);
            }

            if (await operation is { } reason)
            {
                Report(token, "Failed: " + reason);
                Failed("Paste failed", reason);
                return;
            }

            Report(token, "Pasted " + what);
            _history.Push(new Operation(move ? OperationKind.Move : OperationKind.Copy, paths, destinations));

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

            var first = Path.GetFileName(destinations[0]);
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

    /// <summary>Folder → Show current folder in → File Explorer (Ctrl+W): the folder opened in File Explorer, one of the system's own windows; This PC as File Explorer's own This PC.</summary>
    private void ShowFolderInExplorer()
    {
        if (_folder is null)
        {
            Announce("No folder", true);
            return;
        }

        if (FolderListing.IsThisPC(_folder))
        {
            try
            {
                Process.Start(new ProcessStartInfo("explorer.exe", "shell:MyComputerFolder") { UseShellExecute = false })?.Dispose();
            }
            catch (Exception ex) when (ex is Win32Exception or InvalidOperationException)
            {
                Fail("File Explorer could not be started: " + ex.Message);
            }

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
        if (NoFolderHere() || _folder is not { } folder)
        {
            return;
        }

        AxitMenu.Start(verb, folder);
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

    // ---- zip files (AF-4.12): the entries into a new zip, a zip into a new folder, nothing written over ----

    /// <summary>File operations → Compress to zip: the selected entries, or the current one, into a new zip in the folder; "Compressed to <name>", the zip selected; "Compressing, please wait" after a second.</summary>
    private void Compress()
    {
        if (NoFolderHere() || _folder is not { } folder)
        {
            return;
        }

        var targets = Targets();
        if (targets.Count == 0)
        {
            return;
        }

        var sources = targets.Select(target => target.Path).ToList();
        RunArchiveWork(folder, "Compressing", "Compress failed", progress => Archives.Compress(sources, folder, progress), "Compressed to", OperationKind.Compress);
    }

    /// <summary>File operations → Extract here: the zip's contents into a new folder named after it, next to it; "Extracted to <name>", the folder selected; "Not a zip file" on anything else.</summary>
    private void Extract()
    {
        if (NoFolderHere() || _folder is not { } folder)
        {
            return;
        }

        if (CurrentEntry() is not { } entry)
        {
            SayNoEntry();
            return;
        }

        if (entry.IsFolder || !Archives.IsZip(entry.Path))
        {
            Announce("Not a zip file", true);
            return;
        }

        RunArchiveWork(folder, "Extracting", "Extract failed", progress => Archives.Extract(entry.Path, folder, progress), "Extracted to", OperationKind.Extract);
    }

    /// <summary>
    /// Runs a zip operation on a task with its count in the pane's line, "Compressing, 40 of 120 files" (AF-9.3,
    /// at most a few times a second), says "<doing>, please wait" when it passes a second, then shows the folder
    /// with the result selected and says "<done> <name>", the line saying the same.
    /// </summary>
    private void RunArchiveWork(string folder, string doing, string failTitle, Func<Action<int, int>, string> work, string done, OperationKind kind) => Do(async () =>
    {
        var token = Report(-1, doing);
        var reported = Environment.TickCount64;
        var task = Task.Run(() => work((count, total) =>
        {
            var now = Environment.TickCount64;
            if (count == total || now - reported >= 250)
            {
                reported = now;
                if (!IsDisposed)
                {
                    BeginInvoke(() => Report(token, $"{doing}, {count} of {total} files"));
                }
            }
        }));
        if (await Task.WhenAny(task, Task.Delay(1000)) != task)
        {
            Announce($"{doing}, please wait", true);
        }

        string result;
        try
        {
            result = await task;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidDataException or NotSupportedException)
        {
            Report(token, "Failed: " + FileOperations.Message(ex));
            Failed(failTitle, FileOperations.Message(ex));
            return;
        }

        var name = Path.GetFileName(result);
        Report(token, $"{done} {name}");
        _history.Push(new Operation(kind, [], [result]));
        if (await ShowFolderAsync(folder, name, remember: false))
        {
            Announce($"{done} {name}", true);
        }
    });

    // ---- the user's own programs (AF-4.11): a list in the settings, each run on the entries as its arguments ----

    /// <summary>
    /// Open with ▸ (AF-4.7, AF-4.11): Windows default and Choose a program…, then the user's own programs as
    /// "1. name (path)" with Add a program… and Remove a program ▸ under them; refilled each time it opens.
    /// </summary>
    private void FillOpenWith(ToolStripMenuItem menu)
    {
        menu.DropDownItems.Clear();
        menu.DropDownItems.Add(new ToolStripMenuItem("Windows &default", null, (_, _) => OpenSelectedWithWindows()));
        menu.DropDownItems.Add(new ToolStripMenuItem("&Choose a program...", null, (_, _) => OpenWith()));
        menu.DropDownItems.Add(new ToolStripSeparator());
        var programs = _settings.Programs;
        if (programs.Count == 0)
        {
            menu.DropDownItems.Add(new ToolStripMenuItem("&No programs of your own yet"));
        }

        for (var i = 0; i < programs.Count; i++)
        {
            var program = programs[i];
            menu.DropDownItems.Add(new ToolStripMenuItem(AxitWindows.Label(i, $"{program.Name} ({program.Path})"), null, (_, _) => RunProgram(program)));
        }

        menu.DropDownItems.Add(new ToolStripMenuItem("&Add a program...", null, (_, _) => AddProgram()));
        var remove = new ToolStripMenuItem("&Remove a program");
        if (programs.Count == 0)
        {
            remove.DropDownItems.Add(new ToolStripMenuItem("&No programs of your own yet"));
        }

        for (var i = 0; i < programs.Count; i++)
        {
            var program = programs[i];
            remove.DropDownItems.Add(new ToolStripMenuItem(AxitWindows.Label(i, program.Name), null, (_, _) => RemoveProgram(program)));
        }

        menu.DropDownItems.Add(remove);
    }

    /// <summary>Open with a custom program ▸ one program: run on the selected entries, or the current one, as its arguments; "Started <name>".</summary>
    private void RunProgram(UserProgram program)
    {
        var targets = Targets();
        if (targets.Count == 0)
        {
            return;
        }

        try
        {
            Process.Start(UserPrograms.StartInfo(program, targets.Select(target => target.Path).ToList()))?.Dispose();
            Announce($"Started {program.Name}", true);
        }
        catch (Exception ex) when (ex is Win32Exception or InvalidOperationException or IOException)
        {
            Failed($"{program.Name} failed", ex.Message);
        }
    }

    /// <summary>Add a program…: the Windows file picker on programs and scripts, then the notice "Add a program" with the field "Name" holding the file's name; "Added <name>".</summary>
    private void AddProgram()
    {
        using var dialog = new OpenFileDialog
        {
            Title = "Choose a program or script",
            Filter = "Programs and scripts (*.exe;*.cmd;*.bat;*.ps1;*.py)|*.exe;*.cmd;*.bat;*.ps1;*.py|All files (*.*)|*.*",
            CheckFileExists = true,
        };
        var chosen = dialog.ShowDialog(this) == DialogResult.OK ? dialog.FileName : null;
        _list.Select();
        if (chosen is null)
        {
            return;
        }

        ShowNotice(new Notice("Add a program", $"The name the menu shows for {chosen}.",
        [
            new OverlayChoice("&Add", () =>
            {
                var name = Overlay.InputText.Trim();
                if (name.Length == 0)
                {
                    Announce("Type a name", true);
                    return;
                }

                _settings.Programs.Add(new UserProgram(name, chosen));
                SaveSettings();
                Announce($"Added {name}", true);
            }, IsDefault: true),
            new OverlayChoice("&Cancel", IsCancel: true),
        ])
        {
            Input = new OverlayInput("Name:", "Name", UserPrograms.DefaultName(chosen), "Type a name"),
        });
    }

    /// <summary>Remove a program ▸ one program: taken off the list; "Removed <name>".</summary>
    private void RemoveProgram(UserProgram program)
    {
        _settings.Programs.Remove(program);
        SaveSettings();
        Announce($"Removed {program.Name}", true);
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
