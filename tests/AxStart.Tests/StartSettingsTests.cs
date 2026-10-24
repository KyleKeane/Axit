using AxStart.Core;
using Axit.Core;

namespace AxStart.Tests;

/// <summary>axStart's settings around the hotkey (SPEC.md AS-5.1): the values kept, nonsense mended.</summary>
public sealed class StartSettingsTests
{
    [Fact]
    public void Settings_keep_their_values_and_mend_nonsense()
    {
        var folder = Path.Combine(Path.GetTempPath(), "axstart-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        try
        {
            var path = Path.Combine(folder, "axstart.json");
            var settings = StartSettings.Load(path, out var error);
            Assert.Null(error);
            Assert.Equal("Ctrl+Shift+Space", settings.HotkeyText);
            Assert.True(settings.StartWithWindows);
            Assert.True(settings.SayFirstMatch);

            settings.HotkeyText = "Ctrl+Alt+K";
            settings.Started("Notepad");
            settings.Started("notepad");
            settings.Save(path);
            var back = StartSettings.Load(path, out _);
            Assert.Equal(new Hotkey(Hotkey.Control | Hotkey.Alt, 0x4B), back.Key);
            Assert.Equal(2, back.Starts["NOTEPAD"]);

            File.WriteAllText(path, """{ "hotkey": "Shift+A", "starts": { "Notepad": 2, "notepad": 1, "": 4, "Calculator": -3 } }""");
            var mended = StartSettings.Load(path, out _);
            Assert.Equal("Ctrl+Shift+Space", mended.HotkeyText);
            Assert.Equal(3, mended.Starts["Notepad"]);
            Assert.Single(mended.Starts);
        }
        finally
        {
            Directory.Delete(folder, recursive: true);
        }
    }
}
