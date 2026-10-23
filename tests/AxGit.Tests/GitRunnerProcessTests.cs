using AxGit.Core;

namespace AxGit.Tests;

/// <summary>
/// Running git as a process (docs/axgit/SPEC.md AG-1.5), against a fake git.cmd the test writes: the output and the
/// exit code, the timeout that stops a command, and a program git started that keeps the output open after git has
/// finished (the case of 2026-10-23, when a signing agent held the window at "Working").
/// </summary>
public sealed class GitRunnerProcessTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "axgit-tests-" + Guid.NewGuid().ToString("N"));
    private readonly string _program = GitRunner.Program;

    public GitRunnerProcessTests()
    {
        Directory.CreateDirectory(_dir);
    }

    public void Dispose()
    {
        GitRunner.Program = _program;
        try
        {
            Directory.Delete(_dir, recursive: true);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // A fake still running holds its folder for a moment; the temp folder is cleaned by Windows.
        }
    }

    /// <summary>A fake git: a batch file with the body given, which the runner starts in git's place.</summary>
    private void Fake(string body)
    {
        var path = Path.Combine(_dir, "git.cmd");
        File.WriteAllText(path, "@echo off\r\n" + body + "\r\n");
        GitRunner.Program = path;
    }

    [Fact]
    public async Task The_output_and_the_exit_code_come_back()
    {
        Fake("echo hello\r\necho oops 1>&2\r\nexit /b 3");
        var result = await GitRunner.RunAsync(@"C:\repo", ["status"], TimeSpan.FromSeconds(30));
        Assert.Equal(3, result.ExitCode);
        Assert.False(result.Ok);
        Assert.Equal("hello", result.Output.Trim());
        Assert.Equal("oops", result.Error.Trim());
    }

    [Fact]
    public async Task A_command_past_the_timeout_is_stopped()
    {
        Fake("ping -n 30 127.0.0.1 >nul");
        var result = await GitRunner.RunAsync(null, ["fetch"], TimeSpan.FromSeconds(1));
        Assert.Equal(-1, result.ExitCode);
        Assert.Contains("did not finish", result.Error);
    }

    [Fact]
    public async Task A_program_git_started_that_keeps_the_output_open_ends_at_the_timeout()
    {
        // The fake exits at once, but the ping it started inherits its output pipe and keeps it open.
        Fake("echo early\r\nstart \"\" /b ping -n 20 127.0.0.1\r\nexit /b 0");
        var result = await GitRunner.RunAsync(null, ["commit"], TimeSpan.FromSeconds(2));
        Assert.Equal(0, result.ExitCode);
        Assert.Contains("kept its output open", result.Error);
    }
}
