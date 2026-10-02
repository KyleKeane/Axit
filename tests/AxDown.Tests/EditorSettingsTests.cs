using AxDown.Core;
using Axit.Core;

namespace AxDown.Tests;

/// <summary>AxDown's settings file (SPEC.md AD-8): round trip, defaults, sanity checks.</summary>
public sealed class EditorSettingsTests
{
    [Fact]
    public void Settings_round_trip_through_the_file()
    {
        var path = Path.Combine(Path.GetTempPath(), "axdown-tests", Guid.NewGuid().ToString("N"), "axdown.json");
        try
        {
            var settings = new EditorSettings { FontFamily = "Consolas", FontSize = 14, FontBold = true, WordWrap = false, CheckForUpdates = false };
            settings.Window = new WindowPlacement { X = 10, Y = 20, Width = 800, Height = 600, Maximized = true };
            settings.Save(path);

            var loaded = EditorSettings.Load(path, out var error);
            Assert.Null(error);
            Assert.Equal("Consolas", loaded.FontFamily);
            Assert.Equal(14, loaded.FontSize);
            Assert.True(loaded.FontBold);
            Assert.False(loaded.WordWrap);
            Assert.False(loaded.CheckForUpdates);
            Assert.True(loaded.Window!.Maximized);
            Assert.Contains("\"wordWrap\": false", File.ReadAllText(path));
        }
        finally
        {
            Directory.Delete(Path.GetDirectoryName(path)!, true);
        }
    }

    [Fact]
    public void Missing_file_gives_the_defaults()
    {
        var settings = EditorSettings.Load(Path.Combine(Path.GetTempPath(), "axdown-tests", Guid.NewGuid().ToString("N"), "axdown.json"), out var error);
        Assert.Null(error);
        Assert.Null(settings.FontFamily);
        Assert.Equal(0, settings.FontSize);
        Assert.True(settings.WordWrap);
        Assert.True(settings.CheckForUpdates);
        Assert.Null(settings.Window);
    }

    [Fact]
    public void Odd_values_are_put_right_after_loading()
    {
        var path = Path.Combine(Path.GetTempPath(), "axdown-tests", Guid.NewGuid().ToString("N"), "axdown.json");
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        try
        {
            File.WriteAllText(path, "{ \"fontFamily\": \"  \", \"fontSize\": 500, \"window\": { \"width\": 10, \"height\": 10 } }");
            var settings = EditorSettings.Load(path, out var error);
            Assert.Null(error);
            Assert.Null(settings.FontFamily);
            Assert.Equal(0, settings.FontSize);
            Assert.Null(settings.Window);
        }
        finally
        {
            Directory.Delete(Path.GetDirectoryName(path)!, true);
        }
    }
}
