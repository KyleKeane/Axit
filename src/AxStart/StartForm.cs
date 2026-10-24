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
    private readonly StartSettings _settings;
    private readonly MenuStrip _menu = new();
    private readonly StatusStrip _status = new();
    private readonly ToolStripStatusLabel _countLabel;
    private IReadOnlyList<ProgramEntry> _programs = [];
    private IReadOnlyList<ProgramEntry> _matches = [];
    private bool _reading;

    public StartForm(bool hidden, StartSettings settings, string? settingsError)
        : base(AxStartApp.Name, settings, StartSettings.DefaultPath, settingsError, hidden)
    {
        _settings = settings;

        // ---- menus and keys (AS-3.3, AS-5.2, AS-5.3): one command list (docs/axit/app-kit.md AK-2), each key from the
        // table; the match keys first, as the switch had them ----
        AddMatchKeys(AxStartKeys.NextMatch, AxStartKeys.PreviousMatch, AxStartKeys.FirstMatch, AxStartKeys.LastMatch);
        var start = new ToolStripMenuItem("ax&Start");
        Add(start, new Command("&Start", AxStartKeys.Start, () => StartSelected(administrator: false)) { Mark = CommandMark.Act });
        Add(start, new Command("Start as &administrator", AxStartKeys.StartAsAdministrator, () => StartSelected(administrator: true)) { Mark = CommandMark.Act });
        Add(start, new Command("Open file &location", AxStartKeys.OpenFileLocation, OpenFileLocation) { Mark = CommandMark.Open });
        Add(start, new Command("&Information", AxStartKeys.Information, SayInformation) { Mark = CommandMark.Read });
        start.DropDownItems.Add(new ToolStripSeparator());
        Add(start, new Command("&Refresh programs", AxStartKeys.Refresh, () => Do(() => RefreshAsync(announce: true))) { Mark = CommandMark.Read });
        start.DropDownItems.Add(new ToolStripSeparator());
        // AS-3.4: hidden at once, whatever the field holds (asked 2026-10-22); the next showing starts empty.
        Add(start, new Command("&Hide", AxStartKeys.Hide, () =>
        {
            Log.Info("Hidden: Hide");
            HideWindow();
        }));
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

        // ---- the field and the matches (AS-3.1, AS-D2): HotkeyForm's, named here ----
        SearchField.AccessibleName = "Search programs";
        Matches.AccessibleName = "Programs, 0";
        Matches.DoubleClick += (_, _) => StartSelected(administrator: false);

        // ---- the status bar (AS-3.1): the count of programs on the left ----
        (_countLabel, _) = StatusLayout.Pair(_status);

        // Docked from the last control to the first: the menu at the top, the status bar at the bottom, the field
        // under the menu, and the list (or the notice over both) what is left.
        Controls.AddRange([Overlay, Matches, SearchField, _status, _menu]);
        ApplyTextFont();
        Fill(null);
    }

    // ---- what the window base asks for ----

    protected override string Shortcuts => HelpText.Shortcuts;

    protected override void HideForNotice()
    {
        SearchField.Visible = false;
        Matches.Visible = false;
    }

    protected override void ShowAfterNotice()
    {
        SearchField.Visible = true;
        Matches.Visible = true;
    }

    protected override Control FocusTarget => SearchField;

    protected override void ApplyFont(Font font)
    {
        SearchField.Font = font;
        Matches.Font = font;
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
            SearchField.Clear();
        }

        var width = Math.Max(MinimumSize.Width, area.Width / 4);
        var height = Math.Max(MinimumSize.Height, Height - ClientSize.Height + _menu.Height + SearchField.Height + Matches.ItemHeight * 12 + _status.Height + 4);
        Bounds = new Rectangle(area.Left + (area.Width - width) / 2, area.Top + (area.Height - height) / 2, width, height);
    }

    /// <summary>The caret in the field, and a fresh read: a program installed since the last showing is there by the time the typing is done (AS-2.1).</summary>
    protected override void AfterShowing()
    {
        if (!NoticeOpen)
        {
            SearchField.Select();
        }

        Do(() => RefreshAsync(announce: false));
    }


    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        SignInStart.Apply(AxStartApp.Name, Dispatch.AxStart, _settings.StartWithWindows);
        Do(() => RefreshAsync(announce: false));
    }

    // ---- the matches (AS-2.2, AS-3.2, AS-3.3) ----

    private ProgramEntry? Selected => Matches.SelectedIndex >= 0 && Matches.SelectedIndex < _matches.Count ? _matches[Matches.SelectedIndex] : null;

    protected override int MatchCount => _matches.Count;

    protected override string SpokenMatch(int index) => ProgramMatch.Spoken(_matches[index], index, _matches.Count);

    protected override string NothingSaid => Matches.Items.Count > 0 ? Matches.Items[0].ToString() ?? string.Empty : "No programs";

    protected override bool SayFirstMatch => _settings.SayFirstMatch;

    protected override void Refill() => Fill(null);

    /// <summary>The list made again from the typed text, the selection on <paramref name="keep"/> when it is still there, the first otherwise.</summary>
    private void Fill(string? keep)
    {
        _matches = ProgramMatch.Find(_programs, SearchField.Text, _settings.Starts);
        var index = keep is null ? 0 : Math.Max(0, _matches.ToList().FindIndex(entry => entry.Name == keep));
        ShowMatches(_matches.Select(entry => entry.Name).ToList(), _programs.Count == 0 ? "Reading the programs" : ProgramMatch.NoMatch(SearchField.Text), $"Programs, {_matches.Count}", index);
        _countLabel.Text = $"{_programs.Count} programs";
    }

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
    private void SayFirstOrNothing() => Announce(NothingSaid, true);
}
