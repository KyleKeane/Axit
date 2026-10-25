using Axit.Core;

namespace Axit.Tests;

/// <summary>Which kinds run when opened (security review S-46), as Windows itself says.</summary>
public class DangerousFilesTests
{
    [Theory]
    [InlineData(@"C:\x\tool.exe")]
    [InlineData(@"C:\x\invoice.lnk")]
    [InlineData(@"C:\x\run.cmd")]
    [InlineData(@"C:\x\script.vbs")]
    [InlineData(@"C:\x\page.hta")]
    [InlineData(@"C:\x\SETUP.MSI")]
    public void Programs_scripts_and_shortcuts_run_when_opened(string path) => Assert.True(DangerousFiles.RunsWhenOpened(path));

    [Theory]
    [InlineData(@"C:\x\notes.txt")]
    [InlineData(@"C:\x\photo.png")]
    [InlineData(@"C:\x\song.mp3")]
    [InlineData(@"C:\x\no-extension")]
    public void Documents_and_media_do_not(string path) => Assert.False(DangerousFiles.RunsWhenOpened(path));
}
