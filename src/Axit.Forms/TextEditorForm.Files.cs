using Axit.Core;

namespace Axit.Forms;

/// <summary>The editor shell, the file (AD-3, AD-4): the read and the showing, new, open, recent, save, save as, the check against the disk and the reload, the question before a change is lost, and the file in the other apps.</summary>
public sealed partial class TextEditorForm
{
    // ---- the document (AD-3): the read, then the showing ----

    /// <summary>
    /// The file read and shown, or an empty document; the history ends with it unless <paramref name="keepHistory"/>
    /// (a reload, AD-2.5). False, with a notice, when the window must keep what it has (<see cref="ReadDocument"/>).
    /// Until 2026-10-23 a failed read answered true with the editor emptied, so a reload on coming back to the
    /// window could leave an empty Untitled with "Reloaded" said over the error.
    /// </summary>
    private bool LoadDocument(string? path, bool keepHistory = false)
    {
        if (ReadDocument(path, keepHistory) is not var (document, writeTime, shown))
        {
            return false;
        }

        ShowDocument(shown, document, writeTime, keepHistory);
        return true;
    }

    /// <summary>
    /// The read (AD-3.7, AP-2.2): the document, its write time and its path for a file that exists and can be read;
    /// an empty document for no path (New), or, in an editor, for a path that does not exist yet, a new file under
    /// that name written by the first save. Null, with the notice shown, when the window keeps what it has: a file
    /// the app refuses (the viewer's kinds), one that cannot be read on a reload (AD-3.6) or in the viewer at all,
    /// and in the viewer a path that does not exist (AP-1.1; until 2026-10-23 it opened an empty window under the
    /// missing name, and Edit in axDown created the file). In an editor a file that cannot be read on an open is the
    /// notice and Untitled, so that Save cannot write the empty editor over it (AD-3.7).
    /// </summary>
    private (TextDocument Document, DateTime? WriteTime, string? Path)? ReadDocument(string? path, bool reloading)
    {
        if (path is null)
        {
            return (TextDocument.Empty(), null, null);
        }

        if (!File.Exists(path))
        {
            if (!_app.ReadOnly)
            {
                return (TextDocument.Empty(), null, path);
            }

            Refused(Notice.Plain("File not found", $"{path} does not exist."));
            return null;
        }

        if (_app.Refuse is { } refuse && refuse(path) is { } refusal)
        {
            Refused(refusal);
            return null;
        }

        try
        {
            return (TextDocument.Read(path), File.GetLastWriteTimeUtc(path), path);
        }
        catch (Exception ex) when (ex is InvalidOperationException or IOException or UnauthorizedAccessException)
        {
            Fail($"{path} could not be {(reloading ? "reloaded" : "opened")}: {ex.Message}");
            if (reloading)
            {
                // The text stays (AD-3.6); the file is not compared again until it is read or saved.
                _lastWriteTime = null;
                return null;
            }

            if (_app.ReadOnly)
            {
                // The viewer keeps what it showed (AP-2.2); on the first load that is Untitled, which needs its title.
                UpdateTitle();
                UpdateStatus();
                return null;
            }

            return (TextDocument.Empty(), null, null);
        }
    }

    /// <summary>A file the window will not show: the notice, and the title and status of what it keeps, which on the first load is Untitled.</summary>
    private void Refused(Notice notice)
    {
        UpdateTitle();
        UpdateStatus();
        ShowNotice(notice with { Unprompted = true });
    }

    /// <summary>The document into the editor under its path: the recent list and Recently closed, the history, the title and the status (AD-3, AD-4.1, AX-9.4).</summary>
    private void ShowDocument(string? path, TextDocument document, DateTime? writeTime, bool keepHistory)
    {
        if (_path is { } old && !string.Equals(old, path, StringComparison.OrdinalIgnoreCase))
        {
            // AX-9.4: the file left behind joins the bundle's Recently closed list.
            RecentlyClosed.Remember(_app.Name, old);
        }

        if (path is not null && !keepHistory)
        {
            // AD-4.1: Open recent file, written at once so that another window of the app lists it too.
            _settings.RememberFile(path);
            SaveSettings();
        }

        _path = path;
        _document = document;
        _lastWriteTime = writeTime;
        _changeNoticed = null;
        _loading = true;
        _editor.Text = _document.Text;
        _editor.Select(0, 0);
        _editor.ClearUndo();
        _loading = false;
        _text = _editor.Text;
        if (!keepHistory)
        {
            _history.Clear();
            _runTimer.Stop();
        }

        _changed = false;
        UpdateTitle();
        UpdateStatus();
        Log.Info(path is null ? "New document" : $"Opened {path}: {_document.EncodingName}, {_document.LineEndingName}");
    }

    private void OnTextChanged(object? sender, EventArgs e)
    {
        if (!_loading && !_replaying)
        {
            // AD-2.5: every change the user makes, as the text before and after it; a typed key says where it was typed.
            var after = _editor.Text;
            _history.Record(_text, after, _editor.SelectionStart, _typedAt);
            _text = after;
            _typedAt = -1;
            _runTimer.Stop();
            _runTimer.Start();
            // The EDIT control's own undo buffer stays empty (AD-D1): the history is the one undo.
            _editor.ClearUndo();
        }

        if (!_loading && !_changed)
        {
            _changed = true;
            UpdateTitle();
        }

        ScheduleStatus();
    }

    private void NewDocument() => AskToSave(() => LoadDocument(null));

    /// <summary>File → Open recent file (AD-4.1): the last ten files opened or saved under a name, from the settings; one that no longer exists is left out, or, on a share listed without the check (AX-9.5), said when chosen rather than opened as a new file.</summary>
    private void FillRecentFiles() =>
        RecentMenu.Fill(_recentFilesItem, _settings.RecentFiles, File.Exists, Path.GetFileName, "&No recent files", target => AskToSave(() =>
        {
            if (File.Exists(target))
            {
                LoadDocument(target);
            }
            else
            {
                Fail($"{target} no longer exists.");
            }
        }));

    /// <summary>File → Show current file in → File Explorer, Ctrl+W (AD-4.1): the file selected in File Explorer, one of the system's own windows (AX-7.2).</summary>
    private void ShowInExplorer()
    {
        if (_path is null)
        {
            Announce("No file yet", true);
            return;
        }

        OpenInExplorer(_path);
    }

    /// <summary>File → Show current file in → axGIT, axFile (AD-4.1): a new window of that app on the file (the repository that holds it; the folder with the file selected).</summary>
    private void OpenIn(string verb)
    {
        if (_path is null)
        {
            Announce("No file yet", true);
            return;
        }

        AxitMenu.Start(verb, _path);
    }

    /// <summary>File → Show current file in → axTerminal: a new axTerminal window with a shell in the file's folder (docs/axterminal/SPEC.md AT-1.5).</summary>
    private void OpenInAxTerminal()
    {
        if (_path is null || Path.GetDirectoryName(_path) is not { } folder)
        {
            Announce("No file yet", true);
            return;
        }

        AxitMenu.Start(Dispatch.AxTerminal, folder);
    }

    private void OpenFile() => AskToSave(() =>
    {
        using var dialog = new OpenFileDialog { Filter = _app.FileFilter, CheckFileExists = true };
        if (_path is not null)
        {
            dialog.InitialDirectory = Path.GetDirectoryName(_path);
        }

        if (ShowSystemDialog(dialog) == DialogResult.OK)
        {
            LoadDocument(dialog.FileName);
            _editor.Select();
        }
    });

    /// <summary>Ctrl+S: into the file, after a name (AD-3.5) and, when the file changed behind the editor, a question (AD-3.6).</summary>
    private void Save(Action? then = null)
    {
        if (_path is null)
        {
            SaveAs(then);
            return;
        }

        if (ChangedOnDisk())
        {
            ShowNotice(new Notice("The file changed on disk",
                $"{DocumentName} was changed by another program since you opened it. Write over it, or reload it and lose your changes?",
                [
                    new OverlayChoice("&Write over", () => WriteFile(_path, then), IsDefault: true),
                    new OverlayChoice("&Reload and lose mine", () => Reload("Reloaded")),
                    new OverlayChoice("&Cancel", IsCancel: true),
                ]));
            return;
        }

        WriteFile(_path, then);
    }

    /// <summary>
    /// AD-3.6, on activation: a file changed on disk is reloaded with the caret kept on its line and column when
    /// nothing is unsaved here, and said once otherwise, since Save will ask. Nothing is watched while the window
    /// stays active; the user leaves it to let another program (Claude) work and comes back.
    /// </summary>
    private void CheckDisk()
    {
        if (_path is null || _lastWriteTime is not { } opened || !File.Exists(_path))
        {
            return;
        }

        var now = File.GetLastWriteTimeUtc(_path);
        if (now == opened)
        {
            return;
        }

        if (_changed)
        {
            if (_changeNoticed != now)
            {
                _changeNoticed = now;
                Announce($"{DocumentName} changed on disk; Save will ask", false);
            }

            return;
        }

        Reload($"Reloaded, {DocumentName} changed on disk");
    }

    /// <summary>The file read again, the caret back on its line and column, <paramref name="announcement"/> said; one step of the history (AD-2.5).</summary>
    private void Reload(string announcement)
    {
        var before = _text;
        var (line, column, _) = Position();
        if (!LoadDocument(_path, keepHistory: true))
        {
            // The file cannot be read or shown now: the notice says so, and the text stays.
            return;
        }

        MoveTo(line, column);
        _history.Record(before, _text, _editor.SelectionStart, typedAt: -1);
        Announce(announcement, false);
    }

    private void SaveAs(Action? then = null)
    {
        using var dialog = new SaveFileDialog
        {
            Filter = _app.FileFilter,
            FileName = DocumentName,
            DefaultExt = _path is null ? _app.DefaultExtension : Path.GetExtension(_path).TrimStart('.'),
            AddExtension = _path is null,
            OverwritePrompt = true,
        };
        if (_path is not null)
        {
            dialog.InitialDirectory = Path.GetDirectoryName(_path);
        }

        if (ShowSystemDialog(dialog) != DialogResult.OK)
        {
            _editor.Select();
            return;
        }

        // The document is renamed once the write has succeeded (AD-3.5): until 2026-10-23 the new path was taken
        // first, so a failed write left the window under the old name with the new path.
        if (WriteFile(dialog.FileName, then))
        {
            _settings.RememberFile(dialog.FileName);
            SaveSettings();
        }
    }

    /// <summary>The text written to <paramref name="path"/>, which becomes the document's; false, with the error as a notice and nothing changed, when it cannot be written.</summary>
    private bool WriteFile(string path, Action? then)
    {
        try
        {
            _document.Write(path, _editor.Text);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            Fail($"{path} could not be saved: {ex.Message}");
            return false;
        }

        _path = path;
        _lastWriteTime = File.GetLastWriteTimeUtc(path);
        _changeNoticed = null;
        _changed = false;
        UpdateTitle();
        UpdateStatus();
        Log.Info($"Saved {path}");
        Announce("Saved", true);
        then?.Invoke();
        return true;
    }

    private bool ChangedOnDisk() =>
        _path is not null && _lastWriteTime is { } opened && File.Exists(_path) && File.GetLastWriteTimeUtc(_path) != opened;

    /// <summary>AD-6.1: runs <paramref name="then"/> at once without changes, otherwise after Save or Don't save; Cancel runs <paramref name="cancelled"/>.</summary>
    private void AskToSave(Action then, Action? cancelled = null)
    {
        if (!_changed)
        {
            then();
            return;
        }

        ShowNotice(new Notice($"Save changes to {DocumentName}?", string.Empty,
        [
            new OverlayChoice("&Save", () => Save(then), IsDefault: true),
            new OverlayChoice("&Don't save", then),
            new OverlayChoice("&Cancel", cancelled, IsCancel: true),
        ]));
    }
}
