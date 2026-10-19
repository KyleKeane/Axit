using AxFile.Core;

namespace AxFile.Tests;

/// <summary>The player's rules that need no engine (docs/axfile/SPEC.md AF-5.1, AF-5.3, AF-5.4, AF-5.7).</summary>
public sealed class MediaFilesTests
{
    [Theory]
    [InlineData("a.mp3", true)]
    [InlineData("A.MP3", true)]
    [InlineData("book.m4b", true)]
    [InlineData("song.flac", true)]
    [InlineData("talk.opus", true)]
    [InlineData("clip.mp4", true)]
    [InlineData("clip.webm", true)]
    [InlineData("notes.md", false)]
    [InlineData("cover.jpg", false)]
    [InlineData("noext", false)]
    public void Media_files_are_known_by_their_extension(string name, bool expected)
    {
        Assert.Equal(expected, MediaFiles.IsMedia(name));
    }

    [Fact]
    public void The_extension_sets_are_the_spec_lists()
    {
        Assert.Equal(10, MediaFiles.SoundExtensions.Count);
        Assert.Equal(7, MediaFiles.VideoExtensions.Count);
        Assert.Equal(17, MediaFiles.Extensions.Count);
    }

    [Fact]
    public void The_queue_is_the_folders_media_files_in_order()
    {
        var now = DateTime.Now;
        IReadOnlyList<Entry> entries =
        [
            new Entry("sub", "C:\\d\\sub", true, 0, now),
            new Entry("01.mp3", "C:\\d\\01.mp3", false, 1, now),
            new Entry("cover.jpg", "C:\\d\\cover.jpg", false, 1, now),
            new Entry("02.mp3", "C:\\d\\02.mp3", false, 1, now),
            new Entry("03.m4b", "C:\\d\\03.m4b", false, 1, now),
        ];
        Assert.Equal(["C:\\d\\01.mp3", "C:\\d\\02.mp3", "C:\\d\\03.m4b"], MediaFiles.Queue(entries));
    }

    [Theory]
    [InlineData(0, "0:00")]
    [InlineData(5, "0:05")]
    [InlineData(760, "12:40")]
    [InlineData(3730, "1:02:10")]
    [InlineData(36000, "10:00:00")]
    [InlineData(-3, "0:00")]
    public void Time_reads_as_minutes_and_seconds_with_hours_over_an_hour(int seconds, string expected)
    {
        Assert.Equal(expected, MediaFiles.FormatTime(TimeSpan.FromSeconds(seconds)));
    }

    [Theory]
    [InlineData("12:40", 760)]
    [InlineData("1:02:10", 3730)]
    [InlineData("750", 750)]
    [InlineData(" 0:05 ", 5)]
    public void Go_to_time_takes_a_clock_or_seconds(string text, int seconds)
    {
        Assert.Equal(TimeSpan.FromSeconds(seconds), MediaFiles.ParseTime(text));
    }

    [Theory]
    [InlineData("")]
    [InlineData("abc")]
    [InlineData("1:70")]
    [InlineData("1:2:3:4")]
    [InlineData("12:")]
    [InlineData("-5")]
    public void Nonsense_is_not_a_time(string text)
    {
        Assert.Null(MediaFiles.ParseTime(text));
    }

    [Fact]
    public void Speed_steps_by_a_quarter_between_a_half_and_three()
    {
        Assert.Equal(1.25, MediaFiles.Faster(1));
        Assert.Equal(3, MediaFiles.Faster(3));
        Assert.Equal(3, MediaFiles.Faster(2.75));
        Assert.Equal(0.75, MediaFiles.Slower(1));
        Assert.Equal(0.5, MediaFiles.Slower(0.5));
        Assert.Equal(1, MediaFiles.SanitizeSpeed(double.NaN));
        Assert.Equal(1, MediaFiles.SanitizeSpeed(1.1));
        Assert.Equal(3, MediaFiles.SanitizeSpeed(9));
        Assert.Equal(0.5, MediaFiles.SanitizeSpeed(0));
    }

    [Theory]
    [InlineData(1, "Speed 1")]
    [InlineData(1.25, "Speed 1.25")]
    [InlineData(1.5, "Speed 1.5")]
    [InlineData(0.75, "Speed 0.75")]
    public void The_speed_is_said_without_trailing_zeros(double speed, string expected)
    {
        Assert.Equal(expected, MediaFiles.SpeedText(speed));
    }
}
