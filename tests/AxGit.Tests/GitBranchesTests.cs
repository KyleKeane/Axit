using AxGit.Core;

namespace AxGit.Tests;

/// <summary>The Branches view read off git branch --list with AxGit's format (docs/axgit/SPEC.md AG-5).</summary>
public sealed class GitBranchesTests
{
    [Fact]
    public void Branches_carry_their_upstream_state()
    {
        var branches = GitBranches.Parse(
            "axit\t \torigin/axit\t[ahead 3]\n" +
            "main\t*\torigin/main\t\n" +
            "old\t \torigin/old\t[gone]\n" +
            "scratch\t \t\t\n" +
            "both\t \torigin/both\t[ahead 1, behind 2]\n");
        Assert.Equal(
        [
            "axit, origin/axit, 3 ahead",
            "main, current, origin/main, up to date",
            "old, origin/old gone",
            "scratch, no upstream",
            "both, origin/both, 2 behind, 1 ahead",
        ], branches.Select(branch => branch.Label));
        Assert.True(branches[1].Current);
        Assert.False(branches[0].Current);
        Assert.Equal(["switch", "axit"], branches[0].SwitchArguments);
        Assert.Equal("Branches, 5", GitBranches.ListName(branches.Count));
        Assert.Equal(["switch", "-c", "feature"], GitBranches.CreateArguments(" feature "));
    }
}
