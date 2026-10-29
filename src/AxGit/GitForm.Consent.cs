using AxGit.Core;
using Axit.Core;

namespace AxGit;

/// <summary>
/// Bundle AX-11.5: Git's consent before the first repository opens, and each server's before the first fetch, pull or
/// push that reaches it (the remote's host; a remote on this computer or a share asks nothing).
/// </summary>
public sealed partial class GitForm
{
    /// <summary>The servers this window has said the sign-in line for (AX-11.6).</summary>
    private readonly HashSet<string> _reminded = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Runs the remote command once the server it reaches has the user's consent; asks first otherwise.</summary>
    private async Task ToServerAsync(Func<Task> command)
    {
        if (_root is null || _gitMissing)
        {
            await command();
            return;
        }

        var remote = GitRemote.NameOf(_repo.Upstream);
        var url = await GitRunner.RunAsync(_root, GitRemote.UrlArguments(remote), GitRunner.Short, overrides: _overrides);
        var host = url.Ok ? GitRemote.Host(url.Output) : null;
        if (host is null)
        {
            await command();
            return;
        }

        if (Consent.Given(AppPaths.ConsentFile, AppName, host))
        {
            // AX-11.6: git may ask for a sign-in to the server (its credential helper's own window); whose account that
            // is, said the first time this window reaches each server, not on every command.
            if (_reminded.Add(host))
            {
                Announce($"Connecting to {host}; if Git asks you to sign in, the account is between you and {host}", true);
            }

            await command();
            return;
        }

        WithConsent(ConsentTarget.Server(host), () => Do(command), () => Announce($"Not connected to {host}", true));
    }
}
