using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;

namespace AxAutomate;

/// <summary>
/// The runner as an MCP client (docs/axit/mcp.md MC-6): <c>Axit.exe mcp</c> started as its child, as an AI client
/// starts it, with the run's variables, so that it reads the run's settings and key. <c>mcp call</c> sends without
/// waiting, so that a script can answer the pairing notice in the window while the call waits; <c>expect mcp</c> takes
/// the first answer line holding the text. The last window id an answer named is <c>{window}</c> in the next call.
/// </summary>
internal sealed partial class Broker : IDisposable
{
    private readonly Process _process;
    private readonly List<string> _lines = [];
    private readonly object _gate = new();
    private int _id;

    /// <summary>The window id of the last <c>axit_windows</c> answer taken, for <c>{window}</c>.</summary>
    public string? WindowId { get; private set; }

    private Broker(Process process)
    {
        _process = process;
        var reader = new Thread(() =>
        {
            while (_process.StandardOutput.ReadLine() is { } line)
            {
                lock (_gate)
                {
                    _lines.Add(Decoded(line));
                }
            }
        }) { IsBackground = true };
        reader.Start();
    }

    public static Broker Start(string exe)
    {
        var start = new ProcessStartInfo(exe)
        {
            UseShellExecute = false,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            CreateNoWindow = true,
            StandardInputEncoding = new UTF8Encoding(false),
            StandardOutputEncoding = new UTF8Encoding(false),
        };
        start.ArgumentList.Add("mcp");
        // The run's secret and folder, which the windows were started with: the broker reads the run's mcp.json and key.
        foreach (var name in new[] { "AXIT_AUTOMATION", "AXIT_HOME" })
        {
            if (Environment.GetEnvironmentVariable(name) is { } value)
            {
                start.Environment[name] = value;
            }
        }

        var broker = new Broker(Process.Start(start)!);
        broker.Send("""{"jsonrpc":"2.0","id":0,"method":"initialize","params":{"protocolVersion":"2025-06-18","clientInfo":{"name":"axAutomate"}}}""");
        return broker;
    }

    /// <summary>A tools/call of the tool with its arguments' JSON (an empty object when none), {window} filled in.</summary>
    public string Call(string tool, string arguments)
    {
        var json = string.IsNullOrWhiteSpace(arguments) ? "{}" : arguments.Replace("{window}", WindowId ?? "none", StringComparison.Ordinal);
        var line = $$$"""{"jsonrpc":"2.0","id":{{{++_id}}},"method":"tools/call","params":{"name":"{{{tool}}}","arguments":{{{json}}}}}""";
        Send(line);
        return line;
    }

    private void Send(string line)
    {
        _process.StandardInput.WriteLine(line);
        _process.StandardInput.Flush();
    }

    /// <summary>The first answer line holding the text, taken with every line before it; null when none has come.</summary>
    public string? Take(string text)
    {
        lock (_gate)
        {
            var index = _lines.FindIndex(line => line.Contains(text, StringComparison.OrdinalIgnoreCase));
            if (index < 0)
            {
                return null;
            }

            var line = _lines[index];
            _lines.RemoveRange(0, index + 1);
            if (WindowPattern().Match(line) is { Success: true } match)
            {
                WindowId = match.Groups[1].Value;
            }

            return line;
        }
    }

    /// <summary>An answer as a script reads it: a tool's text with "error: " before it when the tool refused; any other line as it came.</summary>
    private static string Decoded(string line)
    {
        try
        {
            var node = System.Text.Json.Nodes.JsonNode.Parse(line);
            if (node?["result"]?["content"]?[0]?["text"]?.GetValue<string>() is { } text)
            {
                return (node["result"]?["isError"]?.GetValue<bool>() == true ? "error: " : string.Empty) + text.Replace("\r", string.Empty).Replace("\n", " ");
            }
        }
        catch (Exception ex) when (ex is System.Text.Json.JsonException or InvalidOperationException)
        {
        }

        return line;
    }

    /// <summary>The first answer waiting that holds the text, left where it is; null when none does.</summary>
    public string? Waiting(string text)
    {
        lock (_gate)
        {
            return _lines.FirstOrDefault(line => line.Contains(text, StringComparison.OrdinalIgnoreCase));
        }
    }

    public string Pending()
    {
        lock (_gate)
        {
            return string.Join(" | ", _lines);
        }
    }

    public void Dispose()
    {
        try
        {
            _process.StandardInput.Close();
            if (!_process.WaitForExit(5000))
            {
                _process.Kill();
            }
        }
        catch (Exception ex) when (ex is InvalidOperationException or IOException)
        {
        }

        _process.Dispose();
    }

    // axit_windows' text: "window": "0123456789abcdef".
    [GeneratedRegex("""
        "window": "([0-9a-f]{16})"
        """)]
    private static partial Regex WindowPattern();
}
