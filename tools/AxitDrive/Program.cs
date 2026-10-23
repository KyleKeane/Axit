// AxitDrive: runs scripts of user steps against Axit's windows through their automation channel
// (docs/axit/automation.md, tier 2). The windows are started on a desktop of the runner's own ("axit-tests"), so that
// they never take the user's keyboard or foreground; the keys go into each window's own message queue, the sounds are
// recorded and not played, and the windows keep their settings and logs in the run's own folder (AXIT_HOME).
//
// Usage:
//   AxitDrive <script or folder of scripts> [--exe <Axit.exe> | --build] [--report <file.md>] [--keep]
//   --build      builds src\Axit in Release into the run's own folder first (the running app's bin is never touched)
//   --exe        the Axit.exe to drive (default: src\Axit\bin\Release\net10.0-windows\Axit.exe)
//   --report     writes every script's steps and its whole timeline, with times, to a Markdown file
//   --keep       keeps %TEMP%\axit-drive\<process id>, the run's folder ({temp} in a script), with its logs
//   A folder runs every *.txt under it, one after another, each with fresh windows and a fresh {temp}; the exit code
//   is the number of scripts that failed.
//
// Script steps, one per line, '#' starts a comment:
//   mkdir <name>              a fresh folder {temp}\<name>
//   file <name> <text>        a file {temp}\<name> holding the text (\n for a new line)
//   start <arguments>         Axit.exe with these arguments, on the test desktop; the window becomes the current one
//   use <n>                   the n-th window started in the script (from 1) becomes the current one
//   keys <key> ...            key presses: ctrl+shift+p, escape, down, enter, f1, alt+f4 ...
//   type <text>               characters into the focused control
//   menu <name>               a menu command by the command palette's name ("Force Claude to restart")
//   said                      prints the timeline since the last look (! interrupting, ♪ sound, → focus, · value)
//   text | list | commands | state | focus   prints the focused text's lines, the focused list's items, the menus'
//                             commands, the window's state (title, notice and its text, status bar), the focus
//   expect said <text>        waits up to 60 s until the timeline holds the text (an announcement, "♪ Edge" for a
//                             sound, "→ Conversation" for a focus move); takes it and what came before it
//   expect focus|state|list|text|commands <text>   waits up to 10 s until that answer holds the text
//   refuse said <text>        fails when the timeline holds the text within 2 s (what must not be said)
//   expect quiet <ms>         fails when anything is announced within the time (sounds and values aside)
//   try said <text>           as expect said, up to 15 s, and never fails: it records whether the text came
//   iffound <step>            the step, only when the last try found its text (a trust prompt that may not come)
//   wait <ms>                 waits
//   close                     closes the current window as Alt+F4 does
// After the last step every window is closed, and the run fails when a window's process ended on its own or a log of
// the run holds an ERROR line.

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
            Console.Error.WriteLine("Usage: AxitDrive <script or folder> [--exe <Axit.exe> | --build] [--report <file.md>] [--keep]");
            return 2;
        }

        Console.OutputEncoding = Encoding.UTF8;
        var target = args[0];
        string? exe = null;
        string? reportPath = null;
        var keep = false;
        var build = false;
        for (var i = 1; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--exe": exe = args[++i]; break;
                case "--build": build = true; break;
                case "--report": reportPath = args[++i]; break;
                case "--keep": keep = true; break;
                default:
                    Console.Error.WriteLine($"Unknown option: {args[i]}");
                    return 2;
            }
        }

        var root = Path.Combine(Path.GetTempPath(), "axit-drive", Environment.ProcessId.ToString());
        if (Directory.Exists(root))
        {
            Directory.Delete(root, recursive: true);
        }

        Directory.CreateDirectory(root);
        var repository = FindRepository();
        if (build)
        {
            exe = Build(repository, Path.Combine(root, "build"));
            if (exe is null)
            {
                return 2;
            }
        }

        exe ??= Path.Combine(repository, "src", "Axit", "bin", "Release", "net10.0-windows", "Axit.exe");
        exe = Path.GetFullPath(exe);
        if (!File.Exists(exe))
        {
            Console.Error.WriteLine($"Axit.exe not found: {exe}. Use --build, or --exe with a built Axit.exe.");
            return 2;
        }

        var scripts = Directory.Exists(target)
            ? Directory.GetFiles(target, "*.txt", SearchOption.AllDirectories).OrderBy(path => path, StringComparer.OrdinalIgnoreCase).ToList()
            : [target];
        var report = new StringBuilder($"# AxitDrive report\n\n{DateTime.Now:yyyy-MM-dd HH:mm}, {exe}\n\n");
        var failed = new List<string>();
        var clock = Stopwatch.StartNew();
        foreach (var script in scripts)
        {
            var name = Path.GetRelativePath(Directory.Exists(target) ? target : Path.GetDirectoryName(Path.GetFullPath(target))!, script);
            Console.WriteLine($"== {name}");
            var temp = Path.Combine(root, Path.GetFileNameWithoutExtension(script) + "-" + scripts.IndexOf(script));
            Directory.CreateDirectory(temp);
            var (ok, log) = RunScript(script, exe, temp);
            report.Append($"## {name}: {(ok ? "PASS" : "FAIL")}\n\n```\n{log}```\n\n");
            if (!ok)
            {
                failed.Add(name);
            }
        }

        if (reportPath is not null)
        {
            File.WriteAllText(reportPath, report.ToString(), new UTF8Encoding(false));
        }

        if (!keep)
        {
            try
            {
                Directory.Delete(root, recursive: true);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                // A process still closing holds its folder; the temp folder is Windows' to clean.
            }
        }

        Console.WriteLine();
        Console.WriteLine(failed.Count == 0
            ? $"PASS: {scripts.Count} script(s), {clock.Elapsed.TotalSeconds:0} s"
            : $"FAILED: {failed.Count} of {scripts.Count} script(s): {string.Join(", ", failed)}");
        return failed.Count;
    }

    /// <summary>One script with its own windows and folder; the console gets each step, the report the steps and the timeline.</summary>
    private static (bool Ok, string Log) RunScript(string script, string exe, string temp)
    {
        var log = new StringBuilder();
        var ok = true;
        var run = new Run(exe, temp);
        try
        {
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
                (bool Ok, string? Detail) result;
                try
                {
                    result = run.Step(line);
                }
                catch (Exception ex) when (ex is IOException or InvalidOperationException or TimeoutException or JsonException or ArgumentException)
                {
                    result = (false, $"{ex.GetType().Name}: {ex.Message}");
                }

                var text = $"{(result.Ok ? "ok  " : "FAIL")} {number}: {raw.Trim()}{(result.Detail is null ? string.Empty : "  -> " + result.Detail)}";
                Console.WriteLine(text);
                log.AppendLine(text);
                if (!result.Ok)
                {
                    ok = false;
                    log.AppendLine("The timeline so far:");
                    log.Append(run.Timeline());
                    Console.Write(run.LastOfTimeline(20));
                    break;
                }
            }
        }
        finally
        {
            foreach (var problem in run.Finish())
            {
                ok = false;
                var text = "FAIL after the steps: " + problem;
                Console.WriteLine(text);
                log.AppendLine(text);
            }
        }

        if (ok)
        {
            log.AppendLine("The timeline:");
            log.Append(run.Timeline());
        }

        return (ok, log.ToString());
    }

    private static string FindRepository()
    {
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
        {
            if (File.Exists(Path.Combine(dir.FullName, "Axit.sln")))
            {
                return dir.FullName;
            }
        }

        return Environment.CurrentDirectory;
    }

    /// <summary>src\Axit in Release into a folder of the run's, never into the running app's bin.</summary>
    private static string? Build(string repository, string output)
    {
        Console.WriteLine($"Building Axit into {output} ...");
        var start = new ProcessStartInfo("dotnet", ["build", Path.Combine(repository, "src", "Axit", "Axit.csproj"), "-c", "Release", "-o", output, "--nologo", "-v", "q"])
        {
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        using var process = Process.Start(start)!;
        var output1 = process.StandardOutput.ReadToEndAsync();
        var output2 = process.StandardError.ReadToEndAsync();
        process.WaitForExit();
        if (process.ExitCode != 0)
        {
            Console.Error.WriteLine(output1.Result + output2.Result);
            return null;
        }

        return Path.Combine(output, "Axit.exe");
    }
}

/// <summary>One event of a window's timeline: when, and what (an announcement, a sound, a focus move, a value change).</summary>
internal sealed record Event(long Ms, string Kind, string Text, bool Interrupt)
{
    /// <summary>As a line of the report and as the text an expectation matches: "! " an interrupting announcement, "♪ " a sound, "→ " a focus move, "· " a value.</summary>
    public string Spoken => Kind switch
    {
        "say" => (Interrupt ? "! " : string.Empty) + Text,
        "sound" => "♪ " + Text,
        "focus" => "→ " + Text,
        _ => "· " + Text,
    };
}

/// <summary>The windows of one script, on the test desktop, each with its pipe.</summary>
internal sealed class Run(string exe, string temp)
{
    private const string DesktopName = "axit-tests";
    private readonly List<Window> _windows = [];
    private readonly nint _desktop = Native.CreateDesktopW(DesktopName, 0, 0, 0, 0x10000000, 0);
    private readonly string _home = Path.Combine(temp, "home");
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
            case "file":
                var at = rest.IndexOf(' ');
                var path = Path.Combine(temp, at < 0 ? rest : rest[..at]);
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                File.WriteAllText(path, at < 0 ? string.Empty : rest[(at + 1)..].Replace("\\n", "\r\n"), new UTF8Encoding(false));
                return (true, null);
            case "start":
                _current = Window.Start(exe, rest, DesktopName, _home);
                _windows.Add(_current);
                return (true, $"process {_current.ProcessId}");
            case "use":
                _current = _windows[int.Parse(rest) - 1];
                return (true, null);
            case "wait":
                Thread.Sleep(int.Parse(rest));
                return (true, null);
            case "said":
                return (true, string.Join(" | ", Current.TakeAll().Select(e => e.Spoken)));
            case "expect":
                return Expect(rest, 0);
            case "refuse":
                return Refuse(rest);
            case "try":
                var (found, seen) = Expect(rest, 15_000);
                _found = found;
                return (true, found ? "found: " + seen : "not found");
            case "iffound":
                return _found ? Step(rest) : (true, "skipped");
            case "text" or "list" or "commands" or "state" or "focus":
                var answer = Current.Send(verb);
                return (!answer.Contains("\"error\""), answer);
            case "keys" or "type" or "menu" or "close":
                var reply = Current.Send(line);
                return (!reply.Contains("\"error\""), reply.Contains("\"error\"") ? reply : null);
            default:
                return (false, $"unknown step {verb}");
        }
    }

    private Window Current => _current ?? throw new InvalidOperationException("no window started yet");

    private (bool, string?) Expect(string rest, int timeoutMs)
    {
        var space = rest.IndexOf(' ');
        var what = space < 0 ? rest : rest[..space];
        var text = space < 0 ? string.Empty : rest[(space + 1)..];
        var window = Current;
        if (what == "quiet")
        {
            Thread.Sleep(int.Parse(text));
            var said = window.Pending().Where(e => e.Kind == "say").ToList();
            window.TakeAll();
            return (said.Count == 0, said.Count == 0 ? null : "said: " + string.Join(" | ", said.Select(e => e.Spoken)));
        }

        var timeout = timeoutMs > 0 ? timeoutMs : what == "said" ? 60_000 : 10_000;
        var clock = Stopwatch.StartNew();
        string seen = string.Empty;
        while (clock.ElapsedMilliseconds < timeout)
        {
            if (what == "said")
            {
                if (window.Take(text) is { } taken)
                {
                    return (true, $"{clock.ElapsedMilliseconds} ms: {string.Join(" | ", taken.Select(e => e.Spoken))}");
                }

                seen = string.Join(" | ", window.Pending().Select(e => e.Spoken));
            }
            else if (what is "focus" or "state" or "list" or "text" or "commands")
            {
                var answer = window.Send(what);
                if (answer.Contains(text, StringComparison.OrdinalIgnoreCase) || JsonUnescaped(answer).Contains(text, StringComparison.OrdinalIgnoreCase))
                {
                    return (true, answer);
                }

                seen = answer;
            }
            else
            {
                return (false, $"unknown expectation {what}");
            }

            Thread.Sleep(100);
        }

        return (false, $"not within {timeout / 1000} s; seen: {seen}");
    }

    private (bool, string?) Refuse(string rest)
    {
        var space = rest.IndexOf(' ');
        var text = rest[(space + 1)..];
        Thread.Sleep(2000);
        var bad = Current.Pending().FirstOrDefault(e => e.Spoken.Contains(text, StringComparison.OrdinalIgnoreCase));
        return bad is null ? (true, null) : (false, $"said at {bad.Ms} ms: {bad.Spoken}");
    }

    /// <summary>A JSON answer with its escapes read, so that "·" or "\\" in it match the text a script holds.</summary>
    private static string JsonUnescaped(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<JsonElement>(json).ToString();
        }
        catch (JsonException)
        {
            return json;
        }
    }

    /// <summary>Every window's whole timeline, for the report.</summary>
    public string Timeline()
    {
        var text = new StringBuilder();
        foreach (var window in _windows)
        {
            text.AppendLine($"window {_windows.IndexOf(window) + 1} (process {window.ProcessId}):");
            foreach (var e in window.Everything())
            {
                text.AppendLine($"  {e.Ms,7} ms  {e.Spoken}");
            }
        }

        return text.ToString();
    }

    public string LastOfTimeline(int count)
    {
        if (_current is null)
        {
            return string.Empty;
        }

        var text = new StringBuilder("  the last of the timeline:\n");
        var all = _current.Everything();
        foreach (var e in all.Skip(Math.Max(0, all.Count - count)))
        {
            text.AppendLine($"  {e.Ms,7} ms  {e.Spoken}");
        }

        return text.ToString();
    }

    /// <summary>Closes every window and says what went wrong outside the steps: a process that ended by itself, an ERROR in a log.</summary>
    public List<string> Finish()
    {
        var problems = new List<string>();
        foreach (var window in _windows)
        {
            if (window.EndedByItself)
            {
                problems.Add($"window {_windows.IndexOf(window) + 1} (process {window.ProcessId}) ended by itself, exit code {window.ExitCode}");
            }

            window.Dispose();
        }

        _windows.Clear();
        if (_desktop != 0)
        {
            Native.CloseDesktop(_desktop);
        }

        var logs = Path.Combine(_home, "Axit", "logs");
        if (Directory.Exists(logs))
        {
            foreach (var file in Directory.GetFiles(logs, "*.log"))
            {
                foreach (var line in File.ReadAllLines(file).Where(line => line.Contains(" ERROR ", StringComparison.Ordinal)))
                {
                    problems.Add($"{Path.GetFileName(file)}: {line}");
                }
            }
        }

        return problems;
    }
}

/// <summary>One Axit process on the test desktop, its automation pipe and its timeline.</summary>
internal sealed class Window : IDisposable
{
    private readonly Process _process;
    private readonly NamedPipeClientStream _pipe;
    private readonly StreamReader _reader;
    private readonly StreamWriter _writer;
    private readonly List<Event> _all = [];
    private int _taken;
    private bool _closing;

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

    public bool EndedByItself => !_closing && _process.HasExited;

    public int ExitCode => _process.HasExited ? _process.ExitCode : 0;

    public static Window Start(string exe, string arguments, string desktop, string home)
    {
        Environment.SetEnvironmentVariable("AXIT_AUTOMATION", "1");
        // The run's own settings, logs and recently closed list, never the user's (AppPaths).
        Environment.SetEnvironmentVariable("AXIT_HOME", home);
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
        if (command == "close")
        {
            _closing = true;
        }

        _writer.WriteLine(command);
        return _reader.ReadLine() ?? "{\"error\":\"the window closed\"}";
    }

    /// <summary>The events not yet taken by an expectation, after fetching what the window has recorded since the last look.</summary>
    public List<Event> Pending()
    {
        Fetch();
        return _all.GetRange(_taken, _all.Count - _taken);
    }

    /// <summary>The first pending event that holds the text, with every pending event before it; null when none does.</summary>
    public List<Event>? Take(string text)
    {
        var pending = Pending();
        var at = pending.FindIndex(e => e.Spoken.Contains(text, StringComparison.OrdinalIgnoreCase));
        if (at < 0)
        {
            return null;
        }

        _taken += at + 1;
        return pending.GetRange(0, at + 1);
    }

    public List<Event> TakeAll()
    {
        var pending = Pending();
        _taken = _all.Count;
        return pending;
    }

    public List<Event> Everything()
    {
        try
        {
            Fetch();
        }
        catch (IOException)
        {
            // The window is gone; what was fetched stays.
        }

        return _all;
    }

    private void Fetch()
    {
        if (_process.HasExited)
        {
            return;
        }

        using var document = JsonDocument.Parse(Send("said"));
        if (document.RootElement.ValueKind != JsonValueKind.Array)
        {
            return;
        }

        foreach (var record in document.RootElement.EnumerateArray())
        {
            var item = record.GetProperty("item");
            _all.Add(new Event(record.GetProperty("t").GetInt64(), item.GetProperty("kind").GetString()!, item.GetProperty("text").GetString() ?? string.Empty, item.GetProperty("interrupt").GetBoolean()));
        }
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
