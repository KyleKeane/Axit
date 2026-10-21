using AxFile.Core;

namespace AxFile.Tests;

/// <summary>The pane's queue and status lines (docs/axfile/SPEC.md AF-9.2, AF-9.3).</summary>
public class PlayQueueTests
{
    private static readonly string[] Three = [@"C:\m\01 One.mp3", @"C:\m\02 Two.mp3", @"C:\m\03 Three.mp3"];

    [Fact]
    public void An_empty_queue_is_one_line_and_a_set_queue_marks_the_file_playing()
    {
        var queue = new PlayQueue();
        Assert.Equal([PlayQueue.EmptyText], queue.Lines(active: false));
        Assert.Null(queue.PlayingPath);

        queue.Set(Three, 1);
        Assert.Equal(1, queue.Playing);
        Assert.Equal("02 Two.mp3", queue.PlayingName);
        Assert.Equal(["01 One.mp3", "02 Two.mp3 (playing)", "03 Three.mp3"], queue.Lines(active: true));
        Assert.Equal(["01 One.mp3", "02 Two.mp3", "03 Three.mp3"], queue.Lines(active: false));
        Assert.Equal("02 Two.mp3, 2 of 3", queue.Information(1));
    }

    [Fact]
    public void Moving_on_stops_at_the_end_and_select_takes_a_file()
    {
        var queue = new PlayQueue();
        queue.Set(Three, 0);
        Assert.True(queue.MoveOn());
        Assert.True(queue.MoveOn());
        Assert.False(queue.MoveOn());
        Assert.Equal(2, queue.Playing);
        Assert.True(queue.Select(0));
        Assert.False(queue.Select(3));
        Assert.Equal(0, queue.Playing);
    }

    [Fact]
    public void Added_files_follow_and_nothing_plays_until_selected()
    {
        var queue = new PlayQueue();
        Assert.Equal(2, queue.Add([Three[0], Three[1]]));
        Assert.Equal(-1, queue.Playing);
        Assert.Null(queue.PlayingName);
        Assert.Equal(1, queue.Add([Three[2]]));
        Assert.Equal(3, queue.Count);
    }

    [Fact]
    public void Removing_a_file_keeps_the_playing_index_on_its_file()
    {
        var queue = new PlayQueue();
        queue.Set(Three, 2);
        Assert.Equal("01 One.mp3", queue.Remove(0));
        Assert.Equal(1, queue.Playing);
        Assert.Equal("03 Three.mp3", queue.PlayingName);

        // The file playing removed: the index now names the one after it, or nothing past the end.
        queue.Set(Three, 1);
        Assert.Equal("02 Two.mp3", queue.Remove(1));
        Assert.Equal(1, queue.Playing);
        Assert.Equal("03 Three.mp3", queue.PlayingName);
        Assert.Equal("03 Three.mp3", queue.Remove(1));
        Assert.Equal(-1, queue.Playing);
        Assert.Null(queue.Remove(5));
    }

    [Fact]
    public void Status_lines_sit_under_the_files_and_a_report_replaces_its_own_line()
    {
        var queue = new PlayQueue();
        queue.Set([Three[0]], 0);
        var token = queue.Report(-1, "Copying 3 items");
        var other = queue.Report(-1, "Compressing, 1 of 4 files");
        Assert.NotEqual(token, other);
        Assert.Equal(token, queue.Report(token, "Copied 3 items"));
        Assert.Equal(["01 One.mp3 (playing)", "Copied 3 items", "Compressing, 1 of 4 files"], queue.Lines(active: true));
        Assert.False(queue.IsFile(1));

        Assert.Equal("Copied 3 items", queue.Remove(1));
        Assert.Equal(["Compressing, 1 of 4 files"], queue.Status);
        // A token whose line is gone starts a new line.
        queue.Report(token, "Copied 3 items");
        Assert.Equal(["Compressing, 1 of 4 files", "Copied 3 items"], queue.Status);
    }

    [Fact]
    public void Only_the_last_twenty_status_lines_stay_and_clear_drops_everything()
    {
        var queue = new PlayQueue();
        for (var i = 1; i <= 25; i++)
        {
            queue.Report(-1, $"Line {i}");
        }

        Assert.Equal(PlayQueue.StatusLimit, queue.Status.Count);
        Assert.Equal("Line 6", queue.Status[0]);
        Assert.Equal("Line 25", queue.Status[^1]);

        queue.Set(Three, 0);
        queue.Clear();
        Assert.Equal(0, queue.Count);
        Assert.Equal(-1, queue.Playing);
        Assert.Equal([PlayQueue.EmptyText], queue.Lines(active: true));
    }
}
