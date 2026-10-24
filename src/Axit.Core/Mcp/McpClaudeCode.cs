namespace Axit.Core.Mcp;

/// <summary>
/// Axit added to Claude Code as an MCP server (docs/axit/mcp.md MC-6.8): where Claude Code is, and the two command lines
/// that register this Axit.exe for every folder of the user, an earlier entry removed first. Pure but for the file
/// lookups, and tested; the window runs them (<c>Axit.Forms/AxitForm.Mcp.cs</c>).
/// </summary>
public static class McpClaudeCode
{
    /// <summary>The name Axit has among Claude Code's MCP servers.</summary>
    public const string ServerName = "axit";

    /// <summary>
    /// Claude Code's program: <c>claude.exe</c> on the PATH's full folders or in its installer's folder, or npm's
    /// <c>claude.cmd</c>; null when it is not installed.
    /// </summary>
    public static string? Find()
    {
        if (SystemPrograms.OnPath("claude.exe") is { } onPath)
        {
            return onPath;
        }

        var native = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".local", "bin", "claude.exe");
        if (File.Exists(native))
        {
            return native;
        }

        return SystemPrograms.OnPath("claude.cmd");
    }

    /// <summary>Claude Code's arguments that remove an earlier entry of Axit's, for every folder.</summary>
    public static IReadOnlyList<string> RemoveArguments() => ["mcp", "remove", "--scope", "user", ServerName];

    /// <summary>Claude Code's arguments that add this Axit.exe with the argument <c>mcp</c>, for every folder.</summary>
    public static IReadOnlyList<string> AddArguments(string axitExe) => ["mcp", "add", "--scope", "user", ServerName, "--", axitExe, "mcp"];

    /// <summary>The add as one command line to type in PowerShell or cmd, for when Claude Code is not found here.</summary>
    public static string AddCommandLine(string axitExe) => $"claude mcp add --scope user {ServerName} -- \"{axitExe}\" mcp";
}
