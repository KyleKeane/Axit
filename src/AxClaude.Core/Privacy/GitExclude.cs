using Axit.Core;

namespace AxClaude.Core.Privacy;

/// <summary>
/// Keeps axClaude's own files (raw recordings, exported conversations; all named <c>axclaude-…</c>) out of the git
/// repository of the project folder Claude runs in (D34). The patterns go into the repository's local exclude file,
/// <c>.git\info\exclude</c>, which git reads like a <c>.gitignore</c> but never commits, so the project's own files are
/// not touched. A marked block holds them; off removes the block, to commit such a file on purpose.
/// </summary>
public static class GitExclude
{
    public const string BlockStart = "# axClaude: its recordings and saved conversations stay out of git (Options menu)";
    public const string BlockEnd = "# end axClaude";

    /// <summary>The patterns: every file axClaude offers to save starts with <c>axclaude-</c>.</summary>
    public static readonly IReadOnlyList<string> Patterns = ["axclaude-*.vt", "axclaude-*.vt.chunks.txt", "axclaude-*.txt"];

    /// <summary>
    /// The exclude file's text with the block present (<paramref name="on"/>) or gone, everything else kept, with the
    /// line ending the file had; the same text when nothing changes.
    /// </summary>
    public static string Apply(string existing, bool on)
    {
        var newline = existing.Contains("\r\n", StringComparison.Ordinal) ? "\r\n" : "\n";
        var lines = existing.Replace("\r\n", "\n").Split('\n').ToList();
        var start = lines.IndexOf(BlockStart);
        var end = start < 0 ? -1 : lines.IndexOf(BlockEnd, start);
        var block = new List<string> { BlockStart };
        block.AddRange(Patterns);
        block.Add(BlockEnd);
        if (start >= 0 && end > start)
        {
            if (on && lines.Skip(start).Take(end - start + 1).SequenceEqual(block))
            {
                return existing;
            }

            lines.RemoveRange(start, end - start + 1);
        }
        else if (!on)
        {
            return existing;
        }

        if (on)
        {
            // After what is there, with the file's last line ended.
            while (lines.Count > 0 && lines[^1].Length == 0)
            {
                lines.RemoveAt(lines.Count - 1);
            }

            lines.AddRange(block);
            lines.Add(string.Empty);
        }

        return string.Join(newline, lines);
    }

    /// <summary>
    /// Writes the block into the exclude file of the repository at <paramref name="folder"/> (a <c>.git</c> folder, or
    /// a <c>.git</c> file pointing at one, as in a worktree), or removes it. Returns the exclude file's path when it
    /// changed; null when there is no repository or nothing to change.
    /// </summary>
    public static string? Update(string folder, bool on)
    {
        if (GitDirectory(folder) is not { } gitDir)
        {
            return null;
        }

        var path = Path.Combine(gitDir, "info", "exclude");
        var existing = File.Exists(path) ? File.ReadAllText(path) : string.Empty;
        var updated = Apply(existing, on);
        if (updated == existing)
        {
            return null;
        }

        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, updated);
        return path;
    }

    private static string? GitDirectory(string folder)
    {
        var dotGit = Path.Combine(folder, ".git");
        if (Directory.Exists(dotGit))
        {
            return dotGit;
        }

        if (File.Exists(dotGit) && File.ReadAllText(dotGit).Trim() is var text && text.StartsWith("gitdir:", StringComparison.Ordinal))
        {
            // S-9: a .git file in a downloaded project can name any folder, a share included; only one on this
            // computer is read or written, so that nothing makes Windows authenticate to someone else's server.
            var full = Resolve(folder, text["gitdir:".Length..]);
            if (!LocalPaths.IsLocal(full) || !Directory.Exists(full))
            {
                return null;
            }

            // A linked worktree's gitdir is <repo>/.git/worktrees/<name>, whose info/exclude git never reads: the
            // exclude file belongs to the common directory, which the commondir file there names.
            var commonDir = Path.Combine(full, "commondir");
            if (!File.Exists(commonDir))
            {
                return full;
            }

            var common = Resolve(full, File.ReadAllText(commonDir));
            return LocalPaths.IsLocal(common) ? common : null;
        }

        return null;
    }

    private static string Resolve(string from, string target)
    {
        target = target.Trim();
        return Path.GetFullPath(Path.IsPathRooted(target) ? target : Path.Combine(from, target));
    }
}
