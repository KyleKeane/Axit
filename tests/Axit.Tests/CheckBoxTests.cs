using System.Text.RegularExpressions;

namespace Axit.Tests;

/// <summary>
/// Every check box is the bundle's <c>Axit.Forms/AxitCheckBox</c> (docs/axit/SPEC.md AX-8.8), which says its new state
/// when the user toggles it, so that a new app gets that by making the box and nothing else. Read off the sources, as
/// <see cref="ListTests"/> reads the lists: a plain <c>CheckBox</c> made anywhere in <c>src</c> fails here.
/// </summary>
public sealed class CheckBoxTests
{
    [Fact]
    public void Every_check_box_is_the_bundles_check_box()
    {
        var root = LayoutTests.RepositoryRoot();
        var offenders = new List<string>();
        foreach (var file in Directory.GetFiles(Path.Combine(root, "src"), "*.cs", SearchOption.AllDirectories))
        {
            var relative = Path.GetRelativePath(root, file);
            if (relative.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}"))
            {
                continue;
            }

            var number = 0;
            foreach (var line in File.ReadLines(file))
            {
                number++;
                // "CheckBox _box = new()", "CheckBox Box { get; } = new()", "new CheckBox()": a plain check box made.
                if (Regex.IsMatch(line, @"(?<!Axit)\bCheckBox\s+_\w+\s*=\s*new\b|(?<!Axit)\bCheckBox\s+\w+\s*\{\s*get;\s*\}\s*=\s*new\b|\bnew\s+CheckBox\s*\("))
                {
                    offenders.Add($"{relative}:{number}: {line.Trim()}");
                }
            }
        }

        Assert.True(offenders.Count == 0, "A plain CheckBox where the bundle's AxitCheckBox belongs (its state said on a toggle, AX-8.8): " + string.Join("; ", offenders));
    }
}
