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

        // axGIT is always named by its verb: a folder alone still means axClaude (docs/axgit/SPEC.md AG-1.1).
        launch = Dispatch.Decide(["git", Folder]);
        Assert.Equal(Dispatch.AxGit, launch.App);
        Assert.Equal([Folder], launch.Arguments);
        Assert.Equal(Dispatch.AxClaude, Dispatch.Decide([Folder]).App);
        Assert.Contains(Dispatch.Apps, app => app.Verb == Dispatch.AxGit && app.Name == "axGIT");
        launch = Dispatch.Decide(["terminal", Folder]);
        Assert.Equal(Dispatch.AxTerminal, launch.App);
        Assert.Equal([Folder], launch.Arguments);
        Assert.Contains(Dispatch.Apps, app => app.Verb == Dispatch.AxTerminal && app.Name == "axTerminal");
        // axFile is always named by its verb too (docs/axfile/SPEC.md AF-1.1).
        launch = Dispatch.Decide(["file", Folder]);
        Assert.Equal(Dispatch.AxFile, launch.App);
        Assert.Equal([Folder], launch.Arguments);
        Assert.Contains(Dispatch.Apps, app => app.Verb == Dispatch.AxFile && app.Name == "axFile");
        Assert.Contains("Axit file", Dispatch.Usage);
        // axMedia takes several paths and its own options, passed on whole (docs/axmedia/SPEC.md AM-1.1).
        launch = Dispatch.Decide(["media", "a.mp3", "b.mp3", "--add"]);
        Assert.Equal(Dispatch.AxMedia, launch.App);
        Assert.Equal(["a.mp3", "b.mp3", "--add"], launch.Arguments);
        Assert.Contains(Dispatch.Apps, app => app.Verb == Dispatch.AxMedia && app.Name == "axMedia");
        Assert.Contains("Axit media", Dispatch.Usage);
        // axPreview is always named by its verb: a file alone keeps going to its editor (docs/axpreview/SPEC.md AP-1.1).
        launch = Dispatch.Decide(["preview", "notes.md", "--new"]);
        Assert.Equal(Dispatch.AxPreview, launch.App);
        Assert.Equal(["notes.md", "--new"], launch.Arguments);
        Assert.Contains(Dispatch.Apps, app => app.Verb == Dispatch.AxPreview && app.Name == "axPreview");
        Assert.Contains("Axit preview", Dispatch.Usage);
        // axStart, the launcher, takes no path (docs/axstart/SPEC.md AS-1.1).
        launch = Dispatch.Decide(["start", "--hidden"]);
        Assert.Equal(Dispatch.AxStart, launch.App);
        Assert.Equal(["--hidden"], launch.Arguments);
        Assert.Contains(Dispatch.Apps, app => app.Verb == Dispatch.AxStart && app.Name == "axStart");
        Assert.Contains("Axit start", Dispatch.Usage);
    }

    /// <summary>AX-1.2: a sound or video file alone on the command line starts axMedia, as a double click in File Explorer would once it is the default.</summary>
    [Fact]
    public void MediaFileGoesToAxMedia()
    {
        var file = Path.Combine(Folder, $"axit-dispatch-{Guid.NewGuid():N}.mp3");
        File.WriteAllBytes(file, []);
        try
        {
            var launch = Dispatch.Decide([file]);
            Assert.Equal(Dispatch.AxMedia, launch.App);
            Assert.Equal([file], launch.Arguments);
        }
        finally
        {
            File.Delete(file);
        }
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
            Assert.Equal(flag != "--version", Dispatch.IsHelpFlag(flag));
        }

        Assert.False(Dispatch.IsHelpFlag("help"));
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

    [Theory]
    [InlineData("notes.md", "down")]
    [InlineData("README.MARKDOWN", "down")]
    [InlineData("report.pdf", "down")]
    [InlineData("settings.JSON", "json")]
    public void The_editor_is_AxJSON_for_json_and_AxDown_for_the_rest(string name, string expected)
    {
        Assert.Equal(expected, Dispatch.EditorOf(Path.Combine("C:\\x", name)));
    }

    [Fact]
    public void KeePassDatabaseGoesToAxPass()
    {
        var file = Path.Combine(Folder, $"axit-dispatch-{Guid.NewGuid():N}.kdbx");
        File.WriteAllBytes(file, [0x03, 0xD9, 0xA2, 0x9A]);
        try
        {
            Assert.Equal(Dispatch.AxPass, Dispatch.Decide([file]).App);
        }
        finally
        {
            File.Delete(file);
        }
    }

    [Fact]
    public void JsonFileGoesToAxJSONAndSoDoesTheVerb()
    {
        var file = Path.Combine(Folder, $"axit-dispatch-{Guid.NewGuid():N}.JSON");
        File.WriteAllText(file, "{}");
        try
        {
            var launch = Dispatch.Decide([file]);
            Assert.Equal(Dispatch.AxJSON, launch.App);
            Assert.Equal([file], launch.Arguments);
        }
        finally
        {
            File.Delete(file);
        }

        Assert.Equal(Dispatch.AxJSON, Dispatch.Decide(["json", "x.json"]).App);
        Assert.Equal(["x.json"], Dispatch.Decide(["JSON", "x.json"]).Arguments);
        Assert.Contains("Axit json", Dispatch.Usage);
        Assert.Equal(["axClaude", "axTerminal", "axGIT", "axFile", "axDown", "axJSON", "axPreview", "axMedia", "axTask", "axPass", "axStart", "axClip", "axCalc", "axProcess"], Dispatch.Apps.Select(a => a.Name));
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
        foreach (var app in Dispatch.Apps)
        {
            Assert.Contains($"Axit {app.Verb}", Dispatch.Usage);
            Assert.Contains(app.Name, Dispatch.Usage);
        }

        Assert.Contains("--version", Dispatch.Usage);
    }

    /// <summary>Every app's verb starts it, with the rest of the arguments (AX-1.1); the apps are the one list, so a new one needs no new branch.</summary>
    [Fact]
    public void EveryVerbStartsItsApp()
    {
        foreach (var app in Dispatch.Apps)
        {
            var launch = Dispatch.Decide([app.Verb, "x", "--y"]);
            Assert.Equal(app.Verb, launch.App);
            Assert.Equal(["x", "--y"], launch.Arguments);
            Assert.Null(launch.Error);
        }
    }

    /// <summary>AX-1.4: <c>Axit apps</c> is the bundle's, never an app, and lists every app for the installer.</summary>
    [Fact]
    public void The_apps_verb_lists_every_app_as_json()
    {
        Assert.Equal(Dispatch.Listing, Dispatch.Decide(["apps"]).App);
        Assert.Equal(Dispatch.Listing, Dispatch.Decide(["APPS"]).App);
        Assert.DoesNotContain(Dispatch.Apps, app => app.Verb == Dispatch.Listing);
        Assert.Contains("Axit apps", Dispatch.Usage);

        var listing = System.Text.Json.Nodes.JsonNode.Parse(Dispatch.ListingJson())!.AsArray();
        Assert.Equal(Dispatch.Apps.Select(app => app.Verb), listing.Select(node => (string)node!["verb"]!));
        var git = listing.Single(node => (string)node!["verb"]! == Dispatch.AxGit)!;
        Assert.Equal(["folders"], git["explorer"]!.AsArray().Select(node => (string)node!));
        var media = listing.Single(node => (string)node!["verb"]! == Dispatch.AxMedia)!;
        Assert.Equal(["files"], media["explorer"]!.AsArray().Select(node => (string)node!));
        Assert.Equal(MediaFiles.Extensions.Order(), media["openWith"]!.AsArray().Select(node => (string)node!).Order());
        var calc = listing.Single(node => (string)node!["verb"]! == Dispatch.AxCalc)!;
        Assert.Empty(calc["explorer"]!.AsArray());
    }

    /// <summary>AX-2.2, AX-2.3: every app has what the installer and the icon script take from it, and no two icons look alike.</summary>
    [Fact]
    public void Every_app_has_its_description_and_icon()
    {
        foreach (var app in Dispatch.Apps)
        {
            Assert.StartsWith(app.Name + ", ", app.Description);
            Assert.Equal(2, app.Icon.Letters.Length);
            Assert.Equal(app.OpenWith.Count > 0, app.TypeName.Length > 0);
        }

        Assert.Equal(Dispatch.Apps.Count, Dispatch.Apps.Select(app => app.Icon.Letters).Distinct().Count());
    }

    /// <summary>AX-9.2: the apps of a group stand together, in the groups' order, so that the menus' separators fall between groups; a new app is placed by its group.</summary>
    [Fact]
    public void The_apps_stand_in_their_groups_in_order()
    {
        var groups = Dispatch.Apps.Select(app => app.Group).ToList();
        Assert.Equal(groups.OrderBy(group => group).ToList(), groups);
        Assert.Equal(Enum.GetValues<AppGroup>(), groups.Distinct());
    }

    /// <summary>AX-9.10, AF-4.9: what an app is started on from a file or a folder in hand.</summary>
    [Fact]
    public void An_app_is_started_on_the_kind_of_path_it_opens()
    {
        var file = Path.Combine("C:\\work", "notes.md");
        Assert.Equal(file, Dispatch.PathFor(Dispatch.Of(Dispatch.AxDown), file, isFolder: false));
        Assert.Null(Dispatch.PathFor(Dispatch.Of(Dispatch.AxDown), "C:\\work", isFolder: true));
        Assert.Equal("C:\\work", Dispatch.PathFor(Dispatch.Of(Dispatch.AxClaude), file, isFolder: false));
        Assert.Equal("C:\\work", Dispatch.PathFor(Dispatch.Of(Dispatch.AxTerminal), "C:\\work", isFolder: true));
        Assert.Equal(file, Dispatch.PathFor(Dispatch.Of(Dispatch.AxFile), file, isFolder: false));
        Assert.Equal(file, Dispatch.PathFor(Dispatch.Of(Dispatch.AxGit), file, isFolder: false));
        Assert.Null(Dispatch.PathFor(Dispatch.Of(Dispatch.AxCalc), file, isFolder: false));
    }

    /// <summary>AX-9.10: the Show in submenus offer the apps that work in a folder, and only apps that open one.</summary>
    [Fact]
    public void Show_in_offers_the_folder_apps()
    {
        Assert.Equal(["axClaude", "axTerminal", "axGIT", "axFile"], Dispatch.Apps.Where(app => app.ShowIn).Select(app => app.Name));
        Assert.All(Dispatch.Apps.Where(app => app.ShowIn), app => Assert.NotEqual(Opens.Files, app.Opens));
    }
}