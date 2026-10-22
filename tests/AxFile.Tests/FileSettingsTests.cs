using AxFile.Core;

namespace AxFile.Tests;

/// <summary>The settings file (docs/axfile/SPEC.md AF-8.1): defaults, the recent list, and nonsense sanitized.</summary>
public sealed class FileSettingsTests
{
    [Fact]
    public void Defaults()
    {
        var settings = new FileSettings();
        Assert.Null(settings.LastFolder);
        Assert.Empty(settings.RecentFolders);
        Assert.False(settings.ShowHidden);
        Assert.False(settings.ShowDotFiles);
        Assert.Equal(SortBy.Name, settings.SortBy);
        Assert.Empty(settings.Favorites);
        Assert.True(settings.CheckForUpdates);
    }

    [Fact]
    public void A_favorite_is_added_once_and_removed_by_the_same_toggle()
    {
        var settings = new FileSettings();
        Assert.True(settings.ToggleFavorite("C:\\Music"));
        Assert.True(settings.ToggleFavorite("C:\\Docs"));
        Assert.True(settings.IsFavorite("c:\\music"));
        Assert.False(settings.ToggleFavorite("c:\\MUSIC"));
        Assert.Equal(["C:\\Docs"], settings.Favorites);
    }

    [Fact]
    public void Remembered_folders_go_first_once_and_stay_ten()
    {
        var settings = new FileSettings();
        for (var i = 0; i < 12; i++)
        {
            settings.RememberFolder($"C:\\f{i}");
        }

        settings.RememberFolder("c:\\F5");
        Assert.Equal(10, settings.RecentFolders.Count);
        Assert.Equal("c:\\F5", settings.RecentFolders[0]);
        Assert.Equal("c:\\F5", settings.LastFolder);
        Assert.Single(settings.RecentFolders, f => string.Equals(f, "C:\\f5", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Nonsense_in_the_file_falls_back()
    {
        var path = Path.Combine(Path.GetTempPath(), "axfile-settings-" + Guid.NewGuid().ToString("N") + ".json");
        File.WriteAllText(path, """{ "speed": 9, "seekSeconds": 0, "lastFolder": " ", "recentFolders": ["", "C:\\x"], "favorites": ["C:\\f", " ", "c:\\F"], "fontSize": 500, "programs": [{ "name": "", "path": "C:\\a.exe" }, { "name": "b", "path": "C:\\b.exe" }] }""");
        try
        {
            // An older file's player keys (speed, seekSeconds) are AxMedia's now and are ignored.
            var settings = FileSettings.Load(path, out var error);
            Assert.Null(error);
            Assert.Null(settings.LastFolder);
            Assert.Equal(["C:\\x"], settings.RecentFolders);
            Assert.Equal(0, settings.FontSize);
            Assert.Equal([new UserProgram("b", "C:\\b.exe")], settings.Programs);
            Assert.Equal(["C:\\f"], settings.Favorites);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void Round_trip()
    {
        var path = Path.Combine(Path.GetTempPath(), "axfile-settings-" + Guid.NewGuid().ToString("N") + ".json");
        try
        {
            var settings = new FileSettings { ShowHidden = true, SortBy = SortBy.Date };
            settings.RememberFolder("C:\\a");
            settings.Programs.Add(new UserProgram("tidy", "C:\\tools\\tidy.ps1"));
            settings.Save(path);
            Assert.Contains("\"sortBy\": \"date\"", File.ReadAllText(path));
            var loaded = FileSettings.Load(path, out _);
            Assert.Equal(SortBy.Date, loaded.SortBy);
            Assert.True(loaded.ShowHidden);
            Assert.Equal("C:\\a", loaded.LastFolder);
            Assert.Equal([new UserProgram("tidy", "C:\\tools\\tidy.ps1")], loaded.Programs);
        }
        finally
        {
            File.Delete(path);
        }
    }
}
