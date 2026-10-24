namespace Axit.Core;

/// <summary>What a command line starts: an app's verb (<see cref="Dispatch.AxClaude"/> and the rest, or
/// <see cref="Dispatch.Bundle"/>), the arguments that app gets, and an error text when the command line names a path
/// that does not exist.</summary>
public sealed record Launch(string App, string[] Arguments, string? Error = null);

/// <summary>
/// An app of the bundle: <paramref name="Name"/> is its name as shown and spoken ("axClaude", "axGIT", lower-case "ax"
/// so that a screen reader says it as a word; 2026-10-23), which ends every title of its windows ("notes.md - axDown")
/// and names its Start menu entry; <paramref name="Verb"/> starts it (AX-1.1); <paramref name="Project"/> is the name
/// of its code (the folder src/AxClaude, the class AxClaudeApp, the icon AxClaude.ico), which keeps the C# spelling.
/// <paramref name="Group"/> is its place among the apps (AX-9.2: the Axit menu's lists keep the groups together with a
/// separator between them); <paramref name="Opens"/> what it opens from a path; <paramref name="ShowIn"/> whether every
/// window's Show current folder in submenu offers it (AX-9.10). Every list of apps in the windows reads these, so a new
/// app is in the Axit menu, Open in Axit, the Show in submenus and Recently closed by its line in <see cref="Dispatch.Apps"/>.
/// </summary>
public sealed record App(string Name, string Verb, string Project, AppGroup Group, Opens Opens, bool ShowIn = false);

/// <summary>The groups of the apps, in the Axit menu's order (AX-9.2).</summary>
public enum AppGroup
{
    /// <summary>Claude, the shell and git: working in a folder.</summary>
    Code,

    /// <summary>Files and what is in them: the file manager, the editors, the viewer, the player.</summary>
    Files,

    /// <summary>Lists of one's own: tasks, passwords.</summary>
    Organise,

    /// <summary>Small tools for the whole computer: the launcher, the clipboard, the calculator, the processes.</summary>
    Utilities,
}

/// <summary>What an app opens from a path: a file's app given a folder is refused, a folder's app given a file gets the file's folder.</summary>
public enum Opens
{
    Nothing,
    Folders,
    Files,
    FilesAndFolders,
}

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

    /// <summary>Not an app: the AI tools' broker, started by an MCP client and speaking to it on standard input and output (docs/axit/mcp.md MC-1.1).</summary>
    public const string Mcp = "mcp";

    /// <summary>
    /// The apps, in the order every list of the windows shows them, by group (AX-9.2, 2026-10-24): one line here puts an
    /// app in the Axit menu, axFile's Open in Axit, the Show in submenus and Recently closed.
    /// </summary>
    public static readonly IReadOnlyList<App> Apps =
    [
        new("axClaude", AxClaude, "AxClaude", AppGroup.Code, Opens.Folders, ShowIn: true),
        new("axTerminal", AxTerminal, "AxTerminal", AppGroup.Code, Opens.Folders, ShowIn: true),
        new("axGIT", AxGit, "AxGit", AppGroup.Code, Opens.FilesAndFolders, ShowIn: true),
        new("axFile", AxFile, "AxFile", AppGroup.Files, Opens.FilesAndFolders, ShowIn: true),
        new("axDown", AxDown, "AxDown", AppGroup.Files, Opens.Files),
        new("axJSON", AxJSON, "AxJSON", AppGroup.Files, Opens.Files),
        new("axPreview", AxPreview, "AxPreview", AppGroup.Files, Opens.Files),
        new("axMedia", AxMedia, "AxMedia", AppGroup.Files, Opens.FilesAndFolders),
        new("axTask", AxTask, "AxTask", AppGroup.Organise, Opens.Folders),
        new("axPass", AxPass, "AxPass", AppGroup.Organise, Opens.Files),
        new("axStart", AxStart, "AxStart", AppGroup.Utilities, Opens.Nothing),
        new("axClip", AxClip, "AxClip", AppGroup.Utilities, Opens.Nothing),
        new("axCalc", AxCalc, "AxCalc", AppGroup.Utilities, Opens.Nothing),
        new("axProcess", AxProcess, "AxProcess", AppGroup.Utilities, Opens.Nothing),
    ];

    /// <summary>The app of a verb.</summary>
    public static App Of(string verb) => Apps.First(app => app.Verb == verb);

    /// <summary>
    /// The path an app is started on from a file or a folder in hand (AX-9.10, axFile AF-4.9): the path itself when the
    /// app opens its kind, the folder of a file for an app that opens folders only, null for a folder given to an app
    /// that opens files only, or for an app that opens nothing.
    /// </summary>
    public static string? PathFor(App app, string path, bool isFolder) => (app.Opens, isFolder) switch
    {
        (Opens.FilesAndFolders, _) or (Opens.Folders, true) or (Opens.Files, false) => path,
        (Opens.Folders, false) => System.IO.Path.GetDirectoryName(path),
        _ => null,
    };

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
          Axit mcp                    the AI tools' server, for an MCP client to start (Options, General, Let AI tools read and open Axit)
          --help, --version           this text, or the version

        The installed console commands axit, axclaude, axdown, axjson, axgit, axterminal, axfile, axmedia, axpreview, axstart, axprocess, axtask, axpass, axcalc and axclip stand for Axit, Axit claude, Axit down, Axit json, Axit git, Axit terminal, Axit file, Axit media, Axit preview, Axit start, Axit process, Axit task, Axit pass, Axit calc and Axit clip.
        """;

    /// <summary>Whether an argument asks for the usage text: the forms Windows and Unix programs take (the apps' hosts read it too).</summary>
    public static bool IsHelpFlag(string argument) => argument is "--help" or "-h" or "-?" or "/?";

    /// <summary>
    /// The editor a file opens in (AX-1.2; axFile's Open in editor AF-1.3, axPreview's Edit AP-1.4, axGIT's Open):
    /// axJSON's verb for a .json file, axDown's for any other.
    /// </summary>
    public static string EditorOf(string path) =>
        Path.GetExtension(path).Equals(".json", StringComparison.OrdinalIgnoreCase) ? AxJSON : AxDown;

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

        if (string.Equals(first, Mcp, StringComparison.OrdinalIgnoreCase))
        {
            return new Launch(Mcp, args[1..]);
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
            if (EditorOf(first) == AxJSON)
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
