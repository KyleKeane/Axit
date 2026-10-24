// AxAutomate: runs scripts of user steps against Axit's windows through their automation channel
// (docs/axit/automation.md, tier 2). The windows are started on a desktop of the runner's own ("axit-tests"), so that
// they never take the user's keyboard or foreground; the keys go into each window's own message queue, the sounds are
// recorded and not played, and the windows keep their settings and logs in the run's own folder (AXIT_HOME).
//
// Usage:
//   AxAutomate <script or folder of scripts> [--exe <Axit.exe> | --build] [--report <file.md>] [--keep]
//   --build      builds src\Axit in Release into the run's own folder first (the running app's bin is never touched)
//   --exe        the Axit.exe to drive (default: src\Axit\bin\Release\net10.0-windows\Axit.exe)
//   --report     writes every script's steps and its whole timeline, with times, to a Markdown file
//   --nvda       the NVDA tier (tier 3): the windows open on the user's own desktop and come to the front, the keys go
//                through SendInput as a keyboard's do, and NVDA's speech is read from its capture add-on
//                (tools/nvda-capture). It takes over the machine: only at the user's word, with the user away.
//   --keep       keeps %TEMP%\axit-automate\<process id>, the run's folder ({temp} in a script), with its logs
//   A folder runs every *.txt under it, one after another, each with fresh windows and a fresh {temp}; the exit code
//   is the number of scripts that failed.
//
// Script steps, one per line, '#' starts a comment:
//   mkdir <name>              a fresh folder {temp}\<name>
//   file <name> <text>        a file {temp}\<name> holding the text (\n for a new line); "<name>" in quotes with a space
//   silence <name> <ms>       a silent WAV file {temp}\<name> of that length, for axMedia without a sound
//   run <command line>        a command through cmd.exe in {temp}, waited for up to 60 s (git init, git commit ...)
//   start <arguments>         Axit.exe with these arguments, on the test desktop; the window becomes the current one
//   launch <arguments>        Axit.exe on the test desktop without waiting for a window: a start that hands over to an
//                             open window (axMedia, axPreview) and ends; waits up to 30 s for the process to end
//   adopt                     waits up to 15 s for an Axit window the app itself started (Ctrl+N, New window, Show
//                             in) and makes it the current one; it is closed at the end like the others
//   use <n>                   the n-th window started in the script (from 1) becomes the current one
//   keys <key> ...            key presses: ctrl+shift+p, escape, down, enter, f1, alt+f4 ...
//   type <text>               characters into the focused control
//   menu <name>               a menu command by the command palette's name ("Force Claude to restart")
//   said                      prints the timeline since the last look (! interrupting, ♪ sound, → focus, · value)
//   text | list | commands | state | focus   prints the focused text's lines, the focused list's items, the menus'
//                             commands, the window's state (title, notice and its text, status bar), the focus
//   check commands            fails when the window's command list has a problem: a key the F1 text does not name, a
//                             command that never runs because an earlier one takes its key everywhere (app-kit AK-2.4)
//   expect said <text>        waits up to 60 s until the timeline holds the text (an announcement, "♪ Edge" for a
//                             sound, "→ Conversation" for a focus move); takes it and what came before it
//   expect focus|state|list|text|commands <text>   waits up to 10 s until that answer holds the text
//   refuse said <text>        fails when the timeline holds the text within 2 s (what must not be said)
//   refuse focus|state|list|text|commands <text>   fails when that answer holds the text after 2 s
//   expect closed             waits up to 10 s for the current window's process to end (a close without a question)
//   allow error <text>        a log's ERROR line that holds the text does not fail the run (a refused file the spec
//                             wants reported)
//   expect quiet <ms>         fails when anything is announced within the time (sounds and values aside)
//   try said <text>           as expect said, up to 15 s, and never fails: it records whether the text came
//   expect spoken <text>      (--nvda) waits up to 15 s until NVDA has spoken the text; takes it and what came before
//   refuse spoken <text>      (--nvda) fails when NVDA speaks the text within 2 s
//   spoken                    (--nvda) prints what NVDA spoke since the last look, "[cancel]" where it stopped speech
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

namespace AxAutomate;

internal static class Program
{
    private static int Main(string[] args)
    {
        if (args.Length == 0)
        {
            Console.Error.WriteLine("Usage: AxAutomate <script or folder> [--exe <Axit.exe> | --build] [--report <file.md>] [--keep]");
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
                case "--nvda": Run.Nvda = true; break;
                default:
                    Console.Error.WriteLine($"Unknown option: {args[i]}");
                    return 2;
            }
        }

        // The process id and the time: a reused process id never meets an old folder (git's read-only objects in it
        // stopped a run once).
        var root = Path.Combine(Path.GetTempPath(), "axit-automate", $"{Environment.ProcessId}-{DateTime.Now:HHmmss}");
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
        if (Run.Nvda)
        {
            // NVDA records only while this file exists (tools/nvda-capture); the log starts empty for the run.
            File.Delete(Speech.LogPath);
            File.WriteAllText(Speech.FlagPath, string.Empty);
        }

        var report = new StringBuilder($"# AxAutomate report\n\n{DateTime.Now:yyyy-MM-dd HH:mm}, {exe}\n\n");
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

        if (Run.Nvda)
        {
            File.Delete(Speech.FlagPath);
            File.Delete(Speech.LogPath);
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

                // {temp}, and \uXXXX escapes, which a script may use for a character it cannot type (· + > ").
                line = System.Text.RegularExpressions.Regex.Replace(line.Replace("{temp}", temp), @"\\u([0-9A-Fa-f]{4})", match => ((char)Convert.ToInt32(match.Groups[1].Value, 16)).ToString());
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
    /// <summary>A desktop of the run's own: runs side by side never share one (a window closing in another run could activate one of this run's).</summary>
    private static readonly string DesktopName = $"axit-tests-{Environment.ProcessId}";

    /// <summary>The NVDA tier: the user's own desktop, the foreground, real keyboard input, NVDA's speech.</summary>
    public static bool Nvda { get; set; }

    private readonly List<Window> _windows = [];
    private readonly nint _desktop = Nvda ? 0 : Native.CreateDesktopW(DesktopName, 0, 0, 0, 0x10000000, 0);
    private readonly Speech _speech = new();
    private readonly List<string> _allowed = [];
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
                // The path ends at the first space, or, in quotes, at the closing quote: axTask's items are named
                // after their titles ("Fix the tap.json").
                var quoted = rest.StartsWith('"') && rest.IndexOf('"', 1) > 0;
                var at = quoted ? rest.IndexOf('"', 1) + 1 : rest.IndexOf(' ');
                var name = quoted ? rest[1..(at - 1)] : at < 0 ? rest : rest[..at];
                var content = at < 0 || at >= rest.Length ? string.Empty : rest[(at + 1)..];
                var path = Path.Combine(temp, name);
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                File.WriteAllText(path, content.Replace("\\n", "\r\n"), new UTF8Encoding(false));
                return (true, null);
            case "silence":
                var parts = rest.Split(' ', 2);
                WriteSilence(Path.Combine(temp, parts[0]), int.Parse(parts[1]));
                return (true, null);
            case "run":
                return RunCommand(rest);
            case "start":
                _current = Window.Start(exe, rest, Nvda ? null : DesktopName, _home);
                _windows.Add(_current);
                _current.WaitForFocus();
                if (Nvda)
                {
                    Native.AllowSetForegroundWindow(_current.ProcessId);
                    _current.Send("activate");
                }

                return (true, $"process {_current.ProcessId}");
            case "use":
                _current = _windows[int.Parse(rest) - 1];
                return (true, null);
            case "launch":
                return Window.Launch(exe, rest, Nvda ? null : DesktopName, _home);
            case "adopt":
                return Adopt();
            case "allow":
                _allowed.Add(rest.StartsWith("error ", StringComparison.Ordinal) ? rest[6..] : rest);
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
            case "check" when rest == "commands":
                // The window's command list (docs/axit/app-kit.md AK-2.4): every key in the F1 text, none shadowed.
                var problems = Current.Send("problems");
                return (problems.Contains("\"problems\":[]", StringComparison.Ordinal), problems);
            case "spoken":
                return (true, string.Join(" | ", _speech.TakeAll()));
            case "keys" when Nvda:
                Native.Keys(rest);
                return (true, null);
            case "type" when Nvda:
                Native.Type(rest);
                return (true, null);
            case "keys":
                // One press at a time: each goes to the control that has the focus after the one before.
                foreach (var press in rest.Split(' ', StringSplitOptions.RemoveEmptyEntries))
                {
                    if (press.Equals("alt+f4", StringComparison.OrdinalIgnoreCase))
                    {
                        Current.Closing();
                    }

                    var pressed = Current.Send("keys " + press);
                    if (pressed.Contains("\"error\""))
                    {
                        return (false, pressed);
                    }

                    Thread.Sleep(60);
                }

                return (true, null);
            case "type" or "menu" or "close":
                var reply = Current.Send(line);
                return (!reply.Contains("\"error\""), reply.Contains("\"error\"") ? reply : null);
            default:
                return (false, $"unknown step {verb}");
        }
    }

    /// <summary>A command through cmd.exe in the run's folder; its output is the step's detail.</summary>
    private (bool, string?) RunCommand(string commandLine)
    {
        var start = new ProcessStartInfo("cmd.exe", "/d /s /c \"" + commandLine + "\"")
        {
            WorkingDirectory = temp,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
        };
        using var process = Process.Start(start)!;
        var output = process.StandardOutput.ReadToEndAsync();
        var error = process.StandardError.ReadToEndAsync();
        if (!process.WaitForExit(60_000))
        {
            process.Kill(entireProcessTree: true);
            return (false, "did not finish within 60 s");
        }

        var text = (output.Result + error.Result).Trim().Replace("\r\n", " / ");
        return (process.ExitCode == 0, $"exit {process.ExitCode}: {text[..Math.Min(text.Length, 300)]}");
    }

    /// <summary>A silent 8 kHz mono 16-bit WAV file.</summary>
    private static void WriteSilence(string path, int milliseconds)
    {
        var samples = (int)(8000L * milliseconds / 1000);
        using var writer = new BinaryWriter(File.Create(path));
        writer.Write("RIFF"u8);
        writer.Write(36 + samples * 2);
        writer.Write("WAVEfmt "u8);
        writer.Write(16);
        writer.Write((short)1);
        writer.Write((short)1);
        writer.Write(8000);
        writer.Write(16000);
        writer.Write((short)2);
        writer.Write((short)16);
        writer.Write("data"u8);
        writer.Write(samples * 2);
        writer.Write(new byte[samples * 2]);
    }

    private Window Current => _current ?? throw new InvalidOperationException("no window started yet");

    private (bool, string?) Expect(string rest, int timeoutMs)
    {
        var space = rest.IndexOf(' ');
        var what = space < 0 ? rest : rest[..space];
        var text = space < 0 ? string.Empty : rest[(space + 1)..];
        var window = Current;
        if (what == "closed")
        {
            return Current.WaitClosed(10_000) ? (true, null) : (false, "the window is still open after 10 s");
        }

        if (what == "spoken")
        {
            var until = Stopwatch.StartNew();
            while (until.ElapsedMilliseconds < 15_000)
            {
                if (_speech.Take(text) is { } heard)
                {
                    return (true, string.Join(" | ", heard));
                }

                Thread.Sleep(100);
            }

            return (false, $"NVDA did not say it within 15 s; it said: {string.Join(" | ", _speech.Pending())}");
        }

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
                if (Matches(answer, text))
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
        var verb = rest[..space];
        if (verb is "focus" or "state" or "list" or "text" or "commands")
        {
            var answer = Current.Send(verb);
            return Matches(answer, text) ? (false, answer) : (true, null);
        }

        if (rest.StartsWith("spoken", StringComparison.Ordinal))
        {
            var spoken = _speech.Pending().FirstOrDefault(said => said.Contains(text, StringComparison.OrdinalIgnoreCase));
            return spoken is null ? (true, null) : (false, "NVDA said: " + spoken);
        }

        var bad = Current.Pending().FirstOrDefault(e => e.Spoken.Contains(text, StringComparison.OrdinalIgnoreCase));
        return bad is null ? (true, null) : (false, $"said at {bad.Ms} ms: {bad.Spoken}");
    }

    /// <summary>
    /// A JSON answer as plain text: every value, its escapes read, joined with " | ", the members as "name: value", so
    /// that a path with backslashes or quotes in it matches the text a script holds.
    /// </summary>
    private static string Flattened(string json)
    {
        try
        {
            var text = new StringBuilder();
            Flatten(JsonSerializer.Deserialize<JsonElement>(json), text);
            return text.ToString();
        }
        catch (JsonException)
        {
            return json;
        }

        static void Flatten(JsonElement element, StringBuilder text)
        {
            switch (element.ValueKind)
            {
                case JsonValueKind.Object:
                    foreach (var member in element.EnumerateObject())
                    {
                        text.Append(member.Name).Append(": ");
                        Flatten(member.Value, text);
                    }

                    break;
                case JsonValueKind.Array:
                    foreach (var item in element.EnumerateArray())
                    {
                        Flatten(item, text);
                    }

                    break;
                case JsonValueKind.String:
                    text.Append(element.GetString()).Append(" | ");
                    break;
                default:
                    text.Append(element.ToString()).Append(" | ");
                    break;
            }
        }
    }

    /// <summary>Whether an answer holds the text: as sent, with its quote and backslash escapes read, or flattened to its values.</summary>
    private static bool Matches(string answer, string text) =>
        answer.Contains(text, StringComparison.OrdinalIgnoreCase)
        || answer.Replace("\\\"", "\"").Replace("\\\\", "\\").Contains(text, StringComparison.OrdinalIgnoreCase)
        || Flattened(answer).Contains(text, StringComparison.OrdinalIgnoreCase);

    /// <summary>A window the app started (it inherits the run's automation variables): the newest Axit process with a pipe that is not one of the run's.</summary>
    private (bool, string?) Adopt()
    {
        var clock = Stopwatch.StartNew();
        var known = _windows.Select(window => window.ProcessId).ToHashSet();
        var name = Path.GetFileNameWithoutExtension(exe);
        while (clock.ElapsedMilliseconds < 15_000)
        {
            foreach (var process in Process.GetProcessesByName(name).OrderByDescending(p => p.Id))
            {
                if (known.Contains(process.Id) || Window.Attach(process) is not { } window)
                {
                    continue;
                }

                _windows.Add(window);
                _current = window;
                window.WaitForFocus();
                return (true, $"process {window.ProcessId}");
            }

            Thread.Sleep(200);
        }

        return (false, "no new Axit window within 15 s");
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
                foreach (var line in File.ReadAllLines(file).Where(line => line.Contains(" ERROR ", StringComparison.Ordinal) && !_allowed.Any(allowed => line.Contains(allowed, StringComparison.OrdinalIgnoreCase))))
                {
                    problems.Add($"{Path.GetFileName(file)}: {line}");
                }
            }
        }

        return problems;
    }
}

/// <summary>What NVDA spoke, from the capture add-on's log (tools/nvda-capture): "speak" lines, and "[cancel]" where NVDA cut its speech.</summary>
internal sealed class Speech
{
    public static readonly string FlagPath = Path.Combine(Path.GetTempPath(), "axit-nvda-capture.on");
    public static readonly string LogPath = Path.Combine(Path.GetTempPath(), "axit-nvda-speech.log");
    private readonly List<string> _all = [];
    private long _read;
    private int _taken;

    public List<string> Pending()
    {
        if (File.Exists(LogPath))
        {
            using var stream = new FileStream(LogPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
            stream.Seek(_read, SeekOrigin.Begin);
            using var reader = new StreamReader(stream, new UTF8Encoding(false));
            var text = reader.ReadToEnd();
            var complete = text.LastIndexOf('\n') + 1;
            _read += Encoding.UTF8.GetByteCount(text[..complete]);
            foreach (var line in text[..complete].Split('\n', StringSplitOptions.RemoveEmptyEntries))
            {
                var parts = line.TrimEnd('\r').Split('\t');
                _all.Add(parts.Length >= 3 && parts[1] == "cancel" ? "[cancel]" : parts.Length >= 3 ? parts[2] : line);
            }
        }

        return _all.GetRange(_taken, _all.Count - _taken);
    }

    public List<string>? Take(string text)
    {
        var pending = Pending();
        var at = pending.FindIndex(said => said.Contains(text, StringComparison.OrdinalIgnoreCase));
        if (at < 0)
        {
            return null;
        }

        _taken += at + 1;
        return pending.GetRange(0, at + 1);
    }

    public List<string> TakeAll()
    {
        var pending = Pending();
        _taken = _all.Count;
        return pending;
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

    private Window(Process process, int connectMs = 30_000)
    {
        _process = process;
        // CurrentUserOnly: the pipe must be the user's own, never one another program made under the same name.
        _pipe = new NamedPipeClientStream(".", $"axit-auto-{process.Id}", PipeDirection.InOut, PipeOptions.CurrentUserOnly);
        // The window opens its pipe when it first shows.
        _pipe.Connect(connectMs);
        _reader = new StreamReader(_pipe, new UTF8Encoding(false));
        _writer = new StreamWriter(_pipe, new UTF8Encoding(false)) { AutoFlush = true };
        _writer.WriteLine(Secret);
        if (_reader.ReadLine() is not { } answer || !answer.Contains("\"ok\":true", StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"The window of process {process.Id} did not accept the run's secret.");
        }
    }

    /// <summary>The run's secret, 256 random bits, made once per run of the runner.</summary>
    public static readonly string Secret = Convert.ToHexString(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32));

    public int ProcessId => _process.Id;

    public bool EndedByItself => !_closing && _process.HasExited;

    public string ExitCode
    {
        get
        {
            try
            {
                return _process.ExitCode.ToString();
            }
            catch (InvalidOperationException)
            {
                return "unknown";
            }
        }
    }

    /// <summary>A close the script asked for (Alt+F4): the process ending is no failure.</summary>
    public void Closing() => _closing = true;

    public bool WaitClosed(int milliseconds)
    {
        _closing = true;
        return _process.WaitForExit(milliseconds);
    }

    /// <summary>Waits up to 5 s for the window's first focus, so that the first keys go where a user's would; a window shown without activation has none, which is no error.</summary>
    public void WaitForFocus()
    {
        var clock = Stopwatch.StartNew();
        while (clock.ElapsedMilliseconds < 5000 && !_process.HasExited)
        {
            if (!Send("focus").Contains("\"name\":null", StringComparison.Ordinal))
            {
                return;
            }

            Thread.Sleep(100);
        }
    }

    /// <summary>An Axit process the run did not start, joined through its pipe; null when it has none (yet).</summary>
    public static Window? Attach(Process process)
    {
        try
        {
            return new Window(process, 500);
        }
        catch (Exception ex) when (ex is TimeoutException or IOException)
        {
            return null;
        }
    }

    /// <summary>A start that hands over to an open window and ends (axMedia, axPreview): run on the desktop and waited for.</summary>
    public static (bool, string?) Launch(string exe, string arguments, string? desktop, string home)
    {
        var id = Create(exe, arguments, desktop, home);
        try
        {
            using var process = Process.GetProcessById(id);
            return process.WaitForExit(30_000) ? (true, $"process {id} ended") : (false, $"process {id} still running after 30 s");
        }
        catch (ArgumentException)
        {
            return (true, $"process {id} ended");
        }
    }

    public static Window Start(string exe, string arguments, string? desktop, string home) =>
        new(Process.GetProcessById(Create(exe, arguments, desktop, home)));

    private static int Create(string exe, string arguments, string? desktop, string home)
    {
        // The run's secret (security audit S-11): the windows open their channel to a client that sends it first.
        Environment.SetEnvironmentVariable("AXIT_AUTOMATION", Secret);
        // The run's own settings, logs and recently closed list, never the user's (AppPaths).
        Environment.SetEnvironmentVariable("AXIT_HOME", home);
        var startup = new Native.STARTUPINFO { cb = Marshal.SizeOf<Native.STARTUPINFO>(), lpDesktop = desktop is null ? null : "WinSta0\\" + desktop };
        var commandLine = new StringBuilder($"\"{exe}\" {arguments}");
        if (!Native.CreateProcessW(null, commandLine, 0, 0, false, 0, 0, null, ref startup, out var info))
        {
            throw new InvalidOperationException($"CreateProcess failed: {Marshal.GetLastWin32Error()}");
        }

        Native.CloseHandle(info.hThread);
        Native.CloseHandle(info.hProcess);
        return info.dwProcessId;
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

    [DllImport("user32.dll")]
    public static extern bool AllowSetForegroundWindow(int processId);

    // ---- the NVDA tier's keyboard: SendInput, as a keyboard types ----

    private const int INPUT_KEYBOARD = 1;
    private const uint KEYEVENTF_KEYUP = 0x0002;
    private const uint KEYEVENTF_UNICODE = 0x0004;

    [StructLayout(LayoutKind.Sequential)]
    private struct KEYBDINPUT
    {
        public ushort wVk;
        public ushort wScan;
        public uint dwFlags;
        public uint time;
        public nint dwExtraInfo;
    }

    [StructLayout(LayoutKind.Explicit, Size = 40)]
    private struct INPUT
    {
        [FieldOffset(0)]
        public int type;

        [FieldOffset(8)]
        public KEYBDINPUT ki;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint SendInput(uint count, INPUT[] inputs, int size);

    /// <summary>Key presses as the channel's keys step spells them, "ctrl+shift+p escape down", each one pressed and released with its modifiers.</summary>
    public static void Keys(string presses)
    {
        foreach (var press in presses.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            var parts = press.Split('+');
            var modifiers = parts[..^1].Select(part => part.ToLowerInvariant() switch
            {
                "ctrl" => (ushort)0x11,
                "shift" => (ushort)0x10,
                "alt" => (ushort)0x12,
                _ => throw new ArgumentException($"unknown modifier {part}"),
            }).ToList();
            var key = VirtualKey(parts[^1]);
            var inputs = new List<INPUT>();
            inputs.AddRange(modifiers.Select(vk => Key(vk, up: false)));
            inputs.Add(Key(key, up: false));
            inputs.Add(Key(key, up: true));
            inputs.AddRange(Enumerable.Reverse(modifiers).Select(vk => Key(vk, up: true)));
            SendInput((uint)inputs.Count, [.. inputs], Marshal.SizeOf<INPUT>());
            Thread.Sleep(150);
        }
    }

    /// <summary>Characters as typed, each a Unicode key press.</summary>
    public static void Type(string text)
    {
        foreach (var c in text)
        {
            INPUT[] inputs =
            [
                new INPUT { type = INPUT_KEYBOARD, ki = new KEYBDINPUT { wScan = c, dwFlags = KEYEVENTF_UNICODE } },
                new INPUT { type = INPUT_KEYBOARD, ki = new KEYBDINPUT { wScan = c, dwFlags = KEYEVENTF_UNICODE | KEYEVENTF_KEYUP } },
            ];
            SendInput(2, inputs, Marshal.SizeOf<INPUT>());
            Thread.Sleep(20);
        }
    }

    private static INPUT Key(ushort vk, bool up) =>
        new() { type = INPUT_KEYBOARD, ki = new KEYBDINPUT { wVk = vk, dwFlags = up ? KEYEVENTF_KEYUP : 0 } };

    private static ushort VirtualKey(string name)
    {
        if (name.Length == 1 && char.IsLetterOrDigit(name[0]))
        {
            return char.ToUpperInvariant(name[0]);
        }

        return name.ToLowerInvariant() switch
        {
            "enter" => 0x0D,
            "escape" or "esc" => 0x1B,
            "tab" => 0x09,
            "space" => 0x20,
            "backspace" => 0x08,
            "delete" => 0x2E,
            "up" => 0x26,
            "down" => 0x28,
            "left" => 0x25,
            "right" => 0x27,
            "home" => 0x24,
            "end" => 0x23,
            "pageup" => 0x21,
            "pagedown" => 0x22,
            _ when name.StartsWith('f') && int.TryParse(name[1..], out var f) && f is >= 1 and <= 12 => (ushort)(0x70 + f - 1),
            _ => throw new ArgumentException($"unknown key {name}"),
        };
    }
}
