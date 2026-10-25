using System.Diagnostics;
using AxClaude.Core.Pty;

namespace AxClaude.Tests;

public class ClaudeLauncherTests
{
    /// <summary>A fake claude.cmd with the body given, in a folder of its own that goes with the test.</summary>
    private sealed class FakeClaude : IDisposable
    {
        private readonly TempFolder _dir = new("axclaude-tests");

        public FakeClaude(string body)
        {
            Command = _dir.File("claude.cmd", "@echo off\r\n" + body + "\r\n");
        }

        public string Command { get; }

        public void Dispose() => _dir.Dispose();
    }

    [Fact]
    public async Task The_version_is_the_first_line_claude_prints_for_version()
    {
        using var claude = new FakeClaude("echo 2.0.30 %1\r\necho a second line");
        Assert.Equal("2.0.30 --version", await ClaudeLauncher.VersionAsync(claude.Command, TimeSpan.FromSeconds(30)));
    }

    [Fact]
    public async Task A_claude_that_does_not_answer_in_time_gives_no_version_and_is_ended()
    {
        using var claude = new FakeClaude("ping -n 30 127.0.0.1 >nul\r\necho 9.9.9");
        var watch = Stopwatch.StartNew();
        Assert.Null(await ClaudeLauncher.VersionAsync(claude.Command, TimeSpan.FromSeconds(1)));
        Assert.True(watch.Elapsed < TimeSpan.FromSeconds(20), $"took {watch.Elapsed}");
    }

    [Fact]
    public async Task A_claude_that_cannot_start_gives_no_version()
    {
        var nowhere = Path.Combine(Path.GetTempPath(), "axclaude-nowhere-" + Guid.NewGuid().ToString("N"), "claude.exe");
        Assert.Null(await ClaudeLauncher.VersionAsync(nowhere, TimeSpan.FromSeconds(5)));
    }

    [Fact]
    public void Split_at_whitespace_including_new_lines()
    {
        Assert.Equal(["--continue", "--permission-mode", "plan"], ClaudeLauncher.SplitArguments("  --continue\r\n--permission-mode   plan \n"));
    }

    [Fact]
    public void Empty_and_blank_text_give_no_arguments()
    {
        Assert.Empty(ClaudeLauncher.SplitArguments(string.Empty));
        Assert.Empty(ClaudeLauncher.SplitArguments(" \r\n\t"));
    }

    [Fact]
    public void Quotes_keep_spaces_and_an_escaped_quote_is_literal()
    {
        Assert.Equal(
            ["--append-system-prompt", "Always say \"hi\" first", "--name", "my feature"],
            ClaudeLauncher.SplitArguments("--append-system-prompt \"Always say \\\"hi\\\" first\" --name \"my feature\""));
        Assert.Equal(["ab c"], ClaudeLauncher.SplitArguments("a\"b c\""));
        Assert.Equal([string.Empty], ClaudeLauncher.SplitArguments("\"\""));
    }

    [Fact]
    public void Join_and_split_round_trip()
    {
        string[] arguments = ["--continue", "--name", "my feature", "say \"hi\"", string.Empty];
        var line = ClaudeLauncher.JoinArguments(arguments);
        Assert.Equal("--continue --name \"my feature\" \"say \\\"hi\\\"\" \"\"", line);
        Assert.Equal(arguments, ClaudeLauncher.SplitArguments(line));
    }

    [Fact]
    public void Backslashes_follow_the_windows_rules()
    {
        // Literal everywhere but in front of a quote: a quoted folder with its trailing backslash keeps the arguments after it.
        string[] arguments = ["--add-dir", @"C:\Dir With Space\", @"C:\plain\", @"back\\slash", "odd\\\"quote"];
        var line = ClaudeLauncher.JoinArguments(arguments);
        Assert.Equal(@"--add-dir ""C:\Dir With Space\\"" C:\plain\ back\\slash ""odd\\\""quote""", line);
        Assert.Equal(arguments, ClaudeLauncher.SplitArguments(line));
        Assert.Equal([@"C:\Dir With Space\", "--verbose"], ClaudeLauncher.SplitArguments(@"""C:\Dir With Space\\"" --verbose"));
    }

    [Fact]
    public void The_command_line_quotes_the_executable_and_the_arguments_that_need_it()
    {
        Assert.Equal(
            @"""C:\Tools\claude.exe"" --ax-screen-reader --name ""my feature""",
            ClaudeLauncher.BuildCommandLine(@"C:\Tools\claude.exe", ["--ax-screen-reader", "--name", "my feature"]));
    }

    [Fact]
    public void A_cmd_wrapper_runs_through_cmd_exe_with_the_whole_line_quoted_once_more()
    {
        // cmd /s strips the outer quotes, leaving the quoted path and the arguments as they are; the extension's case does not matter.
        // S-1: cmd.exe by its full path, never one in the project folder.
        var cmd = "\"" + Axit.Core.SystemPrograms.Cmd + "\"";
        Assert.Equal(
            cmd + @" /d /s /c """"C:\npm\claude.cmd"" --name ""my feature""""",
            ClaudeLauncher.BuildCommandLine(@"C:\npm\claude.cmd", ["--name", "my feature"]));
        Assert.StartsWith(cmd + " /d /s /c ", ClaudeLauncher.BuildCommandLine(@"C:\npm\CLAUDE.CMD", ["--continue"]));
    }
}
