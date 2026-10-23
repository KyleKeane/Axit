using AxStart.Core;

namespace AxStart.Tests;

/// <summary>The hotkey as text both ways and the keys refused (SPEC.md AS-4.2), and the settings around it (AS-5.1).</summary>
public sealed class HotkeyTests
{
    [Fact]
    public void The_default_is_ctrl_shift_space()
    {
        Assert.Equal("Ctrl+Shift+Space", Hotkey.Default.ToString());
    }

    [Theory]
    [InlineData("Ctrl+Shift+Space")]
    [InlineData("Ctrl+Alt+Space")]
    [InlineData("Ctrl+Shift+F12")]
    [InlineData("Ctrl+Windows+Alt+Shift+Page Down")]
    [InlineData("Alt+A")]
    public void The_text_comes_back_the_same(string text)
    {
        Assert.Equal(text, Hotkey.Parse(text)!.ToString());
    }

    [Fact]
    public void Any_order_and_case_is_read()
    {
        Assert.Equal(new Hotkey(Hotkey.Alt | Hotkey.Windows, 0x20), Hotkey.Parse("alt + win + space"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("Ctrl+")]
    [InlineData("Hyper+Space")]
    [InlineData("Ctrl+Comma")]
    public void Nonsense_is_not_a_hotkey(string text)
    {
        Assert.Null(Hotkey.Parse(text));
    }

    [Fact]
    public void A_key_needs_ctrl_alt_or_windows()
    {
        Assert.Equal("Use Ctrl, Alt or the Windows key with it", new Hotkey(Hotkey.Shift, 0x41).Refusal());
        Assert.Equal("Use Ctrl, Alt or the Windows key with it", new Hotkey(0, 0x20).Refusal());
        Assert.Null(new Hotkey(Hotkey.Windows, 0x41).Refusal());
        Assert.Equal("That key cannot be a hotkey", new Hotkey(Hotkey.Control, 0xBC).Refusal());
    }

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
