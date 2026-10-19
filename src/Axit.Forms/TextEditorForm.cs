using Axit.Core;

namespace Axit.Forms;

/// <summary>
/// The text editor window every editor app of the bundle is (docs/axdown/SPEC.md §5, docs/axjson/SPEC.md AJ-D4):
/// a menu bar, one plain multi-line edit control named after the file, and a status bar, with the app's own name,
/// file types, keys, texts and commands as data (<see cref="EditorApp"/>). The notices, the fonts, the window's
/// place, the update flow and find are the bundle's window base (<see cref="AxitForm"/>); the question area
/// (<see cref="ControlArea"/>) holds the Go to line field under the editor. The caret moves only on the user's
/// command, and nothing is spoken unless asked for or a command finishes (AD-2.6, AD-7). It was AxDown's
/// <c>EditorForm</c> and moved here on its second use, AxJSON (AX-6.3).
/// </summary>
public sealed class TextEditorForm : AxitForm
{
    private const string Untitled = "Untitled";

    private readonly EditorApp _app;
    private readonly EditorSettings _settings;
    private readonly MenuStrip _menu = new();
    private readonly ToolStripMenuItem _recentFilesItem = new("Open &recent file");
    private readonly TextBox _editor = new();
    private readonly StatusStrip _status = new();
    private readonly ToolStripStatusLabel _fileLabel;
    private readonly ToolStripStatusLabel _positionLabel;
    private readonly ControlArea _area = new();
    private readonly ToolStripMenuItem _wordWrapItem;
    private readonly System.Windows.Forms.Timer _statusTimer = new() { Interval = 300 };

    private TextDocument _document = TextDocument.Empty();
    private string? _path;
    private DateTime? _lastWriteTime;

    /// <summary>The write time already said as "changed on disk" while there were unsaved changes, so that it is said once (AD-3.6).</summary>
    private DateTime? _changeNoticed;
    private bool _changed;
    private bool _loading;
    private bool _closeConfirmed;

    public TextEditorForm(EditorApp app, string? path, EditorSettings settings, string? settingsError)
        : base(app.Name, settings, AppPaths.SettingsFile(app.SettingsName))
    {
        _app = app;
        _settings = settings;

        // ---- menus (AD-4); the keys are handled in ProcessWindowKey from the tables, the items only show them ----
        // File is the thing worked on, in the standard order of a File menu (AD-4.1, bundle AX-6): New, Open and the
        // recent files, then Save and Save as where every Windows program has them, then the extras, then Exit.
        // AxClaude's Session menu has the same order with the Save slot empty and its export before Exit.
        var file = new ToolStripMenuItem("&File");
        file.DropDownItems.Add(Item("&New", NewDocument, EditorKeys.NewDocument));
        file.DropDownItems.Add(Item("&Open...", OpenFile, EditorKeys.Open));
        _recentFilesItem.DropDownOpening += (_, _) => FillRecentFiles();
        FillRecentFiles();
        file.DropDownItems.Add(_recentFilesItem);
        file.DropDownItems.Add(new ToolStripSeparator());
        file.DropDownItems.Add(Item("&Save", () => Save(), BundleKeys.Save));
        file.DropDownItems.Add(Item("Save &as...", () => SaveAs(), EditorKeys.SaveAs));
        file.DropDownItems.Add(new ToolStripSeparator());
        // Show current file in ▸ File Explorer and the other apps' windows on the file or its folder (AD-4.1).
        var showIn = new ToolStripMenuItem("Show current file &in");
        showIn.DropDownItems.Add(Item("File &Explorer", ShowInExplorer, EditorKeys.ShowInExplorer));
        // AxGit on the repository that holds the file, a new window (AD-4.1; docs/axgit/SPEC.md AG-1.4).
        showIn.DropDownItems.Add(new ToolStripMenuItem("Ax&Git", null, (_, _) => OpenInAxGit()));
        // AxTerminal with a shell in the file's folder, a new window (docs/axterminal/SPEC.md AT-1.5).
        showIn.DropDownItems.Add(new ToolStripMenuItem("Ax&Terminal", null, (_, _) => OpenInAxTerminal()));
        // AxFile on the file's folder with the file selected, a new window (docs/axfile/SPEC.md AF-1.3).
        showIn.DropDownItems.Add(new ToolStripMenuItem("Ax&File", null, (_, _) => OpenInAxFile()));
        file.DropDownItems.Add(showIn);
        file.DropDownItems.Add(new ToolStripSeparator());
        file.DropDownItems.Add(new ToolStripMenuItem("E&xit", null, (_, _) => Close()) { ShortcutKeyDisplayString = "Alt+F4" });

        var edit = new ToolStripMenuItem("&Edit");
        edit.DropDownItems.Add(new ToolStripMenuItem("&Undo", null, (_, _) => _editor.Undo()) { ShortcutKeyDisplayString = "Ctrl+Z" });
        edit.DropDownItems.Add(new ToolStripSeparator());
        edit.DropDownItems.Add(new ToolStripMenuItem("Cu&t", null, (_, _) => _editor.Cut()) { ShortcutKeyDisplayString = "Ctrl+X" });
        edit.DropDownItems.Add(new ToolStripMenuItem("&Copy", null, (_, _) => _editor.Copy()) { ShortcutKeyDisplayString = "Ctrl+C" });
        edit.DropDownItems.Add(new ToolStripMenuItem("&Paste", null, (_, _) => _editor.Paste()) { ShortcutKeyDisplayString = "Ctrl+V" });
        edit.DropDownItems.Add(new ToolStripMenuItem("Select &all", null, (_, _) => _editor.SelectAll()) { ShortcutKeyDisplayString = "Ctrl+A" });
        if (_app.Commands.Count > 0)
        {
            // The app's own commands on the whole text (AJ-2.3), after the control's own.
            edit.DropDownItems.Add(new ToolStripSeparator());
            foreach (var command in _app.Commands)
            {
                edit.DropDownItems.Add(Item(command.Label, () => RunCommand(command), command.Key));
            }
        }

        var navigate = new ToolStripMenuItem("&Navigate");
        navigate.DropDownItems.Add(Item("&Find...", () => ShowFind(_editor), BundleKeys.Find));
        navigate.DropDownItems.Add(Item("Find &next", () => FindInEditor(1), BundleKeys.FindNext));
        navigate.DropDownItems.Add(Item("Find pre&vious", () => FindInEditor(-1), BundleKeys.FindPrevious));
        navigate.DropDownItems.Add(Item("&Go to line...", ShowGoToLine, EditorKeys.GoToLine));
        navigate.DropDownItems.Add(new ToolStripSeparator());
        // One item per kind under Next and under Previous, from the key pairs of the app's table (AD-D7, AX-7.2).
        var next = new ToolStripMenuItem("Ne&xt");
        var previous = new ToolStripMenuItem("&Previous");
        foreach (var jump in _app.Jumps)
        {
            next.DropDownItems.Add(Item(TextJumps.MenuLabel(jump), () => JumpTo(jump, 1), jump.Next));
            previous.DropDownItems.Add(Item(TextJumps.MenuLabel(jump), () => JumpTo(jump, -1), jump.Previous));
        }

        navigate.DropDownItems.Add(next);
        navigate.DropDownItems.Add(previous);

        // Options, as AxClaude's (AD-4.3): every setting in the submenu for what it is about, none outside one.
        var options = new ToolStripMenuItem("&Options");
        _wordWrapItem = Item("&Word wrap", ToggleWordWrap, EditorKeys.WordWrap);
        _wordWrapItem.Checked = _settings.WordWrap;
        options.DropDownItems.Add(CreateTextMenu(_wordWrapItem));
        options.DropDownItems.Add(CreateGeneralMenu());
        options.DropDownItems.Add(CreateDeveloperMenu());

        // As AxClaude's Claude Code resources: where the file format is explained, in the browser (AD-4.4).
        var help = CreateHelpMenu(_app.UserGuide, _app.ResourcesLabel, _app.Resources);

        // The bundle's Axit menu (AX-9) sits before Help in every window and ends with the update items and About.
        _menu.Items.AddRange([file, edit, navigate, options, CreateAxitMenu(), help]);
        MainMenuStrip = _menu;

        // ---- the editor (AD-2) ----
        _editor.Multiline = true;
        _editor.AcceptsTab = true;
        _editor.AcceptsReturn = true;
        _editor.WordWrap = _settings.WordWrap;
        _editor.ScrollBars = _settings.WordWrap ? ScrollBars.Vertical : ScrollBars.Both;
        _editor.HideSelection = false;
        _editor.MaxLength = 0;
        _editor.Dock = DockStyle.Fill;
        _editor.TabIndex = 0;
        _editor.KeyDown += OnEditorKeyDown;
        _editor.KeyUp += (_, _) => ScheduleStatus();
        _editor.MouseUp += (_, _) => ScheduleStatus();
        _editor.TextChanged += OnTextChanged;
        _statusTimer.Tick += (_, _) =>
        {
            _statusTimer.Stop();
            UpdateStatus();
        };

        // ---- the status bar (AD-9): the bundle's pair, no AccessibleName, no Spring ----
        (_fileLabel, _positionLabel) = StatusLayout.Pair(_status);

        Overlay.Jumps = _app.Jumps;
        _area.TabIndex = 1;
        SizeChanged += (_, _) => _area.FitHeight();

        // Docked from the last control to the first: the menu takes the top, the status bar the bottom, the question
        // area the strip above it, and the editor (or the notice over it) what is left.
        Controls.AddRange([Overlay, _editor, _area, _status, _menu]);

        ApplyTextFont();
        RestoreWindow();
        LoadDocument(path);

        if (settingsError is not null)
        {
            ShowNotice(Notice.Plain("Settings", $"The settings file could not be read, so the defaults are used.\n{settingsError}") with { Unprompted = true });
        }
    }

    private string DocumentName => _path is null ? Untitled : Path.GetFileName(_path);

    // ---- what the window base asks for ----

    protected override string Shortcuts => _app.Shortcuts;

    protected override void HideForNotice()
    {
        _editor.Visible = false;
        _area.Visible = false;
    }

    protected override void ShowAfterNotice()
    {
        _editor.Visible = true;
        _area.Visible = _area.Current is not null;
    }

    /// <summary>The editor, or the Go to line field while it shows.</summary>
    protected override Control FocusTarget => _area.Current ?? _editor;

    protected override Control AnnounceSource => _editor;

    protected override void ApplyFont(Font font)
    {
        _editor.Font = font;
        _area.Font = font;
        _area.MinimumHeight = TextRenderer.MeasureText("Wg", font).Height * 2 + 12;
        _area.FitHeight();
    }

    protected override IReadOnlyList<string> RestartArguments() => _path is null ? [_app.Verb] : [_app.Verb, _path];

    protected override string UpdateRestartText =>
        $"closes {_app.Name} (asking first when there are unsaved changes), installs it and starts it again on the same file";

    protected override IEnumerable<string> DiagnosticsLines => [$"File: {_path ?? "(none)"} ({_document.EncodingName}, {_document.LineEndingName})"];

    protected override void OnShown(EventArgs e)
    {
        if (!NoticeOpen)
        {
            _editor.Select();
        }

        base.OnShown(e);
    }

    /// <summary>Back in this window: a file another program changed meanwhile is reloaded, or noted (AD-3.6).</summary>
    protected override void OnActivated(EventArgs e)
    {
        base.OnActivated(e);
        CheckDisk();
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (_changed && !_closeConfirmed)
        {
            // AD-6.1: unsaved changes are never lost without asking, also when Windows closes the window.
            e.Cancel = true;
            AskToSave(() =>
            {
                _closeConfirmed = true;
                Close();
            }, KeepWorkingAfterUpdate);
            base.OnFormClosing(e);
            return;
        }

        if (_path is { } closing)
        {
            // AX-9.4: the file joins the bundle's Recently closed list.
            RecentlyClosed.Remember(_app.Name, closing);
        }

        base.OnFormClosing(e);
    }

    // ---- keys (AD-5): every key comes from BundleKeys, EditorKeys or the app's table, and is handled here only ----

    protected override bool ProcessWindowKey(Keys keyData)
    {
        switch (keyData)
        {
            case BundleKeys.NextControl:
            case BundleKeys.PreviousControl:
            case BundleKeys.NextControlAlso:
                // One main control: the editor, or the Go to line field while it shows.
                (_area.Current ?? _editor).Focus();
                return true;
            case BundleKeys.Find:
                ShowFind(_editor);
                return true;
            case BundleKeys.FindNext:
                FindInEditor(1);
                return true;
            case BundleKeys.FindPrevious:
                FindInEditor(-1);
                return true;
            case BundleKeys.Save:
                Save();
                return true;
            case EditorKeys.NewDocument:
                NewDocument();
                return true;
            case EditorKeys.Open:
                OpenFile();
                return true;
            case EditorKeys.ShowInExplorer:
                ShowInExplorer();
                return true;
            case EditorKeys.SaveAs:
                SaveAs();
                return true;
            case EditorKeys.GoToLine:
                ShowGoToLine();
                return true;
            case EditorKeys.WordWrap:
                ToggleWordWrap();
                return true;
        }

        foreach (var command in _app.Commands)
        {
            if (keyData == command.Key)
            {
                RunCommand(command);
                return true;
            }
        }

        // The app's navigation keys, from the pairs its table gives the notice text as well (AD-D7).
        if (TextJumps.Handle(_editor, _app.Jumps, keyData, Announce))
        {
            UpdateStatus();
            return true;
        }

        return false;
    }

    private void OnEditorKeyDown(object? sender, KeyEventArgs e)
    {
        // AD-2.3: the bundle's page and paragraph keys; the control's own do nothing while the text fits, or scroll a row and leave the caret.
        EditKeys.Handle(sender, e);
        if (e.Handled)
        {
            ScheduleStatus();
            return;
        }

        if (e.Control && !e.Alt && e.KeyCode is Keys.H or Keys.I or Keys.J or Keys.M)
        {
            // AD-5.1: in an EDIT control these arrive as Backspace, Tab, line feed and Enter characters.
            e.Handled = e.SuppressKeyPress = true;
        }
    }

    // ---- the document (AD-3) ----

    private void LoadDocument(string? path)
    {
        if (_path is { } old && !string.Equals(old, path, StringComparison.OrdinalIgnoreCase))
        {
            // AX-9.4: the file left behind joins the bundle's Recently closed list.
            RecentlyClosed.Remember(_app.Name, old);
        }

        _path = path;
        _document = TextDocument.Empty();
        _lastWriteTime = null;
        _changeNoticed = null;
        if (path is not null && File.Exists(path))
        {
            try
            {
                _document = TextDocument.Read(path);
                _lastWriteTime = File.GetLastWriteTimeUtc(path);
                // AD-4.1: Open recent file, written at once so that another window of the app lists it too.
                _settings.RememberFile(path);
                SaveSettings();
            }
            catch (Exception ex) when (ex is InvalidOperationException or IOException or UnauthorizedAccessException)
            {
                // Untitled, so that Save cannot write the empty editor over the file (AD-3.7).
                _path = null;
                Fail($"{path} could not be opened: {ex.Message}");
            }
        }

        _loading = true;
        _editor.Text = _document.Text;
        _editor.Select(0, 0);
        _editor.ClearUndo();
        _loading = false;
        _changed = false;
        UpdateTitle();
        UpdateStatus();
        Log.Info(path is null ? "New document" : $"Opened {path}: {_document.EncodingName}, {_document.LineEndingName}");
    }

    private void OnTextChanged(object? sender, EventArgs e)
    {
        if (!_loading && !_changed)
        {
            _changed = true;
            UpdateTitle();
        }

        ScheduleStatus();
    }

    private void NewDocument() => AskToSave(() => LoadDocument(null));

    /// <summary>File → Open recent file (AD-4.1): the last ten files opened or saved under a name, from the settings; one that no longer exists is left out.</summary>
    private void FillRecentFiles() =>
        RecentMenu.Fill(_recentFilesItem, _settings.RecentFiles, File.Exists, Path.GetFileName, "&No recent files", target => AskToSave(() => LoadDocument(target)));

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

    /// <summary>File → Show current file in → AxGit (AD-4.1): a new AxGit window on the repository that holds the file.</summary>
    private void OpenInAxGit()
    {
        if (_path is null)
        {
            Announce("No file yet", true);
            return;
        }

        AxitMenu.Start(Dispatch.AxGit, _path);
    }

    /// <summary>File → Show current file in → AxTerminal: a new AxTerminal window with a shell in the file's folder (docs/axterminal/SPEC.md AT-1.5).</summary>
    private void OpenInAxTerminal()
    {
        if (_path is null || Path.GetDirectoryName(_path) is not { } folder)
        {
            Announce("No file yet", true);
            return;
        }

        AxitMenu.Start(Dispatch.AxTerminal, folder);
    }

    /// <summary>File → Show current file in → AxFile: a new AxFile window on the file's folder with the file selected (docs/axfile/SPEC.md AF-1.3).</summary>
    private void OpenInAxFile()
    {
        if (_path is null)
        {
            Announce("No file yet", true);
            return;
        }

        AxitMenu.Start(Dispatch.AxFile, _path);
    }

    private void OpenFile() => AskToSave(() =>
    {
        using var dialog = new OpenFileDialog { Filter = _app.FileFilter, CheckFileExists = true };
        if (_path is not null)
        {
            dialog.InitialDirectory = Path.GetDirectoryName(_path);
        }

        if (dialog.ShowDialog(this) == DialogResult.OK)
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

    /// <summary>The file read again, the caret back on its line and column, <paramref name="announcement"/> said.</summary>
    private void Reload(string announcement)
    {
        var (line, column, _) = Position();
        LoadDocument(_path);
        MoveTo(line, column);
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

        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            _editor.Select();
            return;
        }

        _path = dialog.FileName;
        _settings.RememberFile(_path);
        SaveSettings();
        WriteFile(_path, then);
    }

    private void WriteFile(string path, Action? then)
    {
        try
        {
            _document.Write(path, _editor.Text);
            _lastWriteTime = File.GetLastWriteTimeUtc(path);
            _changeNoticed = null;
            _changed = false;
            UpdateTitle();
            UpdateStatus();
            Log.Info($"Saved {path}");
            Announce("Saved", true);
            then?.Invoke();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            Fail($"{path} could not be saved: {ex.Message}");
        }
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

    /// <summary>An app command on the whole text (AJ-2.3): the text replaced in one undo step, the caret kept at its offset, one announcement.</summary>
    private void RunCommand(EditorCommand command)
    {
        var result = command.Run(_editor.Text);
        if (result.Text != _editor.Text)
        {
            var caret = _editor.SelectionStart;
            _editor.SelectAll();
            _editor.SelectedText = result.Text;
            _editor.Select(Math.Min(caret, result.Text.Length), 0);
            _editor.ScrollToCaret();
            UpdateStatus();
        }

        Announce(result.Message, true);
    }

    // ---- find and go to line (AD-6.3, AD-6.4) ----

    /// <summary>F3 and Shift+F3: the base's find on the editor, then the status bar for the new caret.</summary>
    private void FindInEditor(int direction)
    {
        if (FindNext(_editor, direction))
        {
            ScheduleStatus();
        }
    }

    private void ShowGoToLine()
    {
        var (line, column, lines) = Position();
        _area.ShowField($"Go to line (now line {line}, column {column})", focus: true, hold: false,
            enter: text =>
            {
                _area.Clear();
                _editor.Select();
                if (int.TryParse(text.Trim(), out var wanted))
                {
                    GoToLine(Math.Clamp(wanted, 1, lines), lines);
                }
                else
                {
                    Announce("Not a line number", true);
                }
            },
            escape: () =>
            {
                _area.Clear();
                _editor.Select();
            });
    }

    private void GoToLine(int line, int lines)
    {
        MoveTo(line, 1);
        Announce($"Line {line} of {lines}", true);
    }

    /// <summary>The caret to a line and column of the text (1-based), clamped to what is there; the status bar follows.</summary>
    private void MoveTo(int line, int column)
    {
        var text = _editor.Text;
        var index = 0;
        for (var current = 1; current < line && index < text.Length; index++)
        {
            if (text[index] == '\n')
            {
                current++;
            }
        }

        var end = text.IndexOf('\n', index);
        var length = (end < 0 ? text.Length : end) - index;
        _editor.Select(index + Math.Clamp(column - 1, 0, Math.Max(0, length)), 0);
        _editor.ScrollToCaret();
        UpdateStatus();
    }

    /// <summary>A Navigate menu item: the shared jump on the editor (AD-D7), then the status bar.</summary>
    private void JumpTo(TextJump jump, int direction)
    {
        TextJumps.Jump(_editor, jump, direction, Announce);
        UpdateStatus();
    }

    /// <summary>The caret's line and column, counting the file's lines (not the wrapped rows), and the number of lines.</summary>
    private (int Line, int Column, int Lines) Position()
    {
        var text = _editor.Text;
        var caret = Math.Min(_editor.SelectionStart, text.Length);
        var line = 1;
        var lineStart = 0;
        for (var i = 0; i < caret; i++)
        {
            if (text[i] == '\n')
            {
                line++;
                lineStart = i + 1;
            }
        }

        var lines = line;
        for (var i = caret; i < text.Length; i++)
        {
            if (text[i] == '\n')
            {
                lines++;
            }
        }

        return (line, caret - lineStart + 1, lines);
    }

    // ---- title and status (AD-9, AD-10) ----

    private void UpdateTitle()
    {
        Text = (_changed ? "*" : string.Empty) + DocumentName + " - " + _app.Name;
        _editor.AccessibleName = DocumentName;
    }

    private void ScheduleStatus()
    {
        _statusTimer.Stop();
        _statusTimer.Start();
    }

    private void UpdateStatus()
    {
        var (line, column, _) = Position();
        _fileLabel.Text = DocumentName + (_changed ? ", unsaved changes" : string.Empty);
        _positionLabel.Text = $"Line {line}, Column {column} · {_document.EncodingName} · {_document.LineEndingName}";
        StatusLayout.Fit(_status, _fileLabel, _positionLabel);
    }

    // ---- word wrap (AD-2.4) ----

    private void ToggleWordWrap()
    {
        // Changing WordWrap recreates the control's handle, which loses the caret; it is put back.
        var start = _editor.SelectionStart;
        var length = _editor.SelectionLength;
        _settings.WordWrap = !_settings.WordWrap;
        _loading = true;
        _editor.WordWrap = _settings.WordWrap;
        _editor.ScrollBars = _settings.WordWrap ? ScrollBars.Vertical : ScrollBars.Both;
        _loading = false;
        _editor.Select(start, length);
        _editor.ScrollToCaret();
        _wordWrapItem.Checked = _settings.WordWrap;
        SaveSettings();
        Announce(_settings.WordWrap ? "Word wrap on" : "Word wrap off", true);
    }
}
