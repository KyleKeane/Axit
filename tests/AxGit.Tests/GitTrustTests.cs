using AxGit.Core;

namespace AxGit.Tests;

/// <summary>A repository's own programs (SPEC.md AG-1.6, security audit S-2): what is found, and how it is set aside.</summary>
public sealed class GitTrustTests
{
    [Fact]
    public void The_names_that_run_a_program_are_found_and_the_rest_left()
    {
        const string names = "core.repositoryformatversion\ncore.fsmonitor\ndiff.pdf.textconv\nfilter.lfs.clean\nremote.origin.url\nuser.name\ncore.sshCommand\ncredential.helper\n";
        Assert.Equal(["core.fsmonitor", "diff.pdf.textconv", "filter.lfs.clean", "core.sshCommand", "credential.helper"], GitTrust.Risks(names, null));
    }

    [Fact]
    public void A_hook_is_a_risk_and_a_sample_is_not()
    {
        using var temp = new TempFolder("axgit-hooks");
        var hooks = temp.Path;
        File.WriteAllText(Path.Combine(hooks, "pre-commit.sample"), string.Empty);
        File.WriteAllText(Path.Combine(hooks, "post-checkout"), string.Empty);
        Assert.Equal(["hook post-checkout"], GitTrust.Risks(string.Empty, hooks));
    }

    [Fact]
    public void An_untrusted_repository_runs_git_with_each_program_set_to_nothing()
    {
        Assert.Equal(["-c", "diff.pdf.textconv=", "-c", "core.sshCommand=", "-c", "core.hooksPath=NUL"], GitTrust.Overrides(["diff.pdf.textconv", "hook pre-commit", "core.sshCommand"]));
        Assert.Empty(GitTrust.Overrides([]));
    }

    [Fact]
    public void Every_command_keeps_the_file_monitor_and_the_ext_transport_off()
    {
        var command = GitRunner.Command(@"C:\repo", ["status"], ["-c", "core.sshCommand="]);
        Assert.Equal(["-C", @"C:\repo", "-c", "core.quotepath=false", "-c", "color.ui=never", "-c", "core.fsmonitor=false", "-c", "protocol.ext.allow=never", "-c", "core.sshCommand=", "status"], command);
    }
}
