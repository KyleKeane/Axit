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
        _promptAnnounced = false;
        // A question of the new Claude belongs to no slash command of the old one (QuestionRouter).
        _lastCommand = null;
        // Its drafts are counted afresh when it is ready: none of them is a draft of a turn of this run.
        _drafts.Opened(0);
        _draftToReview = null;
        _reviewing = false;
        _settings.RememberFolder(_folder!);
        SaveSettings();
        UpdateStatus();
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
    protected override void StopProcess()
    {
        _attention.Stop();
        base.StopProcess();
    }

    /// <summary>
    /// Stops Claude and starts it again with the current folder and arguments, after the system line. The
    /// conversation stays in the window (Restart Claude) unless <paramref name="clearConversation"/> is set (New
    /// session, a folder change; FR-8.3, FR-8.6), in which case the system line is the first line of the emptied window.
    /// </summary>
    private void Relaunch(string systemLine, bool clearConversation = false)
    {
        StopProcess();
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
