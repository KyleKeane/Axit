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

    /// <summary>Edit → Undo and Redo, in the Edit menu with their keys and in the text's context menu (AD-4.1).</summary>
    private readonly Command _undo;
    private readonly Command _redo;

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
        _undo = new Command("&Undo", EditorKeys.Undo, Undo);
        _redo = new Command("&Redo", EditorKeys.Redo, Redo);

        // ---- menus and keys (AD-4, AD-5): one command list (docs/axit/app-kit.md AK-2), each key from the tables ----
        // File is the thing worked on, in the standard order of a File menu (AD-4.1, bundle AX-6): New, Open and the
        // recent files, then Save and Save as where every Windows program has them, then the extras, then Exit.
        // axClaude's Session menu has the same order with the Save slot empty and its export before Exit.
        // In the viewer (AP-3.2) the commands that write are not made at all, so that their keys do nothing there.
        var file = new ToolStripMenuItem("&File");
        if (_app.ReadOnly)
        {
            Add(file, new Command("&New window", EditorKeys.NewDocument, NewWindow) { Mark = CommandMark.Open });
        }
        else
        {
            Add(file, new Command("&New", EditorKeys.NewDocument, NewDocument));
        }

        Add(file, new Command("&Open...", EditorKeys.Open, OpenFile) { Mark = CommandMark.Open });
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
            // Ctrl+S is the base's key (SaveCommand), so Save stays an item that shows it.
            file.DropDownItems.Add(Item("&Save", () => Save(), BundleKeys.Save));
            Add(file, new Command("Save &as...", EditorKeys.SaveAs, () => SaveAs()));
            file.DropDownItems.Add(new ToolStripSeparator());
        }

        // Show current file in ▸ File Explorer and the other apps' windows on the file or its folder (AD-4.1).
        // axClaude and axTerminal on the file's folder, axGIT on the repository that holds it, axFile with the file selected.
        var explorer = new Command("File &Explorer", EditorKeys.ShowInExplorer, ShowInExplorer) { Mark = CommandMark.Open };
        file.DropDownItems.Add(CreateShowInMenu("Show current file &in", Add(explorer), FileOrSay));
        file.DropDownItems.Add(new ToolStripSeparator());
        file.DropDownItems.Add(CreateExitItem());

        // No Edit menu in the viewer (AP-3.1): the control's own Ctrl+A and Ctrl+C still work there.
        ToolStripMenuItem? edit = null;
        if (!_app.ReadOnly)
        {
            edit = new ToolStripMenuItem("&Edit");
            Add(edit, _undo);
            Add(edit, _redo);
            AddKey(_redo with { Key = EditorKeys.RedoAlso });
            AddClipboardItems(edit.DropDownItems);
            if (_app.Commands.Count > 0)
            {
                // The app's own commands on the whole text (AJ-2.3), after the control's own.
                edit.DropDownItems.Add(new ToolStripSeparator());
                foreach (var command in _app.Commands)
                {
                    Add(edit, new Command(command.Label, command.Key, () => RunCommand(command)));
                }
            }
        }

        var navigate = new ToolStripMenuItem("&Navigate");
        Add(navigate, new Command("&Find...", BundleKeys.Find, () => ShowFind(_editor)) { Mark = CommandMark.Read });
        Add(navigate, new Command("Find &next", BundleKeys.FindNext, () => FindInEditor(1)) { Mark = CommandMark.Read });
        Add(navigate, new Command("Find pre&vious", BundleKeys.FindPrevious, () => FindInEditor(-1)) { Mark = CommandMark.Read });
        Add(navigate, new Command("&Go to line...", EditorKeys.GoToLine, ShowGoToLine) { Mark = CommandMark.Read });
        navigate.DropDownItems.Add(new ToolStripSeparator());
        // One item per kind under Next and under Previous, from the key pairs of the app's table (AD-D7, AX-7.2).
        var next = new ToolStripMenuItem("Ne&xt");
        var previous = new ToolStripMenuItem("&Previous");
        foreach (var jump in _app.Jumps)
        {
            Add(next, new Command(TextJumps.MenuLabel(jump), jump.Next, () => JumpTo(jump, 1)) { Mark = CommandMark.Read });
            Add(previous, new Command(TextJumps.MenuLabel(jump), jump.Previous, () => JumpTo(jump, -1)) { Mark = CommandMark.Read });
        }

        navigate.DropDownItems.Add(next);
        navigate.DropDownItems.Add(previous);

        // Options, as axClaude's (AD-4.3): every setting in the submenu for what it is about, none outside one.
        // The item goes into the Text submenu the base makes, so it is made from the command here and its key added.
        var options = new ToolStripMenuItem("&Options");
        var wordWrap = new Command("&Word wrap", EditorKeys.WordWrap, ToggleWordWrap);
        _wordWrapItem = Add(wordWrap);
        _wordWrapItem.Checked = _settings.WordWrap;
        options.DropDownItems.Add(CreateTextMenu(_wordWrapItem));
        options.DropDownItems.Add(CreateGeneralMenu());
        options.DropDownItems.Add(CreateDeveloperMenu());

        // As axClaude's Claude Code resources: where the file format is explained, in the browser (AD-4.4).
        var help = CreateHelpMenu(_app.UserGuide, _app.ResourcesLabel, _app.Resources);

        // The bundle's Axit menu (AX-9) sits before Help in every window and ends with the update items and About.
        _menu.Items.AddRange(edit is null ? [file, navigate, options, CreateAxitMenu(), help] : [file, edit, navigate, options, CreateAxitMenu(), help]);
        MainMenuStrip = _menu;

        // The keys without a menu item: one main control, the editor, or the Go to line field while it shows; in the
        // viewer with its Details list, the text and the list in turn (AP-6.1); the viewer's line number. The keys the
        // edit control would act on first are OnEditorKeyDown's (AD-5.1).
        foreach (var key in new[] { BundleKeys.NextControl, BundleKeys.PreviousControl, BundleKeys.NextControlAlso })
        {
            AddKey(new Command("Between the text and the details", key, SwitchPane) { Mark = CommandMark.Open });
        }

        if (_app.ReadOnly)
        {
            AddViewerKeys();
        }

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
        if (!_app.ReadOnly)
        {
            context.Items.Add(Item(_undo.Label, _undo.Run, _undo.Key));
            context.Items.Add(Item(_redo.Label, _redo.Run, _redo.Key));
        }

        AddClipboardItems(context.Items);
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

        // Docked from the last control to the first: the menu takes the top, the status bar the bottom, the viewer's
        // Details list the third above it (AP-6.1), the question area the strip above that, under the text, and the
        // editor (or the notice over it) what is left.
        Controls.AddRange(_details is null ? [Overlay, _editor, _area, _status, _menu] : [Overlay, _editor, _area, _details, _status, _menu]);

        ApplyTextFont();
        RestoreWindow();
        FitDetails();
        LoadDocument(path);
    }

    private string DocumentName => _path is null ? Untitled : Path.GetFileName(_path);

    /// <summary>
    /// The Edit menu's and the text's context menu's items after the window's own Undo and Redo (AD-4.1): the control's
    /// Cut, Copy, Paste and Select all, whose keys are the control's own; in the viewer Copy and Select all alone. The
    /// EDIT control's own menu would undo through the control's buffer, outside the window's history (AD-D1; replaced 2026-10-23).
    /// </summary>
    private void AddClipboardItems(ToolStripItemCollection items)
    {
        if (!_app.ReadOnly)
        {
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
        if (_details is not null)
        {
            _details.Visible = false;
        }
    }

    protected override void ShowAfterNotice()
    {
        _editor.Visible = true;
        _area.Visible = _area.Current is not null;
        if (_details is not null)
        {
            _details.Visible = true;
        }
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

    protected override IEnumerable<string> DiagnosticsLines => [$"File: {_path ?? "(none)"} ({_page?.Kind ?? "text"}, {_document.EncodingName}, {_document.LineEndingName})"];

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

    /// <summary>The file, for Recently closed (AX-9.4); an untitled text is left out.</summary>
    protected override string? PathInHand => _path;

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

        base.OnFormClosing(e);
    }

    // ---- keys (AD-5): every key comes from BundleKeys, EditorKeys or the app's table; the window's are its command
    // list (made with the menus above), and here are only the keys the edit control would act on first ----

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
        // AP-7.4: a text names its encoding and line ending; another kind's page its renderer's name.
        _positionLabel.Text = _page is { IsText: false } page
            ? $"Line {line}, Column {column} · {page.Kind}"
            : $"Line {line}, Column {column} · {_document.EncodingName} · {_document.LineEndingName}";
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
