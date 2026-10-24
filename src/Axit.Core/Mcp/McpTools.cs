using System.Text.Json.Nodes;

namespace Axit.Core.Mcp;

/// <summary>
/// The tools of the first stage (docs/axit/mcp.md MC-6.5): what they are called, what they say of themselves, and the
/// check of <c>axit_open</c>'s arguments, which is pure and tested. Reading a window is the window's own work
/// (<c>Axit.Forms/McpChannel</c>); listing and opening are the broker's (<c>Axit.Forms/McpBroker</c>).
/// </summary>
public static class McpTools
{
    public const string Apps = "axit_apps";
    public const string Windows = "axit_windows";
    public const string State = "axit_window_state";
    public const string List = "axit_window_list";
    public const string Text = "axit_window_text";
    public const string Commands = "axit_window_commands";
    public const string Said = "axit_window_said";
    public const string Open = "axit_open";

    /// <summary>The apps that serve no pipe and so cannot be read (MC-4.1, MC-4.2): the password database and the clipboard history.</summary>
    public static readonly IReadOnlyList<string> NeverRead = [Dispatch.AxPass, Dispatch.AxClip];

    /// <summary>The apps <c>axit_open</c> refuses: each starts a program as it opens (Claude Code, a shell), which is an act (MC-3.1), for a later stage with its confirmation.</summary>
    public static readonly IReadOnlyList<string> NeverOpened = [Dispatch.AxClaude, Dispatch.AxTerminal];

    /// <summary>The window tools, which take the window's id from <c>axit_windows</c>, and the request each sends to the window.</summary>
    public static readonly IReadOnlyDictionary<string, string> WindowRequests = new Dictionary<string, string>
    {
        [State] = "state",
        [List] = "list",
        [Text] = "text",
        [Commands] = "commands",
        [Said] = "said",
    };

    public static IReadOnlyList<McpTool> All { get; } =
    [
        new(Apps, "The Axit apps: each one's verb for axit_open, its name, and whether it opens files, folders or nothing.", Schema()),
        new(Windows, "The open Axit windows that can be read: an id for the window tools, the app, the title. axPass and axClip are never listed.", Schema()),
        new(State, "One window's state: its title, its status bar, the notice it shows with its text, and the focused control with its value. A password field reads as protected.", Schema(("window", "The window's id from axit_windows."))),
        new(List, "The items of the window's focused list, or of its first list, with the selected one.", Schema(("window", "The window's id from axit_windows."))),
        new(Text, "The lines of the window's focused text, or of its main text (an editor's file, a viewer's, a transcript), at most 1 MB.", Schema(("window", "The window's id from axit_windows."))),
        new(Commands, "The window's commands as its menus and keys have them: the label, the key, and the mark (read, open, change, act). Listing only: running them comes later.", Schema(("window", "The window's id from axit_windows."))),
        new(Said, "What the window announced to the user since the last call, the last 50 announcements.", Schema(("window", "The window's id from axit_windows."))),
        new(Open, "Opens an Axit app in a new window, on a file or folder of this computer or on nothing. The user sees and hears it open.", Schema(("app", "The app's verb from axit_apps: down, file, git ..."), ("path", "Optional: the full path of a file or folder on this computer."))),
    ];

    /// <summary>
    /// Whether a part of the path, from its root down, is a link (a reparse point): each part's attributes are read
    /// without following it, and a part that does not exist ends the walk, since nothing below it is reached.
    /// </summary>
    public static bool PassesLink(string path)
    {
        var root = Path.GetPathRoot(path) ?? string.Empty;
        var current = root;
        foreach (var part in path[root.Length..].Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries))
        {
            current = Path.Combine(current, part);
            try
            {
                if (File.GetAttributes(current).HasFlag(FileAttributes.ReparsePoint))
                {
                    return true;
                }
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                return false;
            }
        }

        return false;
    }

    /// <summary>A schema of string arguments; the first is required when the tool takes the window's id or an app.</summary>
    private static JsonObject Schema(params (string Name, string Description)[] arguments)
    {
        var properties = new JsonObject();
        foreach (var (name, description) in arguments)
        {
            properties[name] = new JsonObject { ["type"] = "string", ["description"] = description };
        }

        var schema = new JsonObject { ["type"] = "object", ["properties"] = properties, ["additionalProperties"] = false };
        if (arguments.Length > 0)
        {
            schema["required"] = new JsonArray(arguments[0].Name);
        }

        return schema;
    }

    /// <summary>
    /// The command line <c>axit_open</c> starts (MC-6.5), or the reason it does not: the app by its verb; the path, when
    /// given, fully qualified, on this computer, existing, and of a kind the app opens; the arguments as a list, never
    /// a command line a shell would read; no link on the way (a junction or a symbolic link may lead to a network
    /// share, which asking whether the path exists would reach). <paramref name="isFolder"/>,
    /// <paramref name="isFile"/> and <paramref name="passesLink"/> ask the file system, so that the test gives its own
    /// answers; the link is asked first, since the other two follow links.
    /// </summary>
    public static (string[]? Arguments, string? Refusal) OpenArguments(string? verb, string? path, Func<string, bool> isFolder, Func<string, bool> isFile, Func<string, bool> passesLink)
    {
        if (Dispatch.Apps.FirstOrDefault(app => string.Equals(app.Verb, verb, StringComparison.OrdinalIgnoreCase)) is not { } app)
        {
            return (null, $"No app has the verb {verb}; axit_apps lists them.");
        }

        if (NeverOpened.Contains(app.Verb))
        {
            return (null, $"{app.Name} starts a program as it opens, which AI tools cannot do yet: the user opens it.");
        }

        if (string.IsNullOrWhiteSpace(path))
        {
            return ([app.Verb], null);
        }

        if (!Path.IsPathFullyQualified(path) || path.StartsWith(@"\\", StringComparison.Ordinal))
        {
            return (null, "The path must be a full path on this computer, such as C:\\work\\notes.md.");
        }

        if (!LocalPaths.IsLocal(path))
        {
            return (null, "The path is not on a drive of this computer: a network share or a mapped drive is never opened.");
        }

        if (passesLink(path))
        {
            return (null, "The path goes through a link (a junction or a symbolic link), which AI tools never follow.");
        }

        var folder = isFolder(path);
        if (!folder && !isFile(path))
        {
            return (null, $"There is no file or folder at {path}.");
        }

        if (Dispatch.PathFor(app, path, folder) is not { } target)
        {
            return (null, $"{app.Name} does not open {(folder ? "a folder" : "a file")}.");
        }

        return ([app.Verb, target], null);
    }
}
