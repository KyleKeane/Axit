// QaRun: an automated quality check of axClaude's console pipeline without windows (docs/axit/plan.md, automated
// quality checks). It starts several Claude Code sessions at once, each in a fresh test folder, through the app's own
// pieces (PtyHost, ClaudeLauncher's command line and environment, SessionModel), and walks each the way a user does:
// the trust prompt answered, a message sent and its reply awaited, /exit. The model is chosen on Claude's command
// line (--model, as axclaude <folder> -- --model haiku does), which lasts the session: /model saves the choice as the
// user's default in ~/.claude/settings.json, which the first runs of this tool did (2026-10-23). A session
// passes when every step is seen in its transcript; the report is one line per session, and a failing session's last
// lines are printed with personal data replaced (Redaction). No window opens, so it never takes the keyboard.
//
// Usage:
//   QaRun [--sessions N] [--model haiku] [--message <text>] [--claude <path>] [--keep]
//   (defaults: 3 sessions, haiku, "Reply with the single word hello."; the folders are %TEMP%\axit-qa\session-N,
//   made new each run and removed afterwards unless --keep)

using System.Diagnostics;
using System.Text;
using AxClaude.Core.Privacy;
using AxClaude.Core.Pty;
using AxClaude.Core.Transcript;
using Axit.Core.Pty;
using Axit.Core.Transcript;

namespace QaRun;

internal static class Program
{
    private static int Main(string[] args)
    {
        var sessions = 3;
        var model = "haiku";
        var message = "Reply with the single word hello.";
        string? claude = null;
        var keep = false;
        for (var i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--sessions": sessions = int.Parse(args[++i]); break;
                case "--model": model = args[++i]; break;
                case "--message": message = args[++i]; break;
                case "--claude": claude = args[++i]; break;
                case "--keep": keep = true; break;
                case "--enter-delay": Session.EnterDelayMs = int.Parse(args[++i]); break;
                default:
                    Console.Error.WriteLine($"Unknown option: {args[i]}");
                    return 2;
            }
        }

        Console.OutputEncoding = Encoding.UTF8;
        claude = ClaudeLauncher.Find(claude);
        if (claude is null)
        {
            Console.Error.WriteLine("Claude Code was not found.");
            return 2;
        }

        var root = Path.Combine(Path.GetTempPath(), "axit-qa");
        var runs = Enumerable.Range(1, sessions).Select(n => new Session(n, Path.Combine(root, $"session-{n}"), claude, model, message)).ToList();
        var clock = Stopwatch.StartNew();
        // Staggered by half a second, as a user opening windows one after another would be.
        var tasks = runs.Select((run, index) => Task.Run(async () =>
        {
            await Task.Delay(index * 500);
            run.Run();
        })).ToArray();
        Task.WaitAll(tasks);

        Console.WriteLine($"QaRun: {sessions} sessions, --model {model}, {clock.Elapsed.TotalSeconds:0} s");
        foreach (var run in runs)
        {
            Console.WriteLine(run.Report());
        }

        foreach (var run in runs.Where(run => !run.Passed))
        {
            Console.WriteLine($"--- session {run.Number}, its last lines:");
            Console.WriteLine(Redaction.Redact(run.LastLines(20)));
        }

        if (!keep)
        {
            try
            {
                Directory.Delete(root, recursive: true);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                // A Claude still closing holds its folder for a moment; the temp folder is Windows' to clean.
            }
        }

        return runs.All(run => run.Passed) ? 0 : 1;
    }
}

/// <summary>One Claude session walked through the steps, its model fed and framed as the window does (100 ms of quiet ends a frame).</summary>
internal sealed class Session(int number, string folder, string claude, string model, string message)
{
    private const int QuietMs = 100;

    /// <summary>The gap between a message's text and its Enter (the window's is 100 ms).</summary>
    public static int EnterDelayMs { get; set; } = 100;
    private readonly object _gate = new();
    private readonly SessionModel _model = new(240, 50);
    private readonly List<string> _steps = [];
    private readonly Stopwatch _clock = Stopwatch.StartNew();
    private PtyHost? _host;
    private long _lastData;
    private bool _dirty;
    private int? _exitCode;
    private string? _failure;

    public int Number => number;

    public bool Passed => _failure is null;

    public void Run()
    {
        try
        {
            if (Directory.Exists(folder))
            {
                Directory.Delete(folder, recursive: true);
            }

            Directory.CreateDirectory(folder);
            var commandLine = ClaudeLauncher.BuildCommandLine(claude, ["--ax-screen-reader", "--model", model]);
            _host = PtyHost.Start(new PtyOptions(commandLine, folder, 240, 50, ClaudeLauncher.BuildEnvironment()), OnData, code =>
            {
                lock (_gate)
                {
                    _exitCode = code;
                }
            });

            using var framer = new Timer(_ => Frame(), null, QuietMs, QuietMs / 2);
            Walk();
        }
        catch (Exception ex)
        {
            Fail($"exception: {ex.GetType().Name}: {ex.Message}");
        }
        finally
        {
            _host?.Dispose();
        }
    }

    private void Walk()
    {
        // 1. Ready, after the trust prompt a fresh folder brings.
        if (!WaitFor("ready or trust", 60_000, () => Ready || TrustAsked))
        {
            return;
        }

        if (TrustAsked)
        {
            // The trust prompt refuses an answer within about a second of its appearance (record-fixture skill).
            Thread.Sleep(1500);
            Write("y");
            Thread.Sleep(300);
            Write("\r");
            if (!WaitFor("ready", 60_000, () => Ready))
            {
                return;
            }
        }

        Step("ready");

        // 3. A message with its exchange block, as the window sends it, and Claude's reply.
        lock (_gate)
        {
            _model.Send(message);
        }

        Write(message);
        Thread.Sleep(EnterDelayMs);
        Write("\r");
        // Claude drops an Enter in the first moments after its prompt first shows (found by this tool, 2026-10-23):
        // the text stays on the prompt row. The Enter is sent again once, and the report says so.
        // The window's rule for a dropped Enter (FR-2.1, SessionModel.PromptHolds): while the prompt row still holds
        // the message 1.5 s later and Claude neither works nor asks, Enter again, three times at most.
        for (var attempt = 1; attempt <= 3; attempt++)
        {
            Thread.Sleep(1500);
            bool held;
            lock (_gate)
            {
                held = !_model.Working && !_model.PromptPending && _model.PromptHolds(message);
            }

            if (!held)
            {
                break;
            }

            Step($"Enter sent again ({attempt})");
            Write("\r");
        }
        if (!WaitFor("reply", 120_000, () => Replied && Ready))
        {
            return;
        }

        Step("reply: " + Reply());

        // 4. /exit, and the process ends by itself.
        Write("/exit");
        Thread.Sleep(300);
        Write("\r");
        if (!WaitFor("exit", 20_000, () => _exitCode is not null))
        {
            return;
        }

        Step($"exit {_exitCode}");
    }

    private bool Ready => _model.Mode is not null && !_model.Working && !_model.PromptPending;

    private bool TrustAsked => _model.PromptPending && VisibleText().Contains("trust", StringComparison.OrdinalIgnoreCase);

    private bool Replied => _model.Lines.Any(line => line.Kind == LineKind.ClaudeReply && !line.Hidden);

    private string Reply() => _model.Lines.LastOrDefault(line => line.Kind == LineKind.ClaudeReply && !line.Hidden)?.Text ?? "(none)";

    private string VisibleText() => string.Join("\n", _model.Lines.Where(line => !line.Hidden).Select(line => line.Text));

    public string LastLines(int count)
    {
        lock (_gate)
        {
            // Hidden lines too, marked: what the transcript leaves out (the prompt row, the draft) is often the answer.
            var visible = _model.Lines.Select(line => (line.Hidden ? "(hidden) " : string.Empty) + line.Text).ToList();
            return string.Join("\n", visible.Skip(Math.Max(0, visible.Count - count)));
        }
    }

    public string Report() =>
        $"session {number}: {(Passed ? "PASS" : "FAIL")}; {string.Join("; ", _steps)}{(_failure is null ? string.Empty : "; " + _failure)}";

    private void OnData(byte[] data)
    {
        lock (_gate)
        {
            try
            {
                _model.Feed(data);
            }
            catch (Exception ex)
            {
                _failure ??= $"exception in Feed: {ex.GetType().Name}: {ex.Message}";
            }

            _lastData = _clock.ElapsedMilliseconds;
            _dirty = true;
        }
    }

    /// <summary>The frame's end after 100 ms of quiet, as the window's quiet timer does.</summary>
    private void Frame()
    {
        lock (_gate)
        {
            if (!_dirty || _clock.ElapsedMilliseconds - _lastData < QuietMs)
            {
                return;
            }

            _dirty = false;
            try
            {
                _model.EndFrame();
            }
            catch (Exception ex)
            {
                _failure ??= $"exception in EndFrame: {ex.GetType().Name}: {ex.Message}";
            }
        }
    }

    private void Write(string text) => _host!.Write(text);

    private bool WaitFor(string what, int timeoutMs, Func<bool> condition)
    {
        var until = _clock.ElapsedMilliseconds + timeoutMs;
        while (_clock.ElapsedMilliseconds < until)
        {
            lock (_gate)
            {
                if (_failure is not null)
                {
                    return false;
                }

                if (!_dirty && condition())
                {
                    return true;
                }
            }

            Thread.Sleep(50);
        }

        Fail($"timed out waiting for {what} after {timeoutMs / 1000} s");
        return false;
    }

    private void Step(string text) => _steps.Add($"{text} ({_clock.Elapsed.TotalSeconds:0.0} s)");

    private void Fail(string text)
    {
        lock (_gate)
        {
            _failure ??= text;
        }
    }
}
