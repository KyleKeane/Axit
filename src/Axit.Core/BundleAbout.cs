using Axit.Core.Updates;

namespace Axit.Core;

/// <summary>
/// The About submenu of the Axit menu (docs/axit/SPEC.md AX-9.9), the same in every window: Version (with the
/// window's latest-release state), Licence, Attribution and the Axit guide (docs/axit/README.md, embedded by
/// Axit.Core.csproj), each the text of a notice; Axit on GitHub is <see cref="UpdateCheck.RepositoryPage"/>. The
/// Licence notice is NOTICE.md, the terms and the disclaimer; the LICENSE file beside it is the AGPL itself.
/// </summary>
public static class BundleAbout
{
    public static string VersionTitle => $"Axit {BundleInfo.Version}";

    /// <param name="latestRelease">The window's latest-release state: "Axit 2.9.0 (this version)", "not checked yet", "checking…".</param>
    public static string Version(string latestRelease) =>
        $"Axit {BundleInfo.Version}.\n" +
        $"Standalone apps, which need only Windows: {string.Join(", ", Dispatch.Apps.Where(app => !app.IsAccessLayer).Select(app => app.Name))}.\n" +
        $"Access layers, which only show and pass on the work of a program you install, under its maker's terms: {string.Join(", ", Dispatch.Apps.Where(app => app.IsAccessLayer).Select(app => $"{app.Name} for {app.AccessLayerFor}"))}. About, Licence says who is responsible for what; About, Consents what you have agreed to.\n" +
        "The Axit guide below says what each app is.\n\n" +
        $"Latest release on GitHub: {latestRelease}\nReleases: {UpdateCheck.ReleasesPage}\n\n" +
        $"Settings: {Path.GetDirectoryName(AppPaths.RecentlyClosedFile)}, one file per app ({string.Join(", ", Dispatch.Apps.Select(app => app.Name.ToLowerInvariant() + ".json"))})\nLogs: {AppPaths.LogsFolder}";

    public const string LicenceTitle = "Licence";

    /// <summary>NOTICE.md, embedded by Axit.Core.csproj (2026-10-29): the AGPL notice, the additional terms of the licence's section 7 and the disclaimer in full words, the same page as the repository's and the zip's; LICENSE is the licence itself.</summary>
    public static string Licence() => EmbeddedText.Read(typeof(BundleAbout).Assembly, "Notice.md", "NOTICE.md");

    public const string AttributionTitle = "Attribution";

    public const string Attribution = """
        Axit is made by Dr. Kyle Keane, www.kylekeane.com.
        Everything you type in axClaude goes to Claude Code and, through it, to Anthropic under Anthropic's own terms; the developer has no access to it. Axit is an independent project, not affiliated with or endorsed by Anthropic. Claude and Claude Code are products of Anthropic.
        Axit is built with .NET and Windows Forms and uses no third-party packages; About, Third-party notices names what the program carries from others, and About, Privacy everything Axit contacts.
        """;

    public const string PrivacyTitle = "Privacy";

    /// <summary>PRIVACY.md, embedded by Axit.Core.csproj (2026-10-25): what Axit sends anywhere and when, the same page as the repository's and the zip's.</summary>
    public static string Privacy() => EmbeddedText.Read(typeof(BundleAbout).Assembly, "Privacy.md", "PRIVACY.md");

    public const string NoticesTitle = "Third-party notices";

    /// <summary>THIRD-PARTY-NOTICES.md, embedded by Axit.Core.csproj (2026-10-25): the .NET runtime and Windows Forms inside the program, and their licence.</summary>
    public static string Notices() => EmbeddedText.Read(typeof(BundleAbout).Assembly, "ThirdPartyNotices.md", "THIRD-PARTY-NOTICES.md");

    public const string GuideTitle = "Axit guide";

    /// <summary>docs/axit/README.md, the guide shipped in the zip: install, start, update, remove.</summary>
    public static string Guide() => EmbeddedText.Read(typeof(BundleAbout).Assembly, "AxitGuide.md", "docs/axit/README.md");
}
