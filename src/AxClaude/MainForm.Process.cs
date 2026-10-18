using AxClaude.Core.Pty;
using AxClaude.Core.Transcript;
using Axit.Core;
using Axit.Forms;
using Axit.Core.Pty;

namespace AxClaude;

internal sealed partial class MainForm
{
    // ---- Claude process ----

    /// <summary>The arguments Claude gets after --ax-screen-reader: the chosen ones, or the default (FR-9.1).</summary>
    private IReadOnlyList<string> ClaudeArgs => _claudeArgs ?? (_nothingToContinue ? [] : ["--continue"]);

    /// <summary>
    /// Claude → Force Claude to restart (Ctrl+Shift+R, FR-1.8): the same folder and arguments; the conversation
    /// stays. A running Claude is stopped only after the Force Claude to restart? notice; a stopped one starts at once.
    /// </summary>
    private void RestartClaude()
    {
        if (_host is null)
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

        // Chunks that arrive while the UI thread is busy are handed over together: one post per burst, not one per
        // chunk, so a large tool output does not queue up hundreds of window messages ahead of the user's keys. The
        // handlers go in with Start, so that nothing Claude prints first is missed; they run on background threads
        // and only post to this one, which gets to the posted work after host is assigned below.
        var inbox = new List<byte[]>();
        var posted = false;
        PtyHost? host = null;
        try
        {
            host = PtyHost.Start(new PtyOptions(
                ClaudeLauncher.BuildCommandLine(claude, arguments),
                _folder!,
                _model.Columns,
                _model.Rows,
                ClaudeLauncher.BuildEnvironment()), OnData, OnExited);
        }
        catch (Exception ex)
        {
            Log.Error("Claude could not be started", ex);
            var reason = ex is EntryPointNotFoundException
                ? "this version of Windows has no pseudo console. Windows 10 version 1809 or later is needed"
                : ex.Message;
            Fail("Claude could not be started: " + reason);
            return;
        }

        Log.Info($"Started {claude} (pid {host.ProcessId}) in {_folder}, console {_model.Columns}x{_model.Rows}");
        _host = host;
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

        void OnData(byte[] data)
        {
            try
            {
                _recorder?.Write(data);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                // A full disk or a locked file must not end the read loop, which would freeze the transcript.
                Post(() => RecordingFailed(ex.Message));
            }

            lock (inbox)
            {
                inbox.Add(data);
                if (posted)
                {
                    return;
                }

                posted = true;
            }

            if (!Post(FeedInbox))
            {
                lock (inbox)
                {
                    posted = false;
                }
            }
        }

        void FeedInbox()
        {
            byte[][] batch;
            lock (inbox)
            {
                batch = [.. inbox];
                inbox.Clear();
                posted = false;
            }

            if (!ReferenceEquals(host, _host))
            {
                return;
            }

            foreach (var chunk in batch)
            {
                _model.Feed(chunk);
            }

            _quiet.Stop();
            _quiet.Start();
        }

        void OnExited(int code) => Post(() =>
        {
            if (ReferenceEquals(host, _host))
            {
                // Let output that is still in the pipe arrive before the exit is reported.
                _exitCode = code;
                _exitSettle.Stop();
                _exitSettle.Start();
            }
        });
    }

    private void FinishExit()
    {
        _quiet.Stop();
        _model.EndFrame();
        StopClaude();
        Log.Info($"Claude exited with code {_exitCode}");
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

        _model.AddSystemLine($"Claude stopped (exit code {_exitCode}). Press Ctrl+Shift+R to start it again.");
        Announce("Claude stopped", false);
    }

    private void StopClaude()
    {
        var host = _host;
        _host = null;
        _quiet.Stop();
        _attention.Stop();
        _exitSettle.Stop();
        _writes.Clear();
        _writing = false;
        host?.Dispose();
        // Nothing waits on an answer any more: the message field comes back.
        CloseArea();
        UpdateStatus();
        UpdateMusic();
    }

    /// <summary>
    /// Stops Claude and starts it again with the current folder and arguments, after the system line. The
    /// conversation stays in the window (Restart Claude) unless <paramref name="clearConversation"/> is set (New
    /// session, a folder change; FR-8.3, FR-8.6), in which case the system line is the first line of the emptied window.
    /// </summary>
    private void Relaunch(string systemLine, bool clearConversation = false)
    {
        StopClaude();
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

    /// <summary>An error the user must see: logged, a system line in the conversation, the status, and the notice.</summary>
    protected override void Fail(string message)
    {
        Log.Error(message);
        _model.AddSystemLine(message);
        UpdateStatus();
        ShowError("Error", message);
    }
}
