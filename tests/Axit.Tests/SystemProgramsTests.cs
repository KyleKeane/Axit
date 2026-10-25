using Axit.Core;

namespace Axit.Tests;

/// <summary>Where the apps' programs are found (docs/axit/security-audit-2026-10-24.md S-1).</summary>
public sealed class SystemProgramsTests
{
    [Fact]
    public void Windows_programs_are_named_by_their_full_paths()
    {
        Assert.All([SystemPrograms.Explorer, SystemPrograms.Cmd, SystemPrograms.PowerShell], path => Assert.True(Path.IsPathFullyQualified(path)));
    }

    [Fact]
    public void A_relative_path_entry_is_never_searched_as_it_would_be_the_current_folder()
    {
        using var temp = new TempFolder("axit-path");
        var folder = temp.Path;
        var current = Environment.CurrentDirectory;
        try
        {
            File.WriteAllText(Path.Combine(folder, "planted.exe"), string.Empty);
            Environment.CurrentDirectory = folder;
            Assert.Null(SystemPrograms.OnPath("planted.exe", @".;sub;"""""));
            Assert.Equal(Path.Combine(folder, "planted.exe"), SystemPrograms.OnPath("planted.exe", $"relative;\"{folder}\""));
        }
        finally
        {
            Environment.CurrentDirectory = current;
        }
    }
}
