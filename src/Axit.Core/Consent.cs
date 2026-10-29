using System.Globalization;

namespace Axit.Core;

/// <summary>
/// One thing the user agreed to (AX-11.5): an access layer's first connection to a program (App "axClaude", Target
/// "Claude Code"), or the terms of an update (App <see cref="Consent.UpdateApp"/>, Target "Axit 3.8.0"), with Axit's
/// version at the time and the date.
/// </summary>
public sealed record ConsentRecord(string App, string Target, string Version, DateTime When);

/// <summary>
/// A program or service an access layer connects to (AX-11.4): its name, which is the consent's target, what it does,
/// whose terms apply and where they are.
/// </summary>
public sealed record ConsentTarget(string Program, string Work, string Terms, string TermsUrl)
{
    public static readonly ConsentTarget ClaudeCode = new(
        "Claude Code",
        "Claude Code, Anthropic's program, and Anthropic's service behind it do the work under your own Anthropic account: everything you type goes to them, and Claude Code can create, change and delete files, run commands and use your account's usage or money.",
        "Anthropic's terms and privacy policy",
        "https://www.anthropic.com/legal");

    public static readonly ConsentTarget Git = new(
        "Git",
        "Git, a program you install, does the work on your repository: its commands change, remove and publish work.",
        "Git's licence",
        "https://git-scm.com/about/free-and-open-source");

    public static readonly ConsentTarget KeePassXC = new(
        "KeePassXC",
        "KeePassXC's command-line tool, a program you install, reads, encrypts and writes your password database; your master password goes to it.",
        "KeePassXC's licence",
        "https://keepassxc.org/");

    /// <summary>The target of a program Install it now installs (<see cref="ProgramInstall"/>'s name), so that agreeing there counts for the first connection.</summary>
    public static ConsentTarget For(string program) => program switch
    {
        "Claude Code" => ClaudeCode,
        "Git" => Git,
        "KeePassXC" => KeePassXC,
        _ => Shell(program),
    };

    /// <summary>A shell of axTerminal by its name as ShellLauncher says it: each is its own consent (AX-11.5). A custom shell has no address for its terms.</summary>
    public static ConsentTarget Shell(string name) => new(
        name,
        $"{name} runs every command you type, with your rights, on your computer and on any network the commands reach.",
        name switch
        {
            "PowerShell 7" => "PowerShell's licence",
            "Git Bash" => "Git for Windows' licence",
            "Custom shell" => "the terms of the program you set",
            _ => "Microsoft's licence terms for Windows",
        },
        name switch
        {
            "PowerShell 7" => "https://github.com/PowerShell/PowerShell/blob/master/LICENSE.txt",
            "Git Bash" => "https://gitforwindows.org/",
            "Custom shell" => "",
            _ => "https://www.microsoft.com/useterms",
        });

    /// <summary>A server a repository's remote points at (github.com): a new service with its own terms, asked before the first fetch, pull or push that reaches it.</summary>
    public static ConsentTarget Server(string host) => new(
        host,
        $"{host} receives what you push and sends what you fetch or pull, under your account there if it asks for one.",
        $"the terms of {host}",
        $"https://{host}/");

    /// <summary>An AI tool paired with Axit's windows (AX-9, docs/axit/mcp.md): it reads what the windows show.</summary>
    public static ConsentTarget AiTool(string name) => new(
        name,
        $"{name} reads what Axit's windows show and can open apps; what it does with what it reads is under its own terms, and it may send it to its maker's service.",
        $"the terms of {name}'s maker",
        "https://www.anthropic.com/legal");
}

/// <summary>The shape of the file: one list, oldest first.</summary>
public sealed class ConsentList
{
    public List<ConsentRecord> Items { get; set; } = [];
}

/// <summary>
/// What the user has agreed to (docs/axit/SPEC.md AX-11.4 to AX-11.8), in one file for the bundle
/// (<see cref="AppPaths.ConsentFile"/>). An access layer asks <see cref="Given"/> before it connects to a program and
/// calls <see cref="Record"/> when the user checks the box and connects; About, Consents lists <see cref="Read"/> and
/// <see cref="Withdraw"/> takes one back, so that the app asks again. A missing or damaged file means nothing was agreed,
/// so the user is asked again rather than taken to have agreed. Tested in <c>tests/Axit.Tests</c>.
/// </summary>
public static class Consent
{
    /// <summary>The App of an update's acceptance (AX-11.7).</summary>
    public const string UpdateApp = "Update";

    public static IReadOnlyList<ConsentRecord> Read(string file) => SettingsFile.Load<ConsentList>(file, out _).Items;

    public static bool Given(string file, string app, string target) => Read(file).Any(item => Same(item, app, target));

    /// <summary>Keeps the agreement, replacing an earlier one for the same app and program. A file that cannot be written is logged: the user is then asked again next time.</summary>
    public static void Record(string file, string app, string target, string version, DateTime when)
    {
        var items = Read(file).Where(item => !Same(item, app, target)).Append(new ConsentRecord(app, target, version, when));
        Write(file, items);
        Log.Info($"Consent recorded: {app}, {target}, Axit {version}");
    }

    public static void Withdraw(string file, string app, string target)
    {
        Write(file, Read(file).Where(item => !Same(item, app, target)));
        Log.Info($"Consent withdrawn: {app}, {target}");
    }

    /// <summary>"axClaude, Claude Code, 29 October 2026, Axit 3.8.0"; "Update to Axit 3.8.0, 29 October 2026".</summary>
    public static string Line(ConsentRecord item)
    {
        var date = item.When.ToString("d MMMM yyyy", CultureInfo.InvariantCulture);
        return item.App == UpdateApp ? $"Update to {item.Target}, {date}" : $"{item.App}, {item.Target}, {date}, Axit {item.Version}";
    }

    private static void Write(string file, IEnumerable<ConsentRecord> items)
    {
        try
        {
            SettingsFile.Save(file, new ConsentList { Items = [.. items] });
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            Log.Error("The consent file was not written", ex);
        }
    }

    private static bool Same(ConsentRecord item, string app, string target) =>
        string.Equals(item.App, app, StringComparison.OrdinalIgnoreCase) && string.Equals(item.Target, target, StringComparison.OrdinalIgnoreCase);
}
