using AxTerminal.Core;
using Axit.Core.Pty;
using Axit.Forms;

namespace AxTerminal;

internal sealed partial class TerminalForm
{
    // ---- the shell process (AT-1): what is the shell's around the console window's StartProcess and StopProcess ----

    private void StartShell()
    {
        if (!Directory.Exists(_folder))
        {
            Fail($"The folder no longer exists: {_folder}. Press Ctrl+N to choose another.");
            return;
        }

        _model.ContinuationPrompt = ShellLauncher.ContinuationPrompt(_shell);
        // S-37: a new secret for this start; the model takes only marks that carry it (a custom shell prints none).
        var nonce = ShellLauncher.NewNonce();
        _model.MarkNonce = _shell == ShellKind.Custom ? null : nonce;
        var commandLine = ShellLauncher.BuildCommandLine(_shell, _customShell, nonce);
        if (commandLine is null)
        {
            Fail(_shell == ShellKind.Custom
                ? "No custom command line is set. Press Ctrl+N and choose a shell."
                : $"{ShellLauncher.Describe(_shell)} was not found on this computer. Press Ctrl+N and choose another shell.");
            return;
        }

        // Bundle AX-11.5: each shell asks once before its first start, those that come with Windows included.
        var target = Axit.Core.ConsentTarget.Shell(ShellLauncher.Describe(_shell));
        if (!Axit.Core.Consent.Given(Axit.Core.AppPaths.ConsentFile, AppName, target.Program))
        {
            WithConsent(target, StartShell, () => _model.AddSystemLine($"Not connected to {target.Program}. Press Ctrl+N to start a shell."));
            return;
        }

        if (!StartProcess(new PtyOptions(commandLine, _folder, _model.Columns, _model.Rows, ShellLauncher.BuildEnvironment(_shell, nonce)), "The shell"))
        {
            return;
        }

        _askedRow = null;
        _model.AddSystemLine($"Shell started: {ShellLauncher.Describe(_shell)} in {_folder}");
        _settings.RememberFolder(_folder);
        _settings.Shell = ShellLauncher.Key(_shell);
        _settings.CustomShell = _customShell;
        SaveSettings();
        _asks.Start();
        UpdateStatus();
        UpdateTitle();
    }

    /// <summary>The shell's process ended (AT-1.7), its last output in and the process stopped: the system line, the announcement.</summary>
    protected override void OnProcessExited(int code)
    {
        _model.ShellExited(code);
        Announce("The shell ended", false);
        UpdateStatus();
    }

    /// <summary>The question heuristic's timer stops with the shell; the console window stops the process, the writes and the control area.</summary>
    protected override void StopProcess(bool terminateAtOnce = false)
    {
        _asks.Stop();
        base.StopProcess(terminateAtOnce);
    }

    /// <summary>
    /// Session → Restart shell (Ctrl+Shift+R, AT-1.6): the same shell again in its current directory, the transcript
    /// kept. A running command is ended only after the Restart the shell? notice; an ended shell starts at once.
    /// </summary>
    private void RestartShell()
    {
        if (ProcessRunning && _model.Running)
        {
            ShowNotice(new Notice("Restart the shell?",
                $"{_model.RunningCommand} is still running. Restarting ends it and starts the shell again in the same folder. The transcript stays in the window.",
            [
                new OverlayChoice("&Restart now", ForceRestart, IsDefault: true),
                new OverlayChoice("&Keep it running", IsCancel: true),
            ]));
            return;
        }

        ForceRestart();
    }

    private void ForceRestart()
    {
        Announce("Restarting", false);
        _folder = Directory.Exists(CurrentDirectory) ? CurrentDirectory : _folder;
        Relaunch("Restarting the shell", clearTranscript: false);
    }

    /// <summary>
    /// Stops the shell and starts one again in the folder, after the system line. The transcript stays (Restart
    /// shell) unless <paramref name="clearTranscript"/> is set (New shell, a folder change; AT-1.6), in which case the
    /// system line is the first line of the emptied window.
    /// </summary>
    private void Relaunch(string systemLine, bool clearTranscript)
    {
        // A command still running is ended at once (AT-1.6): the user chose the restart or the new session knowing it.
        StopProcess(terminateAtOnce: ProcessRunning && _model.Running);
        // The next shell draws on a fresh screen either way: the old one's cells and hidden rows are the old shell's,
        // and a new shell assumes a blank terminal (until 2026-10-23 a new session kept them, and the new shell's
        // shorter rows showed the old ones' tails).
        _model.ResetScreen();
        if (clearTranscript)
        {
            _model.Clear();
            _arrivals.Reset();
        }

        _model.AddSystemLine(systemLine);
        StartShell();
    }
}
