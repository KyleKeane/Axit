using AxClaude.Core.Pty;
using AxClaude.Core.Transcript;
using Axit.Core;
using Axit.Forms;

namespace AxClaude;

internal sealed partial class MainForm
{
    // ---- the menus (FR-10): the controls are the console window's ----

    private void BuildMenus()
    {
        // The menus (FR-10): short, grouped by what the items are about, every mnemonic unique within its menu.
        // Session is the thing worked on, as AxDown's File is: the session, where Claude runs, what the window holds.
        var session = new ToolStripMenuItem("&Session");
        session.DropDownItems.Add(new ToolStripMenuItem("&New session...", null, (_, _) => NewSession()) { ShortcutKeys = AxClaudeKeys.NewSession });
        session.DropDownItems.Add(new ToolStripMenuItem("&Open folder...", null, (_, _) => OpenFolder()));
        session.DropDownItems.Add(RecentFoldersItem);
        // The standard order of a File menu (bundle AX-6): the editors have Save and Save as here; AxClaude has none.
        session.DropDownItems.Add(new ToolStripSeparator());
        // FR-8.2: Show current folder in ▸ File Explorer and the other apps' windows on the project folder.
        var showIn = new ToolStripMenuItem("Show current folder &in");
        showIn.DropDownItems.Add(new ToolStripMenuItem("File &Explorer", null, (_, _) => ShowInExplorer()) { ShortcutKeys = AxClaudeKeys.ShowInExplorer });
        // AxGit on the repository that holds the project folder, a new window (FR-8.2; docs/axgit/SPEC.md AG-1.4).
        showIn.DropDownItems.Add(new ToolStripMenuItem("Ax&Git", null, (_, _) => OpenInAxGit()));
        // AxTerminal with a shell in the project folder, a new window (docs/axterminal/SPEC.md AT-1.5).
        showIn.DropDownItems.Add(new ToolStripMenuItem("Ax&Terminal", null, (_, _) => OpenInAxTerminal()));
        // AxFile listing the project folder, a new window (docs/axfile/SPEC.md AF-1.3).
        showIn.DropDownItems.Add(new ToolStripMenuItem("Ax&File", null, (_, _) => OpenInAxFile()));
        session.DropDownItems.Add(showIn);
        session.DropDownItems.Add(new ToolStripSeparator());
        // Not a save: the conversation is Claude's own record, and this writes a text copy of it. At the bottom, where
        // the editors have Save and Save as, so that the first menu of every window keeps the same order (bundle AX-6).
        session.DropDownItems.Add(new ToolStripMenuItem("&Export conversation...", null, (_, _) => ExportConversation()) { ShortcutKeyDisplayString = BundleKeys.Text(BundleKeys.Save) });
        session.DropDownItems.Add(new ToolStripSeparator());
        session.DropDownItems.Add(CreateExitItem());

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
        navigate.DropDownItems.Add(new ToolStripMenuItem("&Conversation", null, (_, _) => GoTo(Transcript)) { ShortcutKeyDisplayString = BundleKeys.Text(AxClaudeKeys.GoToConversation) });
        // Ctrl+3 is a ShortcutKeys key, but drawn through Text like Ctrl+1 and Ctrl+2 so that it ends in the full stop (AX-8.5).
        navigate.DropDownItems.Add(new ToolStripMenuItem("&Latest response", null, (_, _) => Transcript.JumpToLatestResponse()) { ShortcutKeys = AxClaudeKeys.LatestResponse, ShortcutKeyDisplayString = BundleKeys.Text(AxClaudeKeys.LatestResponse) });
        navigate.DropDownItems.Add(new ToolStripMenuItem("Claude's &question again", null, (_, _) => ReadQuestionAgain()) { ShortcutKeyDisplayString = BundleKeys.Text(AxClaudeKeys.ReadQuestion) });
        navigate.DropDownItems.Add(new ToolStripSeparator());
        navigate.DropDownItems.AddRange(FindItems());
        navigate.DropDownItems.Add(new ToolStripSeparator());
        navigate.DropDownItems.AddRange(BookmarkItems());

        // The settings: a submenu for each kind, nothing outside one. Each check item is saved as it is toggled.
        var options = new ToolStripMenuItem("&Options");
        var sounds = new ToolStripMenuItem("&Sounds");
        sounds.DropDownItems.Add(Toggle("&Chimes: ready, sent, replying, done, question", _settings.SoundOnBell, v => _settings.SoundOnBell = v));
        sounds.DropDownItems.Add(Toggle("&Ticks for new lines and tool calls", _settings.ClickOnNewLine, v => _settings.ClickOnNewLine = v));
        // FR-7.9: starts or stops at once when toggled during a turn; Ctrl+Space anywhere flips the same item.
        sounds.DropDownItems.Add(CreateMusicItem("&Background music while Claude works", AxClaudeKeys.ToggleMusic));
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

        options.DropDownItems.Add(CreateTextMenu(TranscriptTextItems()));
        options.DropDownItems.Add(CreateGeneralMenu(Toggle("&Keep recordings and exported conversations out of git", _settings.KeepFilesOutOfGit, v =>
        {
            _settings.KeepFilesOutOfGit = v;
            KeepFilesOutOfGit();
        })));
        // What only a developer or a bug report needs (FR-12.3, FR-13.3, FR-9.2), in one place.
        options.DropDownItems.Add(CreateDeveloperMenu(RecordItem));

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
        MenuBar.Items.AddRange([session, claude, navigate, options, CreateAxitMenu(), help]);
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
        Announce(MenuText.Plain(ReplySpeechChoices.First(c => c.Mode == mode).Text), true);
    }
}
