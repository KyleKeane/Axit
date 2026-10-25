using Axit.Core;
using AxProcess.Core;

namespace AxProcess.Tests;

/// <summary>The Processes view (docs/axprocess/SPEC.md PR-3): the processor use between two samples, the labels, the order, the summary.</summary>
public sealed class ProcessTableTests
{
    private const long Mega = 1024 * 1024;

    [Fact]
    public void The_processor_use_is_the_share_of_every_processor_over_the_time_between()
    {
        var first = new[]
        {
            new ProcessSample(10, "busy", 100 * Mega, TimeSpan.FromMilliseconds(5000)),
            new ProcessSample(11, "idle", 50 * Mega, TimeSpan.FromMilliseconds(200)),
            new ProcessSample(12, "reused", 1 * Mega, TimeSpan.FromMilliseconds(0)),
            new ProcessSample(13, "protected", 1 * Mega, null),
        };
        var second = new[]
        {
            // One second on four processors is 4000 ms of processor time: 1000 ms used is 25%.
            new ProcessSample(10, "busy", 100 * Mega, TimeSpan.FromMilliseconds(6000)),
            new ProcessSample(11, "idle", 50 * Mega, TimeSpan.FromMilliseconds(200)),
            new ProcessSample(12, "other", 1 * Mega, TimeSpan.FromMilliseconds(900)),
            new ProcessSample(13, "protected", 1 * Mega, null),
            new ProcessSample(14, "new", 1 * Mega, TimeSpan.FromMilliseconds(10)),
        };
        var rows = ProcessTable.Rows(first, second, TimeSpan.FromSeconds(1), 4).ToDictionary(row => row.Id);
        Assert.Equal(25, rows[10].Processor);
        Assert.Equal(0, rows[11].Processor);
        Assert.Null(rows[12].Processor);
        Assert.Null(rows[13].Processor);
        Assert.Null(rows[14].Processor);
    }

    [Fact]
    public void A_row_reads_name_memory_processor_and_id()
    {
        Assert.Equal("chrome, 350 MB, 3% CPU, process 1234", new ProcessRow(1234, "chrome", 350 * Mega, 2.6).Label);
        Assert.Equal("System, 900 KB, process 4", new ProcessRow(4, "System", 900 * 1024, null).Label);
        Assert.Equal("Idle, 8 KB, 0% CPU, process 0", new ProcessRow(0, "Idle", 8 * 1024, 0.2).Label);
    }

    [Fact]
    public void The_order_follows_the_choice_with_ties_by_name()
    {
        var rows = new[]
        {
            new ProcessRow(1, "beta", 10 * Mega, 5),
            new ProcessRow(2, "Alpha", 10 * Mega, null),
            new ProcessRow(3, "gamma", 300 * Mega, 1),
        };
        Assert.Equal([3, 2, 1], ProcessTable.Sorted(rows, ProcessSort.Memory).Select(row => row.Id));
        Assert.Equal([1, 3, 2], ProcessTable.Sorted(rows, ProcessSort.Processor).Select(row => row.Id));
        Assert.Equal([2, 1, 3], ProcessTable.Sorted(rows, ProcessSort.Name).Select(row => row.Id));
    }

    [Fact]
    public void The_summary_and_the_names_follow_the_spec()
    {
        const long giga = 1024L * Mega;
        Assert.Equal("214 processes. Memory 9.1 of 15.9 GB in use, 57%. Processor 12%",
            ProcessTable.Summary(214, (long)(15.9 * giga), (long)(6.8 * giga), 12.2));
        Assert.Equal("1 process. Memory 0.0 of 0.0 GB in use, 0%.", ProcessTable.Summary(1, 0, 0, null));
        Assert.Equal("Processes, 214", ProcessTable.ListName(214));
        Assert.Equal("Processes, none", ProcessTable.ListName(0));
    }

    [Fact]
    public void A_sample_of_this_computer_holds_this_process()
    {
        var samples = ProcessTable.Sample();
        Assert.Contains(samples, sample => sample.Id == Environment.ProcessId && sample.Memory > 0 && sample.ProcessorTime is not null);
    }

    [Theory]
    [InlineData(0L, "0 bytes")]
    [InlineData(900L * 1024, "900 KB")]
    [InlineData(350L * 1024 * 1024, "350 MB")]
    [InlineData(367420211L, "350.4 MB")]
    [InlineData(1288490189L, "1.2 GB")]
    public void Sizes_read_as_spoken(long bytes, string expected) => Assert.Equal(expected, FileSize.Text(bytes));

    [Fact]
    public void The_system_busy_share_leaves_the_idle_time_out()
    {
        var before = new SystemTimes(Idle: 1000, Kernel: 1500, User: 500);
        var after = new SystemTimes(Idle: 1800, Kernel: 2500, User: 700);
        // 1200 of time passed, 800 of it idle: a third busy.
        Assert.Equal(100d / 3, SystemTimes.Busy(before, after)!.Value, 6);
        Assert.Null(SystemTimes.Busy(before, before));
    }
}
