# axDown manual tests

The steps and every expected announcement are in `docs/axdown/nvda-test-plan.md`. Every file of it is in this one folder. Run `.\manual_tests\prepare.ps1 axdown` first, and again after saves, to have the files as new.

Start with `.\run-axdown.ps1 manual_tests\axdown\long.md`.

## The files

- `plain.txt`: UTF-8 without a mark, CRLF. Section 3, step 3: Ctrl+S without a change leaves it byte for byte.
- `bom.md`: UTF-8 with a mark, LF endings. Section 3, step 1.
- `wide.txt`: UTF-16 little endian. Section 3, step 2.
- `ansi.txt`: Windows-1252 with the accented letter in "café". Section 3, step 2.
- `long.md`: the navigation file, sections 2 and 8. Headings of levels 1 to 6, a line longer than the window for word wrap, paragraphs of one and two lines, links (a Markdown link, a bare address, an angle address), an image that is not a link, a bulleted list with a nested item followed by a numbered list, a block quote of two lines, a table of 3 rows and 2 columns, and a fenced code block holding a line that looks like a heading and a line that looks like a link, which the keys skip.
- `change-me-on-disk.txt` and `append-a-line-to-change-me-on-disk.txt.cmd`: section 3, step 7. Open the text file, run the cmd file from axFile or File Explorer, Alt+Tab back.
- `not-text-image.png`: not text; section 3, step 9.
- `notes.md`: the file the Axit menu steps open, close and find in Recently closed (section 6, step 4).

## Not in a file

- A missing file: `axdown manual_tests\axdown\missing.md` opens an empty document of that name.
- Install, updates and uninstall (sections 1 and 7) need `publish.ps1`, not these files.
