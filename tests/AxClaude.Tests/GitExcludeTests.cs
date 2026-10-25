using AxClaude.Core.Privacy;

namespace AxClaude.Tests;

public class GitExcludeTests
{
    private const string GitDefault = "# git ls-files --others --exclude-from=.git/info/exclude\n# *.[oa]\n";

    [Fact]
    public void On_adds_the_block_after_what_is_there_once()
    {
        var once = GitExclude.Apply(GitDefault, on: true);
        Assert.StartsWith(GitDefault, once);
        Assert.Contains("axclaude-*.vt\n", once);
        Assert.EndsWith(GitExclude.BlockEnd + "\n", once);
        Assert.Same(once, GitExclude.Apply(once, on: true));
    }

    /// <summary>A file written with Windows line endings keeps them (it was rewritten with Unix ones until 2026-10-23).</summary>
    [Fact]
    public void The_file_keeps_its_line_ending()
    {
        var windows = GitDefault.Replace("\n", "\r\n") + "secret.txt\r\n";
        var on = GitExclude.Apply(windows, on: true);
        Assert.DoesNotContain("\n", on.Replace("\r\n", string.Empty));
        Assert.EndsWith(GitExclude.BlockEnd + "\r\n", on);
        Assert.StartsWith(windows, on);
        var off = GitExclude.Apply(on, on: false);
        Assert.Equal(windows, off);
    }

    [Fact]
    public void Off_removes_the_block_and_keeps_the_rest()
    {
        var on = GitExclude.Apply(GitDefault + "secret.txt\n", on: true);
        var off = GitExclude.Apply(on, on: false);
        Assert.DoesNotContain("axclaude", off);
        Assert.Contains("secret.txt", off);
        Assert.Equal(off, GitExclude.Apply(off, on: false));
    }

    [Fact]
    public void Update_writes_into_the_repository_and_leaves_a_plain_folder_alone()
    {
        using var temp = new TempFolder("axclaude-exclude");
        var root = temp.Path;
        Assert.Null(GitExclude.Update(root, on: true));
        Directory.CreateDirectory(Path.Combine(root, ".git"));
        var path = GitExclude.Update(root, on: true);
        Assert.Equal(Path.Combine(root, ".git", "info", "exclude"), path);
        Assert.Contains("axclaude-*.txt", File.ReadAllText(path!));
        Assert.Null(GitExclude.Update(root, on: true));
        GitExclude.Update(root, on: false);
        Assert.DoesNotContain("axclaude", File.ReadAllText(path!));
    }

    [Fact]
    public void A_linked_worktree_writes_into_the_common_directory()
    {
        using var temp = new TempFolder("axclaude-exclude");
        var root = temp.Path;
        // What git worktree add leaves behind: a .git file in the worktree, and a commondir file in its gitdir.
        var repoGit = Path.Combine(root, "repo", ".git");
        var worktreeGit = Path.Combine(repoGit, "worktrees", "feature");
        var worktree = Path.Combine(root, "feature");
        Directory.CreateDirectory(worktreeGit);
        Directory.CreateDirectory(worktree);
        File.WriteAllText(Path.Combine(worktree, ".git"), $"gitdir: {worktreeGit}\n");
        File.WriteAllText(Path.Combine(worktreeGit, "commondir"), "../..\n");

        var path = GitExclude.Update(worktree, on: true);
        Assert.Equal(Path.Combine(repoGit, "info", "exclude"), path);
        Assert.Contains("axclaude-*.txt", File.ReadAllText(path!));
    }

    [Fact]
    public void A_file_the_app_offers_to_save_matches_a_pattern()
    {
        // The default names of Export conversation and Record raw stream (MainForm).
        foreach (var name in new[] { "axclaude-myproject-20260925-1300.txt", "axclaude-20260925-1300.vt", "axclaude-20260925-1300.vt.chunks.txt" })
        {
            Assert.Contains(GitExclude.Patterns, pattern => System.IO.Enumeration.FileSystemName.MatchesSimpleExpression(pattern, name));
        }
    }
}
