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
            ["axDown"] = AxDown.AxDownKeys.All,
            ["axJSON"] = AxJSON.AxJSONKeys.All,
            ["axClaude"] = AxClaude.AxClaudeKeys.All,
            ["axGIT"] = AxGit.AxGitKeys.All,
            ["axTerminal"] = AxTerminal.AxTerminalKeys.All,
            ["axFile"] = AxFile.AxFileKeys.All,
            ["axMedia"] = AxMedia.AxMediaKeys.All,
            ["axPreview"] = AxPreview.AxPreviewKeys.All,
            ["axStart"] = AxStart.AxStartKeys.All,
            ["axProcess"] = AxProcess.AxProcessKeys.All,
            ["axTask"] = AxTask.AxTaskKeys.All,
            ["axPass"] = AxPass.AxPassKeys.All,
        };

    /// <summary>
    /// The window keys that take a bundle key's place on purpose, each with its spec line (bundle AX-8.4). axTerminal's
    /// command field: Ctrl+Up and Ctrl+Down go through the history instead of by paragraph (AT-3.9, the user's choice
    /// on 2026-10-23 over Up and Down on the field's first and last line).
    /// </summary>
    private static readonly HashSet<(string Window, Keys Key)> Overrides =
    [
        ("axTerminal", Keys.Control | Keys.Up),
        ("axTerminal", Keys.Control | Keys.Down),
    ];

    [Fact]
    public void No_window_key_equals_a_bundle_key()
    {
        var bundle = BundleKeys.All.ToDictionary(k => k.Key, k => k.Name);
        var collisions = new List<string>();
        foreach (var (window, keys) in Windows)
        {
            foreach (var (name, key) in keys)
            {
                if (bundle.TryGetValue(key, out var bundleName) && !Overrides.Contains((window, key)))
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

    private static readonly IReadOnlyDictionary<string, string> F1Texts = new Dictionary<string, string>
    {
        ["axDown"] = AxDown.HelpText.Shortcuts,
        ["axJSON"] = AxJSON.HelpText.Shortcuts,
        ["axClaude"] = AxClaude.HelpText.Shortcuts,
        ["axGIT"] = AxGit.HelpText.Shortcuts,
        ["axTerminal"] = AxTerminal.HelpText.Shortcuts,
        ["axFile"] = AxFile.HelpText.Shortcuts,
        ["axMedia"] = AxMedia.HelpText.Shortcuts,
        ["axPreview"] = AxPreview.HelpText.Shortcuts,
        ["axStart"] = AxStart.HelpText.Shortcuts,
        ["axProcess"] = AxProcess.HelpText.Shortcuts,
        ["axTask"] = AxTask.HelpText.Shortcuts,
        ["axPass"] = AxPass.HelpText.Shortcuts,
    };

    /// <summary>
    /// The F1 text of every window names every key of its table that has a modifier or is a function key (AX-8.3:
    /// the text is changed with the table), so that a key change never leaves the help text behind. Plain letters
    /// and digits are left out: a letter is named by its word in the text ("h: the next heading").
    /// </summary>
    [Fact]
    public void Every_window_key_is_named_in_its_F1_text()
    {
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
                if ((modified || function) && !F1Texts[window].Contains(text, StringComparison.OrdinalIgnoreCase))
                {
                    missing.Add($"{window}: {name} ({text})");
                }
            }
        }

        Assert.True(missing.Count == 0, "Not in the F1 text: " + string.Join("; ", missing));
    }

    /// <summary>Every F1 text names the Axit menu with its first items in the bundle's words (AX-9), so that the menu is found from any window's help and the texts do not drift apart.</summary>
    [Fact]
    public void Every_F1_text_names_the_Axit_menu_and_its_first_items()
    {
        const string line = "Alt+A: the Axit menu: the command palette, a new window of any app, the files and folders closed lately";
        var missing = F1Texts.Where(pair => !pair.Value.Contains(line, StringComparison.Ordinal)).Select(pair => pair.Key).ToList();
        Assert.True(missing.Count == 0, "Without the Axit menu line: " + string.Join(", ", missing));
    }

    /// <summary>Every F1 text names the bundle keys every window has (AX-8.2, AX-10.6): F1, the palette, find, the text size.</summary>
    [Fact]
    public void Every_F1_text_names_the_keys_every_window_has()
    {
        Keys[] everywhere = [BundleKeys.Shortcuts, BundleKeys.CommandPalette, BundleKeys.Find, BundleKeys.FindNext, BundleKeys.FindPrevious, BundleKeys.LargerText, BundleKeys.SmallerText];
        var missing = new List<string>();
        foreach (var (window, text) in F1Texts)
        {
            foreach (var key in everywhere)
            {
                if (!text.Contains(BundleKeys.Text(key), StringComparison.OrdinalIgnoreCase))
                {
                    missing.Add($"{window}: {BundleKeys.Text(key)}");
                }
            }
        }

        Assert.True(missing.Count == 0, "Not in the F1 text: " + string.Join("; ", missing));
    }

    /// <summary>
    /// AX-8.4: the keys two windows use for different actions, held here as the known list, so that a new one fails
    /// this test and is added on purpose rather than felt under the fingers. Most are one gesture with the window's
    /// own meaning (Ctrl+N new, Ctrl+O open, Enter act on the item). The list is by key, so that renaming an action
    /// changes nothing here; the test's message names the windows and actions of a key not in the list.
    /// </summary>
    [Fact]
    public void Keys_used_differently_by_two_windows_are_the_known_ones()
    {
        string[] known =
        [
            "Alt+Up", "Ctrl+J", "Ctrl+Shift+N", "F2", "Left", "Right", "Shift+Tab", "Tab",
            "Apps", "Backspace", "Ctrl+1", "Ctrl+2", "Ctrl+3", "Ctrl+C", "Ctrl+E", "Ctrl+Enter", "Ctrl+G", "Ctrl+I", "Ctrl+K", "Ctrl+L",
            "Ctrl+N", "Ctrl+O", "Ctrl+P", "Ctrl+Q", "Ctrl+Shift+A", "Ctrl+Shift+C", "Ctrl+Shift+F", "Ctrl+Shift+H", "Ctrl+Shift+I",
            "Ctrl+Shift+K", "Ctrl+Shift+L", "Ctrl+Shift+Q", "Ctrl+Shift+R", "Ctrl+Shift+S", "Ctrl+Shift+T", "Ctrl+Shift+W", "Ctrl+T",
            "Ctrl+W", "Ctrl+Z", "Delete", "Down", "Enter", "Escape", "F5", "O", "Shift+Escape", "Shift+F10", "Space", "Up",
        ];
        var differing = Windows.SelectMany(w => w.Value.Select(k => (Window: w.Key, k.Name, k.Key)))
            .GroupBy(k => k.Key)
            .Where(g => g.Select(k => k.Name).Distinct().Count() > 1)
            .ToDictionary(g => BundleKeys.Text(g.Key).TrimEnd('.'), g => string.Join(", ", g.Select(k => $"{k.Window} {k.Name}")));
        var unexpected = differing.Where(pair => !known.Contains(pair.Key)).Select(pair => $"{pair.Key}: {pair.Value}").ToList();
        Assert.True(unexpected.Count == 0, "Used differently by two windows and not in the known list: " + string.Join("; ", unexpected));
        Assert.Equal(known.OrderBy(text => text, StringComparer.Ordinal), differing.Keys.OrderBy(text => text, StringComparer.Ordinal));
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
