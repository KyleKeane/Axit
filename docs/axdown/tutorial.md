# AxDown tutorial

Welcome. This file is both a guided tour of AxDown and a practice ground: every kind of Markdown structure has a
section of its own below, so each navigation key has somewhere to land, and the text explains what to try as you
go. `run-axdown.ps1` opens this file when it is given no other; edit it as much as you like, the repository keeps
the original.

Read it the way you read any text: Down Arrow for the next line, Ctrl+Down for the next paragraph in NVDA, NVDA's
say-all to read on. AxDown adds nothing to that reading; it speaks only when you ask it to or when a command
finishes.

## 1. Headings

A heading is a line that starts with one to six hash signs and a space. Ctrl+H moves to the next heading and
Ctrl+Shift+H to the previous one; the caret lands at the start of the heading's line and AxDown says the heading as
NVDA's browse mode would: "Headings, heading level 2". At the last heading Ctrl+H says "No next heading" and the
caret stays. Try it now: press Ctrl+H a few times, then Ctrl+Shift+H back to here.

### 1.1 A third-level heading

Headings nest by their number of hashes. This one has three.

#### 1.1.1 A fourth-level heading

##### A fifth-level heading

###### A sixth-level heading, the deepest Markdown has

### 1.2 Closing hashes

## A heading may end with hashes too ##

Markdown allows closing hashes; AxDown drops them when it speaks the heading, so this one is spoken as "A heading
may end with hashes too, heading level 2".

### 1.3 What is not a heading

A line like #hashtag, with no space after the hash, is not a heading, and neither is a heading-looking line inside
a code fence (see section 7). Ctrl+H passes over both.

## 2. Paragraphs and emphasis

A paragraph is one or more lines of text with a blank line before and after. Markdown joins the lines of a paragraph
when it is rendered, but AxDown is a plain editor and shows the file as it is: each line of the file is one line in
the window. With word wrap on (the default), a long line wraps at the window's edge and Down Arrow reads one screen
line at a time; View, Word wrap (Ctrl+Shift+W) turns that off, and each line of the file is then one line in the
window however long. Your place in the text is kept either way.

Emphasis is written with asterisks or underscores: *italic* with one, **bold** with two, ***both*** with three, and
`inline code` with backticks. AxDown shows the marks as they are; NVDA reads them as punctuation according to its
own punctuation level.

A line that ends with two spaces  
makes a hard line break in rendered Markdown. In AxDown it is simply two lines.

## 3. Lists

### 3.1 Bulleted lists

- A bulleted item starts with a dash, an asterisk or a plus sign, then a space.
- The second item.
  - A nested item is indented by two or more spaces.
  - Another nested item.
- The third item, back at the top level.

### 3.2 Numbered lists

1. A numbered item starts with a number, a full stop and a space.
2. The second step.
3. The third step.
   1. Steps can nest too.
   2. Like this.

### 3.3 Task lists

- [ ] An unticked task: a dash, then square brackets with a space inside.
- [x] A ticked task: an x inside the brackets.
- [ ] Another task.

Lists are the next structure the navigation keys learn: Ctrl+L for the next list and Ctrl+I for the next list item,
the letters NVDA uses on web pages.

## 4. Links and images

An inline link puts the text in square brackets and the address in parentheses: [the Axit repository on
GitHub](https://github.com/KyleKeane/Axit). A bare address stands on its own: https://github.com/KyleKeane/Axit.

A reference link puts a label in a second pair of brackets, [like this][axit], and the address once at the end of
the document or section:

[axit]: https://github.com/KyleKeane/Axit

An image is a link with an exclamation mark in front: ![the Axit icon](AxDown.ico). AxDown shows the text, not the
image. Links are the structure behind Ctrl+K when it arrives.

## 5. Block quotes

> A block quote is a line that starts with a greater-than sign and a space.
> It continues on the next line with another sign.
>
> > Quotes can nest, with two signs.
>
> And come back to one.

Block quotes are the structure behind Ctrl+Q when it arrives.

## 6. Tables

A table is a row of cells separated by vertical bars, a row of dashes that sets the alignment, then the rows:

| Key | What it does | Kind |
| :-- | :-- | :-- |
| Ctrl+H, Ctrl+Shift+H | Next and previous heading | AxDown |
| Ctrl+G | Go to a line; the field says where you are | AxDown |
| Ctrl+F, F3, Shift+F3 | Find, next match, previous match | Bundle |
| Ctrl+S, Ctrl+Shift+S | Save, save as | Bundle, AxDown |
| Ctrl+Shift+W | Word wrap on or off | AxDown |
| Ctrl+Plus, Ctrl+Minus | Larger and smaller text | Bundle |
| F1 | The keyboard shortcuts | Bundle |

NVDA reads a table row as one line of text here, bars and all; a key that moves between tables, Ctrl+T, is still to
come. The Kind column says where each key is defined: bundle-wide keys are the same in every window of Axit,
AxDown's own keys are AxDown's alone.

## 7. Code

Inline code sits between backticks, like `Ctrl+G`. A code block is fenced by three backticks or three tildes on
lines of their own, with the language's name after the opening fence if you like:

```csharp
// A heading-looking line inside a fence is code, not a heading:
# Ctrl+H skips this line.
Console.WriteLine("Hello from AxDown");
```

~~~
Tildes work the same way.
~~~

    A block indented by four spaces is code too, in Markdown's older style.

## 8. Horizontal rules and comments

Three or more dashes, asterisks or underscores on a line of their own draw a horizontal rule when the file is
rendered:

---

An HTML comment is hidden when the file is rendered and visible in AxDown:

<!-- This is a comment. AxDown shows it; a renderer would not. -->

## 9. Finding and going to a line

Ctrl+F opens Find inside the window: type a word, for example `tildes`, and press Enter; the caret lands on the
next match after it and NVDA reads that line. F3 finds the next match and Shift+F3 the previous one without opening
Find again. A word that is not in the file says "Not found".

Ctrl+G asks for a line number. The field is named with where you are, for example "Go to line (now line 120,
column 1)", so Ctrl+G is also how you ask where you are; type a number and press Enter, or press Escape to stay. The
status bar carries the line, the column, the file's encoding and its line ending; NVDA+End reads it.

## 10. Files, saving and encodings

Ctrl+S saves and says "Saved". A new document asks for a name first, with the Windows file dialog, and Ctrl+Shift+S
asks every time. Ctrl+O opens another file in this window and Ctrl+N starts an empty one; both ask first about
unsaved changes, as does closing the window: "Save changes to tutorial.md?" with Save, Don't save and Cancel.

AxDown writes a file back exactly the way it found it: the same encoding (UTF-8, UTF-8 with a byte order mark,
UTF-16, or the Windows code page), with or without the mark, and the same line endings (CRLF, LF or CR). The status
bar shows what it found. A new document is UTF-8 without a mark with CRLF. If the file changed on disk while you
had it open, saving asks before writing over it.

The title is the file's name, with a star in front while there are unsaved changes: "*tutorial.md - AxDown". Make a
change now and listen to the title with NVDA+T, then undo it with Ctrl+Z.

## 11. Text size, fonts and updates

Ctrl+Plus and Ctrl+Minus make the text larger and smaller and say the new size; View, Font opens the Windows font
dialog; View, Windows text size goes back to the system's. All of it is remembered in `%APPDATA%\Axit\axdown.json`.

New versions of Axit are published on GitHub, and every app of Axit updates together. When AxDown starts, it asks
GitHub once whether there is a newer one; if there is, a notice opens with Update and restart, Update and close,
Open release page and Later. One progress bar covers the download and the installation, reported by NVDA the way it
reports any progress bar, and after Update and restart AxDown comes back on this very file.

## 12. The end

This is the last heading of the file. Ctrl+H here says "No next heading"; Ctrl+Shift+H takes you back up through
every section. Ctrl+Home returns to the top.
