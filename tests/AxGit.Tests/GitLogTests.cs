using AxGit.Core;

namespace AxGit.Tests;

/// <summary>The Log view read off git log with AxGit's format (docs/axgit/SPEC.md AG-4).</summary>
public sealed class GitLogTests
{
    [Fact]
    public void Each_line_is_a_commit_with_its_label()
    {
        var commits = GitLog.Parse(
            "3369581\t2026-10-18\tKyle\tRelease 2.11.0\n" +
            "efe0662\t2026-10-18\tKyle\tRecord AxGit and AxFile as later apps in the plan\n" +
            "\n" +
            "c2e4c25\t2026-10-18\tKyle\tSave conversation as is Export conversation: colons, too\r\n");
        Assert.Equal(3, commits.Count);
        Assert.Equal("3369581 2026-10-18 Kyle: Release 2.11.0", commits[0].Label);
        Assert.Equal("c2e4c25 2026-10-18 Kyle: Save conversation as is Export conversation: colons, too", commits[2].Label);
        Assert.Equal(["show", "--stat", "--format=medium", "efe0662"], commits[1].ShowArguments);
    }

    [Fact]
    public void The_arguments_and_the_name_follow_the_spec()
    {
        Assert.Equal(["log", "-n200", "--date=short", "--format=%h%x09%ad%x09%an%x09%s"], GitLog.Arguments);
        Assert.Equal("Log, 200 commits", GitLog.ListName(200));
        Assert.Equal("Log, 1 commit", GitLog.ListName(1));
        Assert.Equal("Log, no commits", GitLog.ListName(0));
        Assert.Empty(GitLog.Parse("not a log line\n"));
    }
}
