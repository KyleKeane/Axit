using System.Diagnostics;
using System.IO.Pipes;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using Axit.Core;
using Axit.Core.Mcp;

namespace Axit.Forms;

/// <summary>
/// <c>Axit.exe mcp</c> (docs/axit/mcp.md MC-1.1, MC-6): the broker an AI client starts as its child and speaks MCP to
/// over standard input and output. It holds no window and no state of the apps: each tool call is a connection to one
/// window's pipe (<see cref="McpChannel"/>), checked both ways (the window runs this very program file, the broker
/// proves the key), or the broker's own work (the apps, the open windows, opening an app). Before any of it: the
/// setting is on, the client is within ten calls a second, and the program that started the broker is paired, or the
/// user is asked in the newest Axit window. Every call is written to the audit log.
/// </summary>
public static class McpBroker
{
    private static readonly JsonSerializerOptions Readable = new() { WriteIndented = true, Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };

    /// <summary>How long a window may take to answer one request; a pairing waits for the user (<see cref="McpChannel.PairingWait"/>).</summary>
    private const int AnswerMs = 10_000;

    /// <summary>Runs until the client closes standard input.</summary>
    public static void Run()
    {
        var program = ThisProgram.ParentId() is { } parent ? ThisProgram.ImageOf(parent) : null;
        var rate = new McpRate();
        var clock = Stopwatch.StartNew();
        McpProtocol? protocol = null;
        protocol = new McpProtocol(() => McpTools.All, (tool, arguments) =>
        {
            var name = protocol!.ClientName;
            McpResult result;
            try
            {
                result = Call(tool, arguments, name, program, rate, clock.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                // Whatever went wrong is the call's answer; the client keeps its server.
                Log.Error($"The AI tool {tool} failed", ex);
                result = new McpResult("Axit could not do it: " + ex.Message, true);
            }

            McpAudit.Write(name, program ?? "unknown program", tool, arguments.ToJsonString(), result.IsError ? "refused or failed: " + FirstLine(result.Text) : "done");
            return result;
        });

        using var input = new StreamReader(Console.OpenStandardInput(), new UTF8Encoding(false));
        using var output = new StreamWriter(Console.OpenStandardOutput(), new UTF8Encoding(false)) { AutoFlush = true, NewLine = "\n" };
        while (input.ReadLine() is { } line)
        {
            if (line.Length > 0 && protocol.Handle(line) is { } answer)
            {
                output.WriteLine(answer);
            }
        }
    }

    private static McpResult Call(string tool, JsonObject arguments, string client, string? program, McpRate rate, long now)
    {
        if (!rate.Allow(now))
        {
            return new McpResult($"More than {McpRate.PerSecond} calls a second: wait a moment.", true);
        }

        var settings = McpAccess.Load();
        if (!settings.Enabled)
        {
            return new McpResult("Axit's AI tools access is off. The user turns it on in any Axit window: Axit menu, AI tools, Let AI tools read and open Axit.", true);
        }

        if (program is null)
        {
            return new McpResult("Axit cannot tell which program started it, so it cannot ask the user about it.", true);
        }

        if (McpAccess.ReadKey() is not { } key)
        {
            return new McpResult("Axit has no key for AI tools: the user turns the access off and on again.", true);
        }

        if (!McpAccess.MayPair(program))
        {
            return new McpResult($"Axit was started by {Path.GetFileName(program)}, which runs any script it is given; only a client program of its own can be allowed.", true);
        }

        if (McpAccess.Find(settings, program) is null && Environment.TickCount64 < _deniedUntil)
        {
            return new McpResult($"The user did not allow {client} a moment ago; it is not asked again within a minute.", true);
        }

        if (McpAccess.Find(settings, program) is null && Pair(key, client, program) is { } refusal)
        {
            return new McpResult(refusal, true);
        }

        if (McpTools.WindowRequests.TryGetValue(tool, out var request))
        {
            return McpProtocol.Text(arguments["window"]) is { } id && PipeOf(id) is { } pipe
                ? Ask(key, pipe, request)
                : new McpResult("No such window: axit_windows lists the ids.", true);
        }

        return tool switch
        {
            McpTools.Apps => new McpResult(JsonSerializer.Serialize(Dispatch.Apps.Select(app => new { verb = app.Verb, name = app.Name, opens = app.Opens.ToString().ToLowerInvariant(), read = !McpTools.NeverRead.Contains(app.Verb), opened = !McpTools.NeverOpened.Contains(app.Verb) }), Readable)),
            McpTools.Windows => new McpResult(JsonSerializer.Serialize(Windows(key).Select(w => new { window = w.Id, app = w.App, title = w.Title }), Readable)),
            McpTools.Open => Open(McpProtocol.Text(arguments["app"]), McpProtocol.Text(arguments["path"])),
            _ => new McpResult($"No tool {tool}.", true),
        };
    }

    /// <summary>MC-6.4: the user asked in the newest Axit window; null when allowed, else what to tell the client.</summary>
    private static string? Pair(byte[] key, string client, string program)
    {
        if (Windows(key).FirstOrDefault() is not { } newest)
        {
            return $"Open any Axit window, so that the user can allow {client} there.";
        }

        var request = "pair " + JsonSerializer.Serialize(new { name = client, program });
        var answer = Exchange(key, newest.Pipe, request, (int)McpChannel.PairingWait.TotalMilliseconds + 5000);
        if (answer is not null && answer.Contains("\"busy\":true", StringComparison.Ordinal))
        {
            return $"The Axit window is showing the user something else; ask again once it is answered.";
        }

        if (answer is not null && answer.Contains("\"allowed\":true", StringComparison.Ordinal))
        {
            return null;
        }

        // A Deny holds for a minute: a client that asks again at once does not wear the user down.
        _deniedUntil = Environment.TickCount64 + 60_000;
        return $"The user did not allow {client} to use Axit.";
    }

    /// <summary>A window's answer to one request, as the tool's text.</summary>
    private static McpResult Ask(byte[] key, string pipe, string request)
    {
        var answer = Exchange(key, pipe, request, AnswerMs);
        if (answer is null)
        {
            return new McpResult("The window did not answer.", true);
        }

        try
        {
            var node = JsonNode.Parse(answer);
            if (node is JsonObject { } error && McpProtocol.Text(error["error"]) is { } text)
            {
                return new McpResult(text, true);
            }

            return new McpResult(node?.ToJsonString(Readable) ?? answer);
        }
        catch (JsonException)
        {
            return new McpResult("The window's answer could not be read.", true);
        }
    }

    /// <summary>Until when a client the user just denied is refused without asking (milliseconds of <see cref="Environment.TickCount64"/>).</summary>
    private static long _deniedUntil;

    private sealed record OpenWindow(string Id, string Pipe, string App, string Title, long Handle);

    /// <summary>The windows that serve a pipe, the most recently used first (the Axit menu's order).</summary>
    private static List<OpenWindow> Windows(byte[] key)
    {
        var windows = new List<OpenWindow>();
        foreach (var pipe in Pipes())
        {
            if (Exchange(key, pipe, "hello", AnswerMs) is not { } answer)
            {
                continue;
            }

            try
            {
                var hello = JsonNode.Parse(answer) as JsonObject;
                if (hello is not null && McpProtocol.Text(hello["app"]) is { } app)
                {
                    windows.Add(new OpenWindow(pipe[McpChannel.Prefix.Length..], pipe, app, McpProtocol.Text(hello["title"]) ?? string.Empty, hello["handle"]?.GetValue<long>() ?? 0));
                }
            }
            catch (Exception ex) when (ex is JsonException or InvalidOperationException or FormatException)
            {
            }
        }

        var order = AxitMenu.OpenWindows().Select(window => (long)window.Handle).ToList();
        return [.. windows.OrderBy(window => order.IndexOf(window.Handle) is var i and >= 0 ? i : int.MaxValue)];
    }

    /// <summary>The names of this user's Axit windows' pipes.</summary>
    private static IEnumerable<string> Pipes()
    {
        var prefix = McpChannel.Prefix;
        try
        {
            return Directory.GetFiles(@"\\.\pipe\").Select(Path.GetFileName).OfType<string>().Where(name => name.StartsWith(prefix, StringComparison.Ordinal)).ToList();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return [];
        }
    }

    /// <summary>The pipe of a window id from <c>axit_windows</c>: its name's random part, sixteen hex digits, nothing else.</summary>
    private static string? PipeOf(string id) =>
        id.Length == 16 && id.All(Uri.IsHexDigit) ? Pipes().FirstOrDefault(pipe => pipe.EndsWith(id, StringComparison.Ordinal)) : null;

    /// <summary>
    /// One request to one window (MC-6.3): connect, check that the window runs this very program file, answer its
    /// challenge with the key, send the request, read the one answer. Null when any of it fails.
    /// </summary>
    private static string? Exchange(byte[] key, string pipeName, string request, int timeoutMs)
    {
        try
        {
            // Identification only: the window may learn who connected, never act as this user.
            using var pipe = new NamedPipeClientStream(".", pipeName, PipeDirection.InOut, PipeOptions.None, System.Security.Principal.TokenImpersonationLevel.Identification);
            pipe.Connect(2000);
            if (UserPipe.ServerOf(pipe) is not { } server || !ThisProgram.Runs(server))
            {
                // Another build of Axit (a checkout's next to the installed one) or a look-alike: its windows are not this
                // broker's to read. Information, not an error: two builds side by side are ordinary.
                Log.Info("An AI tools' pipe is served by another program than this Axit.exe: nothing sent");
                return null;
            }

            using var reader = new StreamReader(pipe, new UTF8Encoding(false));
            using var writer = new StreamWriter(pipe, new UTF8Encoding(false)) { AutoFlush = true };
            var read = reader.ReadLineAsync();
            if (!read.Wait(AnswerMs) || read.Result is not { } first)
            {
                return null;
            }

            var opening = JsonNode.Parse(first) as JsonObject;
            if (McpProtocol.Text(opening?["challenge"]) is not { } challenge)
            {
                // The setting off, or no key: the window's error is the answer.
                return first;
            }

            // Both ends prove the key (MC-6.3): this answer, then the window's to a challenge of the broker's.
            writer.WriteLine(McpAccess.Proof(key, challenge));
            var ours = McpAccess.NewChallenge();
            writer.WriteLine(ours);
            var proof = reader.ReadLineAsync();
            if (!proof.Wait(AnswerMs) || !McpAccess.Proves(key, ours, proof.Result))
            {
                Log.Info("An AI tools' pipe did not prove the key: nothing sent");
                return null;
            }

            writer.WriteLine(request);
            var answer = reader.ReadLineAsync();
            return answer.Wait(timeoutMs) ? answer.Result : null;
        }
        catch (Exception ex) when (ex is IOException or TimeoutException or UnauthorizedAccessException or JsonException or InvalidOperationException or AggregateException)
        {
            return null;
        }
    }

    /// <summary>MC-6.5: an app on a path or none, as its own process; the arguments as a list, never through a shell.</summary>
    private static McpResult Open(string? verb, string? path)
    {
        var (arguments, refusal) = McpTools.OpenArguments(verb, path, Directory.Exists, File.Exists, McpTools.PassesLink);
        if (arguments is null)
        {
            return new McpResult(refusal!, true);
        }

        // Through the shell, which hands the new process none of this one's handles: a window holding the client's
        // output pipe would keep the client waiting for its end after the broker is gone.
        var start = new ProcessStartInfo(Environment.ProcessPath!) { UseShellExecute = true, WorkingDirectory = AppContext.BaseDirectory };
        foreach (var argument in arguments)
        {
            start.ArgumentList.Add(argument);
        }

        using var process = Process.Start(start);
        return new McpResult(process is null ? "The app did not start." : $"Opened {string.Join(" ", arguments)}.", process is null);
    }

    private static string FirstLine(string text) => text.Split('\n')[0];
}
