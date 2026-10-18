# AxJSON — Specification (placeholder)

AxJSON would be the third app of the Axit bundle (`docs/axit/SPEC.md`): an editor for JSON files for a blind developer
who uses the NVDA screen reader. **Status on 2026-10-18: a placeholder.** No code exists and nothing below is built;
AxDown's Markdown navigation (`docs/axdown/SPEC.md` §9) is built, awaiting NVDA. The document records the shape that was agreed
so that the work can start from it, following `docs/axit/adding-an-app.md`. Requirements are numbered `AJ-n.m` and
decisions `AJ-Dn`; the bundle's rules, AX-7 (accessibility) and AX-8 (keys), are cited by number and not repeated.
In AxJSON's code and documents, "SPEC.md" means this file.

## 1. Summary

A blind developer edits JSON all day: Claude Code's `settings.json` and `.mcp.json`, VS Code's settings, `package.json`,
and the settings files of this bundle. AxJSON opens one JSON file in one window and offers it in two ways. **Raw mode**
is AxDown's plain edit control with the whole file in it, plus keys that jump by the file's structure (the next key,
value, object, array, the first error) and a command that formats the text, repairing what it safely can. **Structured
mode** shows the same document as a form of ordinary Windows input fields, one per value: a text field for a string or
a number, a check box for a boolean, a list for an array, a group for an object, laid out from a template when the file
is one the app knows and from the document's own keys otherwise. Native fields mean NVDA reads them by itself, and the
caret cannot leave a value: Backspace held down empties the field and nothing else.

## 2. Goals and non-goals

- **G1 Two views, one document.** Raw mode and structured mode edit the same text; switching re-parses or re-renders,
  and the file on disk changes only on Save. Nothing is lost in a switch: text that does not parse stays in raw mode
  with the first error named.
- **G2 Native controls only.** The raw editor is the EDIT control AxDown uses; the form is made of EDIT, check box,
  list box and group controls (AX-7.1). No tree control of the app's own, no owner-drawn grid.
- **G3 Templated forms, not a schema engine.** A template is a plain record in code: the keys a known file has, each
  with a label, a kind (text, number, boolean, choice, list, object) and a line of help. Unknown files get a form built
  from their keys. JSON Schema is a non-goal for version one.
- **G4 Faithful files.** As AxDown (AD G2): encoding, byte order mark and line endings come back as they were. Formatting
  changes the text only when the user asks for it.
- **G5 Shared behaviour.** Notices, questions, paging, paragraph keys, text size, settings and the update check are the
  bundle's (AX-6); the editor's file handling, find and go-to-line are AxDown's, moved to the shared projects on this
  second use (AX-5.3 M3), not copied.
- **Non-goals.** Several files at once; comments and trailing commas as a dialect to preserve (they are repaired or
  reported); schema validation; colouring; a tree view; editing anything but JSON.

## 3. Scenarios

1. Open `.claude/settings.json`, press the next-key shortcut until "permissions" is heard, jump into its value, add an
   entry, format, save.
2. Open an `.mcp.json` with a missing comma: AxJSON says "Not valid JSON: line 12, expected a comma". Format repairs
   it and says what it changed; the user saves.
3. Switch a known settings file to structured mode: a form with a labelled field per setting, Tab from field to field,
   each read by NVDA as "Model, edit, claude-opus-5-5". Change one, switch back, see the raw text updated, save.
4. Open an unknown JSON file in structured mode: a form from its keys, nested objects as groups, arrays as lists with
   add and remove.

## 4. Functional requirements (sketch)

### AJ-1 Launch

- AJ-1.1 `Axit.exe json [<file>]`; the console command `axjson`; the Start menu entry "Axit AxJSON"; "Open in AxJSON"
  for `.json` in File Explorer, under Open with (AD-D9's pattern: never the default).
- AJ-1.2 One window, one document, as AxDown (AD-1.3).

### AJ-2 Raw mode

- AJ-2.1 The editor, file handling, find, go to line, line and column on demand, unsaved-changes question: AxDown's
  (AD-2, AD-3, AD-6), shared rather than copied.
- AJ-2.2 Structure keys, window-specific, NVDA's browse-mode letters with Ctrl for next and Ctrl+Shift for previous
  (AxDown's convention): K key, V value, O object, A array, E error, and a key for the enclosing object or array. Each
  jump announces what it landed on ("permissions, object, 3 keys"; "allow, array, 5 items"; "model: claude-opus-5-5").
- AJ-2.3 Format: re-indents valid JSON (two spaces, one key per line, the file's line endings). On invalid JSON it
  repairs what is unambiguous (a trailing comma, a missing comma between lines, single quotes, an unquoted key, a
  comment line) and reports each repair and the first error it could not repair, with its line. Formatting is one
  undo step.
- AJ-2.4 The status bar shows the path of the caret in the document ("permissions › allow › 3") on demand, through the
  bundle's line-number key pattern (AX-7.3).

### AJ-3 Structured mode

- AJ-3.1 A form in place of the editor: for each key a labelled control named with the key and the template's label,
  reached with Tab and Shift+Tab, grouped by object. Kinds: text (EDIT), number (EDIT, checked on leaving), boolean
  (check box), choice (a list of the template's values), list (a list box with Add, Edit, Remove), object (a group,
  or a button that opens the group in place).
- AJ-3.2 Templates in code for the files the bundle's users have: Claude Code's `settings.json` and `.mcp.json`, Axit's
  own `axclaude.json` and `axdown.json`; a generic form from the keys for everything else. A template is matched by
  file name and by the keys found.
- AJ-3.3 Leaving the form writes the values back into the text at their places, keeping the file's formatting around
  them; a key the template does not know is kept untouched.
- AJ-3.4 The switch between the modes is one window key; the title says which mode shows.

### AJ-4 Keys, notices, announcements, settings, status bar, title, errors

- As AxDown's AD-4 to AD-11 where they apply, with this file's additions listed in the same shape once they exist.

## 5. Architecture (sketch)

- `AxJSON.Core` (no window): the JSON reader giving every key and value its text range and line (`System.Text.Json`'s
  `Utf8JsonReader`, no package); the repair pass; the formatter; the templates; tests for all of it against files
  under `tests/fixtures`.
- `AxJSON` (window): `JsonForm` with the raw editor and the structured form, the keys table `AxJSONKeys`, `HelpText`.
- Shared first: AxDown's `TextDocument` (files), find and go-to-line move to `Axit.Core` and `Axit.Forms` on this
  second use, one piece per commit, before AxJSON uses them.

## 6. Design decisions

- **AJ-D1 Raw mode is AxDown plus structure keys.** The editor NVDA reads best is already in the bundle; AxJSON adds
  jumps and a formatter, nothing else to the editing.
- **AJ-D2 Structured mode is a form of real fields, from templates.** Real controls give field-bounded editing for
  free (the caret cannot leave a value), NVDA reads them natively, and a template is a record, not a schema engine.
  A tree or grid of the app's own was considered and dropped: it would be custom accessibility work for a worse result.
- **AJ-D3 A placeholder until AxDown is done.** AxDown's remaining Markdown letters came first (2026-10-17); AxJSON
  starts next, spec first, following `docs/axit/adding-an-app.md`.
- **AJ-D4 One editor shell, one app per file type, one file per window (2026-10-18).** From the outside the bundle is
  already one editor for every file type: one program file, one installer, one update, and a path alone picks the
  app by its extension (AX-1.2). Inside, AxDown's editor shell (open, save, find, go to line, status bar, notices,
  settings) moves to the shared projects on this second use (G5, AX-6.3), and each file type is one app over it:
  AxDown is the shell plus Markdown, AxJSON the shell plus JSON plus its form, so that one app's folders hold
  everything about its file type. What differs per type is small and is the seam that exists already: the outline
  that finds the elements, the key pairs (`TextJump`), the Navigate menu made from them, and the type's own commands
  (Format). Tabs for several files in one window were considered and dropped (B-14): one file per window, and the
  Axit menu (AX-9) lists the open windows. Structured mode stays JSON's alone (AJ-D2); Markdown's text is its structure.

## 7. Open questions

- The exact letters of AJ-2.2 against AxDown's and NVDA's, checked by the collision test (AX-8.4). The rule: a letter
  keeps its meaning where the concept exists in both types (L list and array, I item in both); a letter with no
  counterpart is the type's own; K, NVDA's link in AxDown, is the one to decide (AJ-D4).
- Whether structured mode is in version one or follows raw mode as a second version (AxDown's pattern).
- How a list of objects (an array of MCP servers) is edited in the form: a list box with Edit opening the object's
  group, or one group per item.

## 8. Status

Placeholder, 2026-10-17. Not in `Axit.sln`, no verb in `Dispatch`, nothing installed or released.
