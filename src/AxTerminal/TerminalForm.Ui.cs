using AxTerminal.Core;
using Axit.Core;
using Axit.Forms;

namespace AxTerminal;

internal sealed partial class TerminalForm
{
    // ---- UI ----

    private void BuildUi()
    {
        Text = AxTerminalApp.Name;
        Size = new Size(1000, 700);
        MinimumSize = new Size(600, 400);

        // The menus (SPEC.md AT-6.1), every mnemonic unique within its menu; the keys are handled in ProcessWindowKey
        // from the table, the items only show them. Session, the shell in its folder, is the thing worked on, in the standard order of a first
        // menu (bundle AX-6): New, the recent folders, the slot the editors have Save in, Show current folder in, the
        // export, Exit.
        var session = new ToolStripMenuItem("&Session");
        session.DropDownItems.Add(Item("&New session...", NewSession, AxTerminalKeys.NewSession));
        _recentFoldersItem.DropDownOpening += (_, _) => FillRecentFolders();
        FillRecentFolders();
        session.DropDownItems.Add(_recentFoldersItem);
        // AT-1.6: the shell changed in place, the current one checked when the submenu opens.
        var switchShell = new ToolStripMenuItem("S&witch shell");
        foreach (var (kind, text) in ShellChoices)
        {
            var item = new ToolStripMenuItem(text, null, (_, _) => SwitchShell(kind));
            _shellItems.Add((kind, item));
            switchShell.DropDownItems.Add(item);
        }

        switchShell.DropDownItems.Add(new ToolStripMenuItem("C&ustom...", null, (_, _) => NewSession()));
        switchShell.DropDownOpening += (_, _) =>
        {
            foreach (var (kind, item) in _shellItems)
            {
                item.Checked = kind == _shell;
            }
        };
        session.DropDownItems.Add(switchShell);
        session.DropDownItems.Add(new ToolStripSeparator());
        session.DropDownItems.Add(Item("Res&tart shell", RestartShell, AxTerminalKeys.RestartShell));
        session.DropDownItems.Add(Item("&Clear transcript", ClearTranscript, AxTerminalKeys.ClearTranscript));
        session.DropDownItems.Add(new ToolStripSeparator());
        session.DropDownItems.Add(Item("Say current fol&der", SayFolder, AxTerminalKeys.SayFolder));
        // AT-1.5: the way to the other windows, on the shell's current directory.
        var showIn = new ToolStripMenuItem("Show current folder &in");
        showIn.DropDownItems.Add(Item("File &Explorer", ShowInExplorer, AxTerminalKeys.ShowInExplorer));
        showIn.DropDownItems.Add(new ToolStripMenuItem("Ax&Claude", null, (_, _) => OpenIn(Dispatch.AxClaude)));
        showIn.DropDownItems.Add(new ToolStripMenuItem("Ax&Git", null, (_, _) => OpenIn(Dispatch.AxGit)));
        session.DropDownItems.Add(showIn);
        session.DropDownItems.Add(new ToolStripSeparator());
        session.DropDownItems.Add(new ToolStripMenuItem("&Export transcript...", null, (_, _) => ExportTranscript()) { ShortcutKeyDisplayString = BundleKeys.Text(BundleKeys.Save) });
        session.DropDownItems.Add(new ToolStripSeparator());
        session.DropDownItems.Add(new ToolStripMenuItem("E&xit", null, (_, _) => Close()) { ShortcutKeyDisplayString = "Alt+F4" });

        // What goes to the shell itself (AT-3.5), and the field's extras.
        var shell = new ToolStripMenuItem("Sh&ell");
        shell.DropDownItems.Add(Item("&Interrupt", Interrupt, AxTerminalKeys.Interrupt));
        shell.DropDownItems.Add(Item("Send &Escape", SendEscape, AxTerminalKeys.SendEscape));
        var sendKey = new ToolStripMenuItem("Send &key");
        sendKey.DropDownItems.Add(new ToolStripMenuItem("&Up", null, (_, _) => SendKey("\x1b[A", "Up sent")));
        sendKey.DropDownItems.Add(new ToolStripMenuItem("&Down", null, (_, _) => SendKey("\x1b[B", "Down sent")));
        sendKey.DropDownItems.Add(new ToolStripMenuItem("Ctrl+D (&end of input)", null, (_, _) => SendKey("\x04", "Ctrl+D sent")));
        shell.DropDownItems.Add(sendKey);
        shell.DropDownItems.Add(new ToolStripSeparator());
        shell.DropDownItems.Add(Item("&Recent commands...", ShowRecentCommands, AxTerminalKeys.RecentCommands));
        shell.DropDownItems.Add(new ToolStripMenuItem("Send &without recording", null, (_, _) => SendInput(record: false)) { ShortcutKeyDisplayString = "Ctrl+Enter in the command field" });
        shell.DropDownItems.Add(new ToolStripSeparator());
        shell.DropDownItems.Add(Item("What is r&unning", SayWhatIsRunning, AxTerminalKeys.WhatIsRunning));

        var navigate = new ToolStripMenuItem("&Navigate");
        navigate.DropDownItems.Add(Item("&Command field", () => GoTo(BottomControl), AxTerminalKeys.GoToField));
        navigate.DropDownItems.Add(Item("&Terminal", () => GoTo(_transcript), AxTerminalKeys.GoToTerminal));
        navigate.DropDownItems.Add(Item("&Latest output", _transcript.JumpToLatestResponse, AxTerminalKeys.LatestOutput));
        navigate.DropDownItems.Add(new ToolStripSeparator());
        navigate.DropDownItems.Add(Item("&Find...", FindDialog, BundleKeys.Find));
        navigate.DropDownItems.Add(Item("Find &next", () => FindNext(backward: false), BundleKeys.FindNext));
        navigate.DropDownItems.Add(Item("Find &previous", () => FindNext(backward: true), BundleKeys.FindPrevious));
        navigate.DropDownItems.Add(new ToolStripSeparator());
        navigate.DropDownItems.Add(new ToolStripMenuItem("&Bookmark this line", null, (_, _) => _transcript.ToggleBookmark()) { ShortcutKeyDisplayString = $"{BundleKeys.Text(AxTerminalKeys.BookmarkLine)}, or {BundleKeys.Text(AxTerminalKeys.BookmarkToggle)} in the terminal" });
        navigate.DropDownItems.Add(new ToolStripMenuItem("Next bookmar&k", null, (_, _) => _transcript.JumpToBookmark(backward: false)) { ShortcutKeyDisplayString = $"{BundleKeys.Text(AxTerminalKeys.Bookmark)} in the terminal" });
        navigate.DropDownItems.Add(new ToolStripMenuItem("Pre&vious bookmark", null, (_, _) => _transcript.JumpToBookmark(backward: true)) { ShortcutKeyDisplayString = $"Shift+{BundleKeys.Text(AxTerminalKeys.Bookmark)} in the terminal" });

        // The settings: a submenu for each kind, nothing outside one. Each check item is saved as it is toggled.
        var options = new ToolStripMenuItem("&Options");
        var sounds = new ToolStripMenuItem("&Sounds");
        sounds.DropDownItems.Add(Toggle("&Chimes: ready, sent, done, question", _settings.SoundOnBell, v => _settings.SoundOnBell = v));
        sounds.DropDownItems.Add(Toggle("&Ticks for new lines", _settings.ClickOnNewLine, v => _settings.ClickOnNewLine = v));
        _musicItem = Toggle("&Background music while a command runs", _settings.WorkingMusic, v =>
        {
            _settings.WorkingMusic = v;
            UpdateMusic();
        });
        _musicItem.ShortcutKeyDisplayString = BundleKeys.Text(AxTerminalKeys.ToggleMusic);
        sounds.DropDownItems.Add(_musicItem);
        options.DropDownItems.Add(sounds);

        // AT-5.3: one of two, shown checked like a radio group; the choice is announced, since the menu closes on it.
        var speech = new ToolStripMenuItem("S&peech");
        foreach (var (mode, text) in SpeechChoices)
        {
            var item = new ToolStripMenuItem(text) { Checked = _settings.OutputSpeech == mode };
            item.Click += (_, _) => SetOutputSpeech(mode);
            speech.DropDownItems.Add(item);
            _speechItems.Add((mode, item));
        }

        speech.DropDownItems.Add(new ToolStripSeparator());
        speech.DropDownItems.Add(Toggle("Say &when a program asks", _settings.AnnounceQuestions, v => _settings.AnnounceQuestions = v));
        options.DropDownItems.Add(speech);

        var done = new ToolStripMenuItem("When a command is &done");
        done.DropDownItems.Add(Toggle("&Flash the taskbar button", _settings.FlashTaskbar, v => _settings.FlashTaskbar = v));
        done.DropDownItems.Add(Toggle("&Louder alert when the window is in the background", _settings.BackgroundAlert, v => _settings.BackgroundAlert = v));
        options.DropDownItems.Add(done);

        options.DropDownItems.Add(CreateTextMenu(
            Toggle("Time &stamps on Input and Output lines", _settings.MarkerTimeStamps, v =>
            {
                _settings.MarkerTimeStamps = v;
                _model.TimeStamps = v;
            }),
            // AxClaude FR-4.6a: the # markers of the heading lines shown or hidden; the lines, the keys and the export unchanged.
            Toggle("Mar&kers in the text", _settings.ShowMarkers, v =>
            {
                _settings.ShowMarkers = v;
                _transcript.ShowMarkers = v;
                _transcript.Sync(_model.Lines);
            })));
        options.DropDownItems.Add(CreateGeneralMenu());
        _recordItem.Click += (_, _) => ToggleRecording();
        options.DropDownItems.Add(CreateDeveloperMenu(_recordItem));

        var help = CreateHelpMenu(HelpText.UserGuide, "Shell &resources",
        [
            ("&PowerShell documentation", "https://learn.microsoft.com/powershell/"),
            ("&Windows Terminal shell integration (the marks)", "https://learn.microsoft.com/windows/terminal/tutorials/shell-integration"),
            ("&Bash manual", "https://www.gnu.org/software/bash/manual/"),
        ]);

        var axit = CreateAxitMenu();

        _menu.Items.AddRange([session, shell, navigate, options, axit, help]);
        MainMenuStrip = _menu;

        _state.Text = "Starting";

        _input.AccessibleName = "Command";
        _input.PlaceholderText = "Command";
        _input.Multiline = true;
        _input.AcceptsReturn = true;
        _input.AcceptsTab = false;
        _input.WordWrap = true;
        _input.ScrollBars = ScrollBars.Vertical;
        _input.MaxLength = 0;
        _input.Dock = DockStyle.Bottom;
        _input.TabIndex = 0;
        _input.KeyDown += OnInputKeyDown;
        // The bundle's page and paragraph keys (AT-3.8).
        _input.KeyDown += EditKeys.Handle;

        _transcript.Dock = DockStyle.Fill;
        _transcript.TabIndex = 1;
        _transcript.EscapePressed += () => BottomControl.Focus();
        _transcript.LineChosen += CopyLineToField;
        _transcript.BookmarkToggled += ToggleBookmark;
        Overlay.Jumps = AxTerminalKeys.NoticeJumps;

        Controls.Add(Overlay);
        Controls.Add(_transcript);
        Controls.Add(_input);
        // The recent commands list takes the field's place (AT-3.6), one control at a time.
        _area.TabIndex = 0;
        Controls.Add(_area);
        SizeChanged += (_, _) => _area.FitHeight();
        Controls.Add(_status);
        Controls.Add(_menu);
        _transcript.BringToFront();

        UpdateTitle();
        UpdateStatus();
    }

    /// <summary>Options → Speech → Speak output: a mode (AT-5.3): speech starts with what arrives next.</summary>
    private void SetOutputSpeech(OutputSpeechMode mode)
    {
        _settings.OutputSpeech = mode;
        foreach (var (candidate, item) in _speechItems)
        {
            item.Checked = candidate == mode;
        }

        // What is already there is not read out: the next frame's lines are the first spoken.
        _arrivals.Update(_model.Lines, _model.Running, speak: true);
        SaveSettings();
        Announce(SpeechChoices.First(c => c.Mode == mode).Text.Replace("&", string.Empty), true);
    }

    private void FillRecentFolders() =>
        RecentMenu.Fill(_recentFoldersItem, _settings.RecentFolders, Directory.Exists, RecentMenu.FolderName, "&No recent folders", SwitchFolder);

    /// <summary>The title is the current directory's name (AT-2.3), which the Axit menu lists the window by.</summary>
    private void UpdateTitle()
    {
        var text = $"{RecentMenu.FolderName(CurrentDirectory)} - {ShellLauncher.Describe(_shell)} - {AxTerminalApp.Name}";
        if (Text != text)
        {
            Text = text;
        }
    }

    /// <summary>The status bar (AT-2.3): the state on the left, the shell's last prompt (or the directory before the first) on the right.</summary>
    private void UpdateStatus()
    {
        var state = _host is null ? "Shell ended"
            : !_model.HasMarks ? string.Empty
            : _model.Running ? $"Running: {_model.RunningCommand}"
            : _model.Prompt is null ? "Starting"
            : "Idle";
        var prompt = _model.Prompt ?? CurrentDirectory;
        if (_state.Text != state || _promptLabel.Text != prompt)
        {
            _state.Text = state;
            _promptLabel.Text = prompt;
            StatusLayout.Fit(_status, _state, _promptLabel);
        }
    }

    /// <summary>
    /// Ctrl+1, Ctrl+2 and the Navigate menu (AT-2.2): the control takes the focus, and NVDA announces the change. When it
    /// has the focus already, its name is spoken instead, so the key also answers "where am I" (AxClaude D26).
    /// </summary>
    private void GoTo(Control control)
    {
        if (control.Focused)
        {
            Announce(control.AccessibleName ?? string.Empty, true);
            return;
        }

        control.Focus();
    }

    /// <summary>Speaks without moving the focus; from the notice while one shows, since the terminal is hidden then.</summary>
    protected override bool Announce(string text, bool interrupt) =>
        NoticeOpen ? Overlay.Announce(text, interrupt) : _transcript.Announce(text, interrupt);
}
