using System.Text;

namespace AxGit.Core;

/// <summary>What happened to a file, read off the two status letters of <c>git status --porcelain=v2</c> (SPEC.md AG-3.1).</summary>
public enum ChangeKind
{
    Modified,
    Added,
    Deleted,
    Renamed,
    Copied,
    TypeChanged,
    Untracked,
    Conflict,
}

/// <summary>
/// One changed file (SPEC.md AG-3.1): its path from the repository's root, what happened to it, whether the change
/// is staged for the next commit, unstaged in the working tree, or both, and the old path of a rename or copy. The
/// list item's text is <see cref="Label"/>; the git arguments for Space and Enter are here too, so that the window
/// has nothing to know about git.
/// </summary>
public sealed record ChangedFile(string Path, ChangeKind Kind, bool Staged, bool Unstaged, string? OldPath = null)
{
    /// <summary>"Modified src/Foo.cs, unstaged"; "Renamed old.txt → new.txt, staged"; "Untracked notes.txt"; "Conflict src/x.cs".</summary>
    public string Label
    {
        get
        {
            var what = Kind switch
            {
                ChangeKind.Renamed or ChangeKind.Copied when OldPath is not null => $"{KindName} {OldPath} → {Path}",
                _ => $"{KindName} {Path}",
            };
            return Kind is ChangeKind.Untracked or ChangeKind.Conflict ? what : $"{what}, {State}";
        }
    }

    public string KindName => Kind switch
    {
        ChangeKind.Modified => "Modified",
        ChangeKind.Added => "Added",
        ChangeKind.Deleted => "Deleted",
        ChangeKind.Renamed => "Renamed",
        ChangeKind.Copied => "Copied",
        ChangeKind.TypeChanged => "Type changed",
        ChangeKind.Untracked => "Untracked",
        _ => "Conflict",
    };

    /// <summary>"staged", "unstaged" or "staged and unstaged".</summary>
    public string State => Staged && Unstaged ? "staged and unstaged" : Staged ? "staged" : "unstaged";

    /// <summary>A conflict is not staged from the list (AG-3.2).</summary>
    public bool CanStage => Kind != ChangeKind.Conflict;

    /// <summary>Space: the whole file into the index when any of it is unstaged, otherwise out of it (AG-3.2).</summary>
    public IReadOnlyList<string> StageArguments =>
        Unstaged ? ["add", "--", Path] : ["restore", "--staged", "--", Path];

    /// <summary>What Space will say.</summary>
    public string StageAnnouncement => Unstaged ? $"Staged {Path}" : $"Unstaged {Path}";

    /// <summary>Enter: the diff of what changed (AG-3.3); empty for an untracked file, whose own text is shown instead.</summary>
    public IReadOnlyList<string> DiffArguments => Kind switch
    {
        ChangeKind.Untracked => [],
        // --no-ext-diff --no-textconv: never a diff program the repository names (security audit S-2).
        _ when Staged && Unstaged => ["diff", "--no-ext-diff", "--no-textconv", "HEAD", "--", Path],
        _ when Staged => ["diff", "--no-ext-diff", "--no-textconv", "--cached", "--", Path],
        _ => ["diff", "--no-ext-diff", "--no-textconv", "--", Path],
    };

    /// <summary>Delete: the file back to HEAD, index and working tree (AG-3.4); empty for an untracked file.</summary>
    public IReadOnlyList<string> DiscardArguments =>
        Kind == ChangeKind.Untracked ? [] : ["restore", "--staged", "--worktree", "--", Path];
}

/// <summary>The repository's state as one status call gives it (SPEC.md AG-2.3, AG-3.1): the branch, its upstream and the changed files.</summary>
public sealed record RepositoryStatus(string? Branch, string? Upstream, int Ahead, int Behind, IReadOnlyList<ChangedFile> Files)
{
    public static readonly RepositoryStatus Empty = new(null, null, 0, 0, []);

    public int StagedCount => Files.Count(file => file.Staged);

    /// <summary>The list's name: "Changes, 3 files", "Changes, 1 file", "Changes, no changes".</summary>
    public string ListName => Files.Count switch
    {
        0 => "Changes, no changes",
        1 => "Changes, 1 file",
        var n => $"Changes, {n} files",
    };

    /// <summary>"main · 2 ahead", "main · 1 behind, 2 ahead", "main · up to date", "main · no upstream", "detached".</summary>
    public string BranchState
    {
        get
        {
            var branch = Branch ?? "detached";
            if (Upstream is null)
            {
                return $"{branch} · no upstream";
            }

            var parts = new List<string>();
            if (Behind > 0)
            {
                parts.Add($"{Behind} behind");
            }

            if (Ahead > 0)
            {
                parts.Add($"{Ahead} ahead");
            }

            return $"{branch} · {(parts.Count == 0 ? "up to date" : string.Join(", ", parts))}";
        }
    }

    /// <summary>The status bar's left label: "main · up to date · 3 changed, 1 staged".</summary>
    public string StatusText => $"{BranchState} · {Files.Count} changed, {StagedCount} staged";
}

/// <summary>
/// Reads <c>git status --porcelain=v2 --branch</c> (SPEC.md AG-3.1): the branch headers, the ordinary entries
/// ("1 XY …"), renames and copies ("2 XY … R100 new\told"), conflicts ("u …") and untracked files ("? path"). Paths
/// with control characters come C-quoted and are unquoted here (<c>core.quotepath</c> is off, so letters stay as they
/// are). Conflicts are listed first, the rest in git's order.
/// </summary>
public static class GitStatus
{
    public static readonly IReadOnlyList<string> Arguments = ["status", "--porcelain=v2", "--branch", "--untracked-files=all"];

    public static RepositoryStatus Parse(string output)
    {
        string? branch = null;
        string? upstream = null;
        var ahead = 0;
        var behind = 0;
        var files = new List<ChangedFile>();
        var conflicts = new List<ChangedFile>();
        foreach (var raw in output.Split('\n'))
        {
            var line = raw.TrimEnd('\r');
            if (line.Length == 0)
            {
                continue;
            }

            if (line.StartsWith("# branch.head ", StringComparison.Ordinal))
            {
                var head = line["# branch.head ".Length..];
                branch = head == "(detached)" ? null : head;
            }
            else if (line.StartsWith("# branch.upstream ", StringComparison.Ordinal))
            {
                upstream = line["# branch.upstream ".Length..];
            }
            else if (line.StartsWith("# branch.ab ", StringComparison.Ordinal))
            {
                // "+2 -1"
                var parts = line["# branch.ab ".Length..].Split(' ', StringSplitOptions.RemoveEmptyEntries);
                foreach (var part in parts)
                {
                    if (part.Length > 1 && int.TryParse(part[1..], out var n))
                    {
                        if (part[0] == '+')
                        {
                            ahead = n;
                        }
                        else if (part[0] == '-')
                        {
                            behind = n;
                        }
                    }
                }
            }
            else if (line.StartsWith("1 ", StringComparison.Ordinal))
            {
                // 1 <XY> <sub> <mH> <mI> <mW> <hH> <hI> <path>
                var fields = line.Split(' ', 9);
                if (fields.Length == 9)
                {
                    files.Add(Entry(fields[1], Unquote(fields[8]), null));
                }
            }
            else if (line.StartsWith("2 ", StringComparison.Ordinal))
            {
                // 2 <XY> <sub> <mH> <mI> <mW> <hH> <hI> <X><score> <path>\t<origPath>
                var fields = line.Split(' ', 10);
                if (fields.Length == 10)
                {
                    var paths = fields[9].Split('\t', 2);
                    files.Add(Entry(fields[1], Unquote(paths[0]), paths.Length > 1 ? Unquote(paths[1]) : null));
                }
            }
            else if (line.StartsWith("u ", StringComparison.Ordinal))
            {
                // u <XY> <sub> <m1> <m2> <m3> <mW> <h1> <h2> <h3> <path>
                var fields = line.Split(' ', 11);
                if (fields.Length == 11)
                {
                    conflicts.Add(new ChangedFile(Unquote(fields[10]), ChangeKind.Conflict, false, true));
                }
            }
            else if (line.StartsWith("? ", StringComparison.Ordinal))
            {
                files.Add(new ChangedFile(Unquote(line[2..]), ChangeKind.Untracked, false, true));
            }
        }

        return new RepositoryStatus(branch, upstream, ahead, behind, [.. conflicts, .. files]);
    }

    private static ChangedFile Entry(string xy, string path, string? oldPath)
    {
        var index = xy.Length > 0 ? xy[0] : '.';
        var tree = xy.Length > 1 ? xy[1] : '.';
        var staged = index != '.';
        var unstaged = tree != '.';
        var kind = Kind(staged ? index : tree);
        return new ChangedFile(path, kind, staged, unstaged, oldPath);
    }

    private static ChangeKind Kind(char letter) => letter switch
    {
        'A' => ChangeKind.Added,
        'D' => ChangeKind.Deleted,
        'R' => ChangeKind.Renamed,
        'C' => ChangeKind.Copied,
        'T' => ChangeKind.TypeChanged,
        _ => ChangeKind.Modified,
    };

    /// <summary>A path git wrote as a C string ("a\tb.txt", "\303\244.md" without core.quotepath off): the characters back.</summary>
    public static string Unquote(string path)
    {
        if (path.Length < 2 || path[0] != '"' || path[^1] != '"')
        {
            return path;
        }

        var bytes = new List<byte>();
        for (var i = 1; i < path.Length - 1; i++)
        {
            var c = path[i];
            if (c != '\\' || i + 1 >= path.Length - 1)
            {
                bytes.AddRange(Encoding.UTF8.GetBytes(c.ToString()));
                continue;
            }

            var next = path[++i];
            switch (next)
            {
                case 'n': bytes.Add((byte)'\n'); break;
                case 't': bytes.Add((byte)'\t'); break;
                case 'r': bytes.Add((byte)'\r'); break;
                case '\\': bytes.Add((byte)'\\'); break;
                case '"': bytes.Add((byte)'"'); break;
                case >= '0' and <= '7':
                    // Up to three octal digits, one byte of UTF-8.
                    var digits = next.ToString();
                    while (digits.Length < 3 && i + 1 < path.Length - 1 && path[i + 1] is >= '0' and <= '7')
                    {
                        digits += path[++i];
                    }

                    bytes.Add(Convert.ToByte(digits, 8));
                    break;
                default:
                    bytes.AddRange(Encoding.UTF8.GetBytes(next.ToString()));
                    break;
            }
        }

        return Encoding.UTF8.GetString(bytes.ToArray());
    }
}
