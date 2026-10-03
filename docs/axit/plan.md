# How AxClaude became Axit, and what is still to come

The record of the transition of 2026-10-02, kept short: the shape that was chosen, the decisions, what was built,
and the list that remains. The specifications describe the result (`docs/axit/SPEC.md`, `docs/axclaude/SPEC.md`,
`docs/axdown/SPEC.md`); the commit history holds the steps.

## The shape

One executable, `Axit.exe`, with a verb per app (`claude`, `down`); a path alone picks the app. Each app is a
WinForms class library with one entry point, with its pure code in a `.Core` project and its documents in
`docs/<app>/`. Shared code lives in `Axit.Core` (pure) and `Axit.Forms` (WinForms) and moves there on its second
use, unchanged. One installer, one updater, one version, one changelog, one release workflow. A new app follows
`docs/axit/adding-an-app.md`.

## Decisions taken

1. The GitHub repository was renamed `KyleKeane/Axit` with 2.0.0; the old address redirects.
2. `axit` with no argument starts AxClaude on the current folder.
3. AxClaude 1.7.0 was released first as a bridge: its updater accepts a download whose program file is not
   `AxClaude.exe`, so installed copies could update into Axit.
4. AxDown registers under Open with for `.md`, `.markdown` and `.txt` and appears on every file's right-click menu;
   Windows decides defaults.
5. AxDown's navigation keys are NVDA's browse-mode letters with Ctrl (next) and Ctrl+Shift (previous), headings
   first (AxDown SPEC AD-D7).
6. The program file is `Axit.exe`; the Start menu entries are `Axit AxClaude` and `Axit AxDown`.
7. Keys live in tables, bundle-wide (`BundleKeys`) and per window (`AxClaudeKeys`, `AxDownKeys`), with a collision
   test (bundle SPEC AX-8); the notices and the question area are shared, not copied (AX-6.4).
8. Plain code only (B-10), zero third-party dependencies, and the working app never broken: the transition ran on
   a branch, one step per commit, every commit building and passing the tests, merged after NVDA checks.

## What was built, in order (all on 2026-10-02)

The bundle spec and the document split; the 1.7.0 bridge; `Axit.exe` with AxClaude inside and the tested dispatcher;
the shared pure code in `Axit.Core`; the installer, publish and release scripts for the bundle; the AxDown
specification; `AxDown.Core`; the AxDown window and the shared window pieces in `Axit.Forms`; AxDown's installer
entries and icon; Kyle's NVDA checks and the 2.0.0 release; AxClaude's key table (2.1.0); heading navigation in
AxDown (2.1.0); the adding-an-app checklist. After 2.1.0: AxDown's window spoken on opening (2.1.1), the update notice
with its four buttons and the progress bar (2.2.0), the in-place install with one bar for download and install and
the restart by the app itself (2.3.0).

## Still to come

- The rest of NVDA's letters in AxDown: 1 to 6 for heading levels, K links, L lists, I list items, Q block quotes,
  T tables (AxDown SPEC §9, AD-D7).
- Two AxClaude bugs seen in the question interface, in `docs/axclaude/SPEC.md` §12 To do: a two-question
  AskUserQuestion that kept refiring in the control area, and an own-words (Other) answer that reached Claude with
  most characters dropped. Both need a raw recording first.
- The NVDA episode of silent notifications, also in §12 To do: to catch with NVDA's log at Input/output level if it
  returns.
- A shared window base in `Axit.Forms` for the notice and update flows, which exist twice (`MainForm`,
  `EditorForm`), when a third window arrives.
