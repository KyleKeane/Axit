using System.Text.RegularExpressions;

namespace Axit.Tests;

/// <summary>
/// A menu bar item's drop-down is the one WinForms makes (docs/axit/SPEC.md AX-6.6): a hand-made drop-down assigned
/// to <c>DropDown</c> loses its owner item when it closes, after which the bar opens its neighbours' menus on Left and
/// Right (found in axFile's Item menu, 2026-10-21). Read off the sources, so that the construct fails here before it
/// is felt in a window.
/// </summary>
public sealed class MenuTests
{
    [Fact]
    public void No_menu_item_gets_a_hand_made_drop_down()
    {
        var root = LayoutTests.RepositoryRoot();
        var offenders = new List<string>();
        foreach (var file in Directory.GetFiles(Path.Combine(root, "src"), "*.cs", SearchOption.AllDirectories))
        {
            if (file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}"))
            {
                continue;
            }

            var number = 0;
            foreach (var line in File.ReadLines(file))
            {
                number++;
                if (Regex.IsMatch(line, @"\bDropDown\s*=\s*new\b"))
                {
                    offenders.Add($"{Path.GetRelativePath(root, file)}:{number}: {line.Trim()}");
                }
            }
        }

        Assert.True(offenders.Count == 0, "A hand-made drop-down on a menu item: " + string.Join("; ", offenders));
    }
}
