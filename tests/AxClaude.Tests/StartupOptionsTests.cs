using AxClaude.Core;

namespace AxClaude.Tests;

/// <summary>The command line (FR-9): the options, their errors, and everything after <c>--</c> passed on verbatim.</summary>
public class StartupOptionsTests
{
    private static readonly string Folder = Path.GetTempPath();

    [Fact]
    public void Everything_after_the_double_dash_goes_to_claude_and_a_bare_one_starts_afresh()
    {
        var options = StartupOptions.Parse([Folder, "--", "--resume", "abc", "--cols"]);
        Assert.Equal(["--resume", "abc", "--cols"], options.ClaudeArgs!);
        Assert.Empty(StartupOptions.Parse([Folder, "--"]).ClaudeArgs!);
        Assert.Null(StartupOptions.Parse([Folder]).ClaudeArgs);
    }

    [Fact]
    public void The_folder_is_made_absolute_and_must_exist()
    {
        var options = StartupOptions.Parse([Folder]);
        Assert.Equal(Path.GetFullPath(Folder), options.Folder);
        Assert.Equal(options.Folder, StartupOptions.Parse(["--project", Folder]).Folder);
        var missing = Path.Combine(Folder, "axclaude-" + Guid.NewGuid().ToString("N"));
        Assert.Contains("does not exist", Assert.Throws<ArgumentException>(() => StartupOptions.Parse([missing])).Message);
        Assert.Contains("Only one folder", Assert.Throws<ArgumentException>(() => StartupOptions.Parse([Folder, Folder])).Message);
    }

    [Fact]
    public void The_console_size_has_bounds_and_options_need_their_values()
    {
        var options = StartupOptions.Parse([Folder, "--cols", "120", "--rows", "40", "--no-ax", "--claude", "claude.cmd"]);
        Assert.Equal(120, options.Columns);
        Assert.Equal(40, options.Rows);
        Assert.True(options.NoAx);
        Assert.Equal("claude.cmd", options.ClaudePath);
        Assert.Contains("--cols must be a number between 20 and 1000", Assert.Throws<ArgumentException>(() => StartupOptions.Parse([Folder, "--cols", "5"])).Message);
        Assert.Contains("--rows needs a value", Assert.Throws<ArgumentException>(() => StartupOptions.Parse([Folder, "--rows"])).Message);
        Assert.Contains("Unknown option --colour", Assert.Throws<ArgumentException>(() => StartupOptions.Parse([Folder, "--colour"])).Message);
    }

    [Fact]
    public void The_recording_path_is_made_absolute()
    {
        var options = StartupOptions.Parse([Folder, "--record", "session.vt"]);
        Assert.True(Path.IsPathRooted(options.RecordPath));
        Assert.EndsWith("session.vt", options.RecordPath);
    }
}
