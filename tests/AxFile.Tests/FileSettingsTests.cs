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
        Assert.True(settings.SayEachFile);
        Assert.Equal(1, settings.Speed);
        Assert.Equal(15, settings.SeekSeconds);
        Assert.Equal(SortBy.Name, settings.SortBy);
        Assert.Empty(settings.Favourites);
        Assert.True(settings.CheckForUpdates);
    }

    [Fact]
    public void A_favourite_is_added_once_and_removed_by_the_same_toggle()
    {
        var settings = new FileSettings();
        Assert.True(settings.ToggleFavourite("C:\\Music"));
        Assert.True(settings.ToggleFavourite("C:\\Docs"));
        Assert.True(settings.IsFavourite("c:\\music"));
        Assert.False(settings.ToggleFavourite("c:\\MUSIC"));
        Assert.Equal(["C:\\Docs"], settings.Favourites);
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
        File.WriteAllText(path, """{ "speed": 9, "seekSeconds": 0, "lastFolder": " ", "recentFolders": ["", "C:\\x"], "favourites": ["C:\\f", " ", "c:\\F"], "fontSize": 500, "programs": [{ "name": "", "path": "C:\\a.exe" }, { "name": "b", "path": "C:\\b.exe" }] }""");
        try
        {
            var settings = FileSettings.Load(path, out var error);
            Assert.Null(error);
            Assert.Equal(3, settings.Speed);
            Assert.Equal(15, settings.SeekSeconds);
            Assert.Null(settings.LastFolder);
            Assert.Equal(["C:\\x"], settings.RecentFolders);
            Assert.Equal(0, settings.FontSize);
            Assert.Equal([new UserProgram("b", "C:\\b.exe")], settings.Programs);
            Assert.Equal(["C:\\f"], settings.Favourites);
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
            var settings = new FileSettings { Speed = 1.5, ShowHidden = true, SayEachFile = false, SortBy = SortBy.Date };
            settings.RememberFolder("C:\\a");
            settings.Programs.Add(new UserProgram("tidy", "C:\\tools\\tidy.ps1"));
            settings.Save(path);
            Assert.Contains("\"sortBy\": \"date\"", File.ReadAllText(path));
            var loaded = FileSettings.Load(path, out _);
            Assert.Equal(SortBy.Date, loaded.SortBy);
            Assert.Equal(1.5, loaded.Speed);
            Assert.True(loaded.ShowHidden);
            Assert.False(loaded.SayEachFile);
            Assert.Equal("C:\\a", loaded.LastFolder);
            Assert.Equal([new UserProgram("tidy", "C:\\tools\\tidy.ps1")], loaded.Programs);
        }
        finally
        {
            File.Delete(path);
        }
    }
}
