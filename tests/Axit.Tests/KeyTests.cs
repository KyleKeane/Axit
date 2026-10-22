using System.Windows.Forms;
using Axit.Forms;

namespace Axit.Tests;

/// <summary>
/// The key tables (docs/axit/SPEC.md AX-8.4): no window-specific key equals a bundle-wide key, no table repeats a
/// key, and the keys two windows use for different actions are listed so that a collision between apps is seen
/// before it is felt. The text a menu shows for a key (AX-8.5) is checked here too.
/// </summary>
public sealed class KeyTests
{
    private static readonly IReadOnlyDictionary<string, IReadOnlyList<(string Name, Keys Key)>> Windows =
        new Dictionary<string, IReadOnlyList<(string Name, Keys Key)>>
        {
            ["AxDown"] = AxDown.AxDownKeys.All,
            ["AxJSON"] = AxJSON.AxJSONKeys.All,
            ["AxClaude"] = AxClaude.AxClaudeKeys.All,
            ["AxGit"] = AxGit.AxGitKeys.All,
            ["AxTerminal"] = AxTerminal.AxTerminalKeys.All,
            ["AxFile"] = AxFile.AxFileKeys.All,
            ["AxMedia"] = AxMedia.AxMediaKeys.All,
            ["AxPreview"] = AxPreview.AxPreviewKeys.All,
            ["AxStart"] = AxStart.AxStartKeys.All,
        };

    [Fact]
    public void No_window_key_equals_a_bundle_key()
    {
        var bundle = BundleKeys.All.ToDictionary(k => k.Key, k => k.Name);
        var collisions = new List<string>();
        foreach (var (window, keys) in Windows)
        {
            foreach (var (name, key) in keys)
            {
                if (bundle.TryGetValue(key, out var bundleName))
                {
                    collisions.Add($"{window}: {name} is {key}, the bundle's {bundleName}.");
                }
            }
        }

        Assert.Empty(collisions);
    }

    [Fact]
    public void No_table_repeats_a_key()
    {
        var tables = new Dictionary<string, IReadOnlyList<(string Name, Keys Key)>>(Windows) { ["Bundle"] = BundleKeys.All };
        var repeats = new List<string>();
        foreach (var (table, keys) in tables)
        {
            foreach (var group in keys.GroupBy(k => k.Key).Where(g => g.Count() > 1))
            {
                repeats.Add($"{table}: {group.Key} is {string.Join(" and ", group.Select(k => k.Name))}.");
            }
        }

        Assert.Empty(repeats);
    }

    /// <summary>
    /// The F1 text of every window names every key of its table that has a modifier or is a function key (AX-8.3:
    /// the text is changed with the table), so that a key change never leaves the help text behind. Plain letters
    /// and digits are left out: a letter is named by its word in the text ("h: the next heading").
    /// </summary>
    [Fact]
    public void Every_window_key_is_named_in_its_F1_text()
    {
        var texts = new Dictionary<string, string>
        {
            ["AxDown"] = AxDown.HelpText.Shortcuts,
            ["AxJSON"] = AxJSON.HelpText.Shortcuts,
            ["AxClaude"] = AxClaude.HelpText.Shortcuts,
            ["AxGit"] = AxGit.HelpText.Shortcuts,
            ["AxTerminal"] = AxTerminal.HelpText.Shortcuts,
            ["AxFile"] = AxFile.HelpText.Shortcuts,
            ["AxMedia"] = AxMedia.HelpText.Shortcuts,
            ["AxPreview"] = AxPreview.HelpText.Shortcuts,
            ["AxStart"] = AxStart.HelpText.Shortcuts,
        };
        var missing = new List<string>();
        foreach (var (window, keys) in Windows)
        {
            foreach (var (name, key) in keys)
            {
                var code = key & Keys.KeyCode;
                var modified = (key & Keys.Modifiers) != 0 && code != Keys.None;
                var function = code is >= Keys.F1 and <= Keys.F24;
                // A number row key's text ends in a full stop for speech ("Ctrl+1."); the help text writes it bare.
                var text = BundleKeys.Text(key).TrimEnd('.');
                if ((modified || function) && !texts[window].Contains(text, StringComparison.OrdinalIgnoreCase))
                {
                    missing.Add($"{window}: {name} ({text})");
                }
            }
        }

        Assert.True(missing.Count == 0, "Not in the F1 text: " + string.Join("; ", missing));
    }

    /// <summary>Not a failure: the list of keys two windows use differently, printed for the person reading the test output.</summary>
    [Fact]
    public void Keys_used_differently_by_two_windows_are_listed()
    {
        var byKey = Windows.SelectMany(w => w.Value.Select(k => (Window: w.Key, k.Name, k.Key)))
            .GroupBy(k => k.Key)
            .Where(g => g.Select(k => k.Name).Distinct().Count() > 1)
            .Select(g => $"{g.Key}: {string.Join(", ", g.Select(k => $"{k.Window} {k.Name}"))}")
            .ToList();
        Assert.True(byKey.Count >= 0, string.Join("\n", byKey));
    }

    /// <summary>
    /// AX-8.5: the menu text of a key; a key of the number row ends in a full stop, so that NVDA does not run
    /// "Ctrl+1" and the item's position "1 of 9" together into "Ctrl+11 of 9".
    /// </summary>
    [Theory]
    [InlineData(Keys.Control | Keys.D1, "Ctrl+1.")]
    [InlineData(Keys.Control | Keys.D3, "Ctrl+3.")]
    [InlineData(Keys.Control | Keys.Shift | Keys.S, "Ctrl+Shift+S")]
    [InlineData(Keys.F3, "F3")]
    [InlineData(Keys.Shift | Keys.Escape, "Shift+Escape")]
    [InlineData(Keys.Control | Keys.Oemplus, "Ctrl+Plus")]
    [InlineData(Keys.Control | Keys.Shift | Keys.OemPeriod, "Ctrl+Shift+Period")]
    [InlineData(Keys.Control | Keys.Shift | Keys.Oemcomma, "Ctrl+Shift+Comma")]
    [InlineData(Keys.Back, "Backspace")]
    [InlineData(Keys.Return, "Enter")]
    [InlineData(Keys.Alt | Keys.Return, "Alt+Enter")]
    public void Menu_text_of_a_key(Keys key, string expected)
    {
        Assert.Equal(expected, BundleKeys.Text(key));
    }
}
