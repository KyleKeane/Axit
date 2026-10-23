using Axit.Core;

namespace Axit.Tests;

/// <summary>
/// Every app of the bundle (<see cref="Dispatch.Apps"/>) has its places in the repository (docs/axit/SPEC.md 5.3 M1,
/// docs/axit/adding-an-app.md): its folders, its three documents, its notes, its icon, its block in the installer and
/// its line in the icon script, its place in the issue forms, the pages, the map and the spec, its call in the
/// program and its branch in run.ps1. Read off the files, so that a new app that is missing somewhere fails here
/// rather than at its release (AxStart was missing from nine of these on 2026-10-23).
/// </summary>
public sealed class InventoryTests
{
    /// <summary>AxDown's pure code and tests live in Axit.Core and Axit.Tests since AxJSON, the second editor (docs/axdown/SPEC.md §6).</summary>
    private static readonly IReadOnlySet<string> WithoutOwnCore = new HashSet<string> { "AxDown" };

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
            var name = app.Name;
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
            Expect(name, Text("run.ps1").Contains($"'{app.Verb}'", StringComparison.Ordinal), "run.ps1's verb");
            Expect(name, Text("install.ps1").Contains($"Name = '{name}'", StringComparison.Ordinal), "install.ps1's block");
            Expect(name, Text("tools/make-icon.ps1").Contains($"'{name}.ico'", StringComparison.Ordinal), "make-icon.ps1's line");
            Expect(name, Text("src/Axit/Program.cs").Contains($"{name}App.Run", StringComparison.Ordinal), "Program.cs's call");
            Expect(name, Text("src/Axit/Axit.csproj").Contains(name, StringComparison.Ordinal), "Axit.csproj's description");
            Expect(name, Text("CLAUDE.md").Contains($"**{name}**", StringComparison.Ordinal), "the root CLAUDE.md map");
            Expect(name, Text("README.md").Contains($"**{name}**", StringComparison.Ordinal), "README.md");
            Expect(name, Text("docs/axit/README.md").Contains($"**{name}**", StringComparison.Ordinal), "docs/axit/README.md");
            Expect(name, Text("docs/axit/SPEC.md").Contains($"**{name}**", StringComparison.Ordinal), "docs/axit/SPEC.md section 3");
            Expect(name, Text("docs/axit/SPEC.md").Contains($"`{app.Verb}`", StringComparison.Ordinal), "docs/axit/SPEC.md AX-1.1's verb");
            Expect(name, Text(".github/ISSUE_TEMPLATE/bug.yml").Contains($"- {name}", StringComparison.Ordinal), "the bug form's choices");
            Expect(name, Text(".github/ISSUE_TEMPLATE/idea.yml").Contains($"- {name}", StringComparison.Ordinal), "the idea form's choices");
            Expect(name, Text("CHANGELOG.md").Contains($"{name}:", StringComparison.Ordinal), "a CHANGELOG.md bullet");
        }

        Assert.True(missing.Count == 0, "Missing: " + string.Join("; ", missing));
    }
}
