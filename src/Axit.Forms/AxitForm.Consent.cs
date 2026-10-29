using Axit.Core;
using Axit.Core.Updates;

namespace Axit.Forms;

/// <summary>
/// The consent before an access layer first connects to a program or service (docs/axit/SPEC.md AX-11.4, AX-11.5):
/// a notice in the window with a check box, once per app and program, kept in <see cref="AppPaths.ConsentFile"/>.
/// </summary>
public abstract partial class AxitForm
{
    /// <summary>Axit's own terms on GitHub, for a window run from a build that has no NOTICE.md beside it.</summary>
    private static readonly string NoticePage = UpdateCheck.RepositoryPage + "/blob/main/NOTICE.md";

    /// <summary>
    /// Runs <paramref name="connect"/> at once when the user has agreed to <paramref name="target"/> for this app before;
    /// otherwise asks first: Connect (Enter, once the box is checked) records the agreement and connects, Not now
    /// (Escape) runs <paramref name="notNow"/>, which says how to connect later.
    /// </summary>
    protected void WithConsent(ConsentTarget target, Action connect, Action notNow)
    {
        if (Consent.Given(AppPaths.ConsentFile, AppName, target.Program))
        {
            connect();
            return;
        }

        ShowNotice(ConsentNotice(target, $"Before {AppName} connects to {target.Program}", "&Connect", connect, notNow));
    }

    /// <summary>
    /// The notice of AX-11.4: what the access layer does, what the program does and under whose terms, that Axit's author
    /// answers for none of it, the check box, and the buttons. <paramref name="action"/> names the default button
    /// (Connect, Install), which records the agreement before it runs <paramref name="go"/>.
    /// </summary>
    private Notice ConsentNotice(ConsentTarget target, string title, string action, Action go, Action notNow)
    {
        var app = AppTitle;
        var text =
            $"{AppName} is only an access layer: it shows what {target.Program} does and passes your commands to it. Axit does not include {target.Program}.\n" +
            $"{target.Work}\n" +
            $"{target.Program} works under {target.Terms}, which are between you and its maker{(target.TermsUrl.Length > 0 ? ": " + target.TermsUrl : ".")}\n" +
            $"Axit's author is not a party to those terms, gives no warranty, and is not responsible for what {target.Program} does, keeps, sends or charges. Axit's own terms: Axit menu, About, Licence (NOTICE.md).\n" +
            "Check the box below to agree, then press the first button.";
        var choices = new List<OverlayChoice>
        {
            new(action, () =>
            {
                Consent.Record(AppPaths.ConsentFile, AppName, target.Program, BundleInfo.Version, DateTime.Now);
                go();
            }, IsDefault: true),
        };
        if (target.TermsUrl.Length > 0)
        {
            choices.Add(new OverlayChoice("Open &their terms", () => OpenUrl(target.TermsUrl), StaysOpen: true));
        }

        choices.Add(new OverlayChoice("Open &Axit's terms", OpenAxitTerms, StaysOpen: true));
        choices.Add(new OverlayChoice("&Not now", notNow, IsCancel: true));
        return new Notice(title, text, choices)
        {
            Consent = $"I understand that {app} is only an access layer, and I take full responsibility for knowing and following the terms of {target.Program}",
        };
    }

    /// <summary>NOTICE.md beside the program, in axPreview; on GitHub when this build has none beside it.</summary>
    private void OpenAxitTerms()
    {
        var notice = Path.Combine(AppContext.BaseDirectory, "NOTICE.md");
        if (File.Exists(notice))
        {
            AxitMenu.Start(Dispatch.AxPreview, notice);
            Announce("Axit's terms are opening in axPreview", true);
        }
        else
        {
            OpenUrl(NoticePage);
        }
    }
}
