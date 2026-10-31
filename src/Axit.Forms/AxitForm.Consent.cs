using Axit.Core;

namespace Axit.Forms;

/// <summary>
/// The reminder before an access layer first connects to a program or service (docs/axit/SPEC.md AX-11.4, AX-11.5):
/// a notice in the window with one check box and two buttons, once per app and program, kept in
/// <see cref="AppPaths.ConsentFile"/>.
/// </summary>
public abstract partial class AxitForm
{
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
    /// The notice of AX-11.4, a plain reminder: what the access layer does, whose program does the work and under whose
    /// terms (their address in the text), the check box, and two buttons. <paramref name="action"/> names the default
    /// button (Connect, Install), which records the agreement before it runs <paramref name="go"/>.
    /// </summary>
    private Notice ConsentNotice(ConsentTarget target, string title, string action, Action go, Action notNow)
    {
        var text =
            $"A reminder: {AppName} is only an access layer. It shows what {target.Program} does and passes your commands to it; {target.Program} is not part of Axit.\n" +
            $"{target.Work}\n" +
            $"{target.Program}'s terms are between you and its maker{(target.TermsUrl.Length > 0 ? ": " + target.TermsUrl : ".")}\n" +
            "Axit is a set of personal tools shared as they are, not a product or a service (Axit menu, About, Licence).";
        return new Notice(title, text,
        [
            new OverlayChoice(action, () =>
            {
                Consent.Record(AppPaths.ConsentFile, AppName, target.Program, BundleInfo.Version, DateTime.Now);
                go();
            }, IsDefault: true),
            new OverlayChoice("&Not now", notNow, IsCancel: true),
        ])
        {
            Consent = $"I understand that {AppTitle} is only an access layer, and that following the terms of {target.Program} is up to me",
        };
    }
}
