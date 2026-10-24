using AxFile.Core;

namespace AxFile.Tests;

/// <summary>The user's own programs (docs/axfile/SPEC.md AF-4.11): the default name, and how each kind is started on the entries.</summary>
public sealed class UserProgramsTests
{
    [Theory]
    [InlineData(@"C:\Program Files\Notepad++\notepad++.exe", "notepad++")]
    [InlineData(@"C:\tools\tidy.ps1", "tidy")]
    [InlineData(@"C:\tools\run.cmd", "run")]
    public void The_default_name_is_the_file_name_without_its_extension(string path, string expected)
    {
        Assert.Equal(expected, UserPrograms.DefaultName(path));
    }

    [Fact]
    public void A_program_starts_with_every_entry_as_an_argument_in_the_first_ones_folder()
    {
        var info = UserPrograms.StartInfo(new UserProgram("Notepad++", @"C:\Program Files\Notepad++\notepad++.exe"), [@"C:\docs\a.txt", @"C:\docs\b.txt"]);
        Assert.Equal(@"C:\Program Files\Notepad++\notepad++.exe", info.FileName);
        Assert.Equal([@"C:\docs\a.txt", @"C:\docs\b.txt"], info.ArgumentList);
        Assert.Equal(@"C:\docs", info.WorkingDirectory);
        Assert.True(info.UseShellExecute);
    }

    [Fact]
    public void A_powershell_script_runs_through_powershell_with_the_entries_after_it()
    {
        var info = UserPrograms.StartInfo(new UserProgram("tidy", @"C:\tools\tidy.ps1"), [@"C:\docs\a.txt"]);
        // S-1: Windows PowerShell by its full path, never a powershell.exe in the entry's folder.
        Assert.Equal(Axit.Core.SystemPrograms.PowerShell, info.FileName);
        Assert.True(Path.IsPathFullyQualified(info.FileName));
        Assert.Equal(["-NoProfile", "-ExecutionPolicy", "Bypass", "-File", @"C:\tools\tidy.ps1", @"C:\docs\a.txt"], info.ArgumentList);
        Assert.Equal(@"C:\docs", info.WorkingDirectory);
    }

    [Fact]
    public void A_batch_file_is_started_as_the_shell_would()
    {
        var info = UserPrograms.StartInfo(new UserProgram("run", @"C:\tools\run.cmd"), []);
        Assert.Equal(@"C:\tools\run.cmd", info.FileName);
        Assert.Empty(info.ArgumentList);
        Assert.Equal(string.Empty, info.WorkingDirectory);
    }

    /// <summary>S-3: cmd.exe reads &amp; and the like in a batch file's arguments as commands, so such a name is refused for a batch program, and only for one.</summary>
    [Fact]
    public void A_batch_program_is_refused_a_name_cmd_would_run()
    {
        var batch = new UserProgram("run", @"C:\tools\run.cmd");
        Assert.Equal(@"C:\docs\a&calc.txt", UserPrograms.RefusedTarget(batch, [@"C:\docs\plain.txt", @"C:\docs\a&calc.txt"]));
        Assert.Null(UserPrograms.RefusedTarget(batch, [@"C:\docs\plain name.txt"]));
        Assert.Null(UserPrograms.RefusedTarget(new UserProgram("np", @"C:\np\notepad.exe"), [@"C:\docs\a&b.txt"]));
    }
}
