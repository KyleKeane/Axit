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
3. A line in `CHANGELOG.md` under the coming version, starting with the app: `AxFile: …`, `Axit: …` for the bundle.
4. The F1 text (`HelpText`), the user guide and the test plan saying the same as the specification.

Small commits with imperative subject lines; `main` is always releasable, and larger work runs on a branch until its NVDA checks pass.

## Releases

Releases are made by the maintainer with `release.ps1` (README, Release). The version is one number for the whole bundle.

The practices that keep the repository stable over the years, and the yearly check, are `docs/axit/longevity.md`.
