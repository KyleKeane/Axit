# axPass: the KeePass format and the command-line tool, from their sources

Notes for the axPass specification that is still to be written (`docs/axit/plan.md`, axPass). Read on 2026-10-23
from three pages; each fact below is what a page says, and what the pages leave open is listed as open, not filled in
from memory. Check the pages again before the spec is written: the format is KeePass's, the tool KeePassXC's, and
both change.

- KDBX File Format Specification: https://keepass.info/help/kb/kdbx.html (describes version 4.1, no date shown)
- KDBX 4 (the changes from 3.1): https://keepass.info/help/kb/kdbx_4.html
- KDBX 4.1 (the changes from 4.0): https://keepass.info/help/kb/kdbx_4.1.html
- keepassxc-cli manual: https://github.com/keepassxreboot/keepassxc/blob/develop/docs/man/keepassxc-cli.1.adoc

KeePassium, the phone app the idea came from, has no format of its own: it reads and writes KDBX like every
KeePass client. There is no formal standard; the specification page, KeePass's source and the XML schema
`KDBX_XML.xsd` are the definition.

## 1. Versions

| Version | Since | What it brought |
|---|---|---|
| KDBX 3.1 | before KeePass 2.35 | AES-KDF only, Salsa20 inner stream, SHA-256 hash of the plain data per block |
| KDBX 4.0 | KeePass 2.35 | Argon2, ChaCha20, the header HMAC, HMAC-SHA-256 per encrypted block, attachments in a binary inner header |
| KDBX 4.1 | KeePass 2.48 | group tags, `QualityCheck`, previous parent groups, custom icon names and times, custom data times |

KeePass writes the newest version only when the database uses something of it (an Argon2 KDF or ChaCha20 for 4.0;
a group tag, a disabled quality check, a named icon or a timed custom data item for 4.1), because some ports do not
read 4.1 yet. A client therefore meets all three versions.

## 2. The file, in order

All numbers are little-endian.

1. **Signature**: UInt32 `0x9AA2D903`, then UInt32 `0xB54BFB67`.
2. **Version**: UInt32, the high word the major version, the low word the minor; 4.1 is `0x00040001`.
3. **Outer header**: fields of a 1-byte ID, a 4-byte length (2 bytes in 3.1) and the data:

   | ID | Field | Content |
   |---|---|---|
   | 0 | End of header | `0D 0A 0D 0A`, once, last |
   | 2 | Cipher | UUID: `31C1F2E6BF714350BE5805216AFC5AFF` AES-256-CBC with PKCS #7 padding; `D6038A2B8B6F4CB5A524339A31DBB59A` ChaCha20 (RFC 8439) |
   | 3 | Compression | UInt32: 0 none, 1 GZip |
   | 4 | Master seed | 32 bytes, new on every save |
   | 7 | IV or nonce | 16 bytes for AES, 12 for ChaCha20, new on every save |
   | 11 | KDF parameters | a variant dictionary (below) |
   | 12 | Public custom data | a variant dictionary, unencrypted |

   IDs 1, 5, 6, 8, 9 and 10 belong to earlier versions (5 and 6 were 3.1's AES-KDF seed and rounds).
4. **SHA-256 of the header**: against accidental damage only.
5. **HMAC-SHA-256 of the header**: a wrong password or a changed header is found here, before any decryption.
6. **The payload** as an HMAC block stream: each block is HMAC-SHA-256(*i* ‖ *s* ‖ *C*) (32 bytes), the size *s*
   (Int32), then *s* bytes of ciphertext *C*; *i* is the block's UInt64 index, counted, not stored. A block of size 0
   ends the stream. KeePass writes 1 MiB blocks. Encrypt-then-MAC: the HMAC covers the ciphertext.
7. Decrypted (and decompressed if field 3 says so), the payload is the **inner header** followed by the **XML**.

### Variant dictionary

A UInt16 version (`0x0100` now; a higher high byte means refuse the file), then items, then a zero byte. An item is
a 1-byte type, an Int32 key length, the key (UTF-8), an Int32 value length, the value. Types: `0x04` UInt32, `0x05`
UInt64, `0x08` Boolean, `0x0C` Int32, `0x0D` Int64, `0x18` String (UTF-8), `0x42` bytes.

### Key derivation functions

The dictionary's `$UUID` names the KDF:

| KDF | UUID | Parameters |
|---|---|---|
| AES-KDF | `C9D9F39A628A4460BF740D08C18A4FEA` | `S` salt (32 bytes), `R` rounds (UInt64) |
| Argon2d | `EF636DDF8C29444B91F7A9A403E30A0C` | `V` version (UInt32, `0x10` or `0x13`, `0x13` recommended), `S` salt (8 to `0x3FFFFFFF` bytes, 32 recommended), `I` iterations (UInt64), `M` memory in bytes (UInt64), `P` parallelism (UInt32) |
| Argon2id | `9E298B1956DB4773B23DFC3EC6F0A1E6` | the same as Argon2d |

## 3. The keys

- **Composite key** *R*: SHA-256 over these parts in order, each one optional: SHA-256 of the master password
  (UTF-8); the key file's key; a key provider plugin's key; the Windows user account key (DPAPI).
- **Transformed key** *T*: *R* through the KDF.
- **Encryption key**: SHA-256(*S* ‖ *T*), *S* the master seed (its first bytes when the cipher wants fewer).
- **HMAC base key** *K*: SHA-512(*S* ‖ *T* ‖ `0x01`).
- **Block *i*'s HMAC key**: SHA-512(*i* ‖ *K*), *i* as UInt64.
- **Header HMAC key**: the same with *i* = `0xFFFFFFFFFFFFFFFF`.

## 4. Inside the payload

**Inner header** fields: a 1-byte type, an Int32 length, the data.

| Type | Field | Content |
|---|---|---|
| 0 | End | empty, last |
| 1 | Inner cipher | Int32: 2 Salsa20, 3 ChaCha20 (the default) |
| 2 | Inner key | 32 bytes for Salsa20, 64 for ChaCha20 |
| 3 | Attachment | a flag byte (`0x01`: keep protected in memory), then the file's bytes |

The **inner cipher** keeps protected fields, the password above all, encrypted inside the XML even after the file
is opened. ChaCha20: *H* = SHA-512(inner key), the key *H*[0..31], the nonce *H*[32..43]. Salsa20: the key
SHA-256(inner key), the fixed nonce `E8 30 09 4B 97 20 5D 2A`.

The **XML** is the database: groups and entries (an entry's fields, its history, its times), `Meta`, deleted
objects. The structure is in `KDBX_XML.xsd`, which these pages do not reproduce. Times in 4.x are Base64 of an
Int64 count of seconds since 0001-01-01 00:00 UTC.

## 5. What this means for axPass

.NET has AES, SHA-256, SHA-512, HMAC and GZip. It has no Argon2 and no ChaCha20 on its own (only
ChaCha20-Poly1305), so reading a KDBX 4 file without a library means writing Argon2 and ChaCha20 here: the two
security-critical pieces the plan says are never reimplemented. Hence the plan's engine, KeePassXC's command-line
tool, below. Should that tool not work out, the fallback to weigh is a reader written from this page with its own
test vectors, not a library.

## 6. keepassxc-cli, per its manual

- Commands: `add`, `analyze` (offline HIBP check), `attachment-export`, `attachment-import`, `attachment-rm`,
  `clip`, `close`, `db-create`, `db-edit`, `db-info`, `diceware`, `edit`, `estimate`, `exit`/`quit`, `export`,
  `generate`, `help`, `import`, `ls`, `merge`, `mkdir`, `mv`, `open`, `rm`, `rmdir`, `search`, `show`.
- Global: `-k`/`--key-file <path>`, `--no-password`, `-y`/`--yubikey <slot[:serial]>`, `-q`/`--quiet` (no password
  prompt text and other secondary output), `--debug-info`.
- `show`: `-a`/`--attributes <name>` (repeatable), `-s`/`--show-protected`, `--all`, `--show-attachments`,
  `-t`/`--totp`.
- `clip`: an optional timeout in seconds (default 10, 0 never clears), `-a`/`--attribute` (default the password),
  `-t`/`--totp`, `-b`/`--best`.
- `ls`: `-R`/`--recursive`, `-f`/`--flatten` (one line per entry with its group path).
- `add` and `edit`: `-u`/`--username`, `--url`, `--notes`, `-p`/`--password-prompt`, `-g`/`--generate`; `edit` also
  `-t`/`--title`.
- `generate`: `-L`/`--length` (default 16), `-l`, `-U`, `-n` (on by default), `-s`/`--special`, `-e`/`--extended`,
  `-x`/`--exclude <chars>`, `--exclude-similar`, `--every-group`.
- `db-create`: `-p`/`--set-password`, `--set-key-file <path>`, `-t`/`--decryption-time <ms>`.
- `export`: `-f`/`--format xml|csv`.
- `open`: an interactive mode for several commands on one database, left with `close`, `exit` or `quit`; one process
  per open database instead of one per command, which would also hold the password only once.

## 7. Open, to settle by trying the tool

- How the master password reaches the tool from another program. The manual says nothing about standard input
  except for `estimate`, nor about a pipe on Windows. The plan's "the password on standard input" is unverified.
- The exact output formats of `ls`, `show` and `search`, for parsing.
- Whether `open`'s interactive mode suits a window better than one process per command.
- The manual lists `-q`/`--quiet` with a `<path>` argument that does not fit its description; try it.
