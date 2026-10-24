using Axit.Core;

namespace Axit.Tests;

/// <summary>The first line and the bundle's section of every F1 text (docs/axit/app-kit.md AK-2.3, AX-7.2).</summary>
public sealed class BundleHelpTests
{
    private static string[] Lines(string text) => text.Split("\r\n");

    [Fact]
    public void The_section_has_the_header_the_heading_and_the_bundle_lines_around_the_apps_own()
    {
        var lines = Lines(BundleHelp.Section("axThing", ["Tab: between the list and the field", "Ctrl+S: save the thing"]));
        Assert.Equal(
            [
                "In this text: h and Shift+H move by heading, 1 to 6 by level. Ctrl+F finds, F3 and Shift+F3 the next and the previous match",
                "",
                "## In every window of Axit",
                "  F1: this list",
                "  Ctrl+Shift+P: the command palette: type part of a command's name, Down and Up through the matches, Enter runs it",
                "  Tab: between the list and the field",
                "  Ctrl+S: save the thing",
                "  Ctrl+Plus, Ctrl+Minus: bigger or smaller text",
                "  Alt: the menu. Alt+F4: close axThing",
                "  " + BundleHelp.AxitMenuLine,
                "",
                "",
            ],
            lines);
    }

    [Fact]
    public void The_Axit_menu_line_is_the_bundles_words()
    {
        Assert.Contains(
            "  Alt+A: the Axit menu: the command palette, a new window of any app, the files and folders closed lately, the open Axit windows, most recent first (Alt+A, O, 1 is the one you were in last) and by type, Close all windows, the AI tools, Check for updates, and About\r\n",
            BundleHelp.Section("axThing", []));
    }

    [Fact]
    public void The_app_names_what_Alt_F4_does_the_heading_and_the_Navigate_menu()
    {
        var lines = Lines(BundleHelp.Section("axThing", [], altF4: "hide axThing", heading: "Anywhere", navigateMenu: true));
        Assert.Equal("In this text: h and Shift+H move by heading, 1 to 6 by level; the Next and Previous keys of the Navigate menu work here too. Ctrl+F finds, F3 and Shift+F3 the next and the previous match", lines[0]);
        Assert.Equal("## Anywhere", lines[2]);
        Assert.Contains("  Alt: the menu. Alt+F4: hide axThing", lines);
        Assert.DoesNotContain(lines, line => line.Contains("close axThing", StringComparison.Ordinal));
    }

    /// <summary>Every app's F1 text starts with the builder's part, so that the bundle's lines are written once.</summary>
    [Fact]
    public void Every_F1_text_starts_with_the_bundle_section()
    {
        string[] texts =
        [
            AxCalc.HelpText.Shortcuts, AxClaude.HelpText.Shortcuts, AxClip.HelpText.Shortcuts, AxDown.HelpText.Shortcuts,
            AxFile.HelpText.Shortcuts, AxGit.HelpText.Shortcuts, AxJSON.HelpText.Shortcuts, AxMedia.HelpText.Shortcuts,
            AxPass.HelpText.Shortcuts, AxPreview.HelpText.Shortcuts, AxProcess.HelpText.Shortcuts, AxStart.HelpText.Shortcuts,
            AxTask.HelpText.Shortcuts, AxTerminal.HelpText.Shortcuts,
        ];
        foreach (var text in texts)
        {
            var lines = Lines(text);
            Assert.StartsWith("In this text: h and Shift+H move by heading, 1 to 6 by level", lines[0]);
            Assert.StartsWith("## ", lines[2]);
            Assert.Equal("  F1: this list", lines[3]);
            var axitMenu = Array.IndexOf(lines, "  " + BundleHelp.AxitMenuLine);
            Assert.True(axitMenu > 3, "No Axit menu line in the bundle section: " + lines[2]);
            Assert.StartsWith("  Alt: the menu. Alt+F4: ", lines[axitMenu - 1]);
            Assert.Equal("", lines[axitMenu + 1]);
            Assert.StartsWith("## ", lines[axitMenu + 2]);
        }
    }
}
