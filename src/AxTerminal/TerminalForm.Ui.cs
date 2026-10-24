using AxTerminal.Core;
using Axit.Core;
using Axit.Forms;

namespace AxTerminal;

internal sealed partial class TerminalForm
{
    // ---- the menus (AT-6.1), the title and the status bar (AT-2.3); the controls are the console window's ----

    private void BuildMenus()
    {
        // The menus (SPEC.md AT-6.1), every mnemonic unique within its menu; the keys are handled in ProcessConsoleKey
        // from the table, the items only show them. Session, the shell in its folder, is the thing worked on, in the
        // standard order of a first menu (bundle AX-6): New, the recent folders, the slot the editors have Save in,
        // Show current folder in, the export, Exit.
        var session = new ToolStripMenuItem("&Session");
        session.DropDownItems.Add(Item("&New session...", NewSession, AxTerminalKeys.NewSession));
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
        session.DropDownItems.Add(Item("Res&tart shell", RestartShell, AxTerminalKeys.RestartShell));
        session.DropDownItems.Add(Item("&Clear transcript", ClearTranscript, AxTerminalKeys.ClearTranscript));
        session.DropDownItems.Add(new ToolStripSeparator());
        session.DropDownItems.Add(Item("Say current fol&der", SayFolder, AxTerminalKeys.SayFolder));
        // AT-1.5: the way to the other windows, on the shell's current directory.
        var showIn = new ToolStripMenuItem("Show current folder &in");
        showIn.DropDownItems.Add(Item("File &Explorer", ShowInExplorer, AxTerminalKeys.ShowInExplorer));
        showIn.DropDownItems.Add(new ToolStripMenuItem("ax&Claude", null, (_, _) => OpenIn(Dispatch.AxClaude)));
        showIn.DropDownItems.Add(new ToolStripMenuItem("ax&GIT", null, (_, _) => OpenIn(Dispatch.AxGit)));
        showIn.DropDownItems.Add(new ToolStripMenuItem("ax&File", null, (_, _) => OpenIn(Dispatch.AxFile)));
        session.DropDownItems.Add(showIn);
        session.DropDownItems.Add(new ToolStripSeparator());
        session.DropDownItems.Add(new ToolStripMenuItem("&Export transcript...", null, (_, _) => ExportTranscript()) { ShortcutKeyDisplayString = BundleKeys.Text(BundleKeys.Save) });
        session.DropDownItems.Add(new ToolStripSeparator());
        session.DropDownItems.Add(CreateExitItem());

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
        navigate.DropDownItems.Add(Item("&Terminal", () => GoTo(Transcript), AxTerminalKeys.GoToTerminal));
        navigate.DropDownItems.Add(Item("&Latest output", Transcript.JumpToLatestResponse, AxTerminalKeys.LatestOutput));
        navigate.DropDownItems.Add(new ToolStripSeparator());
        navigate.DropDownItems.AddRange(FindItems());
        navigate.DropDownItems.Add(new ToolStripSeparator());
        navigate.DropDownItems.AddRange(BookmarkItems());

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
