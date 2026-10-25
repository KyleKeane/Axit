using Axit.Core;
using Axit.Core.Updates;

namespace Axit.Tests;

/// <summary>The About sections of the Axit menu (docs/axit/SPEC.md AX-9.9): the version text names every app, from the one list, so that it never goes stale again (it named four of nine until 2026-10-23).</summary>
public sealed class BundleAboutTests
{
    [Fact]
    public void The_version_text_names_every_app_and_its_settings_file()
    {
        var text = BundleAbout.Version("not checked yet");
        foreach (var app in Dispatch.Apps)
        {
            Assert.Contains(app.Name, text);
            Assert.Contains(app.Name.ToLowerInvariant() + ".json", text);
        }

        Assert.Contains("not checked yet", text);
        Assert.Contains(UpdateCheck.ReleasesPage, text);
    }

    [Fact]
    public void The_licence_and_attribution_name_the_maker_and_the_terms()
    {
        Assert.Contains("MIT", BundleAbout.Licence);
        Assert.Contains("Dr. Kyle Keane", BundleAbout.Attribution);
        Assert.Contains("no third-party packages", BundleAbout.Attribution);
    }

    [Fact]
    public void Privacy_and_the_notices_are_the_shipped_pages()
    {
        // The pages the zip ships, embedded whole (2026-10-25): the update check, its setting, and .NET's licence.
        Assert.StartsWith("# Privacy", BundleAbout.Privacy());
        Assert.Contains("Check for updates at start", BundleAbout.Privacy());
        Assert.Contains("Copyright (c) .NET Foundation and Contributors", BundleAbout.Notices());
    }

    [Fact]
    public void The_privacy_page_names_the_addresses_the_code_contacts()
    {
        // The page follows the code: the update check's address and the installers Install it now runs, from the
        // places that hold them, so that changing one without the page fails here.
        var privacy = BundleAbout.Privacy();
        Assert.Contains(UpdateCheck.LatestReleaseApi, privacy);
        Assert.Contains(ProgramInstall.ClaudeCode.PowerShellCommand!, privacy);
        Assert.Contains("--source winget", privacy);
        foreach (var program in new[] { ProgramInstall.Git, ProgramInstall.KeePassXC, ProgramInstall.PowerShell7, ProgramInstall.ClaudeCode })
        {
            Assert.Contains(program.Name, privacy);
        }
    }
}
