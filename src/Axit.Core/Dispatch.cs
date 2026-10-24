namespace Axit.Core;

/// <summary>What a command line starts: an app's verb (<see cref="Dispatch.AxClaude"/> and the rest, or
/// <see cref="Dispatch.Bundle"/>), the arguments that app gets, and an error text when the command line names a path
/// that does not exist.</summary>
public sealed record Launch(string App, string[] Arguments, string? Error = null);

/// <summary>An app of the bundle: its name, which ends every title of its windows ("notes.md - axDown") and names its
/// Start menu entry, and the verb that starts it (AX-1.1).</summary>
/// <summary>
/// An app of the bundle: <paramref name="Name"/> is its name as shown and spoken ("axClaude", "axGIT", lower-case "ax"
/// so that a screen reader says it as a word; 2026-10-23), <paramref name="Project"/> the name of its code (the folder
/// src/AxClaude, the class AxClaudeApp, the icon AxClaude.ico), which keeps the C# spelling.
/// </summary>
public sealed record App(string Name, string Verb, string Project);

/// <summary>
/// Decides which app <c>Axit.exe</c> starts (docs/axit/SPEC.md AX-1). The first argument may be a verb naming one of
/// <see cref="Apps"/>; without one, a folder goes to AxClaude and a file to AxDown, AxJSON or AxMedia by its kind; an
/// option (<c>--project</c>, a bare <c>--</c>) or no argument at all means AxClaude (B-8). <c>--help</c> and
/// <c>--version</c> alone are answered by the bundle itself. Pure code: it only looks at the arguments and asks the
/// file system whether a path is a folder or a file.
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
    public const string AxPreview = "preview";
    public const string AxStart = "start";
    public const string AxProcess = "process";
    public const string AxTask = "task";
    public const string AxPass = "pass";
    public const string AxCalc = "calc";
    public const string AxClip = "clip";

    /// <summary>Not an app: the executable answers <c>--help</c> and <c>--version</c>, or reports <see cref="Launch.Error"/>.</summary>
    public const string Bundle = "axit";

    /// <summary>The apps, in the order the Axit menu lists them (AX-9).</summary>
    public static readonly IReadOnlyList<App> Apps = [new("axClaude", AxClaude, "AxClaude"), new("axDown", AxDown, "AxDown"), new("axJSON", AxJSON, "AxJSON"), new("axGIT", AxGit, "AxGit"), new("axTerminal", AxTerminal, "AxTerminal"), new("axFile", AxFile, "AxFile"), new("axMedia", AxMedia, "AxMedia"), new("axPreview", AxPreview, "AxPreview"), new("axStart", AxStart, "AxStart"), new("axProcess", AxProcess, "AxProcess"), new("axTask", AxTask, "AxTask"), new("axPass", AxPass, "AxPass"), new("axCalc", AxCalc, "AxCalc"), new("axClip", AxClip, "AxClip")];

    public const string Usage = """
        Usage: Axit [claude|down|json|git|terminal|file|media|preview|start|process|task|pass|calc|clip] [arguments]

          Axit claude [<folder>] ...  axClaude, the front end for Claude Code (Axit claude --help lists its options)
          Axit down [<file>]          axDown, the text editor
          Axit json [<file>]          axJSON, the JSON editor
          Axit git [<folder or file>] axGIT, the git manager, on the repository that holds the path
          Axit terminal [<folder>]    axTerminal, a shell in the folder as a transcript
          Axit file [<folder or file>] axFile, the file manager, on the folder (a file selected in its folder)
          Axit media [<file or folder>...] [--add]  axMedia, the player: the file with its folder's sound and video files after it, or a folder's; --add appends to the open window's queue
          Axit preview [<file>]       axPreview, the read-only viewer, on the file (the open axPreview window takes it)
          Axit start [--hidden]       axStart, the program launcher: its search field, or with --hidden running until its hotkey
          Axit process                axProcess, the processes, services and startup programs
          Axit task [<folder>]        axTask, tasks and lists as a folder of small files
          Axit pass [<database>]      axPass, a KeePass password database
          Axit calc                   axCalc, the calculator
          Axit clip [--hidden]        axClip, the clipboard history: its search field, or with --hidden running until its hotkey
          Axit <folder>               the same as Axit claude <folder>
          Axit <file>                 the same as Axit down <file>, Axit json <file> for a .json file, Axit pass <file> for a .kdbx file, or Axit media <file> for a sound or video file
          Axit                        axClaude on the current folder
          --help, --version           this text, or the version

        The installed console commands axit, axclaude, axdown, axjson, axgit, axterminal, axfile, axmedia, axpreview, axstart, axprocess, axtask, axpass, axcalc and axclip stand for Axit, Axit claude, Axit down, Axit json, Axit git, Axit terminal, Axit file, Axit media, Axit preview, Axit start, Axit process, Axit task, Axit pass, Axit calc and Axit clip.
        """;

    /// <summary>Whether an argument asks for the usage text: the forms Windows and Unix programs take (the apps' hosts read it too).</summary>
    public static bool IsHelpFlag(string argument) => argument is "--help" or "-h" or "-?" or "/?";

    public static Launch Decide(string[] args)
    {
        if (args.Length == 0)
        {
            return new Launch(AxClaude, args);
        }

        var first = args[0];
        if (Apps.FirstOrDefault(app => string.Equals(first, app.Verb, StringComparison.OrdinalIgnoreCase)) is { } named)
        {
            return new Launch(named.Verb, args[1..]);
        }

        if (IsHelpFlag(first) || first == "--version")
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
            // AX-1.2: a .json file is AxJSON's, a KeePass database axPass's, a sound or video file AxMedia's, any other file AxDown's.
            if (Path.GetExtension(first).Equals(".json", StringComparison.OrdinalIgnoreCase))
            {
                return new Launch(AxJSON, args);
            }

            if (Path.GetExtension(first).Equals(".kdbx", StringComparison.OrdinalIgnoreCase))
            {
                return new Launch(AxPass, args);
            }

            return new Launch(MediaFiles.IsMedia(first) ? AxMedia : AxDown, args);
        }

        return new Launch(Bundle, args, $"There is no folder or file at {first}.");
    }
}
