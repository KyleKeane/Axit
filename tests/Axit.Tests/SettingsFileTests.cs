using Axit.Core;

namespace Axit.Tests;

public enum SampleKind
{
    One,
    TwoWords,
}

/// <summary>A settings class as every app writes one: plain properties with defaults.</summary>
public sealed class SampleSettings
{
    public string Name { get; set; } = "default";
    public int Size { get; set; } = 3;
    public SampleKind Kind { get; set; }
}

/// <summary>The shared read-and-write pattern of a settings file (docs/axit/SPEC.md AX-5.3) and the take-over of an old file (AX-5.2).</summary>
public sealed class SettingsFileTests
{
    private static string TempFolder() => Path.Combine(Path.GetTempPath(), "axit-tests", Guid.NewGuid().ToString("N"));

    [Fact]
    public void Round_trip_writes_camel_case_json_and_leaves_no_temporary_file()
    {
        var folder = TempFolder();
        var path = Path.Combine(folder, "sample.json");
        try
        {
            SettingsFile.Save(path, new SampleSettings { Name = "Consolas", Size = 14, Kind = SampleKind.TwoWords });
            var json = File.ReadAllText(path);
            Assert.Contains("\"name\": \"Consolas\"", json);
            Assert.Contains("\"kind\": \"twoWords\"", json);
            Assert.False(File.Exists(path + ".tmp"));

            var loaded = SettingsFile.Load<SampleSettings>(path, out var error);
            Assert.Null(error);
            Assert.Equal("Consolas", loaded.Name);
            Assert.Equal(14, loaded.Size);
            Assert.Equal(SampleKind.TwoWords, loaded.Kind);
        }
        finally
        {
            Directory.Delete(folder, true);
        }
    }

    [Fact]
    public void Missing_file_gives_defaults_without_an_error()
    {
        var loaded = SettingsFile.Load<SampleSettings>(Path.Combine(TempFolder(), "sample.json"), out var error);
        Assert.Null(error);
        Assert.Equal("default", loaded.Name);
        Assert.Equal(3, loaded.Size);
    }

    [Fact]
    public void Broken_file_gives_defaults_and_names_the_file()
    {
        var folder = TempFolder();
        var path = Path.Combine(folder, "sample.json");
        Directory.CreateDirectory(folder);
        try
        {
            File.WriteAllText(path, "{ not json");
            var loaded = SettingsFile.Load<SampleSettings>(path, out var error);
            Assert.NotNull(error);
            Assert.Contains(path, error);
            Assert.Equal(3, loaded.Size);
        }
        finally
        {
            Directory.Delete(folder, true);
        }
    }

    [Fact]
    public void Old_file_is_copied_once_and_never_over_a_new_one()
    {
        var folder = TempFolder();
        var oldPath = Path.Combine(folder, "old", "settings.json");
        var path = Path.Combine(folder, "new", "app.json");
        Directory.CreateDirectory(Path.GetDirectoryName(oldPath)!);
        try
        {
            SettingsFile.TakeOverOldFile(oldPath, path);
            Assert.False(File.Exists(path));

            File.WriteAllText(oldPath, "{ \"size\": 7 }");
            SettingsFile.TakeOverOldFile(oldPath, path);
            Assert.Equal(7, SettingsFile.Load<SampleSettings>(path, out _).Size);
            Assert.True(File.Exists(oldPath));

            File.WriteAllText(path, "{ \"size\": 9 }");
            SettingsFile.TakeOverOldFile(oldPath, path);
            Assert.Equal(9, SettingsFile.Load<SampleSettings>(path, out _).Size);
        }
        finally
        {
            Directory.Delete(folder, true);
        }
    }
}
