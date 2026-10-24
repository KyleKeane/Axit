using AxGit.Core;

namespace AxGit.Tests;

/// <summary>The Changes view read off git status --porcelain=v2 (docs/axgit/SPEC.md AG-3.1, AG-2.3), as git 2.47 prints it.</summary>
public sealed class GitStatusTests
{
    private const string Sample =
        "# branch.oid 3369581c0ffee\n" +
        "# branch.head main\n" +
        "# branch.upstream origin/main\n" +
        "# branch.ab +2 -1\n" +
        "1 .M N... 100644 100644 100644 abc123 abc123 src/Foo.cs\n" +
        "1 M. N... 100644 100644 100644 abc123 def456 docs/plan.md\n" +
        "1 MM N... 100644 100644 100644 abc123 def456 CHANGELOG.md\n" +
        "1 A. N... 000000 100644 100644 000000 789abc new.txt\n" +
        "1 .D N... 100644 100644 000000 abc123 abc123 gone.txt\n" +
        "2 R. N... 100644 100644 100644 abc123 abc123 R100 docs/new-name.md\tdocs/old-name.md\n" +
        "u UU N... 100644 100644 100644 100644 aaa bbb ccc src/Conflict.cs\n" +
        "? notes.txt\n";

    [Fact]
    public void The_branch_and_its_upstream_are_read()
    {
        var status = GitStatus.Parse(Sample);
        Assert.Equal("main", status.Branch);
        Assert.Equal("origin/main", status.Upstream);
        Assert.Equal(2, status.Ahead);
        Assert.Equal(1, status.Behind);
        Assert.Equal("main · 1 behind, 2 ahead", status.BranchState);
        Assert.Equal("main · 1 behind, 2 ahead · 8 changed, 4 staged", status.StatusText);
        Assert.Equal("Changes, 8 files", status.ListName);
    }

    [Fact]
    public void Every_kind_of_change_gets_its_label_with_conflicts_first()
    {
        var labels = GitStatus.Parse(Sample).Files.Select(file => file.Label).ToList();
        Assert.Equal(
        [
            "Conflict src/Conflict.cs",
            "Modified src/Foo.cs, unstaged",
            "Modified docs/plan.md, staged",
            "Modified CHANGELOG.md, staged and unstaged",
            "Added new.txt, staged",
            "Deleted gone.txt, unstaged",
            "Renamed docs/old-name.md → docs/new-name.md, staged",
            "Untracked notes.txt",
        ], labels);
    }

    [Fact]
    public void Space_stages_what_is_unstaged_and_unstages_the_rest()
    {
        var files = GitStatus.Parse(Sample).Files.ToDictionary(file => file.Path);
        Assert.Equal(["add", "--", "src/Foo.cs"], files["src/Foo.cs"].StageArguments);
        Assert.Equal("Staged src/Foo.cs", files["src/Foo.cs"].StageAnnouncement);
        Assert.Equal(["restore", "--staged", "--", "docs/plan.md"], files["docs/plan.md"].StageArguments);
        Assert.Equal("Unstaged docs/plan.md", files["docs/plan.md"].StageAnnouncement);
        Assert.Equal(["add", "--", "CHANGELOG.md"], files["CHANGELOG.md"].StageArguments);
        Assert.Equal(["add", "--", "notes.txt"], files["notes.txt"].StageArguments);
        Assert.False(files["src/Conflict.cs"].CanStage);
    }

    [Fact]
    public void Enter_and_Delete_take_the_right_diff_and_restore()
    {
        var files = GitStatus.Parse(Sample).Files.ToDictionary(file => file.Path);
        Assert.Equal(["diff", "--no-ext-diff", "--no-textconv", "--", "src/Foo.cs"], files["src/Foo.cs"].DiffArguments);
        Assert.Equal(["diff", "--no-ext-diff", "--no-textconv", "--cached", "--", "docs/plan.md"], files["docs/plan.md"].DiffArguments);
        Assert.Equal(["diff", "--no-ext-diff", "--no-textconv", "HEAD", "--", "CHANGELOG.md"], files["CHANGELOG.md"].DiffArguments);
        Assert.Empty(files["notes.txt"].DiffArguments);
        Assert.Equal(["restore", "--staged", "--worktree", "--", "src/Foo.cs"], files["src/Foo.cs"].DiscardArguments);
        Assert.Empty(files["notes.txt"].DiscardArguments);
    }

    [Fact]
    public void A_clean_tree_a_detached_head_and_no_upstream_are_said_as_such()
    {
        var clean = GitStatus.Parse("# branch.oid abc\n# branch.head axit\n");
        Assert.Equal("Changes, no changes", clean.ListName);
        Assert.Equal("axit · no upstream · 0 changed, 0 staged", clean.StatusText);

        var detached = GitStatus.Parse("# branch.oid abc\n# branch.head (detached)\n1 .M N... 100644 100644 100644 a b x.txt\n");
        Assert.Null(detached.Branch);
        Assert.Equal("detached · no upstream", detached.BranchState);
        Assert.Equal("Changes, 1 file", detached.ListName);

        var upToDate = GitStatus.Parse("# branch.head main\n# branch.upstream origin/main\n# branch.ab +0 -0\n");
        Assert.Equal("main · up to date", upToDate.BranchState);
        Assert.Equal(RepositoryStatus.Empty.ListName, GitStatus.Parse(string.Empty).ListName);
    }

    [Fact]
    public void Quoted_paths_come_back_as_characters()
    {
        Assert.Equal("a\tb.txt", GitStatus.Unquote("\"a\\tb.txt\""));
        Assert.Equal("say \"hi\".md", GitStatus.Unquote("\"say \\\"hi\\\".md\""));
        Assert.Equal("ä.md", GitStatus.Unquote("\"\\303\\244.md\""));
        Assert.Equal("plain.md", GitStatus.Unquote("plain.md"));
        var status = GitStatus.Parse("1 .M N... 100644 100644 100644 a b \"odd\\tname.txt\"\n");
        Assert.Equal("odd\tname.txt", status.Files[0].Path);
    }
}
