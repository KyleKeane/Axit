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
/// the window (AF-1.3, AF-3.2, AF-4.7, AF-4.9). Every operation that changes the disk runs through one of two
/// runners: <see cref="RunOperation"/> for one on a task with a running line in the Actions list, <see cref="RunAtOnceAsync"/>
/// for one that ends at once.
/// </summary>
public sealed partial class FileForm
{
    private const int ErrorNoAssociation = 1155;

    /// <summary>File Explorer's clipboard format beside the file list: a number saying whether the files were cut (move) or copied.</summary>
    private const string DropEffectFormat = "Preferred DropEffect";
    private const int DropEffectCopy = 1;
    private const int DropEffectMove = 2;

    /// <summary>How long an operation runs before "<doing>, please wait" is said (AF-4.5).</summary>
    private const int WaitNoticeMs = 1000;

    /// <summary>How often at most a zip's count is written into its line (AF-9.2).</summary>
    private const int ProgressMs = 250;

    /// <summary>The entry being renamed in place (AF-4.2) while the box over it shows; null otherwise.</summary>
    private Entry? _renaming;

    /// <summary>Whether an undo is on its way: a second Ctrl+Z says so rather than racing it.</summary>
    private bool _undoing;

    /// <summary>
    /// The paths the operations still running are writing, and the ones they are moving away (AF-4.5): every
    /// operation runs on a thread of its own and several run at once, so a second paste chooses its names around the
    /// first's, and a cut whose files are already on their way is not pasted twice.
    /// </summary>
    private readonly HashSet<string> _inFlight = new(StringComparer.OrdinalIgnoreCase);

    // ---- the runners (AF-4.5): what every operation shares ----

    /// <summary>
    /// What an operation ended with: its line in the Actions list ("Moved 3 items"), the operation for its undo and
    /// for Enter on the line, the entry to select in the folder it ran in, and what is said when that differs from the
    /// line ("Pasted 3 items"); or why it failed, when the shell refused it (<see cref="Failure"/>).
    /// </summary>
    private sealed record Outcome(string Line, Operation? Operation, string? Select, string? Said = null, string? Failed = null)
    {
        public static Outcome Failure(string reason) => new(string.Empty, null, null, Failed: reason);
    }

    /// <summary>
    /// Runs one operation on a task (AF-4.5): its line in the Actions list from <paramref name="doing"/> until it ends
    /// (AF-9.2), <paramref name="busy"/>, the paths it writes or moves away, held in <see cref="_inFlight"/> meanwhile,
    /// "<waiting>, please wait" when it passes a second, its failure on the line and said (<see cref="FailAction"/>),
    /// and its result: the line done, then the folder it ran in read again with the result selected and the outcome
    /// said while that is still the folder shown, the outcome said alone otherwise (<see cref="ShowResultAsync"/>).
    /// The window waits for none of them. (Until 2026-10-23 five hand-written runners repeated this.)
    /// </summary>
    private void RunOperation(string folder, string doing, string waiting, string failTitle, IReadOnlyCollection<string> busy, Func<ActionEntry, Task<Outcome>> work) => Do(async () =>
    {
        var action = Begin(doing);
        _inFlight.UnionWith(busy);
        Outcome outcome;
        try
        {
            var task = work(action);
            if (await Task.WhenAny(task, Task.Delay(WaitNoticeMs)) != task)
            {
                Announce($"{waiting}, please wait", true);
            }

            outcome = await task;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidDataException or NotSupportedException)
        {
            outcome = Outcome.Failure(FileOperations.Message(ex));
        }
        finally
        {
            _inFlight.ExceptWith(busy);
        }

        if (outcome.Failed is { } reason)
        {
            FailAction(action, failTitle, reason);
            return;
        }

        End(action, outcome.Line, outcome.Operation);
        await ShowResultAsync(folder, outcome.Select, outcome.Said ?? outcome.Line);
    });

    /// <summary>
    /// An operation that ends at once (a rename, a new folder or file; AF-4.5), <paramref name="work"/> on a task: its
    /// line recorded done, then the folder read again with the result selected and the outcome said (<see cref="ShowResultAsync"/>;
    /// without <paramref name="sayInView"/> the caller says its own once the result is in view); "Failed: …" when it
    /// throws. True when the result is in view.
    /// </summary>
    private async Task<bool> RunAtOnceAsync(string folder, string failTitle, Func<Outcome> work, bool sayInView = true)
    {
        Outcome outcome;
        try
        {
            outcome = await Task.Run(work);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            Failed(failTitle, FileOperations.Message(ex));
            return false;
        }

        if (outcome.Operation is { } operation)
        {
            Record(outcome.Line, operation);
        }

        return await ShowResultAsync(folder, outcome.Select, outcome.Said ?? outcome.Line, sayInView);
    }

    // ---- undo (AF-4.14) ----

    /// <summary>Folder → Undo <operation> (Ctrl+Z, AF-4.14): the newest line of the Actions list that can be taken back; "Nothing to undo" when none can.</summary>
    private void Undo()
    {
        if (_actions.Newest() is not { } entry)
        {
            Announce(_undoing ? "Undo is still running" : "Nothing to undo", true);
            return;
        }

        UndoAction(entry);
    }

    /// <summary>
    /// One line of the Actions list taken back (AF-4.14, AF-9.3) and the entry it touched selected in its folder:
    /// "Undone: …", the line reading ", undone" afterwards; "Not undone: <name> has changed" when something else moved
    /// the entry since, the line saying so and its operation dropped. A delete comes back through the shell's own
    /// Restore on the Recycle Bin's item; the other kinds are the file operations' inverses. An undo that fails (a zip
    /// open elsewhere) leaves the line as it was, to try again.
    /// </summary>
    private void UndoAction(ActionEntry entry)
    {
        if (_undoing)
        {
            Announce("Undo is still running", true);
            return;
        }

        if (entry.Kind == ActionKind.Selection)
        {
            // A change of the marks (AF-9.3): put back at once, in the folder shown, no shell and no check.
            UndoMarks(entry);
            return;
        }

        if (entry.Operation is not { } operation)
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
                    // "Not undone: …": the line keeps the reason without the prefix.
                    _actions.NotUndone(entry, problem.StartsWith("Not undone: ", StringComparison.Ordinal) ? problem["Not undone: ".Length..] : problem);
                    FillPane();
                    Announce(problem, true);
                    return;
                }

                // The Restore walks the whole Recycle Bin, so it runs on a shell thread of its own, as the other shell operations do.
                var owner = Handle;
                var reason = operation.Kind == OperationKind.Delete
                    ? await FileOperations.OnShellThreadAsync(() => Shell.Undelete(owner, operation.Before))
                    : await FileOperations.UndoAsync(operation);
                if (reason == Shell.NotInRecycleBin)
                {
                    // Emptied meanwhile: a retry can never succeed, so the line says so and Ctrl+Z moves on (AF-4.14).
                    _actions.NotUndone(entry, reason);
                    FillPane();
                    Announce("Not undone: " + reason, true);
                    return;
                }

                if (reason is not null)
                {
                    Failed("Undo failed", reason);
                    return;
                }

                _actions.Undone(entry);
                FillPane();
                // The undo's own words are what is heard, the folder's name among them when it is another (AF-4.14).
                if (await ShowFolderAsync(operation.Folder, operation.Select ?? CurrentEntry()?.Name, remember: false, announce: false))
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

    // ---- new entries (AF-4.1, AF-4.13) ----

    /// <summary>Item → New folder… (Ctrl+Shift+N, AF-4.1): the name in a notice, the folder made and selected, "Created <name>".</summary>
    private void NewFolder()
    {
        if (!TryFolder(out var folder))
        {
            return;
        }

        ShowNotice(new Notice("New folder", string.Empty,
        [
            new OverlayChoice("&Create", () =>
            {
                var name = Overlay.InputText.Trim();
                if (CheckNewName(name) is { } problem)
                {
                    Announce(problem, true);
                    return;
                }

                Do(() => RunAtOnceAsync(folder, "New folder failed", () =>
                    new Outcome($"Created {name}", new Operation(OperationKind.NewFolder, [], [FileOperations.CreateFolder(folder, name)]), name)));
            }, IsDefault: true),
            new OverlayChoice("C&ancel", IsCancel: true),
        ])
        {
            Input = new OverlayInput("Name:", "Name", string.Empty, "Type a name"),
        });
    }

    /// <summary>
    /// Item → New text file (Ctrl+Shift+T, AF-4.1): an empty "New file.txt" made and selected, and the
    /// rename box opened on it with the whole name selected, so that the name typed, extension included, replaces
    /// it; "Created <name>" when the box cannot open (the file hidden by the filter, the focus elsewhere).
    /// </summary>
    private void NewFile()
    {
        if (!TryFolder(out var folder))
        {
            return;
        }

        Do(async () =>
        {
            var path = string.Empty;
            var inView = await RunAtOnceAsync(folder, "New file failed", () =>
            {
                path = FileOperations.CreateFile(folder);
                var name = Path.GetFileName(path);
                return new Outcome($"Created {name}", new Operation(OperationKind.NewFile, [], [path]), name);
            }, sayInView: false);
            if (!inView)
            {
                return;
            }

            if (_list.Focused && string.Equals(CurrentEntry()?.Path, path, StringComparison.OrdinalIgnoreCase))
            {
                Rename(wholeName: true);
            }
            else
            {
                Announce($"Created {Path.GetFileName(path)}", true);
            }
        });
    }

    /// <summary>
    /// Item → Move to folder… (AF-4.13): the notice "Move to folder" with Go to folder's completion on Down and Up,
    /// folders alone (asked 2026-10-23); a folder that exists (a name here or a path) takes the selected entries, or
    /// the current one, and any other name is a new folder made here first; "Moved <what> into <name>", the folder
    /// selected when it is here.
    /// </summary>
    private void MoveIntoNewFolder()
    {
        if (!TryFolder(out var folder))
        {
            return;
        }

        var targets = Targets();
        if (targets.Count == 0)
        {
            return;
        }

        var what = targets.Count == 1 ? targets[0].Name : $"{targets.Count} items";
        var sources = targets.Select(target => target.Path).ToList();
        ResetCompletion(foldersOnly: true);
        ShowNotice(new Notice("Move to folder", $"{what} will move into the folder typed: one here or a path, Down and Up completing it, or a new folder of that name made here.",
        [
            new OverlayChoice("&Move", () =>
            {
                var typed = Environment.ExpandEnvironmentVariables(Overlay.InputText.Trim().Trim('"')).Replace('/', '\\').TrimEnd('\\');
                if (typed.Length == 0)
                {
                    Announce("Type a name", true);
                    return;
                }

                var target = Path.IsPathRooted(typed) ? typed : Path.Combine(folder, typed);
                var exists = Directory.Exists(target);
                var here = string.Equals(Path.GetDirectoryName(target), folder.TrimEnd('\\'), StringComparison.OrdinalIgnoreCase);
                var name = Path.GetFileName(target);
                if (sources.Any(source => string.Equals(source, target, StringComparison.OrdinalIgnoreCase)
                    || target.StartsWith(source.TrimEnd('\\') + "\\", StringComparison.OrdinalIgnoreCase)))
                {
                    Announce("Cannot move a folder into itself", true);
                    return;
                }

                if (!exists && !here)
                {
                    Announce("Not found: " + typed, true);
                    return;
                }

                if (!exists && CheckNewName(name) is { } problem)
                {
                    Announce(problem, true);
                    return;
                }

                // The notice stays open on a refusal above (StaysOpen), so that the text can be put right.
                CloseNotice(null);
                RunOperation(folder, $"Moving {what} into {name}", "Moving", "Move failed", sources, async _ =>
                {
                    var made = exists ? null : await Task.Run(() => FileOperations.CreateFolder(folder, name));
                    var into = made ?? target;
                    var destinations = await Task.Run(() => FileOperations.Destinations(sources, into));
                    return await FileOperations.CopyOrMoveAsync(sources, destinations, move: true) is { } reason
                        ? Outcome.Failure(reason)
                        : new Outcome($"Moved {what} into {name}", new Operation(OperationKind.Move, sources, destinations, made), here ? name : null);
                });
            }, IsDefault: true, StaysOpen: true),
            new OverlayChoice("&Cancel", IsCancel: true),
        ])
        {
            Input = new OverlayInput("Folder:", "Folder", string.Empty, "Type a name", KeyHook: OnPathKey),
        });
    }

    /// <summary>Why a name typed for a new entry cannot be used (AF-4.1, AF-4.2): the name check, or an entry here that reads the same; null when it can.</summary>
    private string? CheckNewName(string name, Entry? self = null) =>
        FileOperations.CheckName(name) ?? (Lookalike(name, self) is { } twin ? $"Reads the same as {twin}, which is already here" : null);

    /// <summary>
    /// The entry of the folder whose name reads the same as <paramref name="name"/> without being it (AF-4.2), the
    /// entry being renamed left out; null when none. Every entry read is compared, the hidden ones too.
    /// </summary>
    private string? Lookalike(string name, Entry? self = null) =>
        FileOperations.Lookalike(name, _entries.Where(entry => entry != self).Select(entry => entry.Name));

    // ---- the rename in place (AF-4.2): a box over the entry, Enter renames, Escape or leaving the box keeps the name ----

    /// <summary>Item → Rename (F2): the box over the entry with its name, the part before the extension selected; the whole name for a folder, or when asked (a new file, AF-4.1).</summary>
    private void Rename(bool wholeName = false)
    {
        if (_renaming is not null || NoFolderHere() || !TryEntry(out var entry))
        {
            return;
        }

        _renaming = entry;
        // The bundle's rename box over the entry (AX-8.7): Enter commits, Escape or leaving it keeps the name.
        _list.BeginRename(entry.Name, wholeName || entry.IsFolder ? entry.Name.Length : Path.GetFileNameWithoutExtension(entry.Name).Length,
            CommitRename, () => _renaming = null);
    }

    /// <summary>The typed name checked and applied: "Renamed to <name>", the entry selected under its new name.</summary>
    private void CommitRename(string name)
    {
        if (_renaming is not { } entry || _folder is not { } folder)
        {
            return;
        }

        if (CheckNewName(name, entry) is { } problem)
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

        Do(() => RunAtOnceAsync(folder, "Rename failed", () =>
            new Outcome($"Renamed {entry.Name} to {name}", new Operation(OperationKind.Rename, [entry.Path], [FileOperations.Rename(entry.Path, name)]), name, $"Renamed to {name}")));
    }

    private void EndRename() => _list.EndRename();

    // ---- delete (AF-4.3) and the path (AF-4.4) ----

    /// <summary>
    /// Item → Delete… (Delete, AF-4.3): a question, then the shell sends the selected entries, or the
    /// current one, to the Recycle Bin in one operation; "Deleted <name>" or "Deleted n items", the entry after them
    /// selected.
    /// </summary>
    private void Delete()
    {
        if (!TryFolder(out var folder))
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
        var what = one?.Name ?? $"{targets.Count} items";
        var text = one is null
            ? targets.Any(target => target.IsFolder) ? "They go to the Recycle Bin, the folders with everything in them." : "They go to the Recycle Bin."
            : one.IsFolder ? "It goes to the Recycle Bin with everything in it." : "It goes to the Recycle Bin.";
        var paths = targets.Select(target => target.Path).ToList();
        ShowNotice(new Notice($"Delete {what}?", text,
        [
            new OverlayChoice("&Delete", () => RunOperation(folder, $"Deleting {what}", "Deleting", "Delete failed", paths, async _ =>
                await FileOperations.DeleteToRecycleBinAsync(paths) is { } reason
                    ? Outcome.Failure(reason)
                    : new Outcome($"Deleted {what}", new Operation(OperationKind.Delete, paths, []), next)), IsDefault: true),
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

    /// <summary>Item → Copy path (Ctrl+Shift+C, AF-4.4): the full path of the entry, or of each selected entry on a line, to the clipboard; "Copied path" or "Copied n paths".</summary>
    private void CopyPath()
    {
        var targets = Targets();
        if (targets.Count == 0)
        {
            return;
        }

        CopyText(string.Join("\r\n", targets.Select(target => target.Path)), targets.Count == 1 ? "Copied path" : $"Copied {targets.Count} paths");
    }

    // ---- the clipboard (AF-4.8): the entries as files on it, in File Explorer's formats, and its files pasted into the folder ----

    /// <summary>Item → Cut or Copy (Ctrl+X, Ctrl+C): the selected entries, or the current one, on the clipboard as files, marked to move or to copy; "Cut <name>", "Copied n items".</summary>
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
        // The paths as text too, one per line (2026-10-23): a text field pastes them, where the files alone gave nothing;
        // File Explorer and Paste here take the files.
        data.SetText(string.Join("\r\n", targets.Select(target => target.Path)));
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
    /// Item → Paste (Ctrl+V): the clipboard's files copied, or moved after a cut, into the folder shown
    /// by the shell; "Pasted <name>" or "Pasted n items", with "Pasting, please wait" when it takes longer than a
    /// second. A cut file still on its way from an earlier paste is left to it: "Already pasting" when they all are,
    /// "Pasted 2 items, 1 already pasting" when some are (AF-4.5; until 2026-10-23 those went unmentioned).
    /// </summary>
    private void Paste()
    {
        if (!TryFolder(out var folder))
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

        var stillMoving = 0;
        if (move)
        {
            // A cut entry already in this folder has nowhere to go.
            paths.RemoveAll(path => string.Equals(Path.GetDirectoryName(path), folder, StringComparison.OrdinalIgnoreCase));
            if (paths.Count == 0)
            {
                Announce("Already in this folder", true);
                return;
            }

            // A cut whose files a paste is still moving (AF-4.5): a second paste would race the first.
            stillMoving = paths.RemoveAll(_inFlight.Contains);
            if (paths.Count == 0)
            {
                Announce("Already pasting", true);
                return;
            }
        }

        if (paths.Count == 0)
        {
            Announce("Nothing to paste", true);
            return;
        }

        // The names are chosen first, on a task: a name already in the folder, or one a running paste is writing, becomes "name - 2" (AF-4.8, AF-4.5).
        var reserved = _inFlight.ToList();
        Do(async () =>
        {
            var destinations = await Task.Run(() => FileOperations.Destinations(paths, folder, reserved));
            var first = Path.GetFileName(destinations[0]);
            var what = paths.Count == 1 ? first : $"{paths.Count} items";
            var said = $"Pasted {what}" + (stillMoving > 0 ? $", {stillMoving} already pasting" : string.Empty);
            var busy = new List<string>(destinations);
            if (move)
            {
                busy.AddRange(paths);
            }

            // The shell gives no count, so the line waits as it is until the operation ends (AF-9.2).
            RunOperation(folder, (move ? "Moving " : "Copying ") + what, "Pasting", "Paste failed", busy, async _ =>
            {
                if (await FileOperations.CopyOrMoveAsync(paths, destinations, move) is { } reason)
                {
                    return Outcome.Failure(reason);
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

                return new Outcome((move ? "Moved " : "Copied ") + what, new Operation(move ? OperationKind.Move : OperationKind.Copy, paths, destinations), first, said);
            });
        });
    }

    private static int ReadDropEffect(Stream stream)
    {
        var bytes = new byte[4];
        stream.Position = 0;
        return stream.Read(bytes, 0, bytes.Length) == bytes.Length ? BitConverter.ToInt32(bytes, 0) : 0;
    }

    // ---- zip files (AF-4.12): the entries into a new zip, a zip into a new folder, nothing written over ----

    /// <summary>Item → Compress to zip: the selected entries, or the current one, into a new zip in the folder; "Compressed to <name>", the zip selected; "Compressing, please wait" after a second.</summary>
    private void Compress()
    {
        if (!TryFolder(out var folder))
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

    /// <summary>Item → Extract here: the zip's contents into a new folder named after it, next to it; "Extracted to <name>", the folder selected; "Not a zip file" on anything else.</summary>
    private void Extract()
    {
        if (!TryFolder(out var folder) || !TryEntry(out var entry))
        {
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
    /// Runs a zip operation (<see cref="RunOperation"/>) with its count in the Actions list's line, "Compressing, 40
    /// of 120 files" (AF-9.2, at most a few times a second), then shows the folder with the result selected and says
    /// "<done> <name>", the line saying the same.
    /// </summary>
    private void RunArchiveWork(string folder, string doing, string failTitle, Func<Action<int, int>, string> work, string done, OperationKind kind) =>
        RunOperation(folder, doing, doing, failTitle, [], async action =>
        {
            var reported = Environment.TickCount64;
            var result = await Task.Run(() => work((count, total) =>
            {
                var now = Environment.TickCount64;
                if (count == total || now - reported >= ProgressMs)
                {
                    reported = now;
                    if (!IsDisposed)
                    {
                        BeginInvoke(() => Progress(action, $"{doing}, {count} of {total} files"));
                    }
                }
            }));
            var name = Path.GetFileName(result);
            return new Outcome($"{done} {name}", new Operation(kind, [], [result]), name);
        });

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
                Process.Start(new ProcessStartInfo(SystemPrograms.Explorer, "shell:MyComputerFolder") { UseShellExecute = false })?.Dispose();
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
        if (!TryEntry(out var entry))
        {
            return;
        }

        OpenInExplorer(entry.Path, select: true);
    }

    /// <summary>Folder → New window (Ctrl+N, AF-3.3): another axFile window on the folder shown, as File Explorer's Ctrl+N; without a folder, and at This PC, one that starts as this one did (AF-3.10).</summary>
    private void NewWindow() => AxitMenu.Start(Dispatch.AxFile, RealFolder);

    /// <summary>
    /// Item → Open in Axit → one app (AF-4.9): a new window of it on the entry, by what the app opens
    /// (<see cref="Dispatch.PathFor"/>): a file's app on the file, a folder's app on the folder or a file's folder, axGIT
    /// on the repository that holds it, axFile on the folder or the file in its folder; "Not a file" for a folder given
    /// to an app of files.
    /// </summary>
    private void OpenInAxit(App app)
    {
        if (!TryEntry(out var entry))
        {
            return;
        }

        if (Dispatch.PathFor(app, entry.Path, entry.IsFolder) is { } path)
        {
            AxitMenu.Start(app.Verb, path);
        }
        else
        {
            Announce("Not a file", true);
        }
    }

    /// <summary>Item → Open in editor (Ctrl+E): the file in axJSON for .json and axDown for anything else, whatever Enter would do with it.</summary>
    private void OpenInEditor()
    {
        if (!TryEntry(out var entry))
        {
            return;
        }

        if (entry.IsFolder)
        {
            Announce("Not a file", true);
            return;
        }

        AxitMenu.Start(Dispatch.EditorOf(entry.Path), entry.Path);
    }

    /// <summary>Item → Open with → Windows default (AF-4.7): the program Windows has for the entry, a folder in File Explorer, or the dialog that picks one.</summary>
    private void OpenSelectedWithWindows()
    {
        if (TryEntry(out var entry))
        {
            OpenWithWindows(entry);
        }
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
            if (WindowsOpenWith.Show(Handle, entry.Path) is { } reason)
            {
                Failed("Open with failed", reason);
            }
        }
        catch (Exception ex) when (ex is Win32Exception or InvalidOperationException or IOException)
        {
            Failed("Open failed", ex.Message);
        }
    }
}
