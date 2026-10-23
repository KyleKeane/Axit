namespace AxGit.Core;

/// <summary>One commit of the log (SPEC.md AG-4.1): the short hash, the date, the author and the subject.</summary>
public sealed record Commit(string Hash, string Date, string Author, string Subject)
{
    /// <summary>"3a1f2c0 2026-10-18 Kyle: Release 2.11.0", the list item's text.</summary>
    public string Label => $"{Hash} {Date} {Author}: {Subject}";

    /// <summary>Enter: the commit's message and the files it changed (AG-4.2).</summary>
    public IReadOnlyList<string> ShowArguments => ["show", "--stat", "--format=medium", Hash];
}

/// <summary>Reads the log as axGIT asks for it: one commit per line, the four fields separated by tabs (SPEC.md AG-4.1).</summary>
public static class GitLog
{
    public const int Count = 200;

    public static readonly IReadOnlyList<string> Arguments = ["log", $"-n{Count}", "--date=short", "--format=%h%x09%ad%x09%an%x09%s"];

    public static IReadOnlyList<Commit> Parse(string output)
    {
        var commits = new List<Commit>();
        foreach (var raw in output.Split('\n'))
        {
            var line = raw.TrimEnd('\r');
            if (line.Length == 0)
            {
                continue;
            }

            var fields = line.Split('\t', 4);
            if (fields.Length < 4)
            {
                continue;
            }

            commits.Add(new Commit(fields[0], fields[1], fields[2], fields[3]));
        }

        return commits;
    }

    /// <summary>The list's name: "Log, 200 commits", "Log, 1 commit", "Log, no commits".</summary>
    public static string ListName(int count) => count switch
    {
        0 => "Log, no commits",
        1 => "Log, 1 commit",
        _ => $"Log, {count} commits",
    };
}
