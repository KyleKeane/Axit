using System.Diagnostics;
using Axit.Core;
using static Axit.Forms.User32;

namespace Axit.Forms;

/// <summary>
/// The window every app of the bundle is built on (docs/axit/SPEC.md AX-6.5): what the windows share and no app
/// copies. One class in partial files, one per section: this file (the hooks a window fills in, the menus every
/// window has, the keys, the window's place, the settings, the ways out to the system), <c>AxitForm.Notices.cs</c>
/// (the notice flow with its one focus rule, and <see cref="Announce"/>), <c>AxitForm.Find.cs</c> (find in a text
/// box, a list, a transcript and a notice's text), <c>AxitForm.Fonts.cs</c> (the text font with its size keys and
/// picker), <c>AxitForm.Updates.cs</c> (the update check, the update notice and the install in place, AX-3) and
/// <c>AxitForm.Palette.cs</c> (the command palette, AX-10). A window derives from it, adds its controls (the
/// <see cref="Overlay"/> first, so that it docks over the rest), and fills in the hooks listed under "what the window
/// fills in". It grew out of axDown's window (the editor shell) on the third copy, axGIT (2026-10-18); the notice and
/// focus core was written again on 2026-10-23 (docs/axit/review-2026-10-23.md, 5.3).
/// </summary>
public abstract partial class AxitForm : Form
{
    private const int SC_KEYMENU = 0xF100;
    private const int VK_MENU = 0x12;

    /// <summary>How long after an activation with Alt held a menu-bar Alt is taken as that Alt's release (see <see cref="WndProc"/>).</summary>
    private const int AltCarriedOverMs = 1500;

    /// <summary>Until when (<see cref="Environment.TickCount64"/>) a menu-bar Alt is the release of an Alt held as the window was activated; 0 when none was.</summary>
    private long _altCarriedOverUntil;

    /// <summary>Why the settings file could not be read, reported once the window shows; null when it was read.</summary>
    private readonly string? _settingsError;

    /// <param name="settingsError">What the settings loader reported for a file it could not read; the defaults are in use then (AX-5.2).</param>
    protected AxitForm(string appName, WindowSettings settings, string settingsPath, string? settingsError = null)
    {
        AppName = appName;
        Settings = settings;
        SettingsPath = settingsPath;
        _settingsError = settingsError;
        // With the role Window, NVDA speaks the window when it opens ("notes.md - axDown window") before the focused
        // control; without it the window is a nameless pane to NVDA and the app is never named (AX-7.1).
        AccessibleRole = AccessibleRole.Window;
        MinimumSize = new Size(500, 300);
        Size = new Size(900, 600);
        StartPosition = FormStartPosition.WindowsDefaultLocation;
        Overlay.Chosen += OnNoticeChoice;
        _paletteSpeak.Tick += (_, _) => SayFirstPaletteMatch();
        _checkItem.Click += (_, _) => CheckForUpdates(manual: true);
        _updateItem.Click += (_, _) => OfferKnownUpdate();
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

    // ---- what the window fills in ----

    /// <summary>The F1 text.</summary>
    protected abstract string Shortcuts { get; }

    /// <summary>Hides the main controls while a notice shows; <see cref="ShowAfterNotice"/> brings them back.</summary>
    protected abstract void HideForNotice();

    protected abstract void ShowAfterNotice();

    /// <summary>
    /// The control the focus goes to after a notice when the control that had it before is gone or hidden (an
    /// answered question's list, the message field behind a question), and the one announcements are raised on
    /// unless <see cref="AnnounceSource"/> says otherwise.
    /// </summary>
    protected abstract Control FocusTarget { get; }

    /// <summary>The control <see cref="Announce"/> raises on while no notice shows; the focus target unless the window says otherwise (a transcript, the list that has the focus).</summary>
    protected virtual Control AnnounceSource => FocusTarget;

    /// <summary>Whether an unprompted notice plays the chime; a window with a sound setting says.</summary>
    protected virtual bool NoticeChime => true;

    /// <summary>The first notice of a chain opened, with <paramref name="focusBefore"/> the control that had the focus (null when none had it); the next notice of the same chain does not call here.</summary>
    protected virtual void OnNoticeOpened(Control? focusBefore)
    {
    }

    /// <summary>The last notice of a chain closed, before the focus goes back.</summary>
    protected virtual void OnNoticeClosed()
    {
    }

    /// <summary>Gives the window's controls the text font; the notice gets it from the base.</summary>
    protected abstract void ApplyFont(Font font);

    /// <summary>How the installed new version starts this window again after Update and restart: the verb and what it holds (AX-3.2).</summary>
    protected abstract IReadOnlyList<string> RestartArguments();

    /// <summary>What Update and restart does to this window, in the update notice: "closes axDown (asking first…), installs it and starts it again on the same file".</summary>
    protected virtual string UpdateRestartText => $"closes {AppName}, installs it and starts it again";

    /// <summary>A step of the update worth a line of the window's own (axClaude writes it into the conversation); nothing by default.</summary>
    protected virtual void UpdateNoted(string message)
    {
    }

    /// <summary>The window's own lines of Copy diagnostics, after the version line.</summary>
    protected virtual IEnumerable<string> DiagnosticsLines => [];

    /// <summary>The window's own entries of the command palette, after the menus' commands (AX-10.5): axFile's folder entries; none by default.</summary>
    protected virtual IEnumerable<PaletteEntry> PaletteEntries() => [];

    /// <summary>The window's own keys, from its table; true when the key was handled. Called when no notice shows and after the common keys.</summary>
    protected virtual bool ProcessWindowKey(Keys keyData) => false;

    /// <summary>Ctrl+S, the bundle's save key (AX-8.2): the window saves what it holds; a window with nothing to save says so, which is the default.</summary>
    protected virtual void SaveCommand() => Announce("Nothing to save here", true);

    /// <summary>
    /// What the window does with a hand-over from a starting process of its app (<see cref="AppMessage"/>, AM-1.3):
    /// the action, run after the sender has its answer, so that the sender never waits on a read; null when the
    /// window does not take the message, which is the default (the sender then opens a window of its own).
    /// </summary>
    protected virtual Action? TakeHandOver(HandOver message) => null;

    /// <summary>The settings file could not be read and the defaults are in use (AX-5.2): a notice at the first showing, unless the window says it another way (the console windows write a system line).</summary>
    protected virtual void SettingsNotRead(string error) =>
        ShowNotice(Notice.Plain("Settings", $"The settings file could not be read, so the defaults are used.\n{error}") with { Unprompted = true });

    // ---- the menus every window has (AX-6.5, AX-9) ----

    /// <summary>The bundle's Axit menu (AX-9) with this window's Command palette and update items; the window puts it before Help.</summary>
    protected AxitMenu CreateAxitMenu() => new(this, Item("Command &palette...", ShowCommandPalette, BundleKeys.CommandPalette), _checkItem, _updateItem);

    /// <summary>
    /// The "Show current folder in" submenu of a window (AX-9.10; 2026-10-24 one builder for every window): the
    /// window's File Explorer item, then every app of <see cref="Dispatch.Apps"/> that is offered there
    /// (<see cref="App.ShowIn"/>) but the window's own, in the bundle's order, each a new window on the file or folder
    /// <paramref name="place"/> gives (a folder's app gets a file's folder, <see cref="Dispatch.PathFor"/>).
    /// <paramref name="place"/> says itself why there is none and returns null.
    /// </summary>
    protected ToolStripMenuItem CreateShowInMenu(string text, ToolStripMenuItem explorer, Func<string?> place)
    {
        var menu = new ToolStripMenuItem(text);
        menu.DropDownItems.Add(explorer);
        foreach (var app in Dispatch.Apps.Where(app => app.ShowIn && !string.Equals(app.Name, AppName, StringComparison.OrdinalIgnoreCase)))
        {
            menu.DropDownItems.Add(new ToolStripMenuItem(AxitWindows.AppLabel(app), null, (_, _) =>
            {
                if (place() is { } path && Dispatch.PathFor(app, path, Directory.Exists(path)) is { } target)
                {
                    AxitMenu.Start(app.Verb, target);
                }
            }));
        }

        return menu;
    }

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

    /// <summary>The last item of every first menu (AX-6): Exit, closing the window as Alt+F4 does, which is Windows' own key.</summary>
    protected ToolStripMenuItem CreateExitItem() => new("E&xit", null, (_, _) => Close()) { ShortcutKeyDisplayString = "Alt+F4" };

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

    /// <summary>Help: Keyboard shortcuts (F1, the base's key), User guide, then a submenu of pages opened in the browser. The command palette, updates and About are the Axit menu's.</summary>
    protected ToolStripMenuItem CreateHelpMenu(Func<string> userGuide, string resourcesLabel, IEnumerable<(string Label, string Url)> pages)
    {
        var help = new ToolStripMenuItem("&Help");
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

    /// <summary>
    /// Shift+F10, the Applications key and a right click (docs/axfile/SPEC.md AF-D13; from axFile on its second use,
    /// axProcess, 2026-10-24): the menu bar's menu with the mnemonic <paramref name="letter"/>, opened as Alt and the
    /// letter open it, so that NVDA reads its first item; a context menu of its own never read its first item the first
    /// time it opened, however it was shown (2026-10-23). It waits until the keys that asked are up: F10 is itself the
    /// menu key, and with Shift down the letter is Shift and the letter; sent while they were held, Windows only beeped.
    /// </summary>
    protected void OpenBarMenu(char letter)
    {
        var waited = 0;
        var timer = new System.Windows.Forms.Timer { Interval = 20 };
        timer.Tick += (_, _) =>
        {
            waited += timer.Interval;
            if (IsDown(VK_SHIFT) || IsDown(VK_F10) || IsDown(VK_APPS))
            {
                if (waited < 3000)
                {
                    return;
                }
            }

            timer.Dispose();
            // Alt, then the letter, as the user would press them: the bar in menu mode, then the menu's letter, which
            // opens it from the keyboard with its first item read. A mnemonic in the Alt message itself opened the first menu.
            PostMessage(Handle, WM_SYSCOMMAND, SC_KEYMENU, 0);
            PostMessage(Handle, WM_KEYDOWN, char.ToUpperInvariant(letter), 0);
            PostMessage(Handle, WM_KEYUP, char.ToUpperInvariant(letter), unchecked((nint)0xC0000001));
        };
        timer.Start();
    }

    private static bool IsDown(int key) => (GetAsyncKeyState(key) & 0x8000) != 0;

    private const int VK_SHIFT = 0x10;
    private const int VK_F10 = 0x79;
    private const int VK_APPS = 0x5D;

    // ---- the window ----

    /// <summary>The first showing: a settings file that could not be read is reported, then the update check runs when the setting says (AX-3.1).</summary>
    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);
        AutomationChannel.StartIfRequested(this);
        StartMcp();
        StartUpWork();
    }

    /// <summary>What the automation channel reports for <c>state</c> (docs/axit/automation.md): the title, the notice, the status bar's texts.</summary>
    internal object AutomationState() => new
    {
        title = Text,
        notice = NoticeOpen ? Overlay.Shown?.Title : null,
        noticeText = NoticeOpen ? Overlay.Shown?.Text : null,
        process = Environment.ProcessId,
        status = Controls.OfType<StatusStrip>().SelectMany(strip => strip.Items.Cast<ToolStripItem>()).Select(item => item.Text).ToArray(),
    };

    private void StartUpWork()
    {
        if (_settingsError is { } error)
        {
            SettingsNotRead(error);
        }

        // Never in a test run (docs/axit/automation.md): a network call, and a logged error when it fails.
        if (Settings.CheckForUpdates && !BundleInfo.IsDevelopmentBuild && !AutomationChannel.Requested)
        {
            CheckForUpdates(manual: false);
        }
    }

    /// <summary>
    /// What the window holds, for the bundle's Recently closed list (AX-9.4), read as it closes: the path of its file
    /// or folder; an empty text for an app that holds none, listed by its name alone (the default, so that a new app is
    /// in the list without a line of its own); null for a window that holds nothing to open again (an untitled text).
    /// </summary>
    protected virtual string? PathInHand => string.Empty;

    /// <summary>The window joins Recently closed, the place and the settings are saved, and the installed new version started, unless the window cancels its closing before calling here.</summary>
    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (!e.Cancel)
        {
            if (PathInHand is { } held)
            {
                RecentlyClosed.Remember(AppName, held);
            }

            SaveWindow();
            SaveSettings();
            StartInstalledVersion();
        }

        base.OnFormClosing(e);
    }

    protected override void OnActivated(EventArgs e)
    {
        _altCarriedOverUntil = (GetAsyncKeyState(VK_MENU) & 0x8000) != 0 ? Environment.TickCount64 + AltCarriedOverMs : 0;
        base.OnActivated(e);
    }

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern short GetAsyncKeyState(int key);

    /// <summary>The window's client area as its UI Automation rectangle, so that NVDA+End finds the status bar.</summary>
    protected override AccessibleObject CreateAccessibilityInstance() => new WindowAccessibleObject(this);

    protected override void WndProc(ref Message m)
    {
        // Alt alone and F10 open the menu bar through SC_KEYMENU; while a notice holds the keyboard they do nothing.
        // Alt+Space (the system menu, lParam is the space character) still works.
        if (m.Msg == WM_SYSCOMMAND && ((long)m.WParam & 0xFFF0) == SC_KEYMENU && ((long)m.LParam & 0xFFFF) == 0
            && (NoticeOpen || Environment.TickCount64 < _altCarriedOverUntil))
        {
            // The second case: the window came to the front while Alt was held, as Alt+F4 on another program leaves
            // it, and Alt's release then opened this window's menu bar (the user, 2026-10-23, on closing axFile:
            // "alt+f4 lands me on the session menu"). That Alt was pressed for the other window.
            _altCarriedOverUntil = 0;
            return;
        }

        if (AppMessage.Receive(ref m) is { } handOver && TakeHandOver(handOver) is { } apply)
        {
            // Answered at once and applied after: the starting process waits on the answer, never on a read (AM-1.3).
            m.Result = 1;
            BeginInvoke(() =>
            {
                if (!IsDisposed)
                {
                    apply();
                }
            });
            return;
        }

        base.WndProc(ref m);
    }

    /// <summary>
    /// The common keys (AX-8.2): the text size keys work everywhere, a notice included; while a notice shows, its text
    /// takes the find keys (AX-7.2) and every other shortcut waits (not calling the base skips the menu shortcuts;
    /// Enter and Escape reach the notice's buttons through AcceptButton and CancelButton); F1 shows the shortcuts,
    /// Ctrl+Shift+P the palette, Ctrl+S saves; then the window's own keys.
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

        // AX-7.3 (2026-10-25, the user's ask): a paste into an editable text is said, since the edit control's own
        // paste makes no sound and NVDA says nothing. The edit control's keys, not the bundle's: the paste is the
        // control's, and axFile's list keeps Ctrl+V for files.
        if (keyData is (Keys.Control | Keys.V) or (Keys.Shift | Keys.Insert)
            && Control.FromChildHandle(User32.GetFocus()) is TextBoxBase { ReadOnly: false, Enabled: true } box && box.FindForm() == this)
        {
            SayPasteAfter(box);
        }

        if (NoticeOpen)
        {
            if (Overlay.HasSearchableText)
            {
                switch (keyData)
                {
                    case BundleKeys.Find:
                        ShowFindInNotice();
                        return true;
                    case BundleKeys.FindNext:
                        FindInNotice(1);
                        return true;
                    case BundleKeys.FindPrevious:
                        FindInNotice(-1);
                        return true;
                }
            }

            return false;
        }

        // In menu mode (Alt, a menu's name selected on the bar) WinForms routes the keys to the menu bar, whose
        // ProcessCmdKey hands them up to the window: they are the menu's (AX-6.6), or Right in axFile's list opened
        // the entry instead of moving to the next menu (2026-10-22); F1 opened the shortcuts over an open menu and
        // disabled the bar under it (2026-10-23).
        if (FromHandle(msg.HWnd) is ToolStrip)
        {
            return base.ProcessCmdKey(ref msg, keyData);
        }

        switch (keyData)
        {
            case BundleKeys.Shortcuts:
                ShowNotice(Notice.Plain("Keyboard shortcuts", Shortcuts));
                return true;
            case BundleKeys.CommandPalette:
                ShowCommandPalette();
                return true;
            case BundleKeys.Save:
                SaveCommand();
                return true;
        }

        // The window's own keys first (a rename box's, a state's), then the keys of its command list (app-kit AK-2.3).
        return ProcessWindowKey(keyData) || RunCommandKey(keyData) || base.ProcessCmdKey(ref msg, keyData);
    }

    // ---- the window's commands (docs/axit/app-kit.md AK-2) ----

    private readonly List<Command> _commands = [];

    /// <summary>The window's commands, as its menus and keys were made from them: for the command palette's checks and MCP.</summary>
    protected IReadOnlyList<Command> Commands => _commands;

    /// <summary>A command as an item of <paramref name="menu"/> showing its key, and its key handled with it (AK-2.3); the item, for a window that keeps it.</summary>
    protected ToolStripMenuItem Add(ToolStripMenuItem menu, Command command)
    {
        var item = Add(command);
        menu.DropDownItems.Add(item);
        return item;
    }

    /// <summary>
    /// A command with its key handled, and its item for a menu the window places itself (the first item of
    /// <see cref="CreateShowInMenu"/>, a context menu). One record placed in several menus (axProcess's Details in each
    /// view's menu) is one command: it is listed once.
    /// </summary>
    protected ToolStripMenuItem Add(Command command)
    {
        if (!_commands.Any(known => ReferenceEquals(known, command)))
        {
            _commands.Add(command);
        }

        return Item(command.Label, command.Run, command.Key);
    }

    /// <summary>A command that is a key and no menu item (Tab between a window's panes, a list's Enter).</summary>
    protected void AddKey(Command command) => _commands.Add(command);

    /// <summary>
    /// What is wrong with the window's command list (app-kit AK-2.4), for the automated checks' <c>check commands</c>:
    /// a key of the window's own with a modifier, or a function key, that the F1 text does not name, and a command that never runs because
    /// an earlier one has its key everywhere (no <see cref="Command.When"/>). Empty when all is well.
    /// </summary>
    internal List<string> CommandProblems()
    {
        var problems = new List<string>();
        for (var i = 0; i < _commands.Count; i++)
        {
            var command = _commands[i];
            var key = command.Key;
            if (key == Keys.None)
            {
                continue;
            }

            var code = key & Keys.KeyCode;
            var text = BundleKeys.Text(key).TrimEnd('.');
            // A bundle key (the focus keys, find) is the bundle's to describe; KeyTests checks the ones every F1 text names.
            var bundleKey = BundleKeys.All.Any(known => known.Key == key);
            if (!bundleKey && ((key & Keys.Modifiers) != 0 || code is >= Keys.F1 and <= Keys.F24) && !Shortcuts.Contains(text, StringComparison.OrdinalIgnoreCase))
            {
                problems.Add($"{Plain(command.Label)} ({text}) is not in the F1 text");
            }

            if (_commands.Take(i).FirstOrDefault(earlier => earlier.Key == key && earlier.When is null) is { } shadow)
            {
                problems.Add($"{Plain(command.Label)} ({text}) never runs: {Plain(shadow.Label)} takes the key everywhere");
            }
        }

        return problems;

        static string Plain(string label) => label.Replace("&", string.Empty, StringComparison.Ordinal).TrimEnd('.');
    }

    /// <summary>The first command with the key whose <see cref="Command.When"/> holds, run; true when there was one.</summary>
    private bool RunCommandKey(Keys keyData)
    {
        if (keyData == Keys.None)
        {
            return false;
        }

        foreach (var command in _commands)
        {
            if (command.Key == keyData && (command.When?.Invoke() ?? true))
            {
                command.Run();
                return true;
            }
        }

        return false;
    }

    /// <summary>Runs a command's task from the UI thread and turns an escaped exception into a notice rather than a crash: an exception out of an awaited task would otherwise be unobserved.</summary>
    protected async void Do(Func<Task> action)
    {
        try
        {
            await action();
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            Log.Error($"{AppName} command failed", ex);
            if (!IsDisposed)
            {
                Fail(ex.Message);
            }
        }
    }

    // ---- the window's place and the settings (AX-5, AX-7.6) ----

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

    // ---- the ways out to the system (AX-7.2: the only separate windows) ----

    /// <summary>
    /// Whether one of the Windows dialogs (the folder, file and font pickers) is up. Its closing activates the window
    /// before the command that opened it has gone on, so a window that acts on activation (the editors' reload of a
    /// file changed on disk, axFile's read of the folder) waits while this is set (2026-10-23: "Reloaded" was said in
    /// the middle of Save as).
    /// </summary>
    protected bool DialogShowing { get; private set; }

    /// <summary>Shows one of the Windows dialogs over this window, the only separate windows an app opens (AX-7.2), with <see cref="DialogShowing"/> set meanwhile.</summary>
    protected DialogResult ShowSystemDialog(CommonDialog dialog)
    {
        DialogShowing = true;
        try
        {
            return dialog.ShowDialog(this);
        }
        finally
        {
            DialogShowing = false;
        }
    }

    /// <summary>The Windows folder picker, titled with <paramref name="description"/> and opened on <paramref name="start"/>: the folder chosen, or null.</summary>
    protected string? ChooseFolder(string description, string? start = null)
    {
        using var dialog = new FolderBrowserDialog { Description = description, UseDescriptionForTitle = true, ShowNewFolderButton = true };
        if (start is not null)
        {
            dialog.InitialDirectory = start;
        }

        return ShowSystemDialog(dialog) == DialogResult.OK && dialog.SelectedPath.Length > 0 ? dialog.SelectedPath : null;
    }

    /// <summary>A page in the default browser; a failure is a notice. The Axit menu, in this project, calls it for the repository page.</summary>
    protected internal void OpenUrl(string url)
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

    /// <summary>The path in File Explorer: a folder opened, a file selected in its folder; with <paramref name="select"/> a folder is selected in its parent too (axFile's Show in File Explorer on an entry).</summary>
    protected void OpenInExplorer(string path, bool select = false)
    {
        var arguments = !select && Directory.Exists(path) ? $"\"{path}\"" : $"/select,\"{path}\"";
        try
        {
            Process.Start(new ProcessStartInfo(SystemPrograms.Explorer, arguments) { UseShellExecute = false })?.Dispose();
        }
        catch (Exception ex) when (ex is System.ComponentModel.Win32Exception or InvalidOperationException)
        {
            Fail("File Explorer could not be started: " + ex.Message);
        }
    }

    /// <summary>Options → Developer → Open settings file: the app's settings, written first, in a new axJSON window, as any JSON file.</summary>
    protected void OpenSettingsFile()
    {
        SaveSettings();
        AxitMenu.Start(Dispatch.AxJSON, SettingsPath);
    }

    /// <summary>Options → Developer → Copy diagnostics: the versions, the window's own lines and the end of the log, to the clipboard.</summary>
    private void CopyDiagnostics()
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
        // S-20: meant for a public bug report, so the account's name and paths, e-mail addresses and ids are replaced.
        CopyText(Redaction.Redact(text), "Copied diagnostics");
    }

    /// <summary>
    /// The one way a window puts text on the clipboard: <paramref name="said"/> announced when it is there (null says
    /// nothing), and a clipboard another program holds for a moment reported through <see cref="Fail"/> rather than as a
    /// crash; false then.
    /// </summary>
    protected bool CopyText(string text, string? said)
    {
        try
        {
            Clipboard.SetText(text);
        }
        catch (Exception ex) when (ex is System.Runtime.InteropServices.ExternalException or InvalidOperationException)
        {
            Fail("The clipboard could not be written: " + ex.Message);
            return false;
        }

        if (said is not null)
        {
            Announce(said, true);
        }

        return true;
    }
}
