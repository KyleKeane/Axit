using System.Text;
using AxTerminal.Core;

namespace AxTerminal.Tests;

public sealed class ShellLauncherTests
{
    [Fact]
    public void Command_prompt_gets_the_marks_around_its_prompt()
    {
        Assert.Equal("$e]133;D$e\\$e]133;A$e\\$e]9;9;$P$e\\$P$G$e]133;B$e\\", ShellLauncher.CommandPromptWithMarks(null));
        Assert.Equal("$e]133;D$e\\$e]133;A$e\\$e]9;9;$P$e\\$P$_$G$e]133;B$e\\", ShellLauncher.CommandPromptWithMarks("$P$_$G"));
        var env = ShellLauncher.BuildEnvironment(ShellKind.CommandPrompt);
        Assert.StartsWith("$e]133;D", env["PROMPT"]);
        Assert.Equal("cat", env["GIT_PAGER"]);
        Assert.Equal("cat", env["GH_PAGER"]);
        Assert.Equal("xterm-256color", env["TERM"]);
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
        Assert.False(ShellLauncher.BuildEnvironment(ShellKind.WindowsPowerShell).ContainsKey("PROMPT_AXIT"));
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
        Assert.Equal(ShellLauncher.DefaultKind(), ShellLauncher.Parse("bash"));
        Assert.Equal("Windows PowerShell", ShellLauncher.Describe(ShellKind.WindowsPowerShell));
    }
}
