using Axit.Core.Updates;

namespace Axit.Core;

/// <summary>
/// The About submenu of the Axit menu (docs/axit/SPEC.md AX-9.9), the same in every window: Version (with the
/// window's latest-release state), Licence, Attribution and the Axit guide (docs/axit/README.md, embedded by
/// Axit.Core.csproj), each the text of a notice; Axit on GitHub is <see cref="UpdateCheck.RepositoryPage"/>. The
/// LICENSE file is the binding text; the licence notice repeats it in full words.
/// </summary>
public static class BundleAbout
{
    public static string VersionTitle => $"Axit {BundleInfo.Version}";

    /// <param name="latestRelease">The window's latest-release state: "Axit 2.9.0 (this version)", "not checked yet", "checking…".</param>
    public static string Version(string latestRelease) =>
        $"Axit {BundleInfo.Version}: {string.Join(", ", Dispatch.Apps.Select(app => app.Name))}. The Axit guide below says what each app is.\n\n" +
        $"Latest release on GitHub: {latestRelease}\nReleases: {UpdateCheck.ReleasesPage}\n\n" +
        $"Settings: {Path.GetDirectoryName(AppPaths.RecentlyClosedFile)}, one file per app ({string.Join(", ", Dispatch.Apps.Select(app => app.Name.ToLowerInvariant() + ".json"))})\nLogs: {AppPaths.LogsFolder}";

    public const string LicenceTitle = "Licence";

    public const string Licence = """
        Axit is free software under the MIT licence: use it, change it and pass it on, as long as the note that says who made it stays with it. The LICENSE file next to the program is the binding text; in full words:
        Axit is provided "as is" and "as available", without warranty of any kind, express or implied, including but not limited to the implied warranties of merchantability, fitness for a particular purpose, title and non-infringement. The developer has no obligation to provide support, maintenance, updates or corrections. To the fullest extent permitted by law, the developer is not liable for any claim, damages or other liability, whether in contract, tort or otherwise, arising from or in connection with the software or its use, including loss of data or work and anything done, said or charged by Claude Code or the services behind it. You use Axit at your own risk.
        Claude Code can create, change and delete files and run commands in your project folder: what you allow it to do is your responsibility.
        """;

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
