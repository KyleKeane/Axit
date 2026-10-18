using AxTerminal.Core;
using Axit.Core;
using Axit.Core.Pty;
using Axit.Forms;

namespace AxTerminal;

internal sealed partial class TerminalForm
{
    // ---- the shell process (AT-1) ----

    private void StartShell()
    {
        if (!Directory.Exists(_folder))
        {
            Fail($"The folder no longer exists: {_folder}. Press Ctrl+N to choose another.");
            return;
        }

        var commandLine = ShellLauncher.BuildCommandLine(_shell, _customShell);
        if (commandLine is null)
        {
            Fail(_shell == ShellKind.Custom
                ? "No custom command line is set. Press Ctrl+N and choose a shell."
                : $"{ShellLauncher.Describe(_shell)} was not found on this computer. Press Ctrl+N and choose another shell.");
            return;
        }

        // Chunks that arrive while the UI thread is busy are handed over together: one post per burst, not one per
        // chunk (AxClaude §7.3). The handlers go in with Start, so that nothing the shell prints first is missed.
        var inbox = new List<byte[]>();
        var posted = false;
        PtyHost? host = null;
        try
        {
            host = PtyHost.Start(new PtyOptions(commandLine, _folder, _model.Columns, _model.Rows, ShellLauncher.BuildEnvironment(_shell)), OnData, OnExited);
        }
        catch (Exception ex)
        {
            Log.Error("The shell could not be started", ex);
            var reason = ex is EntryPointNotFoundException
                ? "this version of Windows has no pseudo console. Windows 10 version 1809 or later is needed"
                : ex.Message;
            Fail("The shell could not be started: " + reason);
            return;
        }

        Log.Info($"Started {commandLine} (pid {host.ProcessId}) in {_folder}, console {_model.Columns}x{_model.Rows}");
        _host = host;
        _lastOutputTick = Environment.TickCount64;
        _askedRow = null;
        _model.AddSystemLine($"Shell started: {ShellLauncher.Describe(_shell)} in {_folder}");
        _settings.RememberFolder(_folder);
        _settings.Shell = ShellLauncher.Key(_shell);
        _settings.CustomShell = _customShell;
        SaveSettings();
        _asks.Start();
        UpdateStatus();
        UpdateTitle();

        void OnData(byte[] data)
        {
            try
            {
                _recorder?.Write(data);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
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

            _lastOutputTick = Environment.TickCount64;
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

    /// <summary>The shell's process ended (AT-1.7): the last output in, the system line, the announcement.</summary>
    private void FinishExit()
    {
        _quiet.Stop();
        _model.EndFrame();
        StopShell();
        Log.Info($"The shell exited with code {_exitCode}");
        _model.ShellExited(_exitCode);
        Announce("The shell ended", false);
        UpdateStatus();
    }

    private void StopShell()
    {
        var host = _host;
        _host = null;
        _quiet.Stop();
        _exitSettle.Stop();
        _asks.Stop();
        _writes.Clear();
        _writing = false;
        host?.Dispose();
        CloseArea();
        UpdateStatus();
        UpdateMusic();
    }

    /// <summary>
    /// Terminal → Restart shell (Ctrl+Shift+R, AT-1.6): the same shell again in its current directory, the transcript
    /// kept. A running command is ended only after the Restart the shell? notice; an ended shell starts at once.
    /// </summary>
    private void RestartShell()
    {
        if (_host is not null && _model.Running)
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
        StopShell();
        if (clearTranscript)
        {
            _model.Clear();
            _arrivals.Reset();
        }
        else
        {
            _model.ResetScreen();
        }

        _model.AddSystemLine(systemLine);
        StartShell();
    }

    /// <summary>An error the user must see: logged, a system line in the terminal, the status, and the notice.</summary>
    protected override void Fail(string message)
    {
        Log.Error(message);
        _model.AddSystemLine(message);
        UpdateStatus();
        ShowError("Error", message);
    }

    /// <summary>Runs the action on the UI thread; false when the window is gone.</summary>
    private bool Post(Action action)
    {
        if (IsDisposed || !IsHandleCreated)
        {
            return false;
        }

        try
        {
            BeginInvoke(action);
            return true;
        }
        catch (InvalidOperationException)
        {
            // The window is closing.
            return false;
        }
    }
}
