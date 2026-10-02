using System.Diagnostics;
using System.Net.Http;
using System.Windows.Forms.Automation;
using AxDown.Core;
using Axit.Core;
using Axit.Core.Updates;
using Axit.Forms;

namespace AxDown;

/// <summary>
/// AxDown's window (SPEC.md §5): a menu bar, one plain multi-line edit control named after the file, and a status bar.
/// Notices (the bundle's <see cref="OverlayPanel"/>) take the editor's place for the window's own questions; the
/// question area (<see cref="ControlArea"/>) holds the Go to line field under the editor. The caret moves only on
/// the user's command, and nothing is spoken unless asked for or a command finishes (AD-2.6, AD-7).
/// </summary>
internal sealed class EditorForm : Form
{
    private const string AppName = "AxDown";
    private const string Untitled = "Untitled";
    private const string FileFilter = "Markdown and text files (*.md;*.markdown;*.txt)|*.md;*.markdown;*.txt|All files (*.*)|*.*";

    private readonly EditorSettings _settings;
    private readonly MenuStrip _menu = new();
    private readonly TextBox _editor = new();
    private readonly StatusStrip _status = new();
    private readonly ToolStripStatusLabel _fileLabel = new();
    private readonly ToolStripStatusLabel _positionLabel = new();
    private readonly OverlayPanel _overlay = new();
    private readonly ControlArea _area = new();
    private readonly ToolStripMenuItem _wordWrapItem;
    private readonly ToolStripMenuItem _installedItem = new($"&Installed: Axit {BundleInfo.Version}");
    private readonly ToolStripMenuItem _latestItem = new();
    private readonly ToolStripMenuItem _updateItem = new("Check for &updates...");
    private readonly System.Windows.Forms.Timer _statusTimer = new() { Interval = 300 };

    private TextDocument _document = TextDocument.Empty();
    private string? _path;
    private DateTime? _lastWriteTime;
    private bool _changed;
    private bool _loading;
    private bool _noticeOpen;
    private bool _closeConfirmed;
    private string _findText = string.Empty;
    private string _latestState = "not checked yet";
    private ReleaseInfo? _update;
    private bool _updateBusy;
    private string? _updateFolder;

    public EditorForm(string? path, EditorSettings settings, string? settingsError)
    {
        _settings = settings;
        MinimumSize = new Size(500, 300);
        Size = new Size(900, 600);
        StartPosition = FormStartPosition.WindowsDefaultLocation;

        // ---- menus (AD-4); the keys are handled in ProcessCmdKey from the tables, the items only show them ----
        var file = new ToolStripMenuItem("&File");
        file.DropDownItems.Add(Item("&New", NewDocument, AxDownKeys.NewDocument));
        file.DropDownItems.Add(Item("&Open...", OpenFile, AxDownKeys.Open));
        file.DropDownItems.Add(Item("&Save", () => Save(), BundleKeys.Save));
        file.DropDownItems.Add(Item("Save &as...", () => SaveAs(), AxDownKeys.SaveAs));
        file.DropDownItems.Add(new ToolStripSeparator());
        file.DropDownItems.Add(new ToolStripMenuItem("E&xit", null, (_, _) => Close()) { ShortcutKeyDisplayString = "Alt+F4" });

        var edit = new ToolStripMenuItem("&Edit");
        edit.DropDownItems.Add(new ToolStripMenuItem("&Undo", null, (_, _) => _editor.Undo()) { ShortcutKeyDisplayString = "Ctrl+Z" });
        edit.DropDownItems.Add(new ToolStripSeparator());
        edit.DropDownItems.Add(new ToolStripMenuItem("Cu&t", null, (_, _) => _editor.Cut()) { ShortcutKeyDisplayString = "Ctrl+X" });
        edit.DropDownItems.Add(new ToolStripMenuItem("&Copy", null, (_, _) => _editor.Copy()) { ShortcutKeyDisplayString = "Ctrl+C" });
        edit.DropDownItems.Add(new ToolStripMenuItem("&Paste", null, (_, _) => _editor.Paste()) { ShortcutKeyDisplayString = "Ctrl+V" });
        edit.DropDownItems.Add(new ToolStripMenuItem("Select &all", null, (_, _) => _editor.SelectAll()) { ShortcutKeyDisplayString = "Ctrl+A" });
        edit.DropDownItems.Add(new ToolStripSeparator());
        edit.DropDownItems.Add(Item("&Find...", ShowFind, BundleKeys.Find));
        edit.DropDownItems.Add(Item("Find &next", () => FindNext(1), BundleKeys.FindNext));
        edit.DropDownItems.Add(Item("Find pre&vious", () => FindNext(-1), BundleKeys.FindPrevious));
        edit.DropDownItems.Add(Item("&Go to line...", ShowGoToLine, AxDownKeys.GoToLine));

        var view = new ToolStripMenuItem("&View");
        _wordWrapItem = Item("&Word wrap", ToggleWordWrap, AxDownKeys.WordWrap);
        _wordWrapItem.Checked = _settings.WordWrap;
        view.DropDownItems.Add(_wordWrapItem);
        view.DropDownItems.Add(new ToolStripSeparator());
        view.DropDownItems.Add(Item("&Larger text", () => ChangeTextSize(1), BundleKeys.LargerText));
        view.DropDownItems.Add(Item("S&maller text", () => ChangeTextSize(-1), BundleKeys.SmallerText));
        view.DropDownItems.Add(new ToolStripMenuItem("Windows &text size", null, (_, _) => UseSystemFont()));
        view.DropDownItems.Add(new ToolStripMenuItem("&Font...", null, (_, _) => ChooseFont()));

        var help = new ToolStripMenuItem("&Help");
        help.DropDownItems.Add(Item("&Keyboard shortcuts", () => ShowNotice(Notice.Plain("Keyboard shortcuts", HelpText.Shortcuts)), BundleKeys.Shortcuts));
        help.DropDownItems.Add(new ToolStripMenuItem("&User guide", null, (_, _) => ShowNotice(Notice.Plain("User guide", HelpText.UserGuide()))));
        help.DropDownItems.Add(new ToolStripSeparator());
        // AX-3: the version in use, the latest release GitHub named, and the way to update, always in the menu.
        _installedItem.Click += (_, _) => ShowAbout();
        help.DropDownItems.Add(_installedItem);
        SetLatest(_latestState);
        _latestItem.Click += (_, _) => OpenUrl(UpdateCheck.ReleasesPage);
        help.DropDownItems.Add(_latestItem);
        _updateItem.Click += (_, _) => CheckForUpdates(manual: true);
        help.DropDownItems.Add(_updateItem);
        help.DropDownItems.Add(new ToolStripSeparator());
        help.DropDownItems.Add(new ToolStripMenuItem("Copy diag&nostics", null, (_, _) => CopyDiagnostics()));
        help.DropDownItems.Add(new ToolStripMenuItem("&About", null, (_, _) => ShowAbout()));

        _menu.Items.AddRange([file, edit, view, help]);
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

        // ---- the status bar (AD-9): no AccessibleName, no Spring, as in AxClaude (its SPEC §6.2) ----
        _fileLabel.TextAlign = ContentAlignment.MiddleLeft;
        _positionLabel.TextAlign = ContentAlignment.MiddleLeft;
        _status.Items.AddRange([_fileLabel, _positionLabel]);
        _status.SizeChanged += (_, _) => StatusLayout.Fit(_status, _fileLabel, _positionLabel);

        _overlay.Chosen += OnNoticeChoice;
        _area.TabIndex = 1;
        SizeChanged += (_, _) => _area.FitHeight();

        // Docked from the last control to the first: the menu takes the top, the status bar the bottom, the question
        // area the strip above it, and the editor (or the notice over it) what is left.
        Controls.AddRange([_overlay, _editor, _area, _status, _menu]);

        ApplyTextFont();
        RestoreWindow();
        Load(path);

        if (settingsError is not null)
        {
            ShowNotice(Notice.Plain("Settings", $"The settings file could not be read, so the defaults are used.\n{settingsError}") with { Unprompted = true });
        }
    }

    /// <summary>The crash handler's report (AD-11), shown inside the window like every other notice.</summary>
    public void ShowError(string title, string message) => ShowNotice(Notice.Plain(title, message) with { Unprompted = true });

    private string Name => _path is null ? Untitled : Path.GetFileName(_path);

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);
        if (!_noticeOpen)
        {
            _editor.Select();
        }

        if (_settings.CheckForUpdates)
        {
            CheckForUpdates(manual: false);
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
            });
            base.OnFormClosing(e);
            return;
        }

        SaveWindow();
        SaveSettings();
        if (_updateFolder is { } update)
        {
            // AX-3.2: the downloaded version's installer waits for this process to end and installs.
            Updater.LaunchInstaller(update, projectFolder: null, continueConversation: false);
        }

        base.OnFormClosing(e);
    }

    /// <summary>The window's client area as its UI Automation rectangle, so that NVDA+End finds the status bar.</summary>
    protected override AccessibleObject CreateAccessibilityInstance() => new WindowAccessibleObject(this);

    private const int WM_SYSCOMMAND = 0x0112;
    private const int SC_KEYMENU = 0xF100;

    protected override void WndProc(ref Message m)
    {
        // Alt alone and F10 open the menu bar through SC_KEYMENU; while a notice holds the keyboard they do nothing.
        if (m.Msg == WM_SYSCOMMAND && ((long)m.WParam & 0xFFF0) == SC_KEYMENU && ((long)m.LParam & 0xFFFF) == 0 && _noticeOpen)
        {
            return;
        }

        base.WndProc(ref m);
    }

    // ---- keys (AD-5): every key comes from BundleKeys or AxDownKeys, and is handled here only ----

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        if (_noticeOpen)
        {
            // A notice holds the keyboard: the window's shortcuts and the menu's wait. Only the text size keys stay live.
            switch (keyData)
            {
                case BundleKeys.LargerText:
                case BundleKeys.LargerTextNumpad:
                    ChangeTextSize(1);
                    return true;
                case BundleKeys.SmallerText:
                case BundleKeys.SmallerTextNumpad:
                    ChangeTextSize(-1);
                    return true;
            }

            return false;
        }

        switch (keyData)
        {
            case BundleKeys.Shortcuts:
                ShowNotice(Notice.Plain("Keyboard shortcuts", HelpText.Shortcuts));
                return true;
            case BundleKeys.NextControl:
            case BundleKeys.PreviousControl:
            case BundleKeys.NextControlAlso:
                // One main control: the editor, or the Go to line field while it shows.
                (_area.Current ?? _editor).Focus();
                return true;
            case BundleKeys.Find:
                ShowFind();
                return true;
            case BundleKeys.FindNext:
                FindNext(1);
                return true;
            case BundleKeys.FindPrevious:
                FindNext(-1);
                return true;
            case BundleKeys.Save:
                Save();
                return true;
            case BundleKeys.LargerText:
            case BundleKeys.LargerTextNumpad:
                ChangeTextSize(1);
                return true;
            case BundleKeys.SmallerText:
            case BundleKeys.SmallerTextNumpad:
                ChangeTextSize(-1);
                return true;
            case AxDownKeys.NewDocument:
                NewDocument();
                return true;
            case AxDownKeys.Open:
                OpenFile();
                return true;
            case AxDownKeys.SaveAs:
                SaveAs();
                return true;
            case AxDownKeys.GoToLine:
                ShowGoToLine();
                return true;
            case AxDownKeys.WordWrap:
                ToggleWordWrap();
                return true;
        }

        return base.ProcessCmdKey(ref msg, keyData);
    }

    private void OnEditorKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode is Keys.PageUp or Keys.PageDown && e.Modifiers == Keys.None)
        {
            // AD-2.3: the control's own page keys do nothing while the text fits.
            EditPaging.Page(_editor, e.KeyCode == Keys.PageDown ? 1 : -1);
            e.Handled = e.SuppressKeyPress = true;
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

    private void Load(string? path)
    {
        _path = path;
        _document = TextDocument.Empty();
        _lastWriteTime = null;
        if (path is not null && File.Exists(path))
        {
            try
            {
                _document = TextDocument.Read(path);
                _lastWriteTime = File.GetLastWriteTimeUtc(path);
            }
            catch (Exception ex) when (ex is InvalidOperationException or IOException or UnauthorizedAccessException)
            {
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

    private void NewDocument() => AskToSave(() => Load(null));

    private void OpenFile() => AskToSave(() =>
    {
        using var dialog = new OpenFileDialog { Filter = FileFilter, CheckFileExists = true };
        if (_path is not null)
        {
            dialog.InitialDirectory = Path.GetDirectoryName(_path);
        }

        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            Load(dialog.FileName);
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
                $"{Name} was changed by another program since you opened it. Write over it?",
                [
                    new OverlayChoice("&Write over", () => WriteFile(_path, then), IsDefault: true),
                    new OverlayChoice("&Cancel", IsCancel: true),
                ]));
            return;
        }

        WriteFile(_path, then);
    }

    private void SaveAs(Action? then = null)
    {
        using var dialog = new SaveFileDialog
        {
            Filter = FileFilter,
            FileName = Name,
            DefaultExt = _path is null ? "md" : Path.GetExtension(_path).TrimStart('.'),
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
        WriteFile(_path, then);
    }

    private void WriteFile(string path, Action? then)
    {
        try
        {
            _document.Write(path, _editor.Text);
            _lastWriteTime = File.GetLastWriteTimeUtc(path);
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

    /// <summary>AD-6.1: runs <paramref name="then"/> at once without changes, otherwise after Save or Don't save; Cancel does nothing.</summary>
    private void AskToSave(Action then)
    {
        if (!_changed)
        {
            then();
            return;
        }

        ShowNotice(new Notice($"Save changes to {Name}?", string.Empty,
        [
            new OverlayChoice("&Save", () => Save(then), IsDefault: true),
            new OverlayChoice("&Don't save", then),
            new OverlayChoice("&Cancel", IsCancel: true),
        ]));
    }

    // ---- find and go to line (AD-6.3, AD-6.4) ----

    private void ShowFind() =>
        ShowNotice(new Notice("Find", string.Empty,
        [
            new OverlayChoice("&Find next", () =>
            {
                _findText = _overlay.InputText;
                FindNext(1);
            }, IsDefault: true),
            new OverlayChoice("&Cancel", IsCancel: true),
        ])
        {
            Input = new OverlayInput("Text to find:", "Text to find", _findText, "Type the text to find"),
        });

    /// <summary>The next match after the selection (or the previous one before it), wrapping once; the match is selected and the editor takes the focus.</summary>
    private void FindNext(int direction)
    {
        if (_findText.Length == 0)
        {
            ShowFind();
            return;
        }

        var text = _editor.Text;
        int found;
        if (direction > 0)
        {
            var from = Math.Min(_editor.SelectionStart + _editor.SelectionLength, text.Length);
            found = text.IndexOf(_findText, from, StringComparison.OrdinalIgnoreCase);
            if (found < 0 && from > 0)
            {
                found = text.IndexOf(_findText, 0, StringComparison.OrdinalIgnoreCase);
            }
        }
        else
        {
            var from = _editor.SelectionStart - 1;
            found = from >= 0 ? text.LastIndexOf(_findText, from, StringComparison.OrdinalIgnoreCase) : -1;
            if (found < 0)
            {
                found = text.LastIndexOf(_findText, StringComparison.OrdinalIgnoreCase);
            }
        }

        if (found < 0)
        {
            Announce("Not found", true);
            return;
        }

        _editor.Select(found, _findText.Length);
        _editor.ScrollToCaret();
        _editor.Select();
        ScheduleStatus();
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
            },
            escape: () =>
            {
                _area.Clear();
                _editor.Select();
            });
    }

    private void GoToLine(int line, int lines)
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

        _editor.Select(index, 0);
        _editor.ScrollToCaret();
        UpdateStatus();
        Announce($"Line {line} of {lines}", true);
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
        Text = (_changed ? "*" : string.Empty) + Name + " - " + AppName;
        _editor.AccessibleName = Name;
    }

    private void ScheduleStatus()
    {
        _statusTimer.Stop();
        _statusTimer.Start();
    }

    private void UpdateStatus()
    {
        var (line, column, _) = Position();
        _fileLabel.Text = Name + (_changed ? ", unsaved changes" : string.Empty);
        _positionLabel.Text = $"Line {line}, Column {column} · {_document.EncodingName} · {_document.LineEndingName}";
        StatusLayout.Fit(_status, _fileLabel, _positionLabel);
    }

    // ---- word wrap, fonts and the window (AD-2.4, AD-2.7, AD-8) ----

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

    private static Font SystemTextFont() => SystemFonts.MessageBoxFont ?? DefaultFont;

    private Font TextFont()
    {
        var system = SystemTextFont();
        if (_settings.FontFamily is null && _settings.FontSize <= 0 && !_settings.FontBold)
        {
            return system;
        }

        var size = _settings.FontSize > 0 ? _settings.FontSize : system.SizeInPoints;
        var style = _settings.FontBold ? FontStyle.Bold : FontStyle.Regular;
        try
        {
            return new Font(_settings.FontFamily ?? system.FontFamily.Name, size, style);
        }
        catch (ArgumentException)
        {
            return new Font(system.FontFamily, size, style);
        }
    }

    private void ApplyTextFont()
    {
        var font = TextFont();
        _editor.Font = font;
        _overlay.SetTextFont(font);
        _area.Font = font;
        _area.MinimumHeight = TextRenderer.MeasureText("Wg", font).Height * 2 + 12;
        _area.FitHeight();
    }

    private void ChangeTextSize(int delta)
    {
        var size = Math.Clamp((int)Math.Round(TextFont().SizeInPoints) + delta, 6, 72);
        _settings.FontSize = size;
        ApplyTextFont();
        SaveSettings();
        Announce($"Text size {size}", true);
    }

    private void UseSystemFont()
    {
        _settings.FontFamily = null;
        _settings.FontSize = 0;
        _settings.FontBold = false;
        ApplyTextFont();
        SaveSettings();
        Announce("Windows text size", true);
    }

    private void ChooseFont()
    {
        using var dialog = new FontDialog
        {
            Font = TextFont(),
            ShowEffects = false,
            FontMustExist = true,
            MinSize = 6,
            MaxSize = 72,
        };
        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        _settings.FontFamily = dialog.Font.FontFamily.Name;
        _settings.FontSize = dialog.Font.SizeInPoints;
        _settings.FontBold = dialog.Font.Bold;
        ApplyTextFont();
        SaveSettings();
    }

    private void RestoreWindow()
    {
        if (_settings.Window is not { } placement)
        {
            return;
        }

        var bounds = new Rectangle(placement.X, placement.Y, placement.Width, placement.Height);
        if (!Screen.AllScreens.Any(s => s.WorkingArea.IntersectsWith(bounds)))
        {
            return;
        }

        StartPosition = FormStartPosition.Manual;
        Bounds = bounds;
        if (placement.Maximized)
        {
            WindowState = FormWindowState.Maximized;
        }
    }

    private void SaveWindow()
    {
        var bounds = WindowState == FormWindowState.Normal ? Bounds : RestoreBounds;
        _settings.Window = new WindowPlacement
        {
            X = bounds.X,
            Y = bounds.Y,
            Width = bounds.Width,
            Height = bounds.Height,
            Maximized = WindowState == FormWindowState.Maximized,
        };
    }

    private void SaveSettings()
    {
        try
        {
            _settings.Save(EditorSettings.DefaultPath);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            Log.Error("Settings could not be saved", ex);
        }
    }

    // ---- notices: the window's own dialogs, drawn inside the window (bundle AX-7.2, AX-6.4) ----

    /// <summary>
    /// The one place a notice is shown; callers pass only its content. It adds the key line made from the buttons,
    /// the chime and the hold of a notice the user did not open, the focus in the notice, the blocked menu and
    /// window shortcuts, and Enter and Escape on the default and cancel buttons.
    /// </summary>
    private void ShowNotice(Notice notice)
    {
        if (notice.Unprompted)
        {
            Sounds.Notice();
        }

        _noticeOpen = true;
        _overlay.Populate(notice with { Text = WithKeyLine(notice.Text, notice.Choices) });
        _overlay.Visible = true;
        _overlay.BringToFront();
        AcceptButton = _overlay.DefaultButton;
        CancelButton = _overlay.CancelButton;
        _overlay.FocusStart();
        _editor.Visible = false;
        _area.Visible = false;
        _menu.Enabled = false;
        PerformLayout();
    }

    /// <summary>"Enter: Save. Escape: Cancel." or "Enter or Escape: Close." as the last line of every notice with text.</summary>
    private static string WithKeyLine(string text, IReadOnlyList<OverlayChoice> choices)
    {
        var enter = choices.FirstOrDefault(choice => choice.IsDefault);
        var escape = choices.FirstOrDefault(choice => choice.IsCancel);
        static string Label(OverlayChoice choice) => choice.Text.Replace("&", string.Empty).TrimEnd('.');
        var keys = enter is not null && ReferenceEquals(enter, escape) ? $"Enter or Escape: {Label(enter)}."
            : string.Join(" ", new[] { enter is null ? null : $"Enter: {Label(enter)}.", escape is null ? null : $"Escape: {Label(escape)}." }.OfType<string>());
        return keys.Length == 0 ? text : (text.Length == 0 ? keys : text.TrimEnd('\n') + "\n" + keys);
    }

    private void OnNoticeChoice(OverlayChoice choice)
    {
        if (choice.StaysOpen)
        {
            choice.Action?.Invoke();
            return;
        }

        CloseNotice(choice.Action);
    }

    private void CloseNotice(Action? then)
    {
        if (!_noticeOpen)
        {
            return;
        }

        _noticeOpen = false;
        _editor.Visible = true;
        _area.Visible = _area.Current is not null;
        _menu.Enabled = true;
        AcceptButton = null;
        CancelButton = null;
        PerformLayout();
        then?.Invoke();
        if (IsDisposed)
        {
            return;
        }

        if (ActiveControl is null || _overlay.Contains(ActiveControl))
        {
            (_area.Current ?? _editor).Select();
        }

        _overlay.Visible = false;
    }

    private void Fail(string message)
    {
        Log.Error(message);
        ShowNotice(Notice.Plain("Error", message) with { Unprompted = true });
    }

    /// <summary>Speaks without moving the focus (AD-7): on the editor, or on the notice while one shows.</summary>
    private bool Announce(string text, bool interrupt)
    {
        if (_noticeOpen)
        {
            return _overlay.Announce(text, interrupt);
        }

        if (!_editor.IsHandleCreated)
        {
            return false;
        }

        var processing = interrupt ? AutomationNotificationProcessing.MostRecent : AutomationNotificationProcessing.All;
        return _editor.AccessibilityObject.RaiseAutomationNotification(AutomationNotificationKind.ActionCompleted, processing, text);
    }

    // ---- help, about, diagnostics, updates (AD-4.4, AX-3) ----

    private void ShowAbout() =>
        ShowNotice(Notice.Plain($"About {AppName}",
            $"{AppName}, part of Axit {BundleInfo.Version}\nA plain editor for Markdown and text files.\nMade by Dr. Kyle Keane, www.kylekeane.com. Free under the MIT licence.\n\n" +
            $"Latest release on GitHub: {_latestState}\nSettings: {EditorSettings.DefaultPath}\nLog: {Log.FilePath}\n\n" +
            HelpText.Disclaimer));

    private void CopyDiagnostics()
    {
        var text = string.Join("\r\n",
        [
            $"{AppName}, Axit {BundleInfo.Version} on .NET {Environment.Version}, {Environment.OSVersion}",
            $"File: {_path ?? "(none)"} ({_document.EncodingName}, {_document.LineEndingName})",
            $"Settings: {EditorSettings.DefaultPath}",
            $"Log: {Log.FilePath}",
            string.Empty,
            Log.Tail(200),
        ]);
        try
        {
            Clipboard.SetText(text);
            Announce("Copied diagnostics", true);
        }
        catch (Exception ex) when (ex is System.Runtime.InteropServices.ExternalException or InvalidOperationException)
        {
            Fail("The clipboard could not be written: " + ex.Message);
        }
    }

    private void OpenUrl(string url)
    {
        try
        {
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }
        catch (Exception ex) when (ex is System.ComponentModel.Win32Exception or IOException)
        {
            Fail("The page could not be opened: " + ex.Message);
        }
    }

    private void SetLatest(string state)
    {
        _latestState = state;
        _latestItem.Text = "&Latest release: " + state;
    }

    /// <summary>AX-3: asks GitHub for the latest release; a newer one is offered in a notice, a manual check reports either way.</summary>
    private async void CheckForUpdates(bool manual)
    {
        if (manual && _update is { } known)
        {
            ShowUpdateNotice(known);
            return;
        }

        if (_updateBusy)
        {
            return;
        }

        _updateBusy = true;
        _updateItem.Enabled = false;
        SetLatest("checking...");
        try
        {
            using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            var release = await Updater.CheckAsync(cancellation.Token);
            if (IsDisposed)
            {
                return;
            }

            if (release is null)
            {
                SetLatest("none published yet");
                if (manual)
                {
                    ShowNotice(Notice.Plain("Check for updates", "No release is published yet."));
                }
            }
            else if (UpdateCheck.IsNewer(release, BundleInfo.Version))
            {
                var version = release.Version.ToString(3);
                _update = release;
                SetLatest($"Axit {version} (newer than this one)");
                _updateItem.Text = $"&Update to Axit {version}...";
                Log.Info($"Update available: {release.Tag}, zip {release.ZipUrl ?? "missing"}");
                if (manual || !_noticeOpen)
                {
                    ShowUpdateNotice(release, unprompted: !manual);
                }
                else
                {
                    Announce($"Update {version} in the Help menu", false);
                }
            }
            else
            {
                SetLatest($"Axit {release.Version.ToString(3)}");
                if (manual)
                {
                    ShowNotice(Notice.Plain("Check for updates", $"You have the newest version, Axit {BundleInfo.Version}."));
                }
            }
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or IOException or InvalidOperationException)
        {
            Log.Error("Update check failed", ex);
            SetLatest("unknown, GitHub could not be reached");
            if (manual)
            {
                ShowNotice(new Notice("Check for updates", "GitHub could not be reached: " + ex.Message,
                [
                    new OverlayChoice("&Open releases page", () => OpenUrl(UpdateCheck.ReleasesPage), StaysOpen: true),
                    OverlayChoice.Close,
                ]));
            }
        }
        finally
        {
            _updateBusy = false;
            _updateItem.Enabled = true;
        }
    }

    private void ShowUpdateNotice(ReleaseInfo release, bool unprompted = false)
    {
        var version = release.Version.ToString(3);
        var size = release.ZipSize > 0 ? $" The download is {release.ZipSize / (1024.0 * 1024.0):0} MB." : string.Empty;
        var text =
            $"You have Axit {BundleInfo.Version}. Axit {version} is available.\n\n" +
            (release.Notes.Length > 0 ? release.Notes + "\n\n" : string.Empty) +
            $"Update now downloads the new version, closes {AppName} (asking first when there are unsaved changes) and installs it; " +
            $"start it again from the Start menu.{size}\n" +
            "Later keeps this version; the Help menu offers the update again.";
        ShowNotice(new Notice($"Update to Axit {version}", text,
        [
            new OverlayChoice("&Update now", () => InstallUpdate(release), IsDefault: true),
            new OverlayChoice("&Open release page", () => OpenUrl(release.PageUrl), StaysOpen: true),
            new OverlayChoice("&Later", IsCancel: true),
        ])
        {
            Unprompted = unprompted,
        });
    }

    private async void InstallUpdate(ReleaseInfo release)
    {
        if (_updateBusy)
        {
            return;
        }

        _updateBusy = true;
        _updateItem.Enabled = false;
        var version = release.Version.ToString(3);
        Announce($"Downloading Axit {version}", true);
        try
        {
            using var cancellation = new CancellationTokenSource(TimeSpan.FromMinutes(10));
            var folder = await Updater.DownloadAsync(release, cancellation.Token);
            if (IsDisposed)
            {
                return;
            }

            _updateFolder = folder;
            Log.Info($"Update {release.Tag} downloaded to {folder}");
            Announce("Downloaded. Closing to update", true);
            Close();
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or IOException or InvalidOperationException or UnauthorizedAccessException)
        {
            Log.Error("Update download failed", ex);
            Fail("The update could not be downloaded: " + ex.Message);
        }
        finally
        {
            _updateBusy = false;
            _updateItem.Enabled = true;
        }
    }

    // ---- menu items from the key tables ----

    private static ToolStripMenuItem Item(string text, Action action, Keys key)
    {
        var item = new ToolStripMenuItem(text, null, (_, _) => action());
        if (key != Keys.None)
        {
            item.ShortcutKeyDisplayString = KeyText(key);
        }

        return item;
    }

    /// <summary>"Ctrl+Shift+S", "F3", "Ctrl+Plus": the key as the menu shows it.</summary>
    private static string KeyText(Keys key)
    {
        var parts = new List<string>();
        if (key.HasFlag(Keys.Control)) parts.Add("Ctrl");
        if (key.HasFlag(Keys.Shift)) parts.Add("Shift");
        if (key.HasFlag(Keys.Alt)) parts.Add("Alt");
        var code = key & Keys.KeyCode;
        parts.Add(code switch
        {
            Keys.Oemplus or Keys.Add => "Plus",
            Keys.OemMinus or Keys.Subtract => "Minus",
            >= Keys.D0 and <= Keys.D9 => ((char)('0' + (code - Keys.D0))).ToString(),
            _ => code.ToString(),
        });
        return string.Join("+", parts);
    }
}
