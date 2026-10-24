using System.Security.Cryptography;
using System.Text;

namespace Axit.Core.Mcp;

/// <summary>
/// The record of every AI tool call (docs/axit/mcp.md MC-5.2, MC-6.6): one line in <see cref="AppPaths.McpAuditFile"/>
/// per call, with the time, the client, the tool, a short hash of the arguments (never the arguments themselves, which
/// may name the user's files) and the result. A line that cannot be written is logged and the call goes on.
/// </summary>
public static class McpAudit
{
    /// <summary>One line: "2026-10-24 21:05:03  Claude Code (C:\…\claude.exe)  axit_open  args 3f2a…  done".</summary>
    public static string Line(DateTime time, string client, string program, string tool, string arguments, string result) =>
        $"{time:yyyy-MM-dd HH:mm:ss}  {Clean(client)} ({Clean(program)})  {Clean(tool)}  args {Hash(arguments)}  {Clean(result)}";

    public static void Write(string client, string program, string tool, string arguments, string result)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(AppPaths.McpAuditFile)!);
            File.AppendAllText(AppPaths.McpAuditFile, Line(DateTime.Now, client, program, tool, arguments, result) + Environment.NewLine);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            Log.Error("The AI tools' log was not written", ex);
        }
    }

    /// <summary>The first 16 hex digits of the arguments' SHA-256: the same arguments give the same digits, and nothing of them can be read back.</summary>
    public static string Hash(string arguments) =>
        Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(arguments)))[..16];

    /// <summary>A text on one line: a client's name is its own to choose and may hold line breaks that would forge a line.</summary>
    private static string Clean(string text) =>
        new(text.Select(c => char.IsControl(c) ? ' ' : c).Take(200).ToArray());
}

/// <summary>At most <see cref="PerSecond"/> calls in any second (MC-3.5), for one broker, that is one client.</summary>
public sealed class McpRate
{
    public const int PerSecond = 10;

    private readonly Queue<long> _calls = new();

    /// <summary>Whether a call at this time (milliseconds) is within the limit; a call allowed is counted.</summary>
    public bool Allow(long now)
    {
        while (_calls.Count > 0 && now - _calls.Peek() >= 1000)
        {
            _calls.Dequeue();
        }

        if (_calls.Count >= PerSecond)
        {
            return false;
        }

        _calls.Enqueue(now);
        return true;
    }
}
