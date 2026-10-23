using Axit.Core;
using Axit.Core.Pty;

namespace Axit.Forms;

public abstract partial class ConsoleForm
{
    // ---- the program in its pseudo console (AxClaude FR-1, §7.3; AxTerminal AT-1) ----

    /// <summary>A frame ends after this long without output, besides at the cursor shown again.</summary>
    private readonly System.Windows.Forms.Timer _quiet = new() { Interval = 100 };

    /// <summary>The exit is reported after this long, so that output still in the pipe arrives first.</summary>
    private readonly System.Windows.Forms.Timer _exitSettle = new() { Interval = 300 };

    private readonly Queue<(string Text, int DelayAfter)> _writes = new();

    /// <summary>The one timer between two paced writes: a restart stops it, so that no second pump runs on (two ran after a restart until 2026-10-23).</summary>
    private readonly System.Windows.Forms.Timer _pump = new();

    private PtyHost? _host;
    private int _exitCode;
    private bool _writing;

    /// <summary>The program runs: its process was started and has not ended or been stopped.</summary>
    protected bool ProcessRunning => _host is not null;

    /// <summary>When output last arrived from the program (Environment.TickCount64), for a window's quiet heuristics (AxTerminal AT-5.4).</summary>
    protected long LastOutputTick { get; private set; }

    /// <summary>A write is on its way or queued: the program has not seen everything typed yet.</summary>
    protected bool WritesPending => _writing || _writes.Count > 0;

    /// <summary>
    /// Starts the program in a pseudo console. Chunks that arrive while the UI thread is busy are handed over
    /// together: one post per burst, not one per chunk, so a large output does not queue up hundreds of window
    /// messages ahead of the user's keys. The handlers go in with Start, so that nothing the program prints first is
    /// missed; they run on background threads and only post to this one, which gets to the posted work after the
    /// host is assigned. False, after the error notice, when the program could not be started (<paramref name="program"/>
    /// names it: "Claude", "The shell").
    /// </summary>
    protected bool StartProcess(PtyOptions options, string program)
    {
        var inbox = new List<byte[]>();
        var posted = false;
        PtyHost? host = null;
        try
        {
            host = PtyHost.Start(options, OnData, OnExited);
        }
        catch (Exception ex)
        {
            Log.Error($"{program} could not be started", ex);
            var reason = ex is EntryPointNotFoundException
                ? "this version of Windows has no pseudo console. Windows 10 version 1809 or later is needed"
                : ex.Message;
            Fail($"{program} could not be started: {reason}");
            return false;
        }

        Log.Info($"Started {options.CommandLine} (pid {host.ProcessId}) in {options.WorkingDirectory}, console {options.Columns}x{options.Rows}");
        _host = host;
        LastOutputTick = Environment.TickCount64;
        return true;

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
                Model.Feed(chunk);
            }

            LastOutputTick = Environment.TickCount64;
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

    /// <summary>The process ended and its last output is in: the frame closed, the process stopped, the window told.</summary>
    private void FinishExit()
    {
        _quiet.Stop();
        Model.EndFrame();
        StopProcess();
        Log.Info($"{AppName}'s program exited with code {_exitCode}");
        OnProcessExited(_exitCode);
    }

    /// <summary>
    /// Stops the program and everything that was waiting on it: the timers, the write pump and its queue, the
    /// control area (nothing waits on an answer any more), the status bar and the music. A window with timers of
    /// its own stops them first and calls here.
    /// </summary>
    protected virtual void StopProcess()
    {
        var host = _host;
        _host = null;
        _quiet.Stop();
        _exitSettle.Stop();
        _pump.Stop();
        _writes.Clear();
        _writing = false;
        host?.Dispose();
        CloseArea();
        UpdateStatus();
        UpdateMusic();
    }

    /// <summary>Runs the action on the UI thread; false when the window is gone.</summary>
    protected bool Post(Action action)
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

    // ---- the writes (AxClaude §7.3, AxTerminal AT-3.1): one paced queue, one pump ----

    /// <summary>Text straight to the program, now.</summary>
    protected void Write(string text) => _host?.Write(text);

    /// <summary>A write for the queue, with the pause that follows it before the next; <see cref="StartWrites"/> sends them.</summary>
    protected void Enqueue(string text, int delayAfter) => _writes.Enqueue((text, delayAfter));

    /// <summary>Starts the write pump when it is idle; a pump already running takes what was queued after it.</summary>
    protected void StartWrites()
    {
        if (!_writing)
        {
            PumpWrites();
        }
    }

    /// <summary>What is still queued goes (an interrupt: an answer typed out a key at a time must not land after it).</summary>
    protected void ClearWrites() => _writes.Clear();

    private void PumpWrites()
    {
        if (_writes.Count == 0)
        {
            _writing = false;
            return;
        }

        _writing = true;
        var (text, delay) = _writes.Dequeue();
        Write(text);
        _pump.Interval = delay;
        _pump.Start();
    }
}
