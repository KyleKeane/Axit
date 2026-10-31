# Governance

How Axit is run, who decides, and what its users can count on.

## The project

Axit is the set of tools its maker uses every day as a blind scientist, shared in case they help others; it is not a
product or a service, and nobody is a customer. It is free and open-source software under the GNU Affero General
Public License, version 3 only (`LICENSE`), with
the additional terms in `NOTICE.md`, made for people who use a screen reader (versions up to 3.7.6 were released under
the MIT licence). It has no company, no paid edition, no data collection (`PRIVACY.md`) and no dependency on any
service of the developer's. Anyone may use, study, change and pass it on; a changed version that is shared or offered
over a network gives its source under the same licence and carries a name of its own (`NOTICE.md`). The official
copies are the releases on this repository's releases page. Contributions come under the contributor licence in
`CONTRIBUTING.md`, which lets the copyright holder offer the project under other terms as well.

## People and roles

| Person | Roles |
|---|---|
| Dr. Kyle Keane (www.kylekeane.com), GitHub `KyleKeane` | Maintainer: author, reviewer and release approver |

- **Authors** may commit changes to `main` without another review. Today only the maintainer.
- **Reviewers** review every change from anyone else, a pull request, before it is merged. Today the maintainer.
- **Approvers** approve every release before it is published and, once code signing is in place, every signing
  request (`docs/axit/code-signing.md`). Today the maintainer.

A new role holder is added here, in a commit of its own, before they act in that role. Every role holder signs in to
GitHub, and to the signing service once there is one, with two-factor authentication.

## How decisions are made

- The specifications (`docs/<app>/SPEC.md`, `docs/axit/SPEC.md`) are the source of truth; a change of behaviour changes
  its specification in the same commit, so that every decision is recorded with its reason.
- Accessibility comes first: a change is judged by how it works with NVDA, and checked with it before it is released
  (`docs/axit/nvda-test-record.md`).
- Ideas and bugs come through the issue forms; a larger change is agreed in an issue before the code.

## How releases are made

Every release is built by GitHub Actions from a tag of this repository (`.github/workflows/release.yml`), with GitHub's
attestation of where it was built, and approved by the maintainer before it is published: `release.ps1` runs the
tests, waits for the build, checks the attestation, signs the zip with the release key that never leaves the
maintainer's computer, and only then publishes it. The installed apps install a release only when that signature
checks (`docs/axit/SPEC.md` AX-3.4). Nothing in a release is built anywhere else.

## The maintainer's commitments

- Fix security problems first: a private report (`SECURITY.md`) gets an answer and, when it is real, a release with
  the fix and a published advisory.
- Keep `PRIVACY.md` true: Axit sends nothing anywhere on its own but the update check, which can be turned off; any new
  network contact is added there before it ships.
- Never add telemetry, advertising, tracking, a paid tier, or a dependency on a service of the maintainer's.
- Keep the repository buildable for years with nothing but the .NET SDK (`docs/axit/longevity.md`), so that anyone can
  build exactly what is released.
- If the maintainer can no longer maintain Axit, say so at the top of `README.md` and archive the repository rather
  than leave it silently unmaintained, so that users know to rely on a fork.

## Code of conduct

`CODE_OF_CONDUCT.md`.
