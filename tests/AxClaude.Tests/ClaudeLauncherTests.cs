using AxClaude.Core.Pty;

namespace AxClaude.Tests;

public class ClaudeLauncherTests
{
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
        Assert.Equal(
            @"cmd.exe /d /s /c """"C:\npm\claude.cmd"" --name ""my feature""""",
            ClaudeLauncher.BuildCommandLine(@"C:\npm\claude.cmd", ["--name", "my feature"]));
        Assert.StartsWith("cmd.exe /d /s /c ", ClaudeLauncher.BuildCommandLine(@"C:\npm\CLAUDE.CMD", ["--continue"]));
    }
}
