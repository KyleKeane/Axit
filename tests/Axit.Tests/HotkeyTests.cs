using Axit.Core;

namespace Axit.Tests;

/// <summary>The hotkey as text both ways and the keys refused (docs/axstart/SPEC.md AS-4.2); from AxStart.Tests with the record, 2026-10-23.</summary>
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
    public void A_key_outside_the_names_is_written_by_its_number_and_refused()
    {
        var odd = new Hotkey(Hotkey.Control | Hotkey.Alt, 0xFF);
        Assert.Equal("Ctrl+Alt+Key 255", odd.ToString());
        Assert.Equal("That key cannot be a hotkey", odd.Refusal());
        Assert.Null(Hotkey.Parse(odd.ToString()));
    }
}
