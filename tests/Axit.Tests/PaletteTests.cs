using Axit.Core;

namespace Axit.Tests;

/// <summary>The command palette's lines and search (docs/axit/SPEC.md AX-10.2, AX-10.3, AX-10.4).</summary>
public sealed class PaletteTests
{
    private static readonly IReadOnlyList<PaletteEntry> Entries =
    [
        Entry("Save, File, Ctrl+S"),
        Entry("Save as, File, Ctrl+Shift+S"),
        Entry("Larger text, Options, Text, Ctrl+Plus"),
        Entry("Smaller text, Options, Text, Ctrl+Minus"),
        Entry("Open settings file, Options, Developer"),
    ];

    private static PaletteEntry Entry(string text) => new(text, () => { });

    private static List<string> Lines(string text) => Palette.Find(Entries, text).Select(entry => entry.Text).ToList();

    [Fact]
    public void A_name_is_the_item_text_without_its_mnemonic_and_dots()
    {
        Assert.Equal("Save as", Palette.Name("Save &as..."));
        Assert.Equal("Font", Palette.Name("&Font…"));
        Assert.Equal("Find & replace", Palette.Name("Find && &replace"));
    }

    [Fact]
    public void A_line_is_the_name_the_menus_the_key_and_checked()
    {
        Assert.Equal("Word wrap, Edit, Ctrl+Shift+W, checked", Palette.Text("Word wrap", ["Edit"], "Ctrl+Shift+W", true));
        Assert.Equal("Larger text, Options, Text", Palette.Text("Larger text", ["Options", "Text"], null, false));
    }

    [Fact]
    public void Every_word_counts_in_any_order_and_the_menus_too()
    {
        Assert.Equal(["Larger text, Options, Text, Ctrl+Plus"], Lines("text larger"));
        Assert.Equal(["Open settings file, Options, Developer"], Lines("developer"));
    }

    [Fact]
    public void A_line_starting_with_the_text_comes_first_and_the_menus_order_stays()
    {
        Assert.Equal(["Save, File, Ctrl+S", "Save as, File, Ctrl+Shift+S"], Lines("sa"));
        Assert.Equal(["Save, File, Ctrl+S", "Save as, File, Ctrl+Shift+S", "Open settings file, Options, Developer"], Lines("file"));
    }

    [Fact]
    public void An_empty_text_gives_every_entry_in_the_menus_order()
    {
        Assert.Equal(Entries.Select(entry => entry.Text), Lines(" "));
    }

    [Fact]
    public void What_is_said()
    {
        Assert.Equal("Save as, File, Ctrl+Shift+S, 2 of 5", Palette.Spoken(Entries[1], 1, 5));
        Assert.Equal("No commands match xyz", Palette.NoMatch(" xyz "));
    }
}
