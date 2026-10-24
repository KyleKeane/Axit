using Axit.Core;

namespace Axit.Tests;

/// <summary>The programs an app may install on the user's command (docs/axit/SPEC.md AX-2.9): only from their makers, with every question answered.</summary>
public class ProgramInstallTests
{
    [Fact]
    public void WingetInstallsTheExactPackageSilently() =>
        Assert.Equal(["install", "--id", "Git.Git", "--exact", "--silent", "--accept-package-agreements", "--accept-source-agreements", "--disable-interactivity"],
            ProgramInstall.WingetArguments("Git.Git"));

    [Fact]
    public void EachProgramComesFromItsMaker()
    {
        Assert.Equal("Git.Git", ProgramInstall.Git.WingetId);
        Assert.Equal("KeePassXCTeam.KeePassXC", ProgramInstall.KeePassXC.WingetId);
        Assert.Equal("Microsoft.PowerShell", ProgramInstall.PowerShell7.WingetId);
        Assert.Null(ProgramInstall.ClaudeCode.WingetId);
        Assert.StartsWith("https://", ProgramInstall.ClaudeCode.Page);
        Assert.False(ProgramInstall.ClaudeCode.AsksForAdministrator);
    }

    [Fact]
    public void ClaudeCodesOwnInstallerRunsInPowerShellWithoutAWindow()
    {
        var info = ProgramInstall.ClaudeCode.StartInfo()!;
        Assert.Equal("powershell.exe", info.FileName);
        Assert.Equal(["-NoProfile", "-ExecutionPolicy", "Bypass", "-Command", "irm https://claude.ai/install.ps1 | iex"], info.ArgumentList);
        Assert.True(info.CreateNoWindow);
        Assert.True(ProgramInstall.ClaudeCode.CanInstall);
    }

    [Fact]
    public void ThePathIsTheMachinesThenTheUsersThenTheProcesssOwn() =>
        Assert.Equal(@"C:\Windows;C:\Git\cmd;D:\Tools\bin;C:\Own",
            ProgramInstall.CombinePath(@"C:\Windows;C:\Git\cmd", @"D:\Tools\bin;c:\windows", @"C:\Windows;C:\Own;"));
}
