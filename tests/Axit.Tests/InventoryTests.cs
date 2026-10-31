using Axit.Core;

namespace Axit.Tests;

/// <summary>
/// Every app of the bundle (<see cref="Dispatch.Apps"/>) has its places in the repository (docs/axit/SPEC.md 5.3 M1,
/// docs/axit/adding-an-app.md): its folders, its three documents, its notes, its icon, its place in the issue forms,
/// the pages, the map and the spec, and its call in the program. Read off the files, so that a new app that is
/// missing somewhere fails here rather than at its release (axStart was missing from nine of these on 2026-10-23).
/// The installer, the icon script and run.ps1 read <see cref="Dispatch.Apps"/> itself since 2026-10-24
/// (docs/axit/app-kit.md AK-4.1), so they have no place to miss; tools/new-app.ps1 writes the places it can.
/// </summary>
public sealed class InventoryTests
{
    /// <summary>axDown's pure code and tests live in Axit.Core and Axit.Tests since axJSON, the second editor (docs/axdown/SPEC.md §6).</summary>
    private static readonly IReadOnlySet<string> WithoutOwnCore = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "axDown" };

    [Fact]
    public void Every_app_has_its_places()
    {
        var root = LayoutTests.RepositoryRoot();
        var missing = new List<string>();
        void Expect(string app, bool present, string place)
        {
            if (!present)
            {
                missing.Add($"{app}: {place}");
            }
        }

        var texts = new Dictionary<string, string>();
        string Text(string relative) => texts.TryGetValue(relative, out var text) ? text : texts[relative] = File.ReadAllText(Path.Combine(root, relative));

        foreach (var app in Dispatch.Apps)
        {
            // The code's places by the project name, the texts by the name as shown (Dispatch.App).
            var name = app.Project;
            var shown = app.Name;
            var lower = name.ToLowerInvariant();
            Expect(name, Directory.Exists(Path.Combine(root, "src", name)), "src folder");
            Expect(name, File.Exists(Path.Combine(root, "src", name, "CLAUDE.md")), "src notes (CLAUDE.md)");
            Expect(name, File.Exists(Path.Combine(root, "src", name, "HelpText.cs")), "HelpText.cs");
            Expect(name, File.Exists(Path.Combine(root, "src", name, $"{name}Keys.cs")), "the key table");
            if (!WithoutOwnCore.Contains(name))
            {
                Expect(name, Directory.Exists(Path.Combine(root, "src", $"{name}.Core")), "Core project");
                Expect(name, File.Exists(Path.Combine(root, "src", $"{name}.Core", "CLAUDE.md")), "Core notes (CLAUDE.md)");
                Expect(name, Directory.Exists(Path.Combine(root, "tests", $"{name}.Tests")), "test project");
            }

            foreach (var document in new[] { "SPEC.md", "user-guide.md", "nvda-test-plan.md" })
            {
                Expect(name, File.Exists(Path.Combine(root, "docs", lower, document)), $"docs/{lower}/{document}");
            }

            Expect(name, File.Exists(Path.Combine(root, "src", "Axit", $"{name}.ico")), "icon in src/Axit");
            Expect(name, File.Exists(Path.Combine(root, $"run-{lower}.ps1")), $"run-{lower}.ps1");
            Expect(name, Text("src/Axit/Program.cs").Contains($"{name}App.Run", StringComparison.Ordinal), "Program.cs's call");
            Expect(name, Text("src/Axit/Axit.csproj").Contains(shown, StringComparison.Ordinal), "Axit.csproj's description");
            Expect(name, Text("CLAUDE.md").Contains($"**{shown}**", StringComparison.Ordinal), "the root CLAUDE.md map");
            Expect(name, Text("README.md").Contains($"**{app.Title}**", StringComparison.Ordinal), "README.md, by its title (AX-11.2)");
            Expect(name, Text("docs/axit/README.md").Contains($"**{app.Title}**", StringComparison.Ordinal), "docs/axit/README.md, by its title (AX-11.2)");
            Expect(name, Text("docs/axit/SPEC.md").Contains($"**{shown}**", StringComparison.Ordinal), "docs/axit/SPEC.md section 3");
            Expect(name, Text("docs/axit/SPEC.md").Contains($"`{app.Verb}`", StringComparison.Ordinal), "docs/axit/SPEC.md AX-1.1's verb");
            Expect(name, Text(".github/ISSUE_TEMPLATE/bug.yml").Contains($"- {shown}", StringComparison.Ordinal), "the bug form's choices");
            Expect(name, Text(".github/ISSUE_TEMPLATE/idea.yml").Contains($"- {shown}", StringComparison.Ordinal), "the idea form's choices");
            Expect(name, Text("CHANGELOG.md").Contains($"{shown}:", StringComparison.Ordinal), "a CHANGELOG.md bullet");
        }

        Assert.True(missing.Count == 0, "Missing: " + string.Join("; ", missing));
    }
}
