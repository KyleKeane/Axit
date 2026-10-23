using AxClaude.Core.Pty;
using Axit.Core;
using Axit.Core.Pty;
using Axit.Forms;

namespace AxClaude;

internal sealed partial class MainForm
{
    // ---- Claude's process (FR-1): what is Claude's around the console window's StartProcess and StopProcess ----

    /// <summary>The arguments Claude gets after --ax-screen-reader: the chosen ones, or the default (FR-9.1).</summary>
    private IReadOnlyList<string> ClaudeArgs => _claudeArgs ?? (_nothingToContinue ? [] : ["--continue"]);

    /// <summary>
    /// Claude → Force Claude to restart (Ctrl+Shift+R, FR-1.8): the same folder and arguments; the conversation
    /// stays. A running Claude is stopped only after the Force Claude to restart? notice; a stopped one starts at once.
    /// </summary>
    private void RestartClaude()
    {
        if (!ProcessRunning)
        {
            ForceRestart();
            return;
        }

        var state = !ClaudeBusy ? string.Empty
            : _model.PromptPending ? "Claude is waiting for your answer. "
            : "Claude is still working. ";
        if (ClaudeBackground is { } background)
        {
            state += $"Claude has work running in the background ({background}). ";
        }

        ShowNotice(new Notice("Force Claude to restart?",
            state + "Restarting immediately quits Claude Code and terminates any processes it runs in the background, then starts it again in the same folder with the same options. The conversation stays in the window.",
        [
            new OverlayChoice("&Restart now", ForceRestart, IsDefault: true),
            new OverlayChoice("&Keep Claude running", IsCancel: true),
        ]));
    }

    private void ForceRestart()
    {
        Announce("Restarting", false);
        Relaunch("Restarting Claude");
    }

    private void StartClaude()
    {
        if (!Directory.Exists(_folder))
        {
            Fail($"The project folder no longer exists: {_folder}. Press Ctrl+N to choose another.");
            return;
        }

        var configured = _options.ClaudePath ?? _settings.ClaudePath;
        var claude = ClaudeLauncher.Find(configured);
        if (claude is null)
        {
            // FR-1.9: say where the app looked, offer the install command, or let the user point at claude.exe.
            Log.Error("Claude Code was not found");
            _model.AddSystemLine("Claude Code was not found. Install it, then press Ctrl+Shift+R.");
            UpdateStatus();
            ShowClaudeNotFound(ClaudeLauncher.Candidates(configured));
            return;
        }

        var arguments = new List<string>();
        if (!_options.NoAx)
        {
            arguments.Add("--ax-screen-reader");
        }

        arguments.AddRange(ClaudeArgs);
        _launchedFolder = _folder;
        KeepFilesOutOfGit();
        var options = new PtyOptions(ClaudeLauncher.BuildCommandLine(claude, arguments), _folder!, _model.Columns, _model.Rows, ClaudeLauncher.BuildEnvironment());
        if (!StartProcess(options, "Claude"))
        {
            return;
        }

        _readyAnnounced = false;
        // Nothing of the old Claude's is handled: its question, if any, is gone with it.
        _wait = null;
        // A question of the new Claude belongs to no slash command of the old one (QuestionRouter).
        _lastCommand = null;
        // Its drafts are counted afresh when it is ready: none of them is a draft of a turn of this run.
        _drafts.Opened(0);
        _draft = null;
        _settings.RememberFolder(_folder!);
        SaveSettings();
        UpdateStatus();
    }

    /// <summary>FR-1.9: where the app looked, the install command with a button that copies it, the install page, and Locate claude.exe.</summary>
    private void ShowClaudeNotFound(IReadOnlyList<string> candidates)
    {
        ShowNotice(new Notice("Claude Code was not found", HelpText.ClaudeNotFound(candidates),
        [
            new OverlayChoice("Copy install &command", () =>
            {
                if (CopyToClipboard(ClaudeLauncher.InstallCommand))
                {
                    Announce("Copied. Paste it into PowerShell", true);
                }
            }, StaysOpen: true),
            new OverlayChoice("&Open install instructions", () => OpenUrl(ClaudeLauncher.InstallUrl), StaysOpen: true),
            new OverlayChoice("&Locate claude.exe...", LocateClaude, StaysOpen: true),
            OverlayChoice.Close,
        ])
        {
            Unprompted = true,
        });
    }

    /// <summary>The standard file picker; a choice closes the notice, is saved as claudePath and started at once. Cancel keeps the notice.</summary>
    private void LocateClaude()
    {
        using var dialog = new OpenFileDialog
        {
            Title = "Locate claude.exe",
            Filter = "Claude Code (claude.exe;claude.cmd)|claude.exe;claude.cmd|Programs (*.exe;*.cmd)|*.exe;*.cmd|All files (*.*)|*.*",
            CheckFileExists = true,
        };
        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        CloseNotice(null);
        _settings.ClaudePath = dialog.FileName;
        SaveSettings();
        StartClaude();
    }

    /// <summary>Another program can hold the clipboard for a moment; that is announced rather than reported as a crash (FR-13.1).</summary>
    private bool CopyToClipboard(string text)
    {
        try
        {
            Clipboard.SetText(text);
            return true;
        }
        catch (System.Runtime.InteropServices.ExternalException ex)
        {
            Log.Error("The clipboard could not be written", ex);
            Announce("Could not copy. Try again", true);
            return false;
        }
    }

    /// <summary>Claude's process ended, its last output in and the process stopped: the explanation shown, the default --continue retried as a new conversation (FR-9.1), or "Claude stopped".</summary>
    protected override void OnProcessExited(int code)
    {
        if (_model.HideReplay)
        {
            // Claude ended before it was ready: whatever it printed is the explanation.
            _model.ShowHiddenLines();
        }

        if (_claudeArgs is null && !_nothingToContinue && _model.SaidNoConversationToContinue())
        {
            // The default --continue in a folder without a conversation: Claude says so and exits (FR-9.1).
            _nothingToContinue = true;
            Announce("Nothing to continue, new conversation", false);
            Relaunch("No conversation to continue here; starting a new one");
            return;
        }

        _model.AddSystemLine($"Claude stopped (exit code {code}). Press Ctrl+Shift+R to start it again.");
        Announce("Claude stopped", false);
    }

    /// <summary>The attention timer stops with Claude; the console window stops the process, the writes and the control area.</summary>
    protected override void StopProcess(bool terminateAtOnce = false)
    {
        _attention.Stop();
        base.StopProcess(terminateAtOnce);
    }

    /// <summary>
    /// Stops Claude and starts it again with the current folder and arguments, after the system line. The
    /// conversation stays in the window (Restart Claude) unless <paramref name="clearConversation"/> is set (New
    /// session, a folder change; FR-8.3, FR-8.6), in which case the system line is the first line of the emptied window.
    /// A Claude in the middle of a turn is terminated at once (FR-1.8): the user chose the restart knowing it.
    /// </summary>
    private void Relaunch(string systemLine, bool clearConversation = false)
    {
        StopProcess(terminateAtOnce: ClaudeBusy);
        if (clearConversation)
        {
            _model.Clear();
        }
        else
        {
            _model.ResetScreen();
        }

        _model.AddSystemLine(systemLine);
        // The same folder with --continue replays the conversation the window already shows: hidden until Claude is
        // ready (FR-4.8). A cleared window shows the replay with its blocks, as at startup.
        _model.HideReplay = !clearConversation && _folder is not null && string.Equals(_folder, _launchedFolder, StringComparison.OrdinalIgnoreCase) && ClaudeArgs.Contains("--continue");
        if (_folder is not null)
        {
            StartClaude();
        }
    }
}
