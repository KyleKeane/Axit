using AxTerminal.Core;
using Axit.Core;
using Axit.Forms;

namespace AxTerminal;

internal sealed partial class TerminalForm
{
    // ---- New session and Switch shell (AT-1.6), folders (AT-1.5) ----

    /// <summary>The shells of the New session notice, in its order; Custom comes after them. The argument is the settings' word for the shell.</summary>
    private static readonly SessionPreset[] ShellPresets =
    [
        new(ShellLauncher.Describe(ShellKind.PowerShell7), [ShellLauncher.Key(ShellKind.PowerShell7)]),
        new(ShellLauncher.Describe(ShellKind.WindowsPowerShell), [ShellLauncher.Key(ShellKind.WindowsPowerShell)]),
        new(ShellLauncher.Describe(ShellKind.CommandPrompt), [ShellLauncher.Key(ShellKind.CommandPrompt)]),
        new(ShellLauncher.Describe(ShellKind.GitBash), [ShellLauncher.Key(ShellKind.GitBash)]),
    ];

    /// <summary>The Switch shell submenu's shells, each with its letter; Custom… follows them and opens the notice.</summary>
    private static readonly (ShellKind Kind, string Text)[] ShellChoices =
    [
        (ShellKind.PowerShell7, "PowerShell &7"),
        (ShellKind.WindowsPowerShell, "&Windows PowerShell"),
        (ShellKind.CommandPrompt, "&Command Prompt"),
        (ShellKind.GitBash, "&Git Bash"),
    ];

    /// <summary>
    /// Session → New session… (Ctrl+N): the folder with a Choose folder button under it, the shells as radio buttons,
    /// Custom with a command line field (<see cref="SessionControls"/>, the window's own control in the notice, with
    /// the shell's wording). Enter ends the current shell and starts the chosen one in the folder, the transcript
    /// cleared; Escape keeps the current shell.
    /// </summary>
    private void NewSession()
    {
        var selected = _shell == ShellKind.Custom ? ShellPresets.Length : Array.FindIndex(ShellPresets, p => p.Arguments[0] == ShellLauncher.Key(_shell));
        var text = "Choose the shell and the folder. Starting ends the current shell and starts a new session in the folder.";
        if (ProcessRunning && _model.Running)
        {
            text = $"{_model.RunningCommand} is still running. A new session ends it.\n" + text;
        }

        var controls = new SessionControls(CurrentDirectory, ShellPresets, selected < 0 ? ShellPresets.Length : selected, _customShell ?? string.Empty,
            "Shell", "Command line",
            "The command line of the shell to start, as typed at a prompt. Enter starts the session, Shift+Enter starts a new line.",
            start => PickFolder(start ?? CurrentDirectory), said => Announce(said, true), Overlay.SetHeadLine, Overlay.PressDefault);
        ShowNotice(new Notice("New session", text,
        [
            new OverlayChoice("&Start session", () => StartSession(controls), IsDefault: true),
            new OverlayChoice("Cancel", IsCancel: true),
        ])
        {
            Custom = controls,
            Accepts = controls.Ready,
            HeadLine = controls.FolderLine,
        });
    }

    private void StartSession(SessionControls controls)
    {
        var folder = controls.Folder;
        if (folder is null || !Directory.Exists(folder))
        {
            Fail("The folder no longer exists: " + folder);
            return;
        }

        if (controls.SelectedPreset is { } preset)
        {
            _shell = ShellLauncher.Parse(preset.Arguments[0]);
        }
        else
        {
            _shell = ShellKind.Custom;
            _customShell = controls.CustomArguments.Trim();
        }

        SetFolder(folder);
        Announce($"New session in {PathName.Of(folder)}", false);
        Relaunch($"New session: {ShellLauncher.Describe(_shell)} in {folder}", clearTranscript: true);
    }

    /// <summary>
    /// Session → Switch shell → a shell (AT-1.6): the chosen shell in the current folder, the transcript kept; after the
    /// notice "Switch the shell?" while a command runs. Custom… in the submenu opens the New session notice instead.
    /// </summary>
    private void SwitchShell(ShellKind kind)
    {
        if (ProcessRunning && _model.Running)
        {
            ShowNotice(new Notice("Switch the shell?",
                $"{_model.RunningCommand} is still running. Switching ends it and starts {ShellLauncher.Describe(kind)} in the same folder. The transcript stays in the window.",
            [
                new OverlayChoice("&Switch now", () => ForceSwitch(kind), IsDefault: true),
                new OverlayChoice("&Keep it running", IsCancel: true),
            ]));
            return;
        }

        ForceSwitch(kind);
    }

    private void ForceSwitch(ShellKind kind)
    {
        _shell = kind;
        _folder = Directory.Exists(CurrentDirectory) ? CurrentDirectory : _folder;
        Announce(ShellLauncher.Describe(kind), false);
        Relaunch($"Switched to {ShellLauncher.Describe(kind)} in {_folder}", clearTranscript: false);
    }

    /// <summary>The bundle's folder picker (one of the few separate windows, AX-7.2), on the folder or the user's profile; null when it was cancelled.</summary>
    private string? PickFolder(string? start) =>
        ChooseFolder("Choose the folder the shell starts in", start ?? Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));

    private void SetFolder(string folder)
    {
        if (!string.Equals(_folder, folder, StringComparison.OrdinalIgnoreCase))
        {
            // AX-9.4: the folder left behind joins the bundle's Recently closed list.
            RecentlyClosed.Remember(AxTerminalApp.Name, _folder);
        }

        _folder = folder;
    }

    /// <summary>Session → Open recent folder: a shell in that folder, the transcript cleared, with a system line and an announcement.</summary>
    protected override void SwitchFolder(string folder)
    {
        if (!Directory.Exists(folder))
        {
            Fail("The folder no longer exists: " + folder);
            return;
        }

        SetFolder(folder);
        Announce($"Folder {PathName.Of(folder)}", false);
        Relaunch($"New session: {ShellLauncher.Describe(_shell)} in {folder}", clearTranscript: true);
    }

    /// <summary>Session → Show current folder in → File Explorer (Ctrl+W): the shell's current directory, one of the system's own windows.</summary>
    private void ShowInExplorer()
    {
        if (!Directory.Exists(CurrentDirectory))
        {
            Announce("Folder missing", true);
            return;
        }

        OpenInExplorer(CurrentDirectory);
    }

}
