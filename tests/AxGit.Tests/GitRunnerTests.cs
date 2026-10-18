using AxGit.Core;

namespace AxGit.Tests;

/// <summary>The pure parts of running git and the settings (docs/axgit/SPEC.md AG-1.5, AG-8.1); no git process here.</summary>
public sealed class GitRunnerTests
{
    [Fact]
    public void A_command_names_the_repository_and_turns_colour_and_quoting_off()
    {
        Assert.Equal(
            ["-C", @"C:\repo", "-c", "core.quotepath=false", "-c", "color.ui=never", "status", "--porcelain=v2"],
            GitRunner.Command(@"C:\repo", ["status", "--porcelain=v2"]));
        Assert.Equal(["-c", "core.quotepath=false", "-c", "color.ui=never", "--version"], GitRunner.Command(null, ["--version"]));
    }

    [Fact]
    public void A_result_knows_its_first_line_and_all_its_text()
    {
        var failed = new GitResult(1, "", "\nerror: pathspec 'x' did not match\nhint: more\n");
        Assert.False(failed.Ok);
        Assert.Equal("error: pathspec 'x' did not match", failed.FirstLine);
        Assert.Equal("error: pathspec 'x' did not match\nhint: more", failed.AllText);
        Assert.Equal("Already up to date.", new GitResult(0, "Already up to date.\n", "").FirstLine);
        Assert.Equal("git failed", new GitResult(128, "", "").FirstLine);
    }

    [Fact]
    public void Recent_repositories_are_kept_newest_first_without_repeats()
    {
        var settings = new GitSettings();
        for (var i = 0; i < 12; i++)
        {
            settings.RememberRepository($@"C:\repo{i}");
        }

        settings.RememberRepository(@"c:\REPO11");
        Assert.Equal(GitSettings.MaxRecentRepositories, settings.RecentRepositories.Count);
        Assert.Equal(@"c:\REPO11", settings.RecentRepositories[0]);
        Assert.Equal(@"C:\repo10", settings.RecentRepositories[1]);
        Assert.DoesNotContain(@"C:\repo0", settings.RecentRepositories);
    }
}
