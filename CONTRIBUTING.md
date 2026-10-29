# Contributing to Axit

Axit is built for people who use a screen reader, and every change is judged by how it sounds under NVDA. These are the rules the repository already follows; `CLAUDE.md` has them in full, with the reasons.

## Before you change anything

- Read the app's specification, `docs/<app>/SPEC.md`, and the bundle's, `docs/axit/SPEC.md`. They are the source of truth: a change of behaviour changes the specification in the same commit.
- Open an issue first for anything larger than a fix, so that the behaviour can be agreed before the code.

## How a change is made

- **One app, one set of folders.** An app never references another app; shared code lives in `Axit.Core` and `Axit.Forms` and moves there on its second use (`LayoutTests` checks the references).
- **Plain code.** .NET 10 and WinForms, no NuGet packages in the apps, no dependency injection, no interfaces with one implementation. A smaller model, or a new contributor, should be able to change one app with only that app's folders in view.
- **Accessibility first** (AX-7): every control has an accessible name, everything works without a mouse, the caret moves only on the user's own command, and the result of a command is one announcement. Questions and errors are notices inside the window, never a second window.
- **Keys** live in one table per window, or in `BundleKeys` when they mean the same everywhere; a test fails on a collision, and another on a key the F1 text does not name.
- **No personal data**: no e-mail address and no path with an account name in it, anywhere in the repository (`PrivacyTests`).

## Before you open a pull request

1. `.\test.ps1`: every test project, in Release, as the Build workflow runs them.
2. The steps of `docs/<app>/nvda-test-plan.md` that the change touches, with NVDA running, on the files `.\manual_tests\prepare.ps1` makes. Write the pass into `docs/axit/nvda-test-record.md`, or list the steps in the pull request as not yet run.
3. A line in `CHANGELOG.md` under the coming version, starting with the app: `axFile: …`, `Axit: …` for the bundle.
4. The F1 text (`HelpText`), the user guide and the test plan saying the same as the specification.

Small commits with imperative subject lines; `main` is always releasable, and larger work runs on a branch until its NVDA checks pass.

## Licence of contributions, review and conduct

- **The contributor licence.** By opening a pull request, or sending a change any other way, you agree to these terms
  for that contribution:
  1. You license it under the project's licence, the GNU Affero General Public License version 3 only (`LICENSE`),
     with the additional terms in `NOTICE.md`.
  2. You also grant Dr. Kyle Keane, the copyright holder, and anyone he passes the project on to, a perpetual,
     worldwide, non-exclusive, no-charge, royalty-free, irrevocable licence to use, copy, modify, publish, distribute,
     sublicense and relicense the contribution, under these or any other terms, including in a product or service
     offered for a fee. You keep your copyright.
  3. You have the right to give it: it is your own work, or work under a licence compatible with the AGPL that is
     named in the pull request and added to `THIRD-PARTY-NOTICES.md`; your employer, if it could claim the work, has
     agreed.
  4. You give it "as is", without warranty or support.
  These terms let the project be offered later in other forms, such as a service, without asking every contributor
  again. A pull request whose author does not agree is not merged.
- No third-party package, no binary and no code whose licence is not open source enters the repository.
- Every change from someone other than the maintainer is reviewed before it is merged; the roles are in
  `GOVERNANCE.md`.
- A change that makes Axit contact anything new over the network, or start a program it did not start before,
  updates `PRIVACY.md` in the same pull request.
- Everyone here follows `CODE_OF_CONDUCT.md`.

## Releases

Releases are made by the maintainer with `release.ps1` (README, Release). The version is one number for the whole bundle.

The practices that keep the repository stable over the years, and the yearly check, are `docs/axit/longevity.md`.
