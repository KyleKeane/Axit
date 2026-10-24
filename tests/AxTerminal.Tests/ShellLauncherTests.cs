using System.Text;
using AxTerminal.Core;

namespace AxTerminal.Tests;

public sealed class ShellLauncherTests
{
    [Fact]
    public void Command_prompt_gets_the_marks_around_its_prompt()
    {
        Assert.Equal("$e]133;D;axit=n0$e\\$e]133;A;axit=n0$e\\$e]9;9;$P$e\\$P$G$e]133;B;axit=n0$e\\", ShellLauncher.CommandPromptWithMarks(null, "n0"));
        Assert.Equal("$e]133;D;axit=n0$e\\$e]133;A;axit=n0$e\\$e]9;9;$P$e\\$P$_$G$e]133;B;axit=n0$e\\", ShellLauncher.CommandPromptWithMarks("$P$_$G", "n0"));
        var env = ShellLauncher.BuildEnvironment(ShellKind.CommandPrompt);
        Assert.StartsWith("$e]133;D", env["PROMPT"]);
        Assert.Equal("cat", env["GIT_PAGER"]);
        Assert.Equal("cat", env["GH_PAGER"]);
        Assert.Equal("xterm-256color", env["TERM"]);
        // Command Prompt does not get bash's prompt command (asserted on the value; the earlier form passed with any other value).
        Assert.NotEqual(ShellLauncher.BashPromptCommand, env.GetValueOrDefault("PROMPT_COMMAND"));
    }

    [Fact]
    public void PowerShell_gets_the_snippet_encoded()
    {
        var encoded = ShellLauncher.EncodedSnippet();
        var decoded = Encoding.Unicode.GetString(Convert.FromBase64String(encoded));
        Assert.Contains("133;A", decoded);
        Assert.Contains("133;B", decoded);
        Assert.Contains("133;D", decoded);
        Assert.Contains("9;9", decoded);
        Assert.Contains("ProgressPreference", decoded);
        Assert.DoesNotContain("`e", decoded);
        // PowerShell's marks travel on its command line, not in the environment: no cmd prompt with marks, no bash prompt command.
        var powerShell = ShellLauncher.BuildEnvironment(ShellKind.WindowsPowerShell);
        Assert.False(powerShell.TryGetValue("PROMPT", out var prompt) && prompt.Contains("133;", StringComparison.Ordinal));
        Assert.NotEqual(ShellLauncher.BashPromptCommand, powerShell.GetValueOrDefault("PROMPT_COMMAND"));
        if (ShellLauncher.BuildCommandLine(ShellKind.WindowsPowerShell) is { } commandLine)
        {
            Assert.EndsWith(" -EncodedCommand " + encoded, commandLine);
        }
    }

    [Fact]
    public void Git_bash_gets_the_marks_through_its_prompt_command()
    {
        var command = ShellLauncher.BuildEnvironment(ShellKind.GitBash)["PROMPT_COMMAND"];
        Assert.Equal(ShellLauncher.BashPromptCommand, command);
        // The exit code is read before anything else can change it; the marks and the directory follow; B goes on PS1.
        Assert.StartsWith("__axit_code=$?;", command);
        Assert.Contains("133;D;%s", command);
        Assert.Contains("133;A", command);
        Assert.Contains("9;9;%s", command);
        Assert.Contains("PS1=\"$PS1\"", command);
        Assert.Contains("133;B", command);
        Assert.DoesNotContain("\n", command);
        if (ShellLauncher.Find(ShellKind.GitBash) is { } bash)
        {
            Assert.EndsWith(@"\Git\bin\bash.exe", bash, StringComparison.OrdinalIgnoreCase);
            Assert.EndsWith(" --login -i", ShellLauncher.BuildCommandLine(ShellKind.GitBash));
        }
        else
        {
            Assert.Null(ShellLauncher.BuildCommandLine(ShellKind.GitBash));
        }
    }

    [Fact]
    public void Windows_shells_are_found_and_their_command_lines_built()
    {
        // Every Windows has these two; the command line is the program, with the snippet for PowerShell.
        var powershell = ShellLauncher.BuildCommandLine(ShellKind.WindowsPowerShell);
        Assert.NotNull(powershell);
        Assert.Contains("powershell.exe", powershell, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(" -NoLogo -NoExit -EncodedCommand ", powershell);
        var cmd = ShellLauncher.BuildCommandLine(ShellKind.CommandPrompt);
        Assert.NotNull(cmd);
        Assert.EndsWith("cmd.exe", cmd, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Custom_is_the_line_as_typed()
    {
        Assert.Null(ShellLauncher.BuildCommandLine(ShellKind.Custom, "  "));
        Assert.Equal("wsl.exe -d Ubuntu", ShellLauncher.BuildCommandLine(ShellKind.Custom, " wsl.exe -d Ubuntu "));
        Assert.Null(ShellLauncher.Find(ShellKind.Custom));
    }

    [Fact]
    public void Keys_round_trip_and_unknown_is_the_default()
    {
        foreach (var kind in Enum.GetValues<ShellKind>())
        {
            Assert.Equal(kind, ShellLauncher.Parse(ShellLauncher.Key(kind)));
        }

        Assert.Equal(ShellLauncher.DefaultKind(), ShellLauncher.Parse(null));
        Assert.Equal(ShellLauncher.DefaultKind(), ShellLauncher.Parse("fish"));
        Assert.Equal(ShellKind.GitBash, ShellLauncher.Parse("bash"));
        Assert.Equal("Windows PowerShell", ShellLauncher.Describe(ShellKind.WindowsPowerShell));
        Assert.Equal("Git Bash", ShellLauncher.Describe(ShellKind.GitBash));
        Assert.Equal(">> ", ShellLauncher.ContinuationPrompt(ShellKind.WindowsPowerShell));
        Assert.Equal("> ", ShellLauncher.ContinuationPrompt(ShellKind.GitBash));
        Assert.Equal("More? ", ShellLauncher.ContinuationPrompt(ShellKind.CommandPrompt));
        Assert.Null(ShellLauncher.ContinuationPrompt(ShellKind.Custom));
    }
}
