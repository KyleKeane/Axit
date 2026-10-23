using System.Diagnostics;
using System.Net.Http;
using Axit.Core;
using Axit.Core.Updates;

namespace Axit.Forms;

/// <summary>
/// The window every app of the bundle is built on (docs/axit/SPEC.md AX-6.5): what the windows share and no app
/// copies any more. The notices (<see cref="ShowNotice"/>, <see cref="CloseNotice"/>, <see cref="Fail"/>,
/// <see cref="ShowError"/>; Alt, F10 and the window's shortcuts swallowed while one shows), <see cref="Announce"/>,
/// the text font with its size keys and picker, the window's place and size, the settings saving, the Axit menu
/// with the update check, the update notice and the install in place (AX-3), find in a text box, the diagnostics,
/// <see cref="OpenUrl"/>, and the common part of the key switch (the text size keys everywhere, F1). A window derives
/// from it, adds its controls (the <see cref="Overlay"/> first, so that it docks over the rest), and fills in the
/// hooks: what to hide for a notice, where the focus goes after one, what gets the font, how the new version starts
/// it again. It grew out of AxDown's window (the editor shell) on the third copy, AxGit (2026-10-18).
/// </summary>
public abstract class AxitForm : Form
{
    private readonly ToolStripMenuItem _checkItem = new($"Check for &updates (Axit {BundleInfo.Version})...");
    private readonly ToolStripMenuItem _updateItem = new() { Visible = false };
    private Control? _focusBeforeNotice;
    private string _findText = string.Empty;
    private string _latestState = "not checked yet";
    private ReleaseInfo? _update;
    private bool _updateBusy;
    private IReadOnlyList<PaletteEntry> _paletteAll = [];
    private IReadOnlyList<PaletteEntry> _paletteShown = [];

    /// <summary>The pause after typing in the palette before its first match is said (AX-10.4, AxStart's 300 ms).</summary>
    private readonly System.Windows.Forms.Timer _paletteSpeak = new() { Interval = 300 };

    /// <summary>After Update and restart: how the installed new version is started when this window closes; null when nothing is to start.</summary>
    private string[]? _restartArguments;

    protected AxitForm(string appName, WindowSettings settings, string settingsPath)
    {
        AppName = appName;
        Settings = settings;
        SettingsPath = settingsPath;
        // With the role Window, NVDA speaks the window when it opens ("notes.md - AxDown window") before the focused
        // control; without it the window is a nameless pane to NVDA and the app is never named (AX-7.1).
        AccessibleRole = AccessibleRole.Window;
        MinimumSize = new Size(500, 300);
        Size = new Size(900, 600);
        StartPosition = FormStartPosition.WindowsDefaultLocation;
        Overlay.Chosen += OnNoticeChoice;
        _paletteSpeak.Tick += (_, _) => SayFirstPaletteMatch();
        _checkItem.Click += (_, _) => CheckForUpdates(manual: true);
        _updateItem.Click += (_, _) =>
        {
            if (_update is { } known)
            {
                ShowUpdateNotice(known);
            }
        };
    }

    /// <summary>The app's name, which ends the title of its windows and names them in the Axit menu.</summary>
    protected string AppName { get; }

    /// <summary>The app's settings as the base needs them: the font, the update check and the window's place.</summary>
    protected WindowSettings Settings { get; }

    protected string SettingsPath { get; }

    /// <summary>The notices panel (AX-7.2); the window adds it to its controls first, so that it docks over the rest.</summary>
    protected OverlayPanel Overlay { get; } = new();

    /// <summary>A notice holds the window: the main controls are hidden, the menu and the shortcuts wait.</summary>
    protected bool NoticeOpen { get; private set; }

    /// <summary>Update and restart was chosen and the update is installed: the new version starts when the window closes.</summary>
    protected bool RestartPending => _restartArguments is not null;

    // ---- what the window fills in ----

    /// <summary>The F1 text.</summary>
    protected abstract string Shortcuts { get; }

    /// <summary>Hides the main controls while a notice shows; <see cref="ShowAfterNotice"/> brings them back.</summary>
    protected abstract void HideForNotice();

    protected abstract void ShowAfterNotice();

    /// <summary>The control that takes the focus after a notice closes when nothing else kept it, and that announcements are raised on.</summary>
    protected abstract Control FocusTarget { get; }

    /// <summary>The control <see cref="Announce"/> raises on; the focus target unless the window says otherwise.</summary>
    protected virtual Control AnnounceSource => FocusTarget;

    /// <summary>The control that had the focus as a notice opened and gets it back when it closes; null to use <see cref="FocusTarget"/>.</summary>
    protected virtual Control? FocusToKeep() => null;

    /// <summary>Whether an unprompted notice plays the chime; a window with a sound setting says.</summary>
    protected virtual bool NoticeChime => true;

    /// <summary>A notice opened (or another took the first one's place) with the focus kept at <paramref name="focusBefore"/>.</summary>
    protected virtual void OnNoticeOpened(Control? focusBefore)
    {
    }

    /// <summary>The last notice closed, before the focus goes back.</summary>
    protected virtual void OnNoticeClosed()
    {
    }

    /// <summary>Gives the window's controls the text font; the notice gets it from the base.</summary>
    protected abstract void ApplyFont(Font font);

    /// <summary>How the installed new version starts this window again after Update and restart: the verb and what it holds (AX-3.2).</summary>
    protected abstract IReadOnlyList<string> RestartArguments();

    /// <summary>What Update and restart does to this window, in the update notice: "closes AxDown (asking first…), installs it and starts it again on the same file".</summary>
    protected virtual string UpdateRestartText => $"closes {AppName}, installs it and starts it again";

    /// <summary>A step of the update worth a line of the window's own (AxClaude writes it into the conversation); nothing by default.</summary>
    protected virtual void UpdateNoted(string message)
    {
    }

    /// <summary>The window's own lines of Copy diagnostics, after the version line.</summary>
    protected virtual IEnumerable<string> DiagnosticsLines => [];

    /// <summary>The window's own entries of the command palette, after the menus' commands (AX-10.5): AxFile's folder entries; none by default.</summary>
    protected virtual IEnumerable<PaletteEntry> PaletteEntries() => [];

    /// <summary>The window's own keys, from its table; true when the key was handled. Called when no notice shows and after the common keys.</summary>
    protected virtual bool ProcessWindowKey(Keys keyData) => false;

    // ---- the menu and the window ----

    /// <summary>The bundle's Axit menu (AX-9) with this window's update items; the window puts it before Help.</summary>
    protected AxitMenu CreateAxitMenu() => new(this, _checkItem, _updateItem, () => _latestState, ShowNotice);

    /// <summary>A menu item whose key the window handles itself: the item only shows the key (AX-8).</summary>
    protected static ToolStripMenuItem Item(string text, Action action, Keys key)
    {
        var item = new ToolStripMenuItem(text, null, (_, _) => action());
        if (key != Keys.None)
        {
            item.ShortcutKeyDisplayString = BundleKeys.Text(key);
        }

        return item;
    }

    /// <summary>A checked menu item for a setting: flipping it applies the value and saves the settings.</summary>
    protected ToolStripMenuItem Toggle(string text, bool initial, Action<bool> apply)
    {
        var item = new ToolStripMenuItem(text) { CheckOnClick = true, Checked = initial };
        item.CheckedChanged += (_, _) =>
        {
            apply(item.Checked);
            SaveSettings();
        };

        return item;
    }

    /// <summary>Options → Text (AX-6.5): the window's own items first, then what every window has: Font…, Larger text, Smaller text, Windows text size.</summary>
    protected ToolStripMenuItem CreateTextMenu(params ToolStripItem[] own)
    {
        var menu = new ToolStripMenuItem("&Text");
        menu.DropDownItems.AddRange(own);
        if (own.Length > 0)
        {
            menu.DropDownItems.Add(new ToolStripSeparator());
        }

        menu.DropDownItems.Add(new ToolStripMenuItem("&Font...", null, (_, _) => ChooseFont()));
        menu.DropDownItems.Add(Item("&Larger text", () => ChangeTextSize(1), BundleKeys.LargerText));
        menu.DropDownItems.Add(Item("S&maller text", () => ChangeTextSize(-1), BundleKeys.SmallerText));
        menu.DropDownItems.Add(new ToolStripMenuItem("Windows &text size", null, (_, _) => UseSystemFont()));
        return menu;
    }

    /// <summary>Options → General: Check for updates at start (AX-3.1), then the window's own items.</summary>
    protected ToolStripMenuItem CreateGeneralMenu(params ToolStripItem[] own)
    {
        var menu = new ToolStripMenuItem("&General");
        menu.DropDownItems.Add(Toggle("Check for &updates at start", Settings.CheckForUpdates, v => Settings.CheckForUpdates = v));
        menu.DropDownItems.AddRange(own);
        return menu;
    }

    /// <summary>Options → Developer: Open settings file, the window's own items, Copy diagnostics.</summary>
    protected ToolStripMenuItem CreateDeveloperMenu(params ToolStripItem[] own)
    {
        var menu = new ToolStripMenuItem("De&veloper");
        menu.DropDownItems.Add(new ToolStripMenuItem("Open settings &file", null, (_, _) => OpenSettingsFile()));
        menu.DropDownItems.AddRange(own);
        menu.DropDownItems.Add(new ToolStripMenuItem("Copy diag&nostics", null, (_, _) => CopyDiagnostics()));
        return menu;
    }

    /// <summary>Help: Keyboard shortcuts (F1, the base's key), User guide, then a submenu of pages opened in the browser. Updates and About are the Axit menu's.</summary>
    protected ToolStripMenuItem CreateHelpMenu(Func<string> userGuide, string resourcesLabel, IEnumerable<(string Label, string Url)> pages)
    {
        var help = new ToolStripMenuItem("&Help");
        help.DropDownItems.Add(Item("Command &palette...", ShowCommandPalette, BundleKeys.CommandPalette));
        help.DropDownItems.Add(Item("&Keyboard shortcuts", () => ShowNotice(Notice.Plain("Keyboard shortcuts", Shortcuts)), BundleKeys.Shortcuts));
        help.DropDownItems.Add(new ToolStripMenuItem("User &guide", null, (_, _) => ShowNotice(Notice.Plain("User guide", userGuide()))));
        help.DropDownItems.Add(new ToolStripSeparator());
        var resources = new ToolStripMenuItem(resourcesLabel);
        foreach (var (label, url) in pages)
        {
            resources.DropDownItems.Add(new ToolStripMenuItem(label, null, (_, _) => OpenUrl(url)));
        }

        help.DropDownItems.Add(resources);
        return help;
    }

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);
        if (Settings.CheckForUpdates && !BundleInfo.IsDevelopmentBuild)
        {
            CheckForUpdates(manual: false);
        }
    }

    /// <summary>The place and the settings are saved, and the installed new version started, unless the window cancels its closing before calling here.</summary>
    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (!e.Cancel)
        {
            SaveWindow();
            SaveSettings();
            if (_restartArguments is { } restart)
            {
                // AX-3.2: the update is installed; the new version starts on what this window held as this process ends.
                Updater.StartInstalled(restart);
            }
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
        // Alt+Space (the system menu, lParam is the space character) still works.
        if (m.Msg == WM_SYSCOMMAND && ((long)m.WParam & 0xFFF0) == SC_KEYMENU && ((long)m.LParam & 0xFFFF) == 0 && NoticeOpen)
        {
            return;
        }

        base.WndProc(ref m);
    }

    /// <summary>
    /// The common keys (AX-8.2): the text size keys work everywhere, a notice included; while a notice shows every
    /// other shortcut waits (not calling the base skips the menu shortcuts; Enter and Escape reach the notice's
    /// buttons through AcceptButton and CancelButton); F1 shows the shortcuts; then the window's own keys.
    /// </summary>
    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
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

        if (NoticeOpen)
        {
            return false;
        }

        if (keyData == BundleKeys.Shortcuts)
        {
            ShowNotice(Notice.Plain("Keyboard shortcuts", Shortcuts));
            return true;
        }

        // In menu mode (Alt, a menu's name selected on the bar) WinForms routes the keys to the menu bar, whose
        // ProcessCmdKey hands them up to the window: they are the menu's (AX-6.6), or Right in AxFile's list opened
        // the entry instead of moving to the next menu (2026-10-22).
        if (FromHandle(msg.HWnd) is ToolStrip)
        {
            return base.ProcessCmdKey(ref msg, keyData);
        }

        if (keyData == BundleKeys.CommandPalette)
        {
            ShowCommandPalette();
            return true;
        }

        return ProcessWindowKey(keyData) || base.ProcessCmdKey(ref msg, keyData);
    }

    // ---- notices: the window's own dialogs, drawn inside the window (AX-7.2, AX-6.4) ----

    /// <summary>The crash handler's report, shown inside the window like every other notice.</summary>
    public void ShowError(string title, string message) => ShowNotice(Notice.Plain(title, message) with { Unprompted = true });

    /// <summary>
    /// The one place a notice is shown; callers pass only its content. It adds the chime of a notice the user did
    /// not open, the focus in the notice, the hidden main controls, the blocked menu and window shortcuts, and
    /// Enter and Escape on the default and cancel buttons; the panel's Populate adds the key line made from the
    /// buttons and the hold of an unprompted notice. A notice shown while another is open replaces it.
    /// </summary>
    protected void ShowNotice(Notice notice)
    {
        _paletteSpeak.Stop();
        if (notice.Unprompted && NoticeChime)
        {
            Sounds.Notice();
        }

        if (!NoticeOpen)
        {
            _focusBeforeNotice = FocusToKeep();
            OnNoticeOpened(_focusBeforeNotice);
        }

        NoticeOpen = true;
        Overlay.Populate(notice);
        Overlay.Visible = true;
        Overlay.BringToFront();
        AcceptButton = Overlay.DefaultButton;
        CancelButton = Overlay.CancelButton;
        Overlay.FocusStart();
        HideForNotice();
        if (MainMenuStrip is { } menu)
        {
            menu.Enabled = false;
        }

        PerformLayout();
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

    /// <summary>Closes the notice, runs <paramref name="then"/>, and gives the focus back unless the action opened a notice of its own or closed the window.</summary>
    protected void CloseNotice(Action? then)
    {
        if (!NoticeOpen)
        {
            return;
        }

        _paletteSpeak.Stop();
        var focusBefore = _focusBeforeNotice;
        NoticeOpen = false;
        ShowAfterNotice();
        if (MainMenuStrip is { } menu)
        {
            menu.Enabled = true;
        }

        AcceptButton = null;
        CancelButton = null;
        PerformLayout();
        then?.Invoke();
        if (IsDisposed)
        {
            return;
        }

        if (NoticeOpen)
        {
            // The action opened a notice of its own (the update's progress, an error): that one is showing now, and
            // the place from before the first notice carries over to it.
            _focusBeforeNotice = focusBefore;
            OnNoticeOpened(focusBefore);
            return;
        }

        OnNoticeClosed();
        if (ActiveControl is null || Overlay.Contains(ActiveControl))
        {
            (focusBefore ?? FocusTarget).Select();
        }

        Overlay.Visible = false;
    }

    /// <summary>An error the user must see: logged and shown as a notice.</summary>
    protected virtual void Fail(string message)
    {
        Log.Error(message);
        ShowError("Error", message);
    }

    /// <summary>Speaks without moving the focus (AX-7.3): on the window's control, or on the notice while one shows.</summary>
    protected virtual bool Announce(string text, bool interrupt)
    {
        if (NoticeOpen)
        {
            return Overlay.Announce(text, interrupt);
        }

        return Announcer.Raise(AnnounceSource, text, interrupt);
    }

    // ---- find in a text box ----

    /// <summary>Ctrl+F: the Find notice; Find next closes it and searches <paramref name="box"/> from the selection; an empty field keeps it open.</summary>
    protected void ShowFind(TextBox box) =>
        ShowNotice(new Notice("Find", string.Empty,
        [
            new OverlayChoice("&Find next", () =>
            {
                _findText = Overlay.InputText;
                FindNext(box, 1);
            }, IsDefault: true),
            new OverlayChoice("&Cancel", IsCancel: true),
        ])
        {
            Input = new OverlayInput("Text to find:", "Text to find", _findText, "Type the text to find"),
        });

    /// <summary>
    /// The next match after the selection (or the previous one before it), wrapping once; the match is selected and
    /// the box takes the focus. False, with "Not found" said, when there is none; the Find notice when nothing was
    /// searched for yet.
    /// </summary>
    protected bool FindNext(TextBox box, int direction)
    {
        if (_findText.Length == 0)
        {
            ShowFind(box);
            return false;
        }

        var text = box.Text;
        int found;
        if (direction > 0)
        {
            var from = Math.Min(box.SelectionStart + box.SelectionLength, text.Length);
            found = text.IndexOf(_findText, from, StringComparison.OrdinalIgnoreCase);
            if (found < 0 && from > 0)
            {
                found = text.IndexOf(_findText, 0, StringComparison.OrdinalIgnoreCase);
            }
        }
        else
        {
            var from = box.SelectionStart - 1;
            found = from >= 0 ? text.LastIndexOf(_findText, from, StringComparison.OrdinalIgnoreCase) : -1;
            if (found < 0)
            {
                found = text.LastIndexOf(_findText, StringComparison.OrdinalIgnoreCase);
            }
        }

        if (found < 0)
        {
            Announce("Not found", true);
            return false;
        }

        box.Select(found, _findText.Length);
        box.ScrollToCaret();
        box.Select();
        return true;
    }

    // ---- find in a list (AxFile AF-3.6, AxMedia AM-3.1; shared on the second use, 2026-10-22) ----

    /// <summary>Ctrl+F on a list: the Find notice with the field "Name contains"; Find next closes it and selects the next item of <paramref name="list"/> whose name (from <paramref name="names"/>, one per item) contains the text, wrapping once.</summary>
    protected void ShowFindInList(ListBox list, Func<IReadOnlyList<string>> names) =>
        ShowNotice(new Notice("Find", string.Empty,
        [
            new OverlayChoice("&Find next", () =>
            {
                _findText = Overlay.InputText;
                FindInList(list, names, 1);
            }, IsDefault: true),
            new OverlayChoice("&Cancel", IsCancel: true),
        ])
        {
            Input = new OverlayInput("Name contains:", "Name contains", _findText, "Type the text to find"),
        });

    /// <summary>F3 and Shift+F3 on a list: the next (or previous) item whose name contains the text, wrapping once, selected; "Not found" otherwise; the Find notice when nothing was searched for yet. The selection is NVDA's own reading.</summary>
    protected void FindInList(ListBox list, Func<IReadOnlyList<string>> names, int direction)
    {
        if (_findText.Length == 0)
        {
            ShowFindInList(list, names);
            return;
        }

        var items = names();
        var count = Math.Min(items.Count, list.Items.Count);
        var from = Math.Max(0, list.SelectedIndex);
        for (var step = 1; step <= count; step++)
        {
            var i = ((from + direction * step) % count + count) % count;
            if (items[i].Contains(_findText, StringComparison.OrdinalIgnoreCase))
            {
                list.SelectedIndex = i;
                return;
            }
        }

        Announce("Not found", true);
    }

    // ---- fonts and the window's place (AX-7.6) ----

    private static Font SystemTextFont() => SystemFonts.MessageBoxFont ?? DefaultFont;

    /// <summary>The text font from the settings: the Windows font unless a family, a size or bold is set.</summary>
    // ---- the command palette (AX-10): the window's menu commands by name, in a notice ----

    /// <summary>
    /// The palette as a notice: one field, the matches under it, Enter runs the selected one with the focus back
    /// where it was, Escape closes (AX-10.1, AX-10.4). The entries are read from the menu bar now, before the notice
    /// disables it, so that what the palette offers is what the menus offer at this moment.
    /// </summary>
    protected void ShowCommandPalette()
    {
        if (NoticeOpen)
        {
            return;
        }

        var before = FocusedControl();
        _paletteAll = [.. CommandPalette.Commands(MainMenuStrip, text => Announce(text, true)), .. PaletteEntries()];
        _paletteShown = _paletteAll;
        ShowNotice(new Notice("Command palette", string.Empty,
        [
            new OverlayChoice("&Run", RunPaletteEntry, IsDefault: true, StaysOpen: true),
            new OverlayChoice("&Close", IsCancel: true),
        ])
        {
            Input = new OverlayInput("&Search commands:", "Search commands", string.Empty, string.Empty, Required: false,
                KeyHook: OnPaletteKey, Changed: OnPaletteTyped, ListName: "Commands"),
        });
        // The control the user was in gets the focus back, unless the window keeps a place of its own.
        _focusBeforeNotice ??= before;
        FillPalette();
    }

    /// <summary>The innermost control with the focus, outside the notice.</summary>
    private Control? FocusedControl()
    {
        var control = ActiveControl;
        while (control is ContainerControl { ActiveControl: { } inner })
        {
            control = inner;
        }

        return control is null || Overlay.Contains(control) ? null : control;
    }

    private void OnPaletteTyped()
    {
        _paletteSpeak.Stop();
        FillPalette();
        if (Overlay.InputText.Trim().Length > 0)
        {
            _paletteSpeak.Start();
        }
    }

    private void FillPalette()
    {
        _paletteShown = Palette.Find(_paletteAll, Overlay.InputText);
        var lines = _paletteShown.Count > 0 ? _paletteShown.Select(entry => entry.Text).ToList() : [Palette.NoMatch(Overlay.InputText)];
        Overlay.SetMatches(lines, $"Commands, {_paletteShown.Count}");
    }

    /// <summary>The first match and the count after the typing paused, or that nothing matches (AX-10.4).</summary>
    private void SayFirstPaletteMatch()
    {
        _paletteSpeak.Stop();
        if (!NoticeOpen || Overlay.InputText.Trim().Length == 0)
        {
            return;
        }

        Announce(_paletteShown.Count > 0 ? Palette.Spoken(_paletteShown[0], 0, _paletteShown.Count) : Palette.NoMatch(Overlay.InputText), true);
    }

    /// <summary>Down, Up, Page Down, Page Up, Ctrl+Home and Ctrl+End in the field move through the matches (AX-10.4).</summary>
    private bool OnPaletteKey(Keys key)
    {
        var index = Overlay.MatchIndex;
        int? target = key switch
        {
            CommandPalette.NextMatch => index + 1,
            CommandPalette.PreviousMatch => index - 1,
            BundleKeys.PageDown => index + Overlay.MatchPage,
            BundleKeys.PageUp => index - Overlay.MatchPage,
            CommandPalette.FirstMatch => 0,
            CommandPalette.LastMatch => _paletteShown.Count - 1,
            _ => null,
        };
        if (target is not { } wanted)
        {
            return false;
        }

        _paletteSpeak.Stop();
        wanted = Math.Clamp(wanted, 0, Math.Max(0, _paletteShown.Count - 1));
        if (_paletteShown.Count == 0 || wanted == index)
        {
            Sounds.Edge();
            return true;
        }

        wanted = Math.Clamp(wanted, 0, _paletteShown.Count - 1);
        if (wanted == index)
        {
            // At an end nothing moves: the edge earcon, and then the match again, so that the last thing heard is
            // the position and not the typed text (AX-10.4).
            Sounds.Edge();
        }
        else
        {
            Overlay.SelectMatch(wanted);
        }

        Announce(Palette.Spoken(_paletteShown[wanted], wanted, _paletteShown.Count), true);
        return true;
    }

    /// <summary>Enter: the palette closes, the focus goes back, and then the entry runs where the user was (AX-10.4).</summary>
    private void RunPaletteEntry()
    {
        _paletteSpeak.Stop();
        var index = Overlay.MatchIndex;
        if (index < 0 || index >= _paletteShown.Count)
        {
            Announce(Palette.NoMatch(Overlay.InputText), true);
            return;
        }

        var entry = _paletteShown[index];
        CloseNotice(null);
        BeginInvoke(entry.Run);
    }

    protected Font TextFont()
    {
        var system = SystemTextFont();
        if (Settings.FontFamily is null && Settings.FontSize <= 0 && !Settings.FontBold)
        {
            return system;
        }

        var size = Settings.FontSize > 0 ? Settings.FontSize : system.SizeInPoints;
        var style = Settings.FontBold ? FontStyle.Bold : FontStyle.Regular;
        try
        {
            return new Font(Settings.FontFamily ?? system.FontFamily.Name, size, style);
        }
        catch (ArgumentException)
        {
            return new Font(system.FontFamily, size, style);
        }
    }

    /// <summary>The settings' font onto the window's controls and the notice.</summary>
    protected void ApplyTextFont()
    {
        var font = TextFont();
        ApplyFont(font);
        Overlay.SetTextFont(font);
    }

    protected void ChangeTextSize(int delta)
    {
        var size = Math.Clamp((int)Math.Round(TextFont().SizeInPoints) + delta, 6, 72);
        Settings.FontSize = size;
        ApplyTextFont();
        SaveSettings();
        Announce($"Text size {size}", true);
    }

    protected void UseSystemFont()
    {
        Settings.FontFamily = null;
        Settings.FontSize = 0;
        Settings.FontBold = false;
        ApplyTextFont();
        SaveSettings();
        Announce("Windows text size", true);
    }

    /// <summary>The Windows font picker, one of the system's own windows; the choice is said.</summary>
    protected void ChooseFont()
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

        Settings.FontFamily = dialog.Font.FontFamily.Name;
        Settings.FontSize = dialog.Font.SizeInPoints;
        Settings.FontBold = dialog.Font.Bold;
        ApplyTextFont();
        SaveSettings();
        Announce($"Font {dialog.Font.FontFamily.Name} {Math.Round(dialog.Font.SizeInPoints)} point", true);
    }

    /// <summary>The window where it was last, when that place is still on a screen; the window calls it from its constructor.</summary>
    protected void RestoreWindow()
    {
        if (Settings.Window is not { } placement)
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
        Settings.Window = new WindowPlacement
        {
            X = bounds.X,
            Y = bounds.Y,
            Width = bounds.Width,
            Height = bounds.Height,
            Maximized = WindowState == FormWindowState.Maximized,
        };
    }

    /// <summary>The settings into their file; a file that cannot be written is logged, not reported.</summary>
    protected void SaveSettings()
    {
        try
        {
            Settings.Save(SettingsPath);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            Log.Error("Settings could not be saved", ex);
        }
    }

    // ---- help, diagnostics, updates (AX-3) ----

    /// <summary>Options → Developer → Copy diagnostics: the versions, the window's own lines and the end of the log, to the clipboard.</summary>
    protected virtual void CopyDiagnostics()
    {
        var text = string.Join("\r\n",
        [
            $"{AppName}, Axit {BundleInfo.Version} on .NET {Environment.Version}, {Environment.OSVersion}",
            .. DiagnosticsLines,
            $"Settings: {SettingsPath}",
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

    /// <summary>Options → Developer → Open settings file: the app's settings, written first, in a new AxJSON window, as any JSON file.</summary>
    protected void OpenSettingsFile()
    {
        SaveSettings();
        AxitMenu.Start(Dispatch.AxJSON, SettingsPath);
    }

    /// <summary>A page in the default browser, one of the system's own windows; a failure is a notice.</summary>
    protected void OpenUrl(string url)
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

    /// <summary>The path in File Explorer, one of the system's own windows (AX-7.2): a folder opened, a file selected in its folder.</summary>
    protected void OpenInExplorer(string path)
    {
        var arguments = Directory.Exists(path) ? $"\"{path}\"" : $"/select,\"{path}\"";
        try
        {
            Process.Start(new ProcessStartInfo("explorer.exe", arguments) { UseShellExecute = false });
        }
        catch (Exception ex) when (ex is System.ComponentModel.Win32Exception or InvalidOperationException)
        {
            Fail("File Explorer could not be started: " + ex.Message);
        }
    }

    /// <summary>
    /// AX-3: asks GitHub for the latest release, every time. A newer one is offered in a notice (unprompted at
    /// startup) and noted in the Axit menu; a manual check (Axit → Check for updates) reports the result either
    /// way, including a failure.
    /// </summary>
    protected async void CheckForUpdates(bool manual)
    {
        if (_updateBusy)
        {
            return;
        }

        SetUpdateBusy(true);
        _latestState = "checking...";
        try
        {
            using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            var release = await Updater.CheckAsync(cancellation.Token);
            if (IsDisposed)
            {
                return;
            }

            // A check answers for itself: an update known from an earlier one is replaced, or gone.
            _update = null;
            _updateItem.Visible = false;
            if (release is null)
            {
                _latestState = "none published yet";
            }
            else if (UpdateCheck.IsNewer(release, BundleInfo.Version))
            {
                var version = release.Version.ToString(3);
                _update = release;
                _latestState = $"Axit {version} (newer than this one)";
                _updateItem.Text = $"Update &to Axit {version}...";
                _updateItem.Visible = true;
                Log.Info($"Update available: {release.Tag}, zip {release.ZipUrl ?? "missing"}");
                if (!manual)
                {
                    UpdateNoted($"Axit {version} is available. Axit menu, Update to Axit {version}.");
                }

                if (manual || !NoticeOpen)
                {
                    // At startup the notice opens by itself: Enter updates, Escape keeps this version.
                    ShowUpdateNotice(release, unprompted: !manual);
                }
                else
                {
                    Announce($"Update {version} in the Axit menu", false);
                }

                return;
            }
            else
            {
                var same = release.Version == UpdateCheck.ParseVersion(BundleInfo.Version);
                _latestState = $"Axit {release.Version.ToString(3)} ({(same ? "this version" : "older than this build")})";
            }

            if (manual)
            {
                ShowNotice(Notice.Plain("Check for updates",
                    $"You have the newest version, Axit {BundleInfo.Version}.\n" +
                    (release is null ? "No release is published yet.\n" : string.Empty) +
                    $"Releases: {UpdateCheck.ReleasesPage}"));
            }
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or IOException or InvalidOperationException)
        {
            Log.Error("The update check failed", ex);
            _latestState = "unknown, GitHub could not be reached";
            if (manual && !IsDisposed)
            {
                ShowNotice(new Notice("Check for updates",
                    $"GitHub could not be reached: {ex.Message}\nThe releases page: {UpdateCheck.ReleasesPage}",
                [
                    new OverlayChoice("&Open releases page", () => OpenUrl(UpdateCheck.ReleasesPage), StaysOpen: true),
                    OverlayChoice.Close,
                ]));
            }
        }
        finally
        {
            SetUpdateBusy(false);
        }
    }

    /// <summary>While a check or an install runs, the two update items of the Axit menu are disabled, so that neither starts twice.</summary>
    private void SetUpdateBusy(bool busy)
    {
        _updateBusy = busy;
        if (!IsDisposed)
        {
            _checkItem.Enabled = !busy;
            _updateItem.Enabled = !busy;
        }
    }

    /// <summary>The button that opens a release on GitHub and leaves the notice open.</summary>
    private OverlayChoice ReleasePageChoice(ReleaseInfo release) => new("&Open release page", () => OpenUrl(release.PageUrl), StaysOpen: true);

    /// <summary>The release notes with Update and restart, Update and close, Open release page and Later; <paramref name="unprompted"/> when the check at startup found it.</summary>
    private void ShowUpdateNotice(ReleaseInfo release, bool unprompted = false)
    {
        var version = release.Version.ToString(3);
        var size = release.ZipSize > 0 ? $" The download is {release.ZipSize / (1024.0 * 1024.0):0} MB." : string.Empty;
        var text =
            $"You have Axit {BundleInfo.Version}. Axit {version} is available.\n\n" +
            (release.Notes.Length > 0 ? release.Notes + "\n\n" : string.Empty) +
            $"Update and restart downloads the new version, {UpdateRestartText}. Update and close installs without starting it again. " +
            $"A progress bar shows the download.{size}\n" +
            "Later keeps this version; the Axit menu offers the update again.";
        ShowNotice(new Notice($"Update to Axit {version}", text,
        [
            new OverlayChoice("Update and &restart", () => InstallUpdate(release, restart: true), IsDefault: true),
            new OverlayChoice("Update and &close", () => InstallUpdate(release, restart: false)),
            ReleasePageChoice(release),
            new OverlayChoice("&Later", IsCancel: true),
        ])
        {
            Unprompted = unprompted,
        });
    }

    /// <summary>
    /// Update and restart, or Update and close: downloads the release and installs it in place behind one notice with
    /// one progress bar (the download up to 80 percent, the installer's steps to 100; Cancel stops the download, not
    /// the install), then closes the window; on the way out, OnFormClosing starts the installed new version when
    /// <paramref name="restart"/> was chosen (AX-3.2).
    /// </summary>
    private async void InstallUpdate(ReleaseInfo release, bool restart)
    {
        if (_updateBusy)
        {
            return;
        }

        SetUpdateBusy(true);
        var version = release.Version.ToString(3);
        var size = release.ZipSize > 0 ? $" ({release.ZipSize / (1024.0 * 1024.0):0} MB)" : string.Empty;
        UpdateNoted($"Updating to Axit {version}...");
        using var cancellation = new CancellationTokenSource(TimeSpan.FromMinutes(10));
        var cancelled = false;
        var installing = false;
        ShowNotice(new Notice($"Updating to Axit {version}",
            $"The new version is downloaded{size} and installed; the progress bar shows how far it is. Cancel during the download keeps this version.",
        [
            new OverlayChoice("&Cancel", () =>
            {
                if (installing)
                {
                    Announce("Installing, this cannot be cancelled now", true);
                    return;
                }

                cancelled = true;
                cancellation.Cancel();
                CloseNotice(null);
            }, IsDefault: true, IsCancel: true, StaysOpen: true),
        ])
        {
            Progress = true,
        });
        try
        {
            var folder = await Updater.DownloadAsync(release, cancellation.Token, new Progress<int>(percent => Overlay.SetProgress(percent * 80 / 100)));
            // Cancel pressed while the zip was being unpacked, which the download does not watch the token for.
            cancellation.Token.ThrowIfCancellationRequested();
            if (IsDisposed)
            {
                return;
            }

            installing = true;
            Overlay.SetProgress(80);
            Announce("Downloaded. Installing", true);
            var step = 80;
            var result = await Updater.InstallInPlaceAsync(folder, new Progress<string>(_ => Overlay.SetProgress(step = Math.Min(99, step + 3))));
            if (IsDisposed)
            {
                return;
            }

            if (result.ExitCode != 0)
            {
                Log.Error($"The installer failed with code {result.ExitCode}:\r\n{result.Output}");
                UpdateNoted($"The installation of Axit {version} failed. This version keeps running.");
                ShowNotice(new Notice($"Update to Axit {version}",
                    $"The installer failed; this version keeps running.\n\n{result.Output}\n\nYou can download the zip from the release page and run install.cmd yourself.",
                [
                    ReleasePageChoice(release),
                    OverlayChoice.Close,
                ]));
                return;
            }

            Overlay.SetProgress(100);
            _restartArguments = restart ? [.. RestartArguments()] : null;
            Log.Info($"Update {release.Tag} installed in place; restart: {restart}");
            UpdateNoted(restart
                ? $"Axit {version} installed. {AppName} closes now and starts again."
                : $"Axit {version} installed. {AppName} closes now; the next start is the new version.");
            Announce(restart ? "Installed. Restarting" : "Installed. Closing", true);
            Close();
        }
        catch (OperationCanceledException) when (cancelled && !IsDisposed)
        {
            UpdateNoted($"The download of Axit {version} was cancelled.");
            Announce("Update cancelled", true);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or IOException or InvalidDataException or InvalidOperationException or UnauthorizedAccessException)
        {
            // InvalidDataException: a download that is not a zip.
            Log.Error("The update failed", ex);
            if (!IsDisposed)
            {
                UpdateNoted($"The update to Axit {version} failed: {ex.Message}");
                ShowNotice(new Notice($"Update to Axit {version}",
                    $"The update failed: {ex.Message}\nThis version keeps running. You can download the zip from the release page and run install.cmd yourself.",
                [
                    ReleasePageChoice(release),
                    OverlayChoice.Close,
                ]));
            }
        }
        finally
        {
            SetUpdateBusy(false);
        }
    }

    /// <summary>Keep working after Update and restart: the update is installed already; this window stays, and the new version comes with the next start.</summary>
    protected void KeepWorkingAfterUpdate()
    {
        if (_restartArguments is null)
        {
            return;
        }

        _restartArguments = null;
        Announce($"Update installed. It starts with the next {AppName}", true);
    }
}
