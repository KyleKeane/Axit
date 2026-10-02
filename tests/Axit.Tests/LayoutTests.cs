using System.Xml.Linq;

namespace Axit.Tests;

/// <summary>
/// The project references point one way (docs/axit/SPEC.md 5.1, M2): the shared projects (<c>Axit.*</c>) reference
/// nothing of the apps, an app never references another app, and an app's <c>.Core</c> never references its window.
/// Only <c>Axit</c>, the executable, may reference everything. Read off the project files, so a wrong reference fails
/// here before it is felt.
/// </summary>
public sealed class LayoutTests
{
    [Fact]
    public void References_point_one_way()
    {
        var root = RepositoryRoot();
        var problems = new List<string>();
        foreach (var file in Directory.GetFiles(Path.Combine(root, "src"), "*.csproj", SearchOption.AllDirectories))
        {
            var project = Path.GetFileNameWithoutExtension(file);
            if (project == "Axit")
            {
                continue;
            }

            foreach (var element in XDocument.Load(file).Descendants("ProjectReference"))
            {
                var reference = Path.GetFileNameWithoutExtension((string?)element.Attribute("Include") ?? "");
                if (project.StartsWith("Axit.", StringComparison.Ordinal))
                {
                    if (!reference.StartsWith("Axit.", StringComparison.Ordinal))
                    {
                        problems.Add($"{project} references {reference}: a shared project references nothing of the apps.");
                    }
                }
                else if (!reference.StartsWith("Axit.", StringComparison.Ordinal) && AppOf(reference) != AppOf(project))
                {
                    problems.Add($"{project} references {reference}: an app never references another app.");
                }
                else if (project.EndsWith(".Core", StringComparison.Ordinal) && reference == AppOf(project))
                {
                    problems.Add($"{project} references {reference}: a Core project never references its window.");
                }
            }
        }

        Assert.Empty(problems);
    }

    /// <summary><c>AxDown.Core</c> and <c>AxDown</c> are both the app AxDown.</summary>
    private static string AppOf(string project) =>
        project.EndsWith(".Core", StringComparison.Ordinal) ? project[..^".Core".Length] : project;

    private static string RepositoryRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "Axit.sln")))
        {
            dir = dir.Parent;
        }

        return dir?.FullName ?? throw new InvalidOperationException("Axit.sln not found above the test directory.");
    }
}
