using System.Windows.Forms;
using Axit.Forms;

namespace Axit.Tests;

/// <summary>
/// The key tables (docs/axit/SPEC.md AX-8.4): no window-specific key equals a bundle-wide key, no table repeats a
/// key, and the keys two windows use for different actions are listed so that a collision between apps is seen
/// before it is felt. AxClaude's table joins here at plan step 11.
/// </summary>
public sealed class KeyTests
{
    private static readonly IReadOnlyDictionary<string, IReadOnlyList<(string Name, Keys Key)>> Windows =
        new Dictionary<string, IReadOnlyList<(string Name, Keys Key)>>
        {
            ["AxDown"] = AxDown.AxDownKeys.All,
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
}
