using System.Runtime.InteropServices;
using AxStart.Core;
using Axit.Core;
using Axit.Forms;

namespace AxStart;

/// <summary>
/// AxStart's window (SPEC.md AS-3): a menu bar, one search field with the caret always in it, the list of matches
/// under it that never takes the focus (AS-D2), and a status bar. The process runs from sign-in with the window
/// hidden (AS-D3); the hotkey, registered on this window (AS-4), shows it in front with the field empty, and Enter,
/// Escape, Alt+F4 and the focus going to another program hide it again. Only Quit AxStart ends
/// the process. The notices, the fonts, the update flow and the announcements are the bundle's window base
/// (<see cref="AxitForm"/>).
/// </summary>
public sealed class StartForm : AxitForm
{
    private const int WM_HOTKEY = 0x0312;
    private const int HotkeyId = 1;
    private const int MOD_NOREPEAT = 0x4000;

    /// <summary>How long the typing pauses before the first match is said (AS-3.2).</summary>
    private const int SpeakDelayMs = 300;

    private readonly StartSettings _settings;
    private readonly MenuStrip _menu = new();
    private readonly TextBox _field = new();
    private readonly ListBox _list = new();
    private readonly StatusStrip _status = new();
    private readonly ToolStripStatusLabel _countLabel;
    private readonly System.Windows.Forms.Timer _speak = new() { Interval = SpeakDelayMs };
    private IReadOnlyList<ProgramEntry> _programs = [];
    private IReadOnlyList<ProgramEntry> _matches = [];
    private Hotkey? _registered;
    private Hotkey? _captured;
    private bool _capturing;
    private bool _startHidden = true;
    private readonly bool _showAtStart;
    private bool _quitting;
    private bool _reading;

    public StartForm(bool hidden, StartSettings settings, string? settingsError)
        : base(AxStartApp.Name, settings, StartSettings.DefaultPath)
    {
        _settings = settings;
        _showAtStart = !hidden;
        Text = AxStartApp.Name;
        ShowInTaskbar = false;
        TopMost = true;
        StartPosition = FormStartPosition.Manual;

        // ---- menus (AS-5.2); the keys are handled in ProcessWindowKey from the table, the items only show them ----
        var start = new ToolStripMenuItem("Ax&Start");
        start.DropDownItems.Add(Item("&Start", () => StartSelected(administrator: false), AxStartKeys.Start));
        start.DropDownItems.Add(Item("Start as &administrator", () => StartSelected(administrator: true), AxStartKeys.StartAsAdministrator));
        start.DropDownItems.Add(Item("Open file &location", OpenFileLocation, AxStartKeys.OpenFileLocation));
        start.DropDownItems.Add(Item("&Information", SayInformation, AxStartKeys.Information));
        start.DropDownItems.Add(new ToolStripSeparator());
        start.DropDownItems.Add(Item("&Refresh programs", () => Do(() => RefreshAsync(announce: true)), AxStartKeys.Refresh));
        start.DropDownItems.Add(new ToolStripSeparator());
        start.DropDownItems.Add(Item("&Hide", HideLauncher, AxStartKeys.Hide));
        start.DropDownItems.Add(new ToolStripMenuItem("&Quit AxStart", null, (_, _) => Quit()));

        var options = new ToolStripMenuItem("&Options");
        options.DropDownItems.Add(new ToolStripMenuItem("&Hotkey...", null, (_, _) => ShowHotkeyNotice()));
        options.DropDownItems.Add(CreateTextMenu());
        options.DropDownItems.Add(CreateGeneralMenu(
            Toggle("&Start with Windows", _settings.StartWithWindows, v =>
            {
                _settings.StartWithWindows = v;
                StartupEntry.Apply(v);
            }),
            Toggle("Say the first &match while typing", _settings.SayFirstMatch, v => _settings.SayFirstMatch = v)));
        options.DropDownItems.Add(CreateDeveloperMenu());

        var help = CreateHelpMenu(HelpText.UserGuide, "Windows &resources",
            [("&Keyboard shortcuts in Windows", "https://support.microsoft.com/windows/keyboard-shortcuts-in-windows-dcc61a57-8ff0-cffe-9796-cb9706c75eec")]);

        // The bundle's Axit menu (AX-9) sits before Help in every window.
        _menu.Items.AddRange([start, options, CreateAxitMenu(), help]);
        MainMenuStrip = _menu;

        // ---- the field (AS-3.1): the caret is always here; the list's keys are the field's (AS-3.3) ----
        _field.Dock = DockStyle.Top;
        _field.TabIndex = 0;
        _field.AccessibleName = "Search programs";
        _field.TextChanged += (_, _) => OnTyped();

        // ---- the matches: for the eyes and NVDA's object navigation, never the focus (AS-D2) ----
        _list.Dock = DockStyle.Fill;
        _list.IntegralHeight = false;
        _list.TabStop = false;
        _list.AccessibleName = "Programs, 0";
        _list.GotFocus += (_, _) => _field.Focus();
        _list.DoubleClick += (_, _) => StartSelected(administrator: false);

        _speak.Tick += (_, _) =>
        {
            _speak.Stop();
            SayFirst();
        };

        // ---- the status bar (AS-3.1): the count of programs on the left ----
        (_countLabel, _) = StatusLayout.Pair(_status);

        // AS-3.5: the focus gone to another program hides the window, as the Start menu does.
        Deactivate += (_, _) => BeginInvoke(() => HideIfAnotherProgramIsInFront());

        // Docked from the last control to the first: the menu at the top, the status bar at the bottom, the field
        // under the menu, and the list (or the notice over both) what is left.
        Controls.AddRange([Overlay, _list, _field, _status, _menu]);
        ApplyTextFont();
        Fill(null);

        if (settingsError is not null)
        {
            ShowNotice(Notice.Plain("Settings", $"The settings file could not be read, so the defaults are used.\n{settingsError}") with { Unprompted = true });
        }
    }

    /// <summary>For the starting process (AS-1.2): this process's hidden window, which a second start hands "show" to.</summary>
    public static nint FindRunning()
    {
        var process = (uint)Environment.ProcessId;
        for (var window = FindWindowExW(0, 0, null, AxStartApp.Name); window != 0; window = FindWindowExW(0, window, null, AxStartApp.Name))
        {
            GetWindowThreadProcessId(window, out var owner);
            if (owner != process && OfThisProgram(owner))
            {
                return window;
            }
        }

        return 0;
    }

    private static bool OfThisProgram(uint processId)
    {
        try
        {
            using var other = System.Diagnostics.Process.GetProcessById((int)processId);
            using var self = System.Diagnostics.Process.GetCurrentProcess();
            return string.Equals(other.ProcessName, self.ProcessName, StringComparison.OrdinalIgnoreCase);
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    // ---- what the window base asks for ----

    protected override string Shortcuts => HelpText.Shortcuts;

    protected override void HideForNotice()
    {
        _field.Visible = false;
        _list.Visible = false;
    }

    protected override void ShowAfterNotice()
    {
        _field.Visible = true;
        _list.Visible = true;
    }

    protected override Control FocusTarget => _field;

    protected override void ApplyFont(Font font)
    {
        _field.Font = font;
        _list.Font = font;
    }

    protected override IReadOnlyList<string> RestartArguments() => [Dispatch.AxStart];

    protected override string UpdateRestartText => $"closes {AxStartApp.Name}, installs it and starts it again";

    protected override IEnumerable<string> DiagnosticsLines =>
    [
        $"Programs: {_programs.Count}",
        $"Hotkey: {_settings.Key}, {(_registered is null ? "not registered" : "registered")}",
    ];

    /// <summary>The first showing, which Application.Run asks for (AS-1.1): the handle is made, so that the hotkey and the hand-over reach the window, and nothing shows; without --hidden the window then shows itself centred, as the hotkey shows it.</summary>
    protected override void SetVisibleCore(bool value)
    {
        if (_startHidden)
        {
            _startHidden = false;
            if (!IsHandleCreated)
            {
                CreateHandle();
            }

            value = false;
        }

        base.SetVisibleCore(value);
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        StartupEntry.Apply(_settings.StartWithWindows);
        if (!Register(_settings.Key))
        {
            // AS-4.3: never a launcher that cannot be reached.
            BeginInvoke(() =>
            {
                ShowLauncher();
                ShowNotice(Notice.Plain("Hotkey", $"The hotkey {_settings.Key} is taken by another program. Options, Hotkey, to choose another.") with { Unprompted = true });
            });
        }

        else if (_showAtStart)
        {
            BeginInvoke(() => ShowLauncher());
        }

        Do(() => RefreshAsync(announce: false));
    }

    protected override void OnHandleDestroyed(EventArgs e)
    {
        if (_registered is not null)
        {
            UnregisterHotKey(Handle, HotkeyId);
            _registered = null;
        }

        base.OnHandleDestroyed(e);
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        // AS-1.4: Alt+F4 and Close all windows hide the window; Quit AxStart and Windows' own end end the process.
        if (!_quitting && e.CloseReason == CloseReason.UserClosing)
        {
            e.Cancel = true;
            HideLauncher();
        }

        base.OnFormClosing(e);
    }

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == WM_HOTKEY && m.WParam == HotkeyId)
        {
            if (_capturing)
            {
                // The hotkey pressed in Hotkey…: the key in use chosen again.
                CaptureKey(_settings.Key);
            }
            else if (Visible && ContainsFocus)
            {
                HideLauncher();
            }
            else
            {
                ShowLauncher();
            }

            return;
        }

        if (AppMessage.Receive(ref m) is { } handOver)
        {
            m.Result = 1;
            if (handOver.Verb == AppMessage.Show)
            {
                BeginInvoke(() => ShowLauncher());
            }

            return;
        }

        base.WndProc(ref m);
    }

    // ---- keys (AS-3.3, AS-5.3): every key comes from BundleKeys or AxStartKeys, and is handled here only ----

    protected override bool ProcessWindowKey(Keys keyData)
    {
        switch (keyData)
        {
            case AxStartKeys.NextMatch:
                SelectMatch(_list.SelectedIndex + 1);
                return true;
            case AxStartKeys.PreviousMatch:
                SelectMatch(_list.SelectedIndex - 1);
                return true;
            case BundleKeys.PageDown:
                SelectMatch(Math.Min(_list.SelectedIndex + Page(), _matches.Count - 1));
                return true;
            case BundleKeys.PageUp:
                SelectMatch(Math.Max(_list.SelectedIndex - Page(), 0));
                return true;
            case AxStartKeys.FirstMatch:
                SelectMatch(0);
                return true;
            case AxStartKeys.LastMatch:
                SelectMatch(_matches.Count - 1);
                return true;
            case AxStartKeys.Start:
                StartSelected(administrator: false);
                return true;
            case AxStartKeys.StartAsAdministrator:
                StartSelected(administrator: true);
                return true;
            case AxStartKeys.OpenFileLocation:
                OpenFileLocation();
                return true;
            case AxStartKeys.Information:
                SayInformation();
                return true;
            case AxStartKeys.Refresh:
                Do(() => RefreshAsync(announce: true));
                return true;
            case AxStartKeys.Hide:
                // AS-3.4: hidden at once, whatever the field holds (asked 2026-10-22); the next showing starts empty.
                HideLauncher();
                return true;
        }

        return false;
    }

    // ---- showing and hiding (AS-3.4, AS-4.1) ----

    /// <summary>The window in front, centred on the screen of the program that had the focus, with the field empty (unless <paramref name="keepText"/>: a start that failed) and the caret in it.</summary>
    private void ShowLauncher(bool keepText = false)
    {
        if (Visible && ContainsFocus)
        {
            return;
        }

        if (!NoticeOpen && !keepText)
        {
            _field.Clear();
        }

        var area = Screen.FromHandle(GetForegroundWindow()).WorkingArea;
        var width = Math.Max(MinimumSize.Width, area.Width / 4);
        var height = Math.Max(MinimumSize.Height, Height - ClientSize.Height + _menu.Height + _field.Height + _list.ItemHeight * 12 + _status.Height + 4);
        Bounds = new Rectangle(area.Left + (area.Width - width) / 2, area.Top + (area.Height - height) / 2, width, height);
        Show();
        AxitMenu.Activate(Handle);
        if (!NoticeOpen)
        {
            _field.Select();
        }

        // AS-2.1: a program installed since the last showing is there by the time the typing is done.
        Do(() => RefreshAsync(announce: false));
    }

    /// <summary>The window hidden; Windows gives the focus back to the window that had it before.</summary>
    private void HideLauncher()
    {
        _speak.Stop();
        if (NoticeOpen)
        {
            CloseNotice(null);
        }

        _capturing = false;
        Hide();
    }

    private void HideIfAnotherProgramIsInFront()
    {
        if (!Visible || NoticeOpen)
        {
            return;
        }

        // A picker of our own (the font) is in front of us, not another program: the window stays.
        GetWindowThreadProcessId(GetForegroundWindow(), out var owner);
        if (owner != (uint)Environment.ProcessId)
        {
            HideLauncher();
        }
    }

    private void Quit()
    {
        _quitting = true;
        Close();
    }

    // ---- the matches (AS-2.2, AS-3.2, AS-3.3) ----

    private ProgramEntry? Selected => _list.SelectedIndex >= 0 && _list.SelectedIndex < _matches.Count ? _matches[_list.SelectedIndex] : null;

    private void OnTyped()
    {
        Fill(null);
        _speak.Stop();
        if (_settings.SayFirstMatch && _field.Text.Trim().Length > 0)
        {
            _speak.Start();
        }
    }

    /// <summary>The list made again from the typed text, the selection on <paramref name="keep"/> when it is still there, the first otherwise.</summary>
    private void Fill(string? keep)
    {
        _matches = ProgramMatch.Find(_programs, _field.Text, _settings.Starts);
        _list.BeginUpdate();
        _list.Items.Clear();
        _list.Items.AddRange(_matches.Select(entry => (object)entry.Name).ToArray());
        if (_matches.Count == 0)
        {
            _list.Items.Add(_programs.Count == 0 ? "Reading the programs" : ProgramMatch.NoMatch(_field.Text));
        }

        _list.EndUpdate();
        _list.AccessibleName = $"Programs, {_matches.Count}";
        var index = keep is null ? 0 : Math.Max(0, _matches.ToList().FindIndex(entry => entry.Name == keep));
        _list.SelectedIndex = index;
        _countLabel.Text = $"{_programs.Count} programs";
    }

    /// <summary>The first match and the count, or that nothing matches, after the typing paused (AS-3.2).</summary>
    private void SayFirst()
    {
        if (_field.Text.Trim().Length == 0)
        {
            return;
        }

        Announce(Selected is { } entry ? ProgramMatch.Spoken(entry, _list.SelectedIndex, _matches.Count) : ProgramMatch.NoMatch(_field.Text), true);
    }

    /// <summary>The selection moved to <paramref name="target"/> and said; the edge earcon when it cannot move (AS-3.3).</summary>
    private void SelectMatch(int target)
    {
        _speak.Stop();
        if (_matches.Count == 0)
        {
            Sounds.Edge();
            return;
        }

        target = Math.Clamp(target, 0, _matches.Count - 1);
        if (target == _list.SelectedIndex)
        {
            Sounds.Edge();
            return;
        }

        _list.SelectedIndex = target;
        Announce(ProgramMatch.Spoken(_matches[target], target, _matches.Count), true);
    }

    /// <summary>The matches the list shows at once, for Page Up and Page Down.</summary>
    private int Page() => Math.Max(1, _list.ClientSize.Height / Math.Max(1, _list.ItemHeight));

    /// <summary>The Applications folder read again on its thread; the list swapped in with the selection kept by name (AS-2.1).</summary>
    private async Task RefreshAsync(bool announce)
    {
        if (_reading)
        {
            return;
        }

        _reading = true;
        try
        {
            var programs = await AppsFolder.ReadAsync();
            if (IsDisposed)
            {
                return;
            }

            _programs = programs;
            Log.Info($"Read {programs.Count} programs");
            Fill(Selected?.Name);
            if (announce)
            {
                Announce($"Programs refreshed, {programs.Count} programs", true);
            }
        }
        finally
        {
            _reading = false;
        }
    }

    // ---- what is done with a match (AS-2.3, AS-2.4, AS-3.3) ----

    private void StartSelected(bool administrator)
    {
        if (Selected is not { } entry)
        {
            SayFirstOrNothing();
            return;
        }

        // Hidden first, so that the program comes up in front of the window that had the focus.
        HideLauncher();
        if (AppsFolder.Start(entry, administrator) is { } error)
        {
            ShowLauncher(keepText: true);
            Announce(error, true);
            return;
        }

        _settings.Started(entry.Name);
        SaveSettings();
    }

    private void OpenFileLocation()
    {
        if (Selected is not { } entry)
        {
            SayFirstOrNothing();
            return;
        }

        if (entry.Target is not { } target)
        {
            Announce($"No file location for {entry.Name}", true);
            return;
        }

        HideLauncher();
        AxitMenu.Start(Dispatch.AxFile, target);
    }

    private void SayInformation()
    {
        if (Selected is not { } entry)
        {
            SayFirstOrNothing();
            return;
        }

        Announce(entry.Information, true);
    }

    /// <summary>What a command on a match says with none: the list's one line, "No programs match xyz".</summary>
    private void SayFirstOrNothing() => Announce(_list.Items.Count > 0 ? _list.Items[0].ToString() ?? string.Empty : "No programs", true);

    // ---- the hotkey (AS-4) ----

    /// <summary>The hotkey registered in place of the one before; false, with nothing registered, when another program holds it.</summary>
    private bool Register(Hotkey key)
    {
        if (_registered is not null)
        {
            UnregisterHotKey(Handle, HotkeyId);
            _registered = null;
        }

        if (!RegisterHotKey(Handle, HotkeyId, (uint)(key.Modifiers | MOD_NOREPEAT), (uint)key.Key))
        {
            Log.Info($"The hotkey {key} could not be registered");
            return false;
        }

        _registered = key;
        Log.Info($"Hotkey {key} registered");
        return true;
    }

    /// <summary>Options → Hotkey… (AS-4.2): a field that takes the next key pressed with its modifiers; Enter keeps it.</summary>
    private void ShowHotkeyNotice()
    {
        _captured = null;
        _capturing = true;
        ShowNotice(new Notice("Hotkey",
            $"Press the key that brings AxStart up, with Ctrl, Alt or the Windows key, then Enter. Now: {_settings.Key}.",
            [
                new OverlayChoice("&Save", () => ApplyHotkey(_captured), IsDefault: true),
                new OverlayChoice("&Cancel", () => _capturing = false, IsCancel: true),
            ])
        {
            Input = new OverlayInput("Press the new key:", "Press the new key", _settings.Key.ToString(), "Press a key first", KeyHook: OnHotkeyField),
        });
    }

    /// <summary>A key in the Hotkey field: taken as the new hotkey, or refused with the reason; Enter, Escape and Tab stay the notice's.</summary>
    private bool OnHotkeyField(Keys keyData)
    {
        var code = keyData & Keys.KeyCode;
        var modifiers = keyData & Keys.Modifiers;
        if ((modifiers == Keys.None && code is Keys.Return or Keys.Escape or Keys.Tab) || keyData == (Keys.Shift | Keys.Tab))
        {
            return false;
        }

        if (code is Keys.ShiftKey or Keys.ControlKey or Keys.Menu or Keys.LWin or Keys.RWin)
        {
            return true;
        }

        var flags = ((modifiers & Keys.Control) != 0 ? Hotkey.Control : 0)
            | ((modifiers & Keys.Alt) != 0 ? Hotkey.Alt : 0)
            | ((modifiers & Keys.Shift) != 0 ? Hotkey.Shift : 0)
            | (WindowsKeyDown() ? Hotkey.Windows : 0);
        CaptureKey(new Hotkey(flags, (int)code));
        return true;
    }

    private void CaptureKey(Hotkey key)
    {
        if (key.Refusal() is { } refusal)
        {
            Announce(refusal, true);
            return;
        }

        _captured = key;
        Overlay.SetInputText(key.ToString());
        Announce(key.ToString(), true);
    }

    private void ApplyHotkey(Hotkey? key)
    {
        _capturing = false;
        var old = _settings.Key;
        if (key is null || key == old)
        {
            Announce($"Hotkey {old}", true);
            return;
        }

        if (!Register(key))
        {
            Register(old);
            Announce($"{key} is taken by another program", true);
            return;
        }

        _settings.Hotkey = key.ToString();
        SaveSettings();
        Announce($"Hotkey {key}", true);
    }

    private static bool WindowsKeyDown() => GetKeyState(0x5B) < 0 || GetKeyState(0x5C) < 0;

    /// <summary>Runs a task on the UI thread and turns an escaped exception into a notice rather than a crash.</summary>
    private async void Do(Func<Task> action)
    {
        try
        {
            await action();
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            Log.Error("AxStart command failed", ex);
            if (!IsDisposed)
            {
                Fail(ex.Message);
            }
        }
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool RegisterHotKey(nint hWnd, int id, uint modifiers, uint key);

    [DllImport("user32.dll")]
    private static extern bool UnregisterHotKey(nint hWnd, int id);

    [DllImport("user32.dll")]
    private static extern nint GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(nint hWnd, out uint processId);

    [DllImport("user32.dll")]
    private static extern short GetKeyState(int key);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern nint FindWindowExW(nint parent, nint after, string? className, string title);
}
