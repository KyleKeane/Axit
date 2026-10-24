using AxStart.Core;
using Axit.Core;
using Axit.Forms;

namespace AxStart;

/// <summary>
/// axStart's window (SPEC.md AS-3): a menu bar, one search field with the caret always in it, the list of matches
/// under it that never takes the focus (AS-D2), and a status bar. The process runs from sign-in with the window
/// hidden (AS-D3); the hotkey, registered on this window (AS-4), shows it in front with the field empty, and Enter,
/// Escape, Alt+F4 and the focus going to another program hide it again. Only Quit axStart ends
/// the process. The hidden window with its hotkey, its showing and hiding and the rule of the foreground are the
/// bundle's background window (<see cref="HotkeyForm"/>, since axClip, 2026-10-23); the notices, the fonts, the update
/// flow and the announcements are the window base under it (<see cref="AxitForm"/>).
/// </summary>
public sealed class StartForm : HotkeyForm
{
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
    private bool _reading;

    public StartForm(bool hidden, StartSettings settings, string? settingsError)
        : base(AxStartApp.Name, settings, StartSettings.DefaultPath, settingsError, hidden)
    {
        _settings = settings;

        // ---- menus (AS-5.2); the keys are handled in ProcessWindowKey from the table, the items only show them ----
        var start = new ToolStripMenuItem("Ax&Start");
        start.DropDownItems.Add(Item("&Start", () => StartSelected(administrator: false), AxStartKeys.Start));
        start.DropDownItems.Add(Item("Start as &administrator", () => StartSelected(administrator: true), AxStartKeys.StartAsAdministrator));
        start.DropDownItems.Add(Item("Open file &location", OpenFileLocation, AxStartKeys.OpenFileLocation));
        start.DropDownItems.Add(Item("&Information", SayInformation, AxStartKeys.Information));
        start.DropDownItems.Add(new ToolStripSeparator());
        start.DropDownItems.Add(Item("&Refresh programs", () => Do(() => RefreshAsync(announce: true)), AxStartKeys.Refresh));
        start.DropDownItems.Add(new ToolStripSeparator());
        start.DropDownItems.Add(Item("&Hide", () => HideWindow(), AxStartKeys.Hide));
        start.DropDownItems.Add(new ToolStripMenuItem("&Quit axStart", null, (_, _) => Quit()));

        var options = new ToolStripMenuItem("&Options");
        options.DropDownItems.Add(new ToolStripMenuItem("&Hotkey...", null, (_, _) => ShowHotkeyNotice()));
        options.DropDownItems.Add(CreateTextMenu());
        options.DropDownItems.Add(CreateGeneralMenu(
            Toggle("&Start with Windows", _settings.StartWithWindows, v =>
            {
                _settings.StartWithWindows = v;
                SignInStart.Apply(AxStartApp.Name, Dispatch.AxStart, v);
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

        // Docked from the last control to the first: the menu at the top, the status bar at the bottom, the field
        // under the menu, and the list (or the notice over both) what is left.
        Controls.AddRange([Overlay, _list, _field, _status, _menu]);
        ApplyTextFont();
        Fill(null);
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
        $"Hotkey: {_settings.Key}, {(HotkeyRegistered ? "registered" : "not registered")}",
    ];

    protected override Hotkey CurrentHotkey => _settings.Key;

    protected override void KeepHotkey(Hotkey key)
    {
        _settings.HotkeyText = key.ToString();
        SaveSettings();
    }

    /// <summary>The field emptied (unless a start failed) and the window centred on the screen of the program that had the focus (AS-3.4).</summary>
    protected override void PrepareShowing(Rectangle area, bool keepText)
    {
        if (!NoticeOpen && !keepText)
        {
            _field.Clear();
        }

        var width = Math.Max(MinimumSize.Width, area.Width / 4);
        var height = Math.Max(MinimumSize.Height, Height - ClientSize.Height + _menu.Height + _field.Height + _list.ItemHeight * 12 + _status.Height + 4);
        Bounds = new Rectangle(area.Left + (area.Width - width) / 2, area.Top + (area.Height - height) / 2, width, height);
    }

    /// <summary>The caret in the field, and a fresh read: a program installed since the last showing is there by the time the typing is done (AS-2.1).</summary>
    protected override void AfterShowing()
    {
        if (!NoticeOpen)
        {
            _field.Select();
        }

        Do(() => RefreshAsync(announce: false));
    }

    protected override void OnHiding() => _speak.Stop();

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        SignInStart.Apply(AxStartApp.Name, Dispatch.AxStart, _settings.StartWithWindows);
        Do(() => RefreshAsync(announce: false));
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
                Log.Info("Hidden: Escape");
                HideWindow();
                return true;
        }

        return false;
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

    /// <summary>The selection moved to <paramref name="target"/> and said; at an end the edge earcon first, and the match it stays on said all the same (AS-3.3).</summary>
    private void SelectMatch(int target)
    {
        _speak.Stop();
        if (_matches.Count == 0)
        {
            Sounds.Edge();
            SayFirstOrNothing();
            return;
        }

        target = Math.Clamp(target, 0, _matches.Count - 1);
        if (target == _list.SelectedIndex)
        {
            // Nothing moves: the earcon, then the match again, so that the last thing heard is the position and not
            // the typed text.
            Sounds.Edge();
        }
        else
        {
            _list.SelectedIndex = target;
        }

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

        // Hidden first, so that the program comes up in front; the foreground is not handed back, or the program,
        // started by a process no longer in front, would open behind.
        Log.Info("Hidden: a program started");
        HideWindow(giveBack: false);
        if (AppsFolder.Start(entry, administrator) is { } error)
        {
            ShowWindowInFront(keepText: true);
            Announce(error, true);
            return;
        }

        _settings.Started(entry.Name);
        SaveSettings();
        Do(GiveForegroundBackLaterAsync);
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

        HideWindow(giveBack: false);
        AxitMenu.Start(Dispatch.AxFile, target);
        Do(GiveForegroundBackLaterAsync);
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
}
