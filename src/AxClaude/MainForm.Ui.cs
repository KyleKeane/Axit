using AxClaude.Core.Pty;
using AxClaude.Core.Transcript;
using Axit.Forms;

namespace AxClaude;

internal sealed partial class MainForm
{
    // ---- UI ----

    private void BuildUi()
    {
        Text = "AxClaude";
        Size = new Size(1000, 700);
        MinimumSize = new Size(600, 400);

        // The menus (FR-10): short, grouped by what the items are about, every mnemonic unique within its menu.
        // Session is the thing worked on, as AxDown's File is: the session, where Claude runs, what the window holds.
        var session = new ToolStripMenuItem("&Session");
        session.DropDownItems.Add(new ToolStripMenuItem("&New session...", null, (_, _) => NewSession()) { ShortcutKeys = AxClaudeKeys.NewSession });
        session.DropDownItems.Add(new ToolStripMenuItem("&Open folder...", null, (_, _) => OpenFolder()));
        _recentFoldersItem.DropDownOpening += (_, _) => FillRecentFolders();
        FillRecentFolders();
        session.DropDownItems.Add(_recentFoldersItem);
        // The standard order of a File menu (bundle AX-6): the editors have Save and Save as here; AxClaude has none.
        session.DropDownItems.Add(new ToolStripSeparator());
        // FR-8.2: Show current folder in ▸ File Explorer; an Axit file manager joins the submenu when there is one.
        var showIn = new ToolStripMenuItem("Show current folder &in");
        showIn.DropDownItems.Add(new ToolStripMenuItem("File &Explorer", null, (_, _) => ShowInExplorer()) { ShortcutKeys = AxClaudeKeys.ShowInExplorer });
        // AxGit on the repository that holds the project folder, a new window (FR-8.2; docs/axgit/SPEC.md AG-1.4).
        showIn.DropDownItems.Add(new ToolStripMenuItem("Ax&Git", null, (_, _) => OpenInAxGit()));
        // AxTerminal with a shell in the project folder, a new window (docs/axterminal/SPEC.md AT-1.5).
        showIn.DropDownItems.Add(new ToolStripMenuItem("Ax&Terminal", null, (_, _) => OpenInAxTerminal()));
        session.DropDownItems.Add(showIn);
        session.DropDownItems.Add(new ToolStripSeparator());
        // Not a save: the conversation is Claude's own record, and this writes a text copy of it. At the bottom, where
        // the editors have Save and Save as, so that the first menu of every window keeps the same order (bundle AX-6).
        session.DropDownItems.Add(new ToolStripMenuItem("&Export conversation...", null, (_, _) => ExportConversation()) { ShortcutKeyDisplayString = BundleKeys.Text(BundleKeys.Save) });
        session.DropDownItems.Add(new ToolStripSeparator());
        session.DropDownItems.Add(new ToolStripMenuItem("E&xit", null, (_, _) => Close()) { ShortcutKeyDisplayString = "Alt+F4" });

        // What goes to Claude itself (FR-11); the raw sequences are fixed here and listed in Appendix B.
        var claude = new ToolStripMenuItem("&Claude");
        // The process first (FR-1.8), then the turn.
        claude.DropDownItems.Add(new ToolStripMenuItem("Force Claude to res&tart", null, (_, _) => RestartClaude()) { ShortcutKeys = AxClaudeKeys.RestartClaude });
        claude.DropDownItems.Add(new ToolStripSeparator());
        claude.DropDownItems.Add(new ToolStripMenuItem("Send &message", null, (_, _) => SendInput()) { ShortcutKeyDisplayString = "Enter in the message field" });
        claude.DropDownItems.Add(new ToolStripMenuItem("Send &queued messages now", null, (_, _) => SendNow()) { ShortcutKeys = AxClaudeKeys.SendNow });
        claude.DropDownItems.Add(new ToolStripMenuItem("&Interrupt", null, (_, _) => Interrupt()) { ShortcutKeyDisplayString = BundleKeys.Text(AxClaudeKeys.Interrupt) });
        claude.DropDownItems.Add(new ToolStripMenuItem("Next &permission mode", null, (_, _) => Write("\x1b[Z")) { ShortcutKeys = AxClaudeKeys.NextPermissionMode });
        claude.DropDownItems.Add(new ToolStripSeparator());
        // The keys Claude's own menus take. Up and Down are menu items only (FR-2.4): Ctrl+Up and Ctrl+Down move by
        // paragraph in every edit control of the bundle.
        var sendKey = new ToolStripMenuItem("Send &key");
        sendKey.DropDownItems.Add(new ToolStripMenuItem("Ctrl+&C", null, (_, _) => Write("\x03")) { ShortcutKeys = AxClaudeKeys.SendCtrlC });
        sendKey.DropDownItems.Add(new ToolStripMenuItem("Ctrl+&D", null, (_, _) => Write("\x04")));
        sendKey.DropDownItems.Add(new ToolStripMenuItem("&Tab", null, (_, _) => Write("\t")));
        sendKey.DropDownItems.Add(new ToolStripMenuItem("&Up", null, (_, _) => Write("\x1b[A")));
        sendKey.DropDownItems.Add(new ToolStripMenuItem("Do&wn", null, (_, _) => Write("\x1b[B")));
        claude.DropDownItems.Add(sendKey);

        var navigate = new ToolStripMenuItem("&Navigate");
        navigate.DropDownItems.Add(new ToolStripMenuItem("&Message field", null, (_, _) => GoTo(BottomControl)) { ShortcutKeyDisplayString = BundleKeys.Text(AxClaudeKeys.GoToField) });
        navigate.DropDownItems.Add(new ToolStripMenuItem("&Conversation", null, (_, _) => GoTo(_transcript)) { ShortcutKeyDisplayString = BundleKeys.Text(AxClaudeKeys.GoToConversation) });
        // Ctrl+3 is a ShortcutKeys key, but drawn through Text like Ctrl+1 and Ctrl+2 so that it ends in the full stop (AX-8.5).
        navigate.DropDownItems.Add(new ToolStripMenuItem("&Latest response", null, (_, _) => _transcript.JumpToLatestResponse()) { ShortcutKeys = AxClaudeKeys.LatestResponse, ShortcutKeyDisplayString = BundleKeys.Text(AxClaudeKeys.LatestResponse) });
        navigate.DropDownItems.Add(new ToolStripMenuItem("Claude's &question again", null, (_, _) => ReadQuestionAgain()) { ShortcutKeyDisplayString = BundleKeys.Text(AxClaudeKeys.ReadQuestion) });
        navigate.DropDownItems.Add(new ToolStripSeparator());
        navigate.DropDownItems.Add(new ToolStripMenuItem("&Find...", null, (_, _) => FindDialog()) { ShortcutKeyDisplayString = BundleKeys.Text(BundleKeys.Find) });
        navigate.DropDownItems.Add(new ToolStripMenuItem("Find &next", null, (_, _) => FindNext(backward: false)) { ShortcutKeyDisplayString = BundleKeys.Text(BundleKeys.FindNext) });
        navigate.DropDownItems.Add(new ToolStripMenuItem("Find &previous", null, (_, _) => FindNext(backward: true)) { ShortcutKeyDisplayString = BundleKeys.Text(BundleKeys.FindPrevious) });
        navigate.DropDownItems.Add(new ToolStripSeparator());
        navigate.DropDownItems.Add(new ToolStripMenuItem("&Bookmark this line", null, (_, _) => _transcript.ToggleBookmark()) { ShortcutKeys = AxClaudeKeys.BookmarkLine, ShortcutKeyDisplayString = $"{BundleKeys.Text(AxClaudeKeys.BookmarkLine)}, or {BundleKeys.Text(AxClaudeKeys.BookmarkToggle)} in the conversation" });
        navigate.DropDownItems.Add(new ToolStripMenuItem("Next bookmar&k", null, (_, _) => _transcript.JumpToBookmark(backward: false)) { ShortcutKeyDisplayString = $"{BundleKeys.Text(AxClaudeKeys.Bookmark)} in the conversation" });
        navigate.DropDownItems.Add(new ToolStripMenuItem("Pre&vious bookmark", null, (_, _) => _transcript.JumpToBookmark(backward: true)) { ShortcutKeyDisplayString = $"Shift+{BundleKeys.Text(AxClaudeKeys.Bookmark)} in the conversation" });

        // The settings: a submenu for each kind, nothing outside one. Each check item is saved as it is toggled.
        var options = new ToolStripMenuItem("&Options");
        var sounds = new ToolStripMenuItem("&Sounds");
        sounds.DropDownItems.Add(Toggle("&Chimes: ready, sent, replying, done, question", _settings.SoundOnBell, v => _settings.SoundOnBell = v));
        sounds.DropDownItems.Add(Toggle("&Ticks for new lines and tool calls", _settings.ClickOnNewLine, v => _settings.ClickOnNewLine = v));
        // FR-7.9: starts or stops at once when toggled during a turn; Ctrl+Space anywhere flips the same item.
        _musicItem = Toggle("&Background music while Claude works", _settings.WorkingMusic, v =>
        {
            _settings.WorkingMusic = v;
            UpdateMusic();
        });
        _musicItem.ShortcutKeyDisplayString = BundleKeys.Text(AxClaudeKeys.ToggleMusic);
        sounds.DropDownItems.Add(_musicItem);
        options.DropDownItems.Add(sounds);

        // FR-7.4: the reply choices are one of three, shown checked like a radio group; the choice is announced, since
        // the menu closes on it. The tool: row of each call is spoken whatever the reply mode; it starts with what arrives next.
        var speech = new ToolStripMenuItem("S&peech");
        foreach (var (mode, text) in ReplySpeechChoices)
        {
            var item = new ToolStripMenuItem(text) { Checked = _settings.ReplySpeech == mode };
            item.Click += (_, _) => SetReplySpeech(mode);
            speech.DropDownItems.Add(item);
            _replySpeechItems.Add((mode, item));
        }

        speech.DropDownItems.Add(Toggle("Speak each &tool call", _settings.SpeakToolCalls, v =>
        {
            _settings.SpeakToolCalls = v;
            _arrivals.Skip(_model.Lines);
        }));
        options.DropDownItems.Add(speech);

        var done = new ToolStripMenuItem("When Claude is &done");
        done.DropDownItems.Add(Toggle("&Announce \"Done\"", _settings.AnnounceBell, v => _settings.AnnounceBell = v));
        done.DropDownItems.Add(Toggle("&Flash the taskbar button", _settings.FlashTaskbar, v => _settings.FlashTaskbar = v));
        done.DropDownItems.Add(Toggle("&Louder alert when the window is in the background", _settings.BackgroundAlert, v => _settings.BackgroundAlert = v));
        options.DropDownItems.Add(done);

        // D33: with the list off, what Claude waits on is only announced, as before 1.4.0: no control area, no guard, no waiting screens.
        var questions = new ToolStripMenuItem("&Questions");
        questions.DropDownItems.Add(Toggle("&Answer from a list", _settings.QuestionNotices, v =>
        {
            _settings.QuestionNotices = v;
            _model.DetectScreens = v;
            if (!v)
            {
                CloseArea();
            }
        }));
        questions.DropDownItems.Add(Toggle("&Go to them at once, interrupting speech", _settings.InterruptForQuestions, v => _settings.InterruptForQuestions = v));
        questions.DropDownItems.Add(Toggle("&Review answers before they go to Claude", _settings.ReviewAnswers, v => _settings.ReviewAnswers = v));
        options.DropDownItems.Add(questions);

        options.DropDownItems.Add(CreateTextMenu(
            Toggle("Time &stamps on Input and Output lines", _settings.MarkerTimeStamps, v =>
            {
                _settings.MarkerTimeStamps = v;
                _model.TimeStamps = v;
            }),
            // FR-4.6a: the # markers of the heading lines shown or hidden; the lines, the keys and the export unchanged.
            Toggle("Mar&kers in the text", _settings.ShowMarkers, v =>
            {
                _settings.ShowMarkers = v;
                _transcript.ShowMarkers = v;
                _transcript.Sync(_model.Lines);
            })));
        options.DropDownItems.Add(CreateGeneralMenu(Toggle("&Keep recordings and exported conversations out of git", _settings.KeepFilesOutOfGit, v =>
        {
            _settings.KeepFilesOutOfGit = v;
            KeepFilesOutOfGit();
        })));
        // What only a developer or a bug report needs (FR-12.3, FR-13.3, FR-9.2), in one place.
        _recordItem.Click += (_, _) => ToggleRecording();
        options.DropDownItems.Add(CreateDeveloperMenu(_recordItem));

        var help = CreateHelpMenu(HelpText.UserGuide, "Claude Code &resources",
        [
            ("&Documentation", "https://code.claude.com/docs/en/overview"),
            ("Slash &commands", "https://code.claude.com/docs/en/commands"),
            ("&Keys", "https://code.claude.com/docs/en/interactive-mode"),
            ("Command &line", "https://code.claude.com/docs/en/cli-reference"),
            ("&Install or update Claude Code", ClaudeLauncher.InstallUrl),
        ]);

        // The bundle's Axit menu (AX-9) sits before Help in every window and ends with the update items and About
        // (AX-3.1, FR-1.10): Check for updates names the version in use and asks GitHub every time; Update to Axit n
        // stands below it while a newer release is known; About is the bundle's sections, shown as this window's notices.
        var axit = CreateAxitMenu();

        _menu.Items.AddRange([session, claude, navigate, options, axit, help]);
        MainMenuStrip = _menu;

        _state.Text = "Claude: starting";

        _input.AccessibleName = "Message to Claude";
        _input.PlaceholderText = "Message to Claude";
        _input.Multiline = true;
        _input.AcceptsReturn = true;
        _input.AcceptsTab = false;
        _input.WordWrap = true;
        _input.ScrollBars = ScrollBars.Vertical;
        // The control's default of 32 767 characters cut a long paste short without a word (FR-2.1).
        _input.MaxLength = 0;
        _input.Dock = DockStyle.Bottom;
        _input.TabIndex = 0;
        _input.KeyDown += OnInputKeyDown;
        // The bundle's page and paragraph keys (FR-2.1): the control's own keys do nothing while the message fits, or scroll a row and leave the caret.
        _input.KeyDown += EditKeys.Handle;

        _transcript.Dock = DockStyle.Fill;
        _transcript.TabIndex = 1;
        _transcript.EscapePressed += () => BottomControl.Focus();
        _transcript.LineChosen += QuoteLineInMessage;
        _transcript.BookmarkToggled += ToggleBookmark;
        Overlay.Jumps = AxClaudeKeys.NoticeJumps;

        // The overlay is hidden until a notice shows; it then fills the space of the hidden conversation and field.
        Controls.Add(Overlay);
        Controls.Add(_transcript);
        Controls.Add(_input);
        // What Claude waits on takes the message field's place (D33), one control at a time.
        _area.TabIndex = 0;
        Controls.Add(_area);
        SizeChanged += (_, _) => _area.FitHeight();
        Controls.Add(_status);
        Controls.Add(_menu);
        _transcript.BringToFront();

        UpdateFolderUi();
    }

    /// <summary>Options → Speech → Speak replies: a mode (FR-7.4): speech starts with what arrives next.</summary>
    private void SetReplySpeech(ReplySpeechMode mode)
    {
        _settings.ReplySpeech = mode;
        foreach (var (candidate, item) in _replySpeechItems)
        {
            item.Checked = candidate == mode;
        }

        _arrivals.Skip(_model.Lines);
        SaveSettings();
        Announce(ReplySpeechChoices.First(c => c.Mode == mode).Text.Replace("&", string.Empty), true);
    }

    private void FillRecentFolders() =>
        RecentMenu.Fill(_recentFoldersItem, _settings.RecentFolders, Directory.Exists, RecentMenu.FolderName, "&No recent folders", SwitchFolder);
}
