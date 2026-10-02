namespace Axit.Core;

/// <summary>What a command line starts: an app name (<see cref="Dispatch.AxClaude"/>, <see cref="Dispatch.AxDown"/> or
/// <see cref="Dispatch.Bundle"/>), the arguments that app gets, and an error text when the command line names a path
/// that does not exist.</summary>
public sealed record Launch(string App, string[] Arguments, string? Error = null);

/// <summary>
/// Decides which app <c>Axit.exe</c> starts (docs/axit/SPEC.md AX-1). The first argument may be a verb naming the app;
/// without one, a folder goes to AxClaude and a file to AxDown; an option (<c>--project</c>, a bare <c>--</c>) or no
/// argument at all means AxClaude (B-8). <c>--help</c> and <c>--version</c> alone are answered by the bundle itself.
/// Pure code: it only looks at the arguments and asks the file system whether a path is a folder or a file.
/// </summary>
public static class Dispatch
{
    public const string AxClaude = "claude";
    public const string AxDown = "down";

    /// <summary>Not an app: the executable answers <c>--help</c> and <c>--version</c>, or reports <see cref="Launch.Error"/>.</summary>
    public const string Bundle = "axit";

    public const string Usage = """
        Usage: Axit [claude|down] [arguments]

          Axit claude [<folder>] ...  AxClaude, the front end for Claude Code (Axit claude --help lists its options)
          Axit down [<file>]          AxDown, the text editor
          Axit <folder>               the same as Axit claude <folder>
          Axit <file>                 the same as Axit down <file>
          Axit                        AxClaude on the current folder
          --help, --version           this text, or the version

        The installed console commands axit, axclaude and axdown stand for Axit, Axit claude and Axit down.
        """;

    public static Launch Decide(string[] args)
    {
        if (args.Length == 0)
        {
            return new Launch(AxClaude, args);
        }

        var first = args[0];
        if (string.Equals(first, AxClaude, StringComparison.OrdinalIgnoreCase))
        {
            return new Launch(AxClaude, args[1..]);
        }

        if (string.Equals(first, AxDown, StringComparison.OrdinalIgnoreCase))
        {
            return new Launch(AxDown, args[1..]);
        }

        if (first is "--help" or "-h" or "-?" or "/?" or "--version")
        {
            return new Launch(Bundle, args);
        }

        if (first.StartsWith("--", StringComparison.Ordinal))
        {
            // An AxClaude option such as --project, or the bare -- that starts a new conversation.
            return new Launch(AxClaude, args);
        }

        if (Directory.Exists(first))
        {
            return new Launch(AxClaude, args);
        }

        if (File.Exists(first))
        {
            return new Launch(AxDown, args);
        }

        return new Launch(Bundle, args, $"There is no folder or file at {first}.");
    }
}
