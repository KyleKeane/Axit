# Code signing for Windows (prepared 2026-10-25, not set up yet)

Why: Windows 11's Smart App Control blocks a program that carries no Authenticode signature and that Microsoft's cloud
does not know yet. Every Axit release is a new program file, so on a computer where Smart App Control is on, an update
can be refused ("An Application Control policy has blocked this file"; 3.7.1 and 3.7.4 on the maintainer's second
computer). Axit's own release key (`tools/release-key.ps1`, AX-3.4) signs the zip for the apps' updater; Windows does
not know that key and never checks it. Since 3.7.5 a blocked update stops before it changes anything (AX-3.6). The
lasting fix is an Authenticode signature on `Axit.exe`, which Smart App Control accepts.

The plan: **SignPath Foundation**, which signs open-source projects for free with its own certificate, from the
project's GitHub build. The paid alternative is Microsoft's cloud signing service (about $10 a month after an identity
check); the workflow step would differ, the rest of this plan would not.

## 1. Does Axit qualify (SignPath Foundation's terms, read 2026-10-25)

| Condition | Axit |
|---|---|
| An OSI-approved licence, no commercial dual licensing | MIT (`LICENSE`) |
| Only open-source components, no proprietary code of the maintainer's | Yes: no packages; the .NET runtime inside the single file is Microsoft's, already signed by Microsoft |
| Actively maintained, already released in the form to be signed | Yes: releases since 1.0.0, the zip on GitHub |
| Functionality described on the download page | The release page and `README.md` |
| Own project: the signing team develops it and owns the repository | Yes (the maintainer) |
| No hacking tools, nothing that circumvents security measures | Yes. Axit installs per user, never elevated; the AI tools are off until the user turns them on |
| Privacy: data sent elsewhere needs a privacy policy shown at install, and a way to turn it off | Done 2026-10-25: `PRIVACY.md` (every network contact and outside program, per app), in the zip, in every window (About, Privacy), summarised in the README, and said by the installer before it changes anything; the update check has its setting |
| System changes come with clear warnings; uninstall is provided | Done: the installer's first lines say every place it writes before it writes them; Settings, Apps has Uninstall (AX-2.5) |
| Multi-factor sign-in for every team member on SignPath and GitHub | The user turns it on for GitHub and for SignPath (`GOVERNANCE.md` commits to it) |
| Roles: authors, reviewers, approvers | Done: `GOVERNANCE.md` and the README's code signing policy (the maintainer, all three) |
| A "Code signing policy" on the home page and the release pages | Done: the README's section, linked from every release page's notes (the release workflow); its one sentence about SignPath changes once they accept |
| Product name and version in the file's metadata, the same on every build | `src/Axit/Axit.csproj`: `<Product>Axit</Product>`, `<Version>` set by `release.ps1` |
| Built from source by an automated build; each release approved by hand | The release workflow builds the tag; the maintainer approves each signing in SignPath's web page |

Nothing found that disqualifies Axit; SignPath decides on the application.

## 2. What the user does (once)

1. Turn on two-factor sign-in on GitHub, if not on yet (the audit's open item S-36).
2. Apply at SignPath Foundation (signpath.org, Apply) for the project `KyleKeane/Axit`, an MIT-licensed bundle of
   accessible Windows apps built by GitHub Actions. Their review takes days to weeks.
3. Once accepted: in SignPath, add the predefined GitHub.com trusted build system to the organisation and link it to
   the project; make a signing policy (for example `release-signing`) with the maintainer as approver; make an
   artifact configuration that signs `Axit.exe` with the metadata restrictions Product "Axit" and the version.
4. In SignPath, make an API token for a user with submitter rights; in the GitHub repository's settings, add it as the
   secret `SIGNPATH_API_TOKEN`, and the organisation id as the variable `SIGNPATH_ORGANIZATION_ID`.
5. Tell Claude in this repository "SignPath is set up", and it does section 4.

## 3. The code signing policy (in `README.md` since 2026-10-25; the first paragraph's wording once accepted)

> **Code signing policy.** Free code signing provided by SignPath.io, certificate by SignPath Foundation.
> Team: Dr. Kyle Keane (www.kylekeane.com): author, reviewer and approver. Every release is built from this repository
> by GitHub Actions and approved by hand before it is signed.
>
> **Privacy.** Axit asks GitHub for the latest release at start, to offer updates; this is a setting (Options, General,
> Check for updates at start) and can be turned off. axClaude runs Claude Code, the user's own program, which talks to
> Anthropic as Claude Code does. Nothing else leaves the computer: Axit sends no other data to any networked system
> unless the user asks it to.

## 4. What changes in the repository (Claude does this after section 2)

- `.github/workflows/release.yml`, in the build job after `publish.ps1 -NoInstall`, before the zip is used:
  1. upload `publish/win-x64/Axit.exe` alone as a workflow artifact (`actions/upload-artifact`, pinned to a commit
     hash as the others are);
  2. `signpath/github-action-submit-signing-request` (pinned to a commit hash of its v3), with `api-token`,
     `organization-id`, `project-slug: Axit`, `signing-policy-slug: release-signing`, `github-artifact-id` from the
     upload, `wait-for-completion: true` and `output-artifact-directory`; the job waits for the maintainer's approval
     in SignPath;
  3. the signed `Axit.exe` put back into `publish/win-x64`, checked with `Get-AuthenticodeSignature` (status Valid,
     signer SignPath Foundation), and the zip and `apps.json` made again from it (`publish.ps1` gains a `-ZipOnly`
     switch for this, or the zip step moves after the signing);
  4. the job's token stays read-only (S-30); the SignPath step needs only its own API token.
- `release.ps1` and `tools/sign-release.ps1`: unchanged in order: the draft is made after the Authenticode signing,
  and the release key still signs the zip, as the apps' updater checks (AX-3.4). The release waits for the
  maintainer's approval in SignPath before the draft exists, so `release.ps1`'s wait for the workflow covers it.
- `docs/axit/SPEC.md` AX-3.4 says the program is Authenticode-signed by SignPath Foundation; the audit's open item
  closes; `README.md`, `SECURITY.md` and the release notes gain section 3.
- A test in `tests/Axit.Tests` that `release.yml` signs before it zips (reading the workflow, as `LayoutTests` reads
  the sources), so that a later edit cannot zip an unsigned program.
- The first signed release is checked on the second computer: Smart App Control on, Update and restart, no block.

## 5. Until then

- Users whose Smart App Control is off or in evaluation update as before.
- Users whose Smart App Control is on may be refused an update; since 3.7.5 the update stops cleanly with the reason,
  and they stay on their version, or turn Smart App Control off (Windows Security, App and browser control), which
  Windows cannot turn on again without a reset.
- After a release, `Axit.exe` can be submitted to Microsoft's file submission page as a developer's file wrongly
  blocked, which can build its reputation sooner; per release, and not certain.
