namespace AxGit.Core;

/// <summary>One local branch (SPEC.md AG-5.1): its name, whether it is checked out, its upstream and how far they are apart.</summary>
public sealed record Branch(string Name, bool Current, string? Upstream, int Ahead, int Behind, bool Gone = false)
{
    /// <summary>"main, current, origin/main, 1 ahead"; "axit, no upstream"; "old, origin/old gone".</summary>
    public string Label
    {
        get
        {
            var parts = new List<string> { Name };
            if (Current)
            {
                parts.Add("current");
            }

            if (Upstream is null)
            {
                parts.Add("no upstream");
            }
            else if (Gone)
            {
                parts.Add($"{Upstream} gone");
            }
            else
            {
                parts.Add(Upstream);
                var state = new List<string>();
                if (Behind > 0)
                {
                    state.Add($"{Behind} behind");
                }

                if (Ahead > 0)
                {
                    state.Add($"{Ahead} ahead");
                }

                parts.Add(state.Count == 0 ? "up to date" : string.Join(", ", state));
            }

            return string.Join(", ", parts);
        }
    }

    /// <summary>Enter: check the branch out (AG-5.2).</summary>
    public IReadOnlyList<string> SwitchArguments => ["switch", Name];
}

/// <summary>Reads the branch list as AxGit asks for it: name, HEAD mark, upstream and its track state, separated by tabs (SPEC.md AG-5.1).</summary>
public static class GitBranches
{
    public static readonly IReadOnlyList<string> Arguments =
        ["branch", "--list", "--format=%(refname:short)%09%(HEAD)%09%(upstream:short)%09%(upstream:track)"];

    public static IReadOnlyList<Branch> Parse(string output)
    {
        var branches = new List<Branch>();
        foreach (var raw in output.Split('\n'))
        {
            var line = raw.TrimEnd('\r');
            if (line.Length == 0)
            {
                continue;
            }

            var fields = line.Split('\t', 4);
            // git lists a detached HEAD as "(HEAD detached at 3a1f2c0)", in parentheses: no branch (AG-5.1).
            if (fields.Length < 2 || fields[0].Length == 0 || fields[0].StartsWith('('))
            {
                continue;
            }

            var upstream = fields.Length > 2 && fields[2].Length > 0 ? fields[2] : null;
            var (ahead, behind, gone) = Track(fields.Length > 3 ? fields[3] : string.Empty);
            branches.Add(new Branch(fields[0], fields[1].Trim() == "*", upstream, ahead, behind, gone));
        }

        return branches;
    }

    /// <summary>"[ahead 1, behind 2]", "[gone]" or nothing.</summary>
    private static (int Ahead, int Behind, bool Gone) Track(string text)
    {
        var inner = text.Trim().TrimStart('[').TrimEnd(']');
        if (inner == "gone")
        {
            return (0, 0, true);
        }

        var ahead = 0;
        var behind = 0;
        foreach (var part in inner.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
        {
            var words = part.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (words.Length == 2 && int.TryParse(words[1], out var n))
            {
                if (words[0] == "ahead")
                {
                    ahead = n;
                }
                else if (words[0] == "behind")
                {
                    behind = n;
                }
            }
        }

        return (ahead, behind, false);
    }

    /// <summary>The list's name: "Branches, 2".</summary>
    public static string ListName(int count) => $"Branches, {count}";

    /// <summary>Branches → New branch…: a new branch from the current commit, checked out (AG-5.3).</summary>
    public static IReadOnlyList<string> CreateArguments(string name) => ["switch", "-c", name.Trim()];
}
