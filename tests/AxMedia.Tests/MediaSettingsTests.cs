using AxMedia.Core;

namespace AxMedia.Tests;

/// <summary>The settings file (docs/axmedia/SPEC.md AM-7.1): defaults, the recent list, and nonsense sanitized.</summary>
public sealed class MediaSettingsTests
{
    [Fact]
    public void Defaults()
    {
        var settings = new MediaSettings();
        Assert.True(settings.SayEachFile);
        Assert.Equal(1, settings.Speed);
        Assert.Equal(15, settings.SeekSeconds);
        Assert.Empty(settings.Recent);
        Assert.True(settings.CheckForUpdates);
    }

    [Fact]
    public void Remembered_paths_go_first_once_and_stay_ten()
    {
        var settings = new MediaSettings();
        for (var i = 0; i < 12; i++)
        {
            settings.Remember($"C:\\m{i}");
        }

        settings.Remember("c:\\M5");
        Assert.Equal(10, settings.Recent.Count);
        Assert.Equal("c:\\M5", settings.Recent[0]);
        Assert.Single(settings.Recent, f => string.Equals(f, "C:\\m5", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Nonsense_in_the_file_falls_back_and_a_round_trip_keeps_the_values()
    {
        var path = Path.Combine(Path.GetTempPath(), "axmedia-settings-" + Guid.NewGuid().ToString("N") + ".json");
        File.WriteAllText(path, """{ "speed": 9, "seekSeconds": 0, "recent": ["", "C:\\x"], "fontSize": 500 }""");
        try
        {
            var settings = MediaSettings.Load(path, out var error);
            Assert.Null(error);
            Assert.Equal(3, settings.Speed);
            Assert.Equal(15, settings.SeekSeconds);
            Assert.Equal(["C:\\x"], settings.Recent);
            Assert.Equal(0, settings.FontSize);

            settings.Speed = 1.5;
            settings.SayEachFile = false;
            settings.Save(path);
            var loaded = MediaSettings.Load(path, out _);
            Assert.Equal(1.5, loaded.Speed);
            Assert.False(loaded.SayEachFile);
            Assert.Equal(["C:\\x"], loaded.Recent);
        }
        finally
        {
            File.Delete(path);
        }
    }
}
