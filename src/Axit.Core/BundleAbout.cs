using Axit.Core.Updates;

namespace Axit.Core;

/// <summary>
/// The one About notice of the bundle (docs/axit/SPEC.md AX-3.1, AX-9.1), shown from every window's Axit menu with the
/// same text: the version, the apps, the maker, the latest release as the window knows it, the releases page, the
/// folders, and the disclaimer. The LICENSE file is the binding text; the disclaimer repeats it in full words.
/// </summary>
public static class BundleAbout
{
    public const string Title = "About Axit";

    /// <param name="latestRelease">The window's latest-release state: "Axit 2.9.0 (this version)", "not checked yet", "checking…".</param>
    public static string Text(string latestRelease) =>
        $"Axit {BundleInfo.Version}\n" +
        "AxClaude, a screen reader friendly window for Claude Code, and AxDown, a plain editor for Markdown and text files.\n" +
        "Made by Dr. Kyle Keane, www.kylekeane.com. Free under the MIT licence.\n\n" +
        $"Latest release on GitHub: {latestRelease}\nReleases: {UpdateCheck.ReleasesPage}\nSource: {UpdateCheck.RepositoryPage}\n" +
        "Claude Code documentation: https://code.claude.com/docs\n" +
        $"Settings: {Path.GetDirectoryName(AppPaths.RecentlyClosedFile)} (axclaude.json, axdown.json)\nLogs: {AppPaths.LogsFolder}\n\n" +
        Disclaimer;

    public const string Disclaimer = """
        Disclaimer
        Axit is free software offered under the MIT licence. It is provided "as is" and "as available", without warranty of any kind, express or implied, including but not limited to the implied warranties of merchantability, fitness for a particular purpose, title and non-infringement. The developer has no obligation to provide support, maintenance, updates or corrections. To the fullest extent permitted by law, the developer is not liable for any claim, damages or other liability, whether in contract, tort or otherwise, arising from or in connection with the software or its use, including loss of data or work and anything done, said or charged by Claude Code or the services behind it. You use Axit at your own risk.
        Everything you type in AxClaude goes to Claude Code and, through it, to Anthropic under Anthropic's own terms; the developer has no access to it. Claude Code can create, change and delete files and run commands in your project folder: what you allow it to do is your responsibility. Axit is an independent project, not affiliated with or endorsed by Anthropic. Claude and Claude Code are products of Anthropic.
        """;
}
