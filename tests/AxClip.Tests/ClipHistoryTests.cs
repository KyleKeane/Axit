using AxClip.Core;

namespace AxClip.Tests;

/// <summary>The history (docs/axclip/SPEC.md CP-2.3, CP-2.5, CP-3), what a copy is kept for (CP-3.1) and the settings (CP-6.1).</summary>
public sealed class ClipHistoryTests
{
    private static readonly DateTime Now = new(2026, 10, 23, 14, 0, 0, DateTimeKind.Local);

    [Fact]
    public void A_copy_is_the_newest_and_a_copy_again_moves_it_to_the_top()
    {
        var history = new ClipHistory();
        history.Add("one", Now);
        history.Add("two", Now.AddMinutes(1));
        history.Add("one", Now.AddMinutes(2));
        Assert.Equal(["one", "two"], history.Items.Select(item => item.Text));
        Assert.Equal(Now.AddMinutes(2), history.Items[0].Copied);
    }

    [Fact]
    public void The_history_keeps_the_newest_two_hundred()
    {
        var history = new ClipHistory();
        for (var i = 0; i < ClipHistory.MaxItems + 5; i++)
        {
            history.Add($"item {i}", Now.AddSeconds(i));
        }

        Assert.Equal(ClipHistory.MaxItems, history.Items.Count);
        Assert.Equal($"item {ClipHistory.MaxItems + 4}", history.Items[0].Text);
        Assert.Equal("item 5", history.Items[^1].Text);
    }

    [Fact]
    public void A_label_is_the_first_line_with_text_squeezed_and_cut()
    {
        Assert.Equal("git status --porcelain", new ClipEntry("git status --porcelain", Now).Label);
        Assert.Equal("First line, 3 lines", new ClipEntry("\r\n  First   line  \r\nsecond\r\nthird\r\n", Now).Label);
        var long_ = new ClipEntry(new string('a', 200), Now).Label;
        Assert.Equal(ClipEntry.LabelLength + 1, long_.Length);
        Assert.EndsWith("…", long_);
        Assert.Equal(3, new ClipEntry("a\nb\nc\n", Now).LineCount);
    }

    [Fact]
    public void Typing_narrows_to_the_items_with_every_word_newest_first()
    {
        var history = new ClipHistory();
        history.Add("export TOKEN=abc", Now);
        history.Add("git status", Now.AddMinutes(1));
        history.Add("Café au lait token", Now.AddMinutes(2));
        Assert.Equal(["Café au lait token", "export TOKEN=abc"], history.Find("token").Select(item => item.Text));
        Assert.Equal(["Café au lait token"], history.Find("cafe TOKEN").Select(item => item.Text));
        Assert.Equal(3, history.Find("  ").Count);
        Assert.Equal("Nothing in the clipboard history matches xyz", history.Nothing(" xyz "));
        Assert.Equal("git status, 2 of 3", ClipHistory.Spoken(history.Items[1], 1, 3));
        Assert.Equal("The clipboard history is empty", new ClipHistory().Nothing("x"));
    }

    [Fact]
    public void Remove_and_clear()
    {
        var history = new ClipHistory([new ClipEntry("a", Now), new ClipEntry("b", Now), new ClipEntry("a", Now)]);
        Assert.Equal(["a", "b"], history.Items.Select(item => item.Text));
        history.Remove(history.Items[0]);
        Assert.Equal(["b"], history.Items.Select(item => item.Text));
        history.Clear();
        Assert.Empty(history.Items);
    }

    [Theory]
    [InlineData("text", false, null, false, true)]
    [InlineData("text", true, null, false, false)]
    [InlineData("text", false, 0, false, false)]
    [InlineData("text", false, 1, false, true)]
    [InlineData("text", false, null, true, false)]
    [InlineData("   ", false, null, false, false)]
    [InlineData("", false, null, false, false)]
    [InlineData(null, false, null, false, false)]
    public void A_copy_is_kept_unless_marked_paused_or_empty(string? text, bool excluded, int? historyValue, bool paused, bool kept) =>
        Assert.Equal(kept, ClipPolicy.Keeps(text, excluded, historyValue, paused));

    [Fact]
    public void A_copy_over_a_million_characters_is_not_kept() =>
        Assert.False(ClipPolicy.Keeps(new string('x', ClipPolicy.MaxLength + 1), false, null, false));

    [Fact]
    public void The_settings_keep_the_history_only_while_asked()
    {
        var folder = Path.Combine(Path.GetTempPath(), "axclip-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        try
        {
            var path = Path.Combine(folder, "axclip.json");
            var settings = ClipSettings.Load(path, out var error);
            Assert.Null(error);
            Assert.Equal("Windows+Alt+V", settings.HotkeyText);
            Assert.False(settings.KeepHistory);
            settings.Keep([new ClipEntry("secret", Now)]);
            Assert.Empty(settings.History);

            settings.KeepHistory = true;
            settings.Keep([new ClipEntry("kept", Now)]);
            settings.Save(path);
            var back = ClipSettings.Load(path, out _);
            Assert.Equal("kept", back.History.Single().Text);
            // S-21: the file holds the history only encrypted for this Windows account.
            Assert.DoesNotContain("kept", File.ReadAllText(path), StringComparison.Ordinal);

            File.WriteAllText(path, """{ "hotkey": "Shift+V", "keepHistory": false, "history": [ { "text": "left over", "copied": "2026-10-23T14:00:00" } ] }""");
            var mended = ClipSettings.Load(path, out _);
            Assert.Equal("Windows+Alt+V", mended.HotkeyText);
            Assert.Empty(mended.History);
        }
        finally
        {
            Directory.Delete(folder, recursive: true);
        }
    }
}
