// AxitDrive: runs a script of user steps against Axit's windows through their automation channel
// (docs/axit/automation.md, tier 2). The windows are started on a desktop of the runner's own ("axit-tests"), so that
// they never take the user's keyboard or foreground; the keys go into each window's own message queue.
//
// Usage:
//   AxitDrive <script> [--exe <Axit.exe>] [--keep]
//   (the default exe is the scratch build next to this tool's output, or src\Axit\bin\Release; {temp} in a script is
//   the run's folder, %TEMP%\axit-drive, made new for each run and removed afterwards unless --keep)
//
// Script steps, one per line, '#' starts a comment:
//   mkdir <name>              a fresh folder {temp}\<name>
//   start <arguments>         Axit.exe with these arguments, on the test desktop; the window becomes the current one
//   use <n>                   the n-th window started (from 1) becomes the current one
//   keys <key> ...            key presses: ctrl+shift+p, escape, down, enter, f1, alt+f4 ...
//   type <text>               characters into the focused control
//   menu <name>               a menu command by the command palette's name ("Force Claude to restart")
//   said                      prints what the window announced since the last look
//   expect said <text>        waits up to 60 s until an announcement holds the text
//   expect focus <text>       waits up to 10 s until the focused control's name or value holds the text
//   expect state <text>       waits up to 10 s until the title, the notice or the status bar holds the text
//   try said <text>           as expect said, up to 15 s, and never fails: it records whether the text came
//   iffound <step>            the step, only when the last try found its text (a trust prompt that may not come)
//   wait <ms>                 waits
//   close                     closes the current window as Alt+F4 does

using System.Diagnostics;
using System.IO.Pipes;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;

namespace AxitDrive;

internal static class Program
{
    private static int Main(string[] args)
    {
        if (args.Length == 0)
        {
            Console.Error.WriteLine("Usage: AxitDrive <script> [--exe <Axit.exe>] [--keep]");
            return 2;
        }

        Console.OutputEncoding = Encoding.UTF8;
        var script = args[0];
        string? exe = null;
        var keep = false;
        for (var i = 1; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--exe": exe = args[++i]; break;
                case "--keep": keep = true; break;
                default:
                    Console.Error.WriteLine($"Unknown option: {args[i]}");
                    return 2;
            }
        }

        exe ??= Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "src", "Axit", "bin", "Release", "net10.0-windows", "Axit.exe");
        exe = Path.GetFullPath(exe);
        if (!File.Exists(exe))
        {
            Console.Error.WriteLine($"Axit.exe not found: {exe}");
            return 2;
        }

        var temp = Path.Combine(Path.GetTempPath(), "axit-drive");
        if (Directory.Exists(temp))
        {
            Directory.Delete(temp, recursive: true);
        }

        Directory.CreateDirectory(temp);
        using var run = new Run(exe, temp);
        var failed = 0;
        var number = 0;
        foreach (var raw in File.ReadAllLines(script))
        {
            number++;
            var line = raw.Trim();
            if (line.Length == 0 || line.StartsWith('#'))
            {
                continue;
            }

            line = line.Replace("{temp}", temp);
            var (ok, detail) = run.Step(line);
            Console.WriteLine($"{(ok ? "ok  " : "FAIL")} {number}: {raw.Trim()}{(detail is null ? string.Empty : "  -> " + detail)}");
            if (!ok)
            {
                failed++;
                break;
            }
        }

        run.Dispose();
        if (!keep)
        {
            try
            {
                Directory.Delete(temp, recursive: true);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                // A process still closing holds its folder; the temp folder is Windows' to clean.
            }
        }

        Console.WriteLine(failed == 0 ? "PASS" : "FAILED");
        return failed == 0 ? 0 : 1;
    }
}

/// <summary>The windows of one run, on the test desktop, each with its pipe.</summary>
internal sealed class Run(string exe, string temp) : IDisposable
{
    private const string DesktopName = "axit-tests";
    private readonly List<Window> _windows = [];
    private readonly nint _desktop = Native.CreateDesktopW(DesktopName, 0, 0, 0, 0x10000000, 0);
    private Window? _current;
    private bool _found;

    public (bool Ok, string? Detail) Step(string line)
    {
        var space = line.IndexOf(' ');
        var verb = space < 0 ? line : line[..space];
        var rest = space < 0 ? string.Empty : line[(space + 1)..];
        switch (verb)
        {
            case "mkdir":
                Directory.CreateDirectory(Path.Combine(temp, rest));
                return (true, null);
            case "start":
                _current = Window.Start(exe, rest, DesktopName);
                _windows.Add(_current);
                return (true, $"process {_current.ProcessId}");
            case "use":
                _current = _windows[int.Parse(rest) - 1];
                return (true, null);
            case "wait":
                Thread.Sleep(int.Parse(rest));
                return (true, null);
            case "said":
                _current!.Heard.AddRange(_current.Said());
                var heard = string.Join(" | ", _current.Heard);
                _current.Heard.Clear();
                return (true, heard);
            case "expect":
                return Expect(rest, 0);
            case "try":
                var (found, seen) = Expect(rest, 15_000);
                _found = found;
                return (true, found ? "found: " + seen : "not found");
            case "iffound":
                return _found ? Step(rest) : (true, "skipped");
            case "keys" or "type" or "menu" or "close":
                var answer = _current!.Send(line);
                return (!answer.Contains("\"error\""), answer.Contains("\"error\"") ? answer : null);
            default:
                return (false, $"unknown step {verb}");
        }
    }

    private (bool, string?) Expect(string rest, int timeoutMs)
    {
        var space = rest.IndexOf(' ');
        var what = rest[..space];
        var text = rest[(space + 1)..];
        var window = _current!;
        var timeout = timeoutMs > 0 ? timeoutMs : what == "said" ? 60_000 : 10_000;
        var clock = Stopwatch.StartNew();
        var seen = new List<string>();
        while (clock.ElapsedMilliseconds < timeout)
        {
            switch (what)
            {
                case "said":
                    // What was said stays queued until an expectation matches it, which takes it and what came before.
                    window.Heard.AddRange(window.Said());
                    var at = window.Heard.FindIndex(said => said.Contains(text, StringComparison.OrdinalIgnoreCase));
                    if (at >= 0)
                    {
                        var taken = window.Heard.GetRange(0, at + 1);
                        window.Heard.RemoveRange(0, at + 1);
                        return (true, $"{clock.ElapsedMilliseconds} ms: {string.Join(" | ", taken)}");
                    }

                    seen.Clear();
                    seen.AddRange(window.Heard);
                    break;
                case "focus":
                case "state":
                    var answer = window.Send(what);
                    if (answer.Contains(text, StringComparison.OrdinalIgnoreCase))
                    {
                        return (true, answer);
                    }

                    seen.Clear();
                    seen.Add(answer);
                    break;
                default:
                    return (false, $"unknown expectation {what}");
            }

            Thread.Sleep(100);
        }

        return (false, $"not within {timeout / 1000} s; seen: {string.Join(" | ", seen)}");
    }

    public void Dispose()
    {
        foreach (var window in _windows)
        {
            window.Dispose();
        }

        _windows.Clear();
        if (_desktop != 0)
        {
            Native.CloseDesktop(_desktop);
        }
    }
}

/// <summary>One Axit process on the test desktop and its automation pipe.</summary>
internal sealed class Window : IDisposable
{
    private readonly Process _process;
    private readonly NamedPipeClientStream _pipe;
    private readonly StreamReader _reader;
    private readonly StreamWriter _writer;

    private Window(Process process)
    {
        _process = process;
        _pipe = new NamedPipeClientStream(".", $"axit-auto-{process.Id}", PipeDirection.InOut);
        // The window opens its pipe when it first shows.
        _pipe.Connect(30_000);
        _reader = new StreamReader(_pipe, new UTF8Encoding(false));
        _writer = new StreamWriter(_pipe, new UTF8Encoding(false)) { AutoFlush = true };
    }

    public int ProcessId => _process.Id;

    /// <summary>What the window said that no expectation has taken yet, oldest first.</summary>
    public List<string> Heard { get; } = [];

    public static Window Start(string exe, string arguments, string desktop)
    {
        Environment.SetEnvironmentVariable("AXIT_AUTOMATION", "1");
        // The run's own settings, logs and recently closed list, never the user's (AppPaths).
        Environment.SetEnvironmentVariable("AXIT_HOME", Path.Combine(Path.GetTempPath(), "axit-drive", "home"));
        var startup = new Native.STARTUPINFO { cb = Marshal.SizeOf<Native.STARTUPINFO>(), lpDesktop = "WinSta0\\" + desktop };
        var commandLine = new StringBuilder($"\"{exe}\" {arguments}");
        if (!Native.CreateProcessW(null, commandLine, 0, 0, false, 0, 0, null, ref startup, out var info))
        {
            throw new InvalidOperationException($"CreateProcess failed: {Marshal.GetLastWin32Error()}");
        }

        Native.CloseHandle(info.hThread);
        Native.CloseHandle(info.hProcess);
        return new Window(Process.GetProcessById(info.dwProcessId));
    }

    public string Send(string command)
    {
        _writer.WriteLine(command);
        return _reader.ReadLine() ?? "{\"error\":\"the window closed\"}";
    }

    public IEnumerable<string> Said()
    {
        using var document = JsonDocument.Parse(Send("said"));
        return document.RootElement.ValueKind == JsonValueKind.Array
            ? document.RootElement.EnumerateArray().Select(item => (item.GetProperty("interrupt").GetBoolean() ? "! " : string.Empty) + item.GetProperty("text").GetString()).ToList()
            : [];
    }

    public void Dispose()
    {
        try
        {
            if (!_process.HasExited)
            {
                Send("close");
                if (!_process.WaitForExit(5000))
                {
                    _process.Kill(entireProcessTree: true);
                }
            }
        }
        catch (Exception ex) when (ex is IOException or InvalidOperationException)
        {
            // Gone already, or its pipe with it.
            try
            {
                _process.Kill(entireProcessTree: true);
            }
            catch (Exception) when (_process.HasExited)
            {
            }
        }

        _pipe.Dispose();
        _process.Dispose();
    }
}

internal static class Native
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct STARTUPINFO
    {
        public int cb;
        public string? lpReserved;
        public string? lpDesktop;
        public string? lpTitle;
        public int dwX, dwY, dwXSize, dwYSize, dwXCountChars, dwYCountChars, dwFillAttribute, dwFlags;
        public short wShowWindow, cbReserved2;
        public nint lpReserved2, hStdInput, hStdOutput, hStdError;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct PROCESS_INFORMATION
    {
        public nint hProcess, hThread;
        public int dwProcessId, dwThreadId;
    }

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    public static extern nint CreateDesktopW(string name, nint device, nint devmode, int flags, uint access, nint attributes);

    [DllImport("user32.dll")]
    public static extern bool CloseDesktop(nint desktop);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    public static extern bool CreateProcessW(string? application, StringBuilder commandLine, nint processAttributes, nint threadAttributes,
        bool inheritHandles, int flags, nint environment, string? directory, ref STARTUPINFO startup, out PROCESS_INFORMATION info);

    [DllImport("kernel32.dll")]
    public static extern bool CloseHandle(nint handle);
}
