using AxTerminal.Core;
using Axit.Core;
using Axit.Forms;

namespace AxTerminal;

internal sealed partial class TerminalForm
{
    // ---- the menus (AT-6.1), the title and the status bar (AT-2.3); the controls are the console window's ----

    private void BuildMenus()
    {
        // The menus (SPEC.md AT-6.1), every mnemonic unique within its menu; each item with a key is a command of the
        // window's one list (docs/axit/app-kit.md AK-2), its key from the table. Session, the shell in its folder, is the
        // thing worked on, in the standard order of a first menu (bundle AX-6): New, the recent folders, the slot the
        // editors have Save in, Show current folder in, the export, Exit.
        var session = new ToolStripMenuItem("&Session");
        Add(session, new Command("&New session...", AxTerminalKeys.NewSession, NewSession) { Mark = CommandMark.Act });
        session.DropDownItems.Add(RecentFoldersItem);
        session.DropDownItems.Add(RecentFoldersInNewWindowItem);
        // AT-1.6: the shell changed in place, the current one checked when the submenu opens.
        var switchShell = new ToolStripMenuItem("S&witch shell");
        foreach (var (kind, text) in ShellChoices)
        {
            var item = new ToolStripMenuItem(text, null, (_, _) => SwitchShell(kind));
            _shellItems.Add((kind, item));
            switchShell.DropDownItems.Add(item);
        }

        switchShell.DropDownItems.Add(new ToolStripMenuItem("C&ustom...", null, (_, _) => NewSession()));
        // The shell in use checked as the menu opens; the palette refreshes it the same way (AX-10.2).
        CommandPalette.RefreshedOnOpening(switchShell, () =>
        {
            foreach (var (kind, item) in _shellItems)
            {
                item.Checked = kind == _shell;
            }
        });
        session.DropDownItems.Add(switchShell);
        session.DropDownItems.Add(new ToolStripSeparator());
        Add(session, new Command("Res&tart shell", AxTerminalKeys.RestartShell, RestartShell) { Mark = CommandMark.Act });
        Add(session, new Command("&Clear transcript", AxTerminalKeys.ClearTranscript, ClearTranscript));
        session.DropDownItems.Add(new ToolStripSeparator());
        Add(session, new Command("Say current fol&der", AxTerminalKeys.SayFolder, SayFolder) { Mark = CommandMark.Read });
        // AT-1.5: the way to the other windows, on the shell's current directory.
        var explorer = new Command("File &Explorer", AxTerminalKeys.ShowInExplorer, ShowInExplorer) { Mark = CommandMark.Open };
        session.DropDownItems.Add(CreateShowInMenu("Show current folder &in", Add(explorer), () => CurrentDirectory));
        session.DropDownItems.Add(new ToolStripSeparator());
        session.DropDownItems.Add(new ToolStripMenuItem("&Export transcript...", null, (_, _) => ExportTranscript()) { ShortcutKeyDisplayString = BundleKeys.Text(BundleKeys.Save) });
        session.DropDownItems.Add(new ToolStripSeparator());
        session.DropDownItems.Add(CreateExitItem());

        // What goes to the shell itself (AT-3.5), and the field's extras.
        var shell = new ToolStripMenuItem("Sh&ell");
        Add(shell, new Command("&Interrupt", AxTerminalKeys.Interrupt, Interrupt) { Mark = CommandMark.Act });
        Add(shell, new Command("Send &Escape", AxTerminalKeys.SendEscape, SendEscape) { Mark = CommandMark.Act });
        var sendKey = new ToolStripMenuItem("Send &key");
        sendKey.DropDownItems.Add(new ToolStripMenuItem("&Up", null, (_, _) => SendKey("\x1b[A", "Up sent")));
        sendKey.DropDownItems.Add(new ToolStripMenuItem("&Down", null, (_, _) => SendKey("\x1b[B", "Down sent")));
        sendKey.DropDownItems.Add(new ToolStripMenuItem("Ctrl+D (&end of input)", null, (_, _) => SendKey("\x04", "Ctrl+D sent")));
        shell.DropDownItems.Add(sendKey);
        shell.DropDownItems.Add(new ToolStripSeparator());
        Add(shell, new Command("&Recent commands...", AxTerminalKeys.RecentCommands, ShowRecentCommands) { Mark = CommandMark.Open });
        shell.DropDownItems.Add(new ToolStripMenuItem("Send &without recording", null, (_, _) => SendInput(record: false)) { ShortcutKeyDisplayString = "Ctrl+Enter in the command field" });
        shell.DropDownItems.Add(new ToolStripSeparator());
        Add(shell, new Command("What is r&unning", AxTerminalKeys.WhatIsRunning, SayWhatIsRunning) { Mark = CommandMark.Read });

        var navigate = new ToolStripMenuItem("&Navigate");
        Add(navigate, new Command("&Command field", AxTerminalKeys.GoToField, () => GoTo(BottomControl)) { Mark = CommandMark.Open });
        Add(navigate, new Command("&Terminal", AxTerminalKeys.GoToTerminal, () => GoTo(Transcript)) { Mark = CommandMark.Open });
        Add(navigate, new Command("&Latest output", AxTerminalKeys.LatestOutput, Transcript.JumpToLatestResponse) { Mark = CommandMark.Open });
        navigate.DropDownItems.Add(new ToolStripSeparator());
        AddFindItems(navigate);
        navigate.DropDownItems.Add(new ToolStripSeparator());
        AddBookmarkItems(navigate);

        // The settings: a submenu for each kind, nothing outside one. Each check item is saved as it is toggled.
        var options = new ToolStripMenuItem("&Options");
        var sounds = new ToolStripMenuItem("&Sounds");
        sounds.DropDownItems.Add(Toggle("&Chimes: ready, sent, done, question", _settings.SoundOnBell, v => _settings.SoundOnBell = v));
        sounds.DropDownItems.Add(Toggle("&Ticks for new lines", _settings.ClickOnNewLine, v => _settings.ClickOnNewLine = v));
        sounds.DropDownItems.Add(CreateMusicItem("&Background music while a command runs", AxTerminalKeys.ToggleMusic));
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

        options.DropDownItems.Add(CreateTextMenu(TranscriptTextItems()));
        options.DropDownItems.Add(CreateGeneralMenu());
        options.DropDownItems.Add(CreateDeveloperMenu(RecordItem));

        var help = CreateHelpMenu(HelpText.UserGuide, "Shell &resources",
        [
            ("&PowerShell documentation", "https://learn.microsoft.com/powershell/"),
            ("&Windows Terminal shell integration (the marks)", "https://learn.microsoft.com/windows/terminal/tutorials/shell-integration"),
            ("&Bash manual", "https://www.gnu.org/software/bash/manual/"),
        ]);

        MenuBar.Items.AddRange([session, shell, navigate, options, CreateAxitMenu(), help]);
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
        Announce(MenuText.Plain(SpeechChoices.First(c => c.Mode == mode).Text), true);
    }

    /// <summary>The title is the current directory's name (AT-2.3), which the Axit menu lists the window by.</summary>
    private void UpdateTitle()
    {
        var text = $"{PathName.Of(CurrentDirectory)} - {ShellLauncher.Describe(_shell)} - {AxTerminalApp.Name}";
        if (Text != text)
        {
            Text = text;
        }
    }

    /// <summary>The status bar (AT-2.3): the state on the left, the shell's last prompt (or the directory before the first) on the right.</summary>
    protected override void UpdateStatus()
    {
        var state = !ProcessRunning ? "Shell ended"
            : !_model.HasMarks ? string.Empty
            : _model.Running ? $"Running: {_model.RunningCommand}"
            : _model.Prompt is null ? "Starting"
            : "Idle";
        SetStatus(state, _model.Prompt ?? CurrentDirectory);
    }
}
