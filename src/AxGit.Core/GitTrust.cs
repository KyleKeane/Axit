using System.Text.RegularExpressions;

namespace AxGit.Core;

/// <summary>
/// A repository's own programs (SPEC.md AG-1.6; docs/axit/security-audit-2026-10-24.md S-2). A repository that comes in
/// an archive, on a USB stick or a share brings its own <c>.git/config</c> and <c>.git/hooks</c>, which git obeys: a
/// file monitor run by <c>git status</c>, diff and filter programs, hooks on commit and switch, an ssh command on fetch.
/// axGIT reads which of these the repository names (reading the configuration runs nothing) and, until the user trusts
/// the repository, runs every git command with each of them set to nothing. Pure, tested.
/// </summary>
public static partial class GitTrust
{
    /// <summary>The configuration's names, the repository's own file and what it includes, one per line.</summary>
    public static readonly IReadOnlyList<string> ConfigArguments = ["config", "--local", "--includes", "--name-only", "--list"];

    /// <summary>The hooks folder git would use.</summary>
    public static readonly IReadOnlyList<string> HooksArguments = ["rev-parse", "--git-path", "hooks"];

    /// <summary>
    /// A hooks folder that holds nothing: git looks for <c>NUL\pre-commit</c> and finds no file, on every Windows
    /// (a folder of Axit's own could be filled by another program).
    /// </summary>
    public const string NoHooks = "NUL";

    /// <summary>Whether a configuration name makes git run a program.</summary>
    public static bool RunsAProgram(string name) => ProgramNames().IsMatch(name.Trim());

    /// <summary>
    /// What in the repository runs a program: the configuration names that do, from <paramref name="configNames"/>
    /// (git's answer to <see cref="ConfigArguments"/>), and "hook <name>" for each hook in <paramref name="hooksFolder"/>
    /// that is not one of git's own samples.
    /// </summary>
    public static IReadOnlyList<string> Risks(string configNames, string? hooksFolder)
    {
        var risks = configNames.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(RunsAProgram)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (hooksFolder is not null && Directory.Exists(hooksFolder))
        {
            risks.AddRange(Directory.EnumerateFiles(hooksFolder)
                .Select(Path.GetFileName)
                .Where(name => name is not null && !name.EndsWith(".sample", StringComparison.OrdinalIgnoreCase))
                .Order(StringComparer.OrdinalIgnoreCase)
                .Select(name => "hook " + name));
        }

        return risks;
    }

    /// <summary>
    /// The <c>-c</c> settings that make git ignore what <paramref name="risks"/> names: every such name set to nothing,
    /// and the hooks folder to <see cref="NoHooks"/> when any hook or hooks folder is named. Empty for a trusted
    /// repository.
    /// </summary>
    public static IReadOnlyList<string> Overrides(IReadOnlyList<string> risks)
    {
        var list = new List<string>();
        var hooks = false;
        foreach (var risk in risks)
        {
            if (risk.StartsWith("hook ", StringComparison.Ordinal) || risk.Equals("core.hookspath", StringComparison.OrdinalIgnoreCase))
            {
                hooks = true;
                continue;
            }

            list.Add("-c");
            list.Add(risk + "=");
        }

        if (hooks)
        {
            list.Add("-c");
            list.Add("core.hooksPath=" + NoHooks);
        }

        return list;
    }

    /// <summary>The names that run a program: the core programs, diff, merge and filter drivers, credential helpers, signing, the remote's own programs; an included file's names are listed among the repository's own (<c>--includes</c>), and aliases run only when called by name, which axGIT never does.</summary>
    [GeneratedRegex(@"^(core\.(fsmonitor|hookspath|sshcommand|pager|editor|askpass|gitproxy|alternaterefscommand)|(diff|merge|filter|difftool|mergetool)\..+\.(command|textconv|driver|clean|smudge|process|cmd|path)|credential(\..+)?\.helper|gpg(\..+)?\.program|sequence\.editor|remote\..+\.(uploadpack|receivepack|proxy)|(uploadpack|receivepack)\..+)$", RegexOptions.IgnoreCase)]
    private static partial Regex ProgramNames();
}
