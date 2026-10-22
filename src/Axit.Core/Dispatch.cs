namespace Axit.Core;

/// <summary>What a command line starts: an app name (<see cref="Dispatch.AxClaude"/>, <see cref="Dispatch.AxDown"/> or
/// <see cref="Dispatch.Bundle"/>), the arguments that app gets, and an error text when the command line names a path
/// that does not exist.</summary>
public sealed record Launch(string App, string[] Arguments, string? Error = null);

/// <summary>An app of the bundle: its name, which ends every title of its windows ("notes.md - AxDown") and names its
/// Start menu entry, and the verb that starts it (AX-1.1).</summary>
public sealed record App(string Name, string Verb);

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
    public const string AxJSON = "json";
    public const string AxGit = "git";
    public const string AxTerminal = "terminal";
    public const string AxFile = "file";
    public const string AxMedia = "media";

    /// <summary>Not an app: the executable answers <c>--help</c> and <c>--version</c>, or reports <see cref="Launch.Error"/>.</summary>
    public const string Bundle = "axit";

    /// <summary>The apps, in the order the Axit menu lists them (AX-9).</summary>
    public static readonly IReadOnlyList<App> Apps = [new("AxClaude", AxClaude), new("AxDown", AxDown), new("AxJSON", AxJSON), new("AxGit", AxGit), new("AxTerminal", AxTerminal), new("AxFile", AxFile), new("AxMedia", AxMedia)];

    public const string Usage = """
        Usage: Axit [claude|down|json|git|terminal|file|media] [arguments]

          Axit claude [<folder>] ...  AxClaude, the front end for Claude Code (Axit claude --help lists its options)
          Axit down [<file>]          AxDown, the text editor
          Axit json [<file>]          AxJSON, the JSON editor
          Axit git [<folder or file>] AxGit, the git manager, on the repository that holds the path
          Axit terminal [<folder>]    AxTerminal, a shell in the folder as a transcript
          Axit file [<folder or file>] AxFile, the file manager, on the folder (a file selected in its folder)
          Axit media [<file or folder>...] [--add]  AxMedia, the player: the file with its folder's sound and video files after it, or a folder's; --add appends to the open window's queue
          Axit <folder>               the same as Axit claude <folder>
          Axit <file>                 the same as Axit down <file>, Axit json <file> for a .json file, or Axit media <file> for a sound or video file
          Axit                        AxClaude on the current folder
          --help, --version           this text, or the version

        The installed console commands axit, axclaude, axdown, axjson, axgit, axterminal, axfile and axmedia stand for Axit, Axit claude, Axit down, Axit json, Axit git, Axit terminal, Axit file and Axit media.
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

        if (string.Equals(first, AxJSON, StringComparison.OrdinalIgnoreCase))
        {
            return new Launch(AxJSON, args[1..]);
        }

        if (string.Equals(first, AxGit, StringComparison.OrdinalIgnoreCase))
        {
            return new Launch(AxGit, args[1..]);
        }

        if (string.Equals(first, AxTerminal, StringComparison.OrdinalIgnoreCase))
        {
            return new Launch(AxTerminal, args[1..]);
        }

        if (string.Equals(first, AxFile, StringComparison.OrdinalIgnoreCase))
        {
            return new Launch(AxFile, args[1..]);
        }

        if (string.Equals(first, AxMedia, StringComparison.OrdinalIgnoreCase))
        {
            return new Launch(AxMedia, args[1..]);
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
            // AX-1.2: a .json file is AxJSON's, a sound or video file AxMedia's, any other file AxDown's.
            if (Path.GetExtension(first).Equals(".json", StringComparison.OrdinalIgnoreCase))
            {
                return new Launch(AxJSON, args);
            }

            return new Launch(MediaFiles.IsMedia(first) ? AxMedia : AxDown, args);
        }

        return new Launch(Bundle, args, $"There is no folder or file at {first}.");
    }
}
