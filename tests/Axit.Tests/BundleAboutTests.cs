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
}
