using Axit.Core;

namespace Axit.Forms;

/// <summary>
/// The text editor window every editor app of the bundle is (docs/axdown/SPEC.md §5, docs/axjson/SPEC.md AJ-D4):
/// a menu bar, one plain multi-line edit control named after the file, and a status bar, with the app's own name,
/// file types, keys, texts and commands as data (<see cref="EditorApp"/>). The notices, the fonts, the window's
/// place, the update flow and find are the bundle's window base (<see cref="AxitForm"/>); the question area
/// (<see cref="ControlArea"/>) holds the Go to line field under the editor. The caret moves only on the user's
/// command, and nothing is spoken unless asked for or a command finishes (AD-2.6, AD-7). It was axDown's
/// <c>EditorForm</c> and moved here on its second use, axJSON (AX-6.3). What the viewer alone does is
/// <c>TextEditorForm.Viewer.cs</c>.
/// </summary>
public sealed partial class TextEditorForm : AxitForm
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

    // ---- undo and redo (AD-2.5): the window's own history over the text, one step per change ----
    private readonly EditHistory _history = new();

    /// <summary>A second without a change ends a run of typing, so that the next key starts a step of its own.</summary>
    private readonly System.Windows.Forms.Timer _runTimer = new() { Interval = 1000 };

    /// <summary>The text as the history last saw it: the before of the next change.</summary>
    private string _text = string.Empty;

    /// <summary>Where the caret was when a character, Backspace or Delete was typed, until the change it makes is recorded; -1 otherwise.</summary>
    private int _typedAt = -1;
    private bool _replaying;

    private TextDocument _document = TextDocument.Empty();
    private string? _path;
    private DateTime? _lastWriteTime;

    /// <summary>The write time already said as "changed on disk" while there were unsaved changes, so that it is said once (AD-3.6).</summary>
    private DateTime? _changeNoticed;
    private bool _changed;
    private bool _loading;
    private bool _closeConfirmed;

    public TextEditorForm(EditorApp app, string? path, EditorSettings settings, string? settingsError)
        : base(app.Name, settings, AppPaths.SettingsFile(app.SettingsName), settingsError)
    {
        _app = app;
        _settings = settings;

        // ---- menus (AD-4); the keys are handled in ProcessWindowKey from the tables, the items only show them ----
        // File is the thing worked on, in the standard order of a File menu (AD-4.1, bundle AX-6): New, Open and the
        // recent files, then Save and Save as where every Windows program has them, then the extras, then Exit.
        // axClaude's Session menu has the same order with the Save slot empty and its export before Exit.
        var file = new ToolStripMenuItem("&File");
        file.DropDownItems.Add(_app.ReadOnly
            ? Item("&New window", NewWindow, EditorKeys.NewDocument)
            : Item("&New", NewDocument, EditorKeys.NewDocument));
        file.DropDownItems.Add(Item("&Open...", OpenFile, EditorKeys.Open));
        CommandPalette.FilledOnOpening(_recentFilesItem).DropDownOpening += (_, _) => FillRecentFiles();
        FillRecentFiles();
        file.DropDownItems.Add(_recentFilesItem);
        file.DropDownItems.Add(new ToolStripSeparator());
        if (_app.ReadOnly)
        {
            AddViewerFileItems(file);
        }
        else
        {
            file.DropDownItems.Add(Item("&Save", () => Save(), BundleKeys.Save));
            file.DropDownItems.Add(Item("Save &as...", () => SaveAs(), EditorKeys.SaveAs));
            file.DropDownItems.Add(new ToolStripSeparator());
        }

        // Show current file in ▸ File Explorer and the other apps' windows on the file or its folder (AD-4.1).
        var showIn = new ToolStripMenuItem("Show current file &in");
        showIn.DropDownItems.Add(Item("File &Explorer", ShowInExplorer, EditorKeys.ShowInExplorer));
        // axGIT on the repository that holds the file, a new window (AD-4.1; docs/axgit/SPEC.md AG-1.4).
        showIn.DropDownItems.Add(new ToolStripMenuItem("Ax&Git", null, (_, _) => OpenIn(Dispatch.AxGit)));
        // axTerminal with a shell in the file's folder, a new window (docs/axterminal/SPEC.md AT-1.5).
        showIn.DropDownItems.Add(new ToolStripMenuItem("Ax&Terminal", null, (_, _) => OpenInAxTerminal()));
        // axFile on the file's folder with the file selected, a new window (docs/axfile/SPEC.md AF-1.3).
        showIn.DropDownItems.Add(new ToolStripMenuItem("Ax&File", null, (_, _) => OpenIn(Dispatch.AxFile)));
        file.DropDownItems.Add(showIn);
        file.DropDownItems.Add(new ToolStripSeparator());
        file.DropDownItems.Add(CreateExitItem());

        // No Edit menu in the viewer (AP-3.1): the control's own Ctrl+A and Ctrl+C still work there.
        ToolStripMenuItem? edit = null;
        if (!_app.ReadOnly)
        {
            edit = new ToolStripMenuItem("&Edit");
            AddEditItems(edit.DropDownItems);
            if (_app.Commands.Count > 0)
            {
                // The app's own commands on the whole text (AJ-2.3), after the control's own.
                edit.DropDownItems.Add(new ToolStripSeparator());
                foreach (var command in _app.Commands)
                {
                    edit.DropDownItems.Add(Item(command.Label, () => RunCommand(command), command.Key));
                }
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

        // Options, as axClaude's (AD-4.3): every setting in the submenu for what it is about, none outside one.
        var options = new ToolStripMenuItem("&Options");
        _wordWrapItem = Item("&Word wrap", ToggleWordWrap, EditorKeys.WordWrap);
        _wordWrapItem.Checked = _settings.WordWrap;
        options.DropDownItems.Add(CreateTextMenu(_wordWrapItem));
        options.DropDownItems.Add(CreateGeneralMenu());
        options.DropDownItems.Add(CreateDeveloperMenu());

        // As axClaude's Claude Code resources: where the file format is explained, in the browser (AD-4.4).
        var help = CreateHelpMenu(_app.UserGuide, _app.ResourcesLabel, _app.Resources);

        // The bundle's Axit menu (AX-9) sits before Help in every window and ends with the update items and About.
        _menu.Items.AddRange(edit is null ? [file, navigate, options, CreateAxitMenu(), help] : [file, edit, navigate, options, CreateAxitMenu(), help]);
        MainMenuStrip = _menu;

        // ---- the editor (AD-2) ----
        _editor.Multiline = true;
        _editor.AcceptsTab = true;
        _editor.AcceptsReturn = true;
        if (_app.ReadOnly)
        {
            SetUpViewer();
        }

        _editor.WordWrap = _settings.WordWrap;
        _editor.ScrollBars = _settings.WordWrap ? ScrollBars.Vertical : ScrollBars.Both;
        _editor.HideSelection = false;
        _editor.MaxLength = 0;
        _editor.Dock = DockStyle.Fill;
        _editor.TabIndex = 0;
        // The window's own context menu in place of the EDIT control's, whose Undo reaches the control's buffer (AD-D1).
        var context = new ContextMenuStrip();
        AddEditItems(context.Items);
        _editor.ContextMenuStrip = context;
        _editor.KeyDown += OnEditorKeyDown;
        _editor.KeyPress += OnEditorKeyPress;
        _editor.KeyUp += (_, _) =>
        {
            _typedAt = -1;
            ScheduleStatus();
        };
        _editor.MouseUp += (_, _) => ScheduleStatus();
        _editor.TextChanged += OnTextChanged;
        _statusTimer.Tick += (_, _) =>
        {
            _statusTimer.Stop();
            UpdateStatus();
        };
        _runTimer.Tick += (_, _) =>
        {
            _runTimer.Stop();
            _history.Break();
        };

        // ---- the status bar (AD-9): the bundle's pair, no AccessibleName, no Spring ----
        (_fileLabel, _positionLabel) = StatusLayout.Pair(_status);

        // A notice's text takes the heading letters of every window and the editor's own pairs as well (AX-7.2).
        Overlay.Jumps = [.. TextJump.HeadingLetters, .. _app.Jumps];
        _area.TabIndex = 1;
        SizeChanged += (_, _) => _area.FitHeight();

        // Docked from the last control to the first: the menu takes the top, the status bar the bottom, the question
        // area the strip above it, and the editor (or the notice over it) what is left.
        Controls.AddRange([Overlay, _editor, _area, _status, _menu]);

        ApplyTextFont();
        RestoreWindow();
        LoadDocument(path);
    }

    private string DocumentName => _path is null ? Untitled : Path.GetFileName(_path);

    /// <summary>
    /// The Edit menu's items and the text's context menu (AD-4.1): the window's own Undo and Redo, then the control's
    /// Cut, Copy, Paste and Select all; in the viewer Copy and Select all alone. The EDIT control's own menu would undo
    /// through the control's buffer, outside the window's history (AD-D1; replaced 2026-10-23).
    /// </summary>
    private void AddEditItems(ToolStripItemCollection items)
    {
        if (!_app.ReadOnly)
        {
            items.Add(Item("&Undo", Undo, EditorKeys.Undo));
            items.Add(Item("&Redo", Redo, EditorKeys.Redo));
            items.Add(new ToolStripSeparator());
            items.Add(new ToolStripMenuItem("Cu&t", null, (_, _) => _editor.Cut()) { ShortcutKeyDisplayString = "Ctrl+X" });
        }

        items.Add(new ToolStripMenuItem("&Copy", null, (_, _) => _editor.Copy()) { ShortcutKeyDisplayString = "Ctrl+C" });
        if (!_app.ReadOnly)
        {
            items.Add(new ToolStripMenuItem("&Paste", null, (_, _) => _editor.Paste()) { ShortcutKeyDisplayString = "Ctrl+V" });
        }

        items.Add(new ToolStripMenuItem("Select &all", null, (_, _) => _editor.SelectAll()) { ShortcutKeyDisplayString = "Ctrl+A" });
    }

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

    /// <summary>Ctrl+S: into the file (AD-3.5); nothing to save in the viewer (AP-3.2), which the base says.</summary>
    protected override void SaveCommand()
    {
        if (_app.ReadOnly)
        {
            base.SaveCommand();
            return;
        }

        Save();
    }

    protected override void ApplyFont(Font font)
    {
        _editor.Font = font;
        _area.Font = font;
        _area.MinimumHeight = TextRenderer.MeasureText("Wg", font).Height * 2 + 12;
        _area.FitHeight();
    }

    protected override IReadOnlyList<string> RestartArguments() => _path is null ? [_app.Verb] : [_app.Verb, _path];

    protected override string UpdateRestartText => _app.ReadOnly
        ? $"closes {_app.Name}, installs it and starts it again on the same file"
        : $"closes {_app.Name} (asking first when there are unsaved changes), installs it and starts it again on the same file";

    protected override IEnumerable<string> DiagnosticsLines => [$"File: {_path ?? "(none)"} ({_document.EncodingName}, {_document.LineEndingName})"];

    protected override void OnShown(EventArgs e)
    {
        if (!NoticeOpen)
        {
            _editor.Select();
        }

        base.OnShown(e);
    }

    /// <summary>
    /// Back in this window: a file another program changed meanwhile is reloaded, or noted (AD-3.6). Not when one of
    /// the Windows dialogs (Open, Save as, the font) is closing, which activates the window before the command has
    /// gone on: until 2026-10-23 "Reloaded" could be said in the middle of Save as.
    /// </summary>
    protected override void OnActivated(EventArgs e)
    {
        base.OnActivated(e);
        if (!DialogShowing)
        {
            CheckDisk();
        }
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
        if (_app.ReadOnly && ViewerKey(keyData) is { } taken)
        {
            return taken;
        }

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
            case EditorKeys.Undo:
                Undo();
                return true;
            case EditorKeys.Redo:
            case EditorKeys.RedoAlso:
                Redo();
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
            return;
        }

        if (e.Alt && !e.Control && e.KeyCode == Keys.Back)
        {
            // The EDIT control's own undo key (AD-D1): the window's Undo in its place, nothing in the viewer.
            e.Handled = e.SuppressKeyPress = true;
            if (!_app.ReadOnly)
            {
                Undo();
            }

            return;
        }

        if (e.KeyCode == Keys.Delete && !e.Shift)
        {
            // Delete, and Ctrl+Delete for a word, come as no character: typed for the history (AD-2.5); Shift+Delete is a cut.
            _typedAt = _editor.SelectionStart;
        }
    }

    /// <summary>A character, Backspace, Enter or Tab about to change the text: where it was typed, for the history's runs (AD-2.5); the control characters of Ctrl+letter are not typing.</summary>
    private void OnEditorKeyPress(object? sender, KeyPressEventArgs e)
    {
        if (e.KeyChar >= ' ' || e.KeyChar is '\b' or '\r' or '\t')
        {
            _typedAt = _editor.SelectionStart;
        }
    }

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

    // ---- undo and redo (AD-2.5) ----

    private void Undo()
    {
        if (_history.Undo() is not { } step)
        {
            Announce("Nothing to undo", true);
            return;
        }

        Replace(step.Start, step.Inserted.Length, step.Removed);
        Announce("Undone", true);
    }

    private void Redo()
    {
        if (_history.Redo() is not { } step)
        {
            Announce("Nothing to redo", true);
            return;
        }

        Replace(step.Start, step.Removed.Length, step.Inserted);
        Announce("Redone", true);
    }

    /// <summary>A step applied through the selection, as Format is, so that NVDA hears only the announcement; the caret lands after the text put in, where the change was.</summary>
    private void Replace(int start, int length, string text)
    {
        _replaying = true;
        _editor.Select(start, length);
        _editor.SelectedText = text;
        _replaying = false;
        _text = _editor.Text;
        _runTimer.Stop();
        _editor.Select(start + text.Length, 0);
        _editor.ScrollToCaret();
        UpdateStatus();
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

    /// <summary>An app command on the whole text (AJ-2.3): the text replaced in one undo step, the caret kept at its offset, one announcement.</summary>
    private void RunCommand(EditorCommand command)
    {
        var result = command.Run(_editor.Text);
        if (result.Text != _editor.Text)
        {
            var caret = _editor.SelectionStart;
            var before = _editor.Text;
            // One undo step for the whole command (AJ-2.3): the control reports the replacement as two changes, the
            // text removed and the text put in, which were two steps, the first leaving an empty document (found by
            // tools/AxAutomate, 2026-10-23); so the change is recorded here, once.
            _replaying = true;
            _editor.SelectAll();
            _editor.SelectedText = result.Text;
            _replaying = false;
            _history.Record(before, _editor.Text, _editor.SelectionStart, -1);
            _text = _editor.Text;
            _editor.ClearUndo();
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

    /// <summary>The caret to a line and column of the text (1-based), clamped to what is there (<see cref="TextLines"/>); the status bar follows.</summary>
    private void MoveTo(int line, int column)
    {
        _editor.Select(TextLines.IndexOf(_editor.Text, line, column), 0);
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
    private (int Line, int Column, int Lines) Position() => TextLines.Position(_editor.Text, _editor.SelectionStart);

    // ---- title and status (AD-9, AD-10) ----

    private void UpdateTitle()
    {
        Text = (_changed ? "*" : string.Empty) + DocumentName + " - " + _app.Name;
        _editor.AccessibleName = DocumentName;
        UpdateEditItem();
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
