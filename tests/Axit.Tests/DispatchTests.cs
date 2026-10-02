using Axit.Core;

namespace Axit.Tests;

/// <summary>Which app a command line starts (docs/axit/SPEC.md AX-1.1 to AX-1.3).</summary>
public sealed class DispatchTests
{
    private static readonly string Folder = Path.GetTempPath();

    [Fact]
    public void NoArgumentsStartAxClaude()
    {
        var launch = Dispatch.Decide([]);
        Assert.Equal(Dispatch.AxClaude, launch.App);
        Assert.Empty(launch.Arguments);
        Assert.Null(launch.Error);
    }

    [Fact]
    public void VerbNamesTheAppAndPassesTheRestOn()
    {
        var launch = Dispatch.Decide(["claude", Folder, "--record", "x.vt"]);
        Assert.Equal(Dispatch.AxClaude, launch.App);
        Assert.Equal([Folder, "--record", "x.vt"], launch.Arguments);

        launch = Dispatch.Decide(["down", "notes.md"]);
        Assert.Equal(Dispatch.AxDown, launch.App);
        Assert.Equal(["notes.md"], launch.Arguments);
    }

    [Fact]
    public void VerbIgnoresCase()
    {
        Assert.Equal(Dispatch.AxDown, Dispatch.Decide(["Down"]).App);
        Assert.Equal(Dispatch.AxClaude, Dispatch.Decide(["CLAUDE"]).App);
    }

    [Fact]
    public void HelpAndVersionGoToTheBundle()
    {
        foreach (var flag in new[] { "--help", "-h", "-?", "/?", "--version" })
        {
            var launch = Dispatch.Decide([flag]);
            Assert.Equal(Dispatch.Bundle, launch.App);
            Assert.Equal([flag], launch.Arguments);
            Assert.Null(launch.Error);
        }
    }

    [Fact]
    public void OptionsWithoutAVerbMeanAxClaude()
    {
        var launch = Dispatch.Decide(["--project", Folder]);
        Assert.Equal(Dispatch.AxClaude, launch.App);
        Assert.Equal(["--project", Folder], launch.Arguments);

        launch = Dispatch.Decide(["--"]);
        Assert.Equal(Dispatch.AxClaude, launch.App);
        Assert.Equal(["--"], launch.Arguments);
    }

    [Fact]
    public void FolderGoesToAxClaudeWithEveryArgument()
    {
        var launch = Dispatch.Decide([Folder, "--", "--resume"]);
        Assert.Equal(Dispatch.AxClaude, launch.App);
        Assert.Equal([Folder, "--", "--resume"], launch.Arguments);
    }

    [Fact]
    public void FileGoesToAxDown()
    {
        var file = Path.Combine(Folder, $"axit-dispatch-{Guid.NewGuid():N}.md");
        File.WriteAllText(file, "# Hello");
        try
        {
            var launch = Dispatch.Decide([file]);
            Assert.Equal(Dispatch.AxDown, launch.App);
            Assert.Equal([file], launch.Arguments);
        }
        finally
        {
            File.Delete(file);
        }
    }

    [Fact]
    public void MissingPathIsAnError()
    {
        var missing = Path.Combine(Folder, $"axit-dispatch-{Guid.NewGuid():N}");
        var launch = Dispatch.Decide([missing]);
        Assert.Equal(Dispatch.Bundle, launch.App);
        Assert.Contains(missing, launch.Error);
    }

    [Fact]
    public void UsageNamesEveryWayToStart()
    {
        Assert.Contains("Axit claude", Dispatch.Usage);
        Assert.Contains("Axit down", Dispatch.Usage);
        Assert.Contains("--version", Dispatch.Usage);
    }
}
