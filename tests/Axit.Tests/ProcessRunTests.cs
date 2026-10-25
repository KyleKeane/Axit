using System.Diagnostics;
using Axit.Core;

namespace Axit.Tests;

/// <summary>The bundle's run of a program to its end (inventory item 3), with Windows' own small programs.</summary>
public class ProcessRunTests
{
    private static string System32(string name) => Path.Combine(Environment.SystemDirectory, name);

    [Fact]
    public async Task The_output_and_the_exit_code_come_back()
    {
        var start = new ProcessStartInfo(SystemPrograms.Cmd);
        start.ArgumentList.Add("/d");
        start.ArgumentList.Add("/c");
        start.ArgumentList.Add("echo hello& exit 3");
        var result = await ProcessRun.RunAsync(start, [], TimeSpan.FromSeconds(20));
        Assert.False(result.TimedOut);
        Assert.Equal(3, result.ExitCode);
        Assert.Equal("hello", result.Output.Trim());
    }

    [Fact]
    public async Task The_input_lines_reach_the_program()
    {
        // findstr prints the lines of its input that hold the text.
        var start = new ProcessStartInfo(System32("findstr.exe"));
        start.ArgumentList.Add("x");
        var result = await ProcessRun.RunAsync(start, ["x one", "two", "x three"], TimeSpan.FromSeconds(20));
        Assert.Equal(["x one", "x three"], result.Output.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
    }

    [Fact]
    public async Task A_program_past_its_time_is_ended_and_said_so()
    {
        var start = new ProcessStartInfo(System32("ping.exe"));
        foreach (var argument in new[] { "-n", "30", "127.0.0.1" })
        {
            start.ArgumentList.Add(argument);
        }

        var watch = Stopwatch.StartNew();
        var result = ProcessRun.Run(start, [], TimeSpan.FromMilliseconds(500));
        Assert.True(result.TimedOut);
        Assert.False(result.Finished);
        Assert.Equal(-1, result.ExitCode);
        Assert.True(watch.Elapsed < TimeSpan.FromSeconds(15));
    }
}
