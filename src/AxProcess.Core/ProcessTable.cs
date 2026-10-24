using System.ComponentModel;
using System.Diagnostics;

namespace AxProcess.Core;

/// <summary>One process at one moment: what the processor time is measured from (SPEC.md PR-D3). The time is null when Windows does not let the user read it.</summary>
public sealed record ProcessSample(int Id, string Name, long Memory, TimeSpan? ProcessorTime);

/// <summary>One item of the Processes view (PR-3.1): the processor use is a percentage of the whole computer over the last read, null when unknown.</summary>
public sealed record ProcessRow(int Id, string Name, long Memory, double? Processor)
{
    /// <summary>"chrome, 350 MB, 2% CPU, process 1234"; without the CPU part when it is unknown.</summary>
    public string Label => Processor is { } cpu
        ? $"{Name}, {SizeText.Format(Memory)}, {Math.Round(cpu):0}% CPU, process {Id}"
        : $"{Name}, {SizeText.Format(Memory)}, process {Id}";
}

/// <summary>How the Processes view is ordered (PR-3.2).</summary>
public enum ProcessSort
{
    Memory,
    Processor,
    Name,
}

/// <summary>
/// The Processes view (SPEC.md PR-3): two samples a second apart give each process's share of the processor over that
/// second; the rows are ordered by the user's choice. <see cref="Sample"/> reads the system; the rest is pure.
/// </summary>
public static class ProcessTable
{
    /// <summary>Every process now, its name, working set and processor time; a process gone while it was read is left out.</summary>
    public static IReadOnlyList<ProcessSample> Sample()
    {
        var samples = new List<ProcessSample>();
        foreach (var process in Process.GetProcesses())
        {
            using (process)
            {
                try
                {
                    TimeSpan? time = null;
                    try
                    {
                        time = process.TotalProcessorTime;
                    }
                    catch (Exception ex) when (ex is Win32Exception or NotSupportedException)
                    {
                        // A protected process: its memory is readable, its time is not.
                    }

                    samples.Add(new ProcessSample(process.Id, process.ProcessName, process.WorkingSet64, time));
                }
                catch (InvalidOperationException)
                {
                    // It ended between the list and the read.
                }
            }
        }

        return samples;
    }

    /// <summary>
    /// The rows of the second sample, each with its processor use since the first: the time it used over the time that
    /// passed on every processor, as a percentage from 0 to 100. A process new in the second sample, or whose ID came
    /// back under another name, has no use yet.
    /// </summary>
    public static IReadOnlyList<ProcessRow> Rows(IReadOnlyList<ProcessSample> first, IReadOnlyList<ProcessSample> second, TimeSpan elapsed, int processors)
    {
        var before = new Dictionary<int, ProcessSample>();
        foreach (var sample in first)
        {
            before[sample.Id] = sample;
        }

        var available = elapsed.TotalMilliseconds * Math.Max(1, processors);
        var rows = new List<ProcessRow>(second.Count);
        foreach (var sample in second)
        {
            double? use = null;
            if (available > 0 && sample.ProcessorTime is { } now && before.TryGetValue(sample.Id, out var earlier)
                && earlier.Name == sample.Name && earlier.ProcessorTime is { } then)
            {
                use = Math.Clamp((now - then).TotalMilliseconds / available * 100, 0, 100);
            }

            rows.Add(new ProcessRow(sample.Id, sample.Name, sample.Memory, use));
        }

        return rows;
    }

    /// <summary>Memory and processor the largest first, an unknown use last; name in the alphabet's order; ties by name, then by ID.</summary>
    public static IReadOnlyList<ProcessRow> Sorted(IEnumerable<ProcessRow> rows, ProcessSort sort)
    {
        var byName = StringComparer.OrdinalIgnoreCase;
        return sort switch
        {
            ProcessSort.Processor => rows.OrderByDescending(row => row.Processor ?? -1).ThenBy(row => row.Name, byName).ThenBy(row => row.Id).ToList(),
            ProcessSort.Name => rows.OrderBy(row => row.Name, byName).ThenBy(row => row.Id).ToList(),
            _ => rows.OrderByDescending(row => row.Memory).ThenBy(row => row.Name, byName).ThenBy(row => row.Id).ToList(),
        };
    }

    public static string ListName(int count) => count == 0 ? "Processes, none" : $"Processes, {count}";

    /// <summary>The summary of PR-2.5: "214 processes. Memory 9.1 of 15.9 GB in use, 57%. Processor 12%".</summary>
    public static string Summary(int processes, long totalMemory, long availableMemory, double? processor)
    {
        var used = Math.Max(0, totalMemory - availableMemory);
        var percent = totalMemory > 0 ? Math.Round(used * 100d / totalMemory) : 0;
        var text = $"{processes} {(processes == 1 ? "process" : "processes")}. Memory {SizeText.Gigabytes(used)} of {SizeText.Gigabytes(totalMemory)} GB in use, {percent:0}%.";
        return processor is { } cpu ? $"{text} Processor {Math.Round(cpu):0}%" : text;
    }

    public static string SortName(ProcessSort sort) => sort switch
    {
        ProcessSort.Processor => "processor",
        ProcessSort.Name => "name",
        _ => "memory",
    };
}
