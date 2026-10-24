using System.Text.Json.Nodes;
using AxPass.Core;
using Axit.Core;
using Axit.Forms;

namespace AxPass;

/// <summary>
/// axPass's commands (docs/axpass/SPEC.md PW-4 to PW-7): each one or two runs of the tool, the list read again after,
/// and for Ctrl+Z the runs that take it back. The field commands are the shared window's; they reach the database
/// through <see cref="SaveFields"/>.
/// </summary>
public sealed partial class PassForm
{
    /// <summary>One run of the tool: the command, its options, the paths after the database.</summary>
    private sealed record Call(string Command, string[] Options, string[] Rest);

    /// <summary>A change Ctrl+Z can take back: its words, the runs that undo it, and the row to select after.</summary>
    private sealed record Step(string Said, IReadOnlyList<Call> Undo, string? Select);

    private const int MaxUndo = 20;

    private readonly List<Step> _undo = [];

    /// <summary>A change to an entry's fields (PW-6.1): <c>edit</c> with the options that make it, the way back kept.</summary>
    protected override void SaveFields(ItemDetail detail, ItemDocument changed, string said, int selectField)
    {
        if (detail.Source is not string path || detail.Document is not { } before)
        {
            return;
        }

        var options = EntryFields.EditOptions(before, changed);
        if (options.Count == 0)
        {
            Announce("Only KeePass's own fields change here", true);
            return;
        }

        Do(async () =>
        {
            if (await Cli("edit", options, [path]) is null)
            {
                return;
            }

            _entries[path] = changed;
            Remember(new Step(said, [new Call("edit", [.. EntryFields.EditOptions(changed, before)], [path])], path));
            Fill(path);
            SelectField(selectField);
            Announce(said, true);
        });
    }

    /// <summary>An entry's Title, from F2 or the Title field: <c>edit -t</c>, its path changing with it.</summary>
    protected override void RenameItem(ItemRow row, string title)
    {
        if (row.Tag is not PassNode entry || entry.IsGroup)
        {
            return;
        }

        if (PassTree.RefusedName(title) is { } why)
        {
            Announce(why, true);
            return;
        }

        var to = PassTree.Join(entry.Group, title);
        Do(async () =>
        {
            if (await Cli("edit", ["-t", title], [entry.Path]) is null)
            {
                return;
            }

            if (_entries.Remove(entry.Path, out var fields))
            {
                _entries[to] = fields.WithTitle(title);
            }

            Remember(new Step("Renamed " + title, [new Call("edit", ["-t", entry.Name], [to])], entry.Path));
            await ReloadAsync(to);
            Announce("Renamed " + title, true);
        });
    }

    /// <summary>F2 in the navigator (AX-8.7): an entry's title in place; a group is renamed in KeePassXC's own window.</summary>
    protected override string? RenameRefused(ItemRow row) =>
        row.Tag is PassNode { IsGroup: true } ? "A group is renamed in KeePassXC's own window: its tool has no command for it" : null;

    protected override string? RefusedName(ItemRow row, string name) => PassTree.RefusedName(name);

    protected override void RenameTo(ItemRow row, string name) => RenameItem(row, name);
    /// <summary>
    /// Delete (PW-6.4): an entry to the Recycle Bin, back with Ctrl+Z; a group after a question, since only KeePassXC
    /// brings a group back; in the Recycle Bin, for good after a question.
    /// </summary>
    protected override void DeleteRow(ItemRow row)
    {
        if (row.Tag is not PassNode node || _tree is null)
        {
            return;
        }

        var forGood = PassTree.InRecycleBin(node.Path) && node.Path != PassTree.RecycleBin;
        if (node.Path == PassTree.RecycleBin)
        {
            Announce("The Recycle Bin stays; KeePassXC empties it", true);
            return;
        }

        var command = node.IsGroup ? "rmdir" : "rm";
        if (!node.IsGroup && !forGood)
        {
            Do(async () =>
            {
                var select = NeighbourKey();
                if (await Cli(command, null, [node.Path]) is null)
                {
                    return;
                }

                _entries.Remove(node.Path);
                Remember(new Step($"Moved {node.Name} to the Recycle Bin", [new Call("mv", [], [PassTree.Join(PassTree.RecycleBin, node.Name), node.Group.Length == 0 ? "/" : node.Group])], node.Path));
                await ReloadAsync(select);
                Announce($"Moved {node.Name} to the Recycle Bin; Ctrl+Z brings it back", true);
            });
            return;
        }

        var question = forGood
            ? $"Delete {node.Name} for good? It is in the Recycle Bin, and nothing brings it back."
            : $"Move the group {node.Name} and its {Count(_tree.CountIn(node.Path), "item")} to the Recycle Bin? Ctrl+Z cannot bring a group back: KeePassXC's own window can.";
        ShowNotice(new Notice(forGood ? "Delete for good" : "Move the group", question,
        [
            new OverlayChoice(forGood ? "&Delete" : "&Move", () => Do(async () =>
            {
                var select = NeighbourKey();
                if (await Cli(command, null, [node.Path]) is null)
                {
                    return;
                }

                _entries.Remove(node.Path);
                await ReloadAsync(select);
                Announce(forGood ? $"Deleted {node.Name}" : $"Moved {node.Name} to the Recycle Bin", true);
            })),
            new OverlayChoice("&Cancel", IsDefault: true, IsCancel: true),
        ]));
    }

    /// <summary>
    /// Ctrl+M (PW-6.5): the entry moved to another group chosen from a list of them, the top of the database first;
    /// <c>mv</c>, with the way back kept for Ctrl+Z.
    /// </summary>
    private void MoveEntry()
    {
        if (SelectedEntry() is not { } entry || _tree is null)
        {
            return;
        }

        const string TopName = "The top of the database";
        var groups = _tree.Nodes.Where(node => node.IsGroup && !PassTree.InRecycleBin(node.Path) && node.Path != entry.Group).Select(node => node.Path).ToList();
        List<string> choices = entry.Group.Length == 0 ? groups : [TopName, .. groups];
        if (choices.Count == 0)
        {
            Announce("There is no other group: Ctrl+Shift+N makes one", true);
            return;
        }

        ChooseFromList("Move " + entry.Name, $"Now in {(entry.Group.Length == 0 ? "the top of the database" : entry.Group)}.", "Group", choices.Select(choice => choice.Replace("/", ", ")).ToList(), 0, chosen =>
        {
            var group = choices[chosen] == TopName ? string.Empty : choices[chosen];
            if (_tree.HasEntry(PassTree.Join(group, entry.Name)))
            {
                Announce($"There is an entry called {entry.Name} there already", true);
                return;
            }

            Do(async () =>
            {
                var select = NeighbourKey();
                if (await Cli("mv", null, [entry.Path, group.Length == 0 ? "/" : group]) is null)
                {
                    return;
                }

                var moved = PassTree.Join(group, entry.Name);
                if (_entries.Remove(entry.Path, out var fields))
                {
                    _entries[moved] = fields;
                }

                var where = group.Length == 0 ? "the top of the database" : group.Replace("/", ", ");
                Remember(new Step($"Moved {entry.Name} to {where}", [new Call("mv", [], [moved, entry.Group.Length == 0 ? "/" : entry.Group])], entry.Path));
                await ReloadAsync(select);
                Announce($"Moved {entry.Name} to {where}", true);
            });
        });
    }

    /// <summary>Ctrl+N in the navigator: a new entry in the group shown, its title asked; no password until one is set.</summary>
    protected override void NewItem()
    {
        if (_password is null)
        {
            Announce("Locked: Enter unlocks", true);
            return;
        }

        var group = _group;
        AskText("New entry", group.Length == 0 ? "At the top of the database." : $"In {group}.", "Title:", "Title", string.Empty, "&Add", title => Do(async () =>
        {
            var path = PassTree.Join(group, title.Trim());
            if (await Cli("add", [], [path]) is null)
            {
                return;
            }

            Remember(new Step("Added " + title.Trim(), [new Call("rm", [], [path]), new Call("rm", [], [PassTree.Join(PassTree.RecycleBin, title.Trim())])], null));
            await ReloadAsync(path);
            Announce($"Added {title.Trim()}; Tab, then Enter on Password sets its password", true);
        }), typed => PassTree.RefusedName(typed) ?? (_tree?.HasEntry(PassTree.Join(group, typed.Trim())) == true ? $"There is an entry called {typed.Trim()} here already" : null));
    }

    /// <summary>Ctrl+Shift+N: a new group in the group shown.</summary>
    protected override void NewList()
    {
        if (_password is null)
        {
            Announce("Locked: Enter unlocks", true);
            return;
        }

        var group = _group;
        AskText("New group", group.Length == 0 ? "At the top of the database." : $"In {group}.", "Name:", "Name", string.Empty, "&Make", name => Do(async () =>
        {
            var path = PassTree.Join(group, name.Trim());
            if (await Cli("mkdir", [], [path]) is null)
            {
                return;
            }

            Remember(new Step("Made " + name.Trim(), [new Call("rmdir", [], [path]), new Call("rmdir", [], [PassTree.Join(PassTree.RecycleBin, name.Trim())])], null));
            await ReloadAsync(GroupKey(path));
            Announce("Made " + name.Trim(), true);
        }), typed => PassTree.RefusedName(typed) ?? (_tree?.HasGroup(PassTree.Join(group, typed.Trim())) == true ? $"There is a group called {typed.Trim()} here already" : null));
    }

    /// <summary>Ctrl+Z: the last step's runs, in order.</summary>
    protected override void Undo()
    {
        if (_undo.Count == 0)
        {
            Announce("Nothing to undo", true);
            return;
        }

        var step = _undo[^1];
        _undo.RemoveAt(_undo.Count - 1);
        Do(async () =>
        {
            foreach (var call in step.Undo)
            {
                if (await Cli(call.Command, call.Options, call.Rest) is null)
                {
                    await ReloadAsync(SelectedKey());
                    return;
                }
            }

            _entries.Clear();
            if (step.Select is { } path && !path.EndsWith('/') && await Cli("show", null, [path]) is { } shown)
            {
                // The entry read again before the words, so that its fields are there for the next key.
                _entries[path] = EntryFields.Parse(shown.Output);
            }

            await ReloadAsync(step.Select ?? SelectedKey());
            Announce("Undone: " + step.Said, true);
        });
    }

    private void Remember(Step step)
    {
        _undo.Add(step);
        if (_undo.Count > MaxUndo)
        {
            _undo.RemoveAt(0);
        }
    }

    // ---- the password field (PW-6.2) ----

    /// <summary>Enter on the Password field: a new one typed or generated, asked twice when typed, set with <c>edit -p</c>; the field for anything else not KeePass's own is read only.</summary>
    protected override bool ChangeSpecial(ItemRow row, int index, ItemField field)
    {
        if (row.Tag is not PassNode entry)
        {
            return false;
        }

        if (field.Type != FieldTypes.Password)
        {
            if (EntryFields.IsStandard(field.Title))
            {
                return false;
            }

            Announce($"{field.Title} is changed in KeePassXC's own window", true);
            return true;
        }

        var generated = false;
        ShowNotice(new Notice("New password for " + entry.Name, "Type it, or Generate makes one of 20 characters. Ctrl+Z does not bring the old one back.",
        [
            new OverlayChoice("&Set", () =>
            {
                var typed = Overlay.InputText;
                Overlay.SetInputText(string.Empty);
                if (generated)
                {
                    SetPassword(entry, typed);
                    return;
                }

                ShowNotice(new Notice("Type it again", "The same password once more.",
                [
                    new OverlayChoice("&Set", () =>
                    {
                        var again = Overlay.InputText;
                        Overlay.SetInputText(string.Empty);
                        if (again == typed)
                        {
                            SetPassword(entry, typed);
                        }
                        else
                        {
                            Announce("The two passwords differ: nothing changed", true);
                        }
                    }, IsDefault: true),
                    new OverlayChoice("&Cancel", IsCancel: true),
                ])
                {
                    Input = new OverlayInput($"{entry.Name}'s new password again:", $"{entry.Name}'s new password again", string.Empty, "Type the password again", Hidden: true),
                });
            }, IsDefault: true),
            new OverlayChoice("&Generate", () => Do(async () =>
            {
                if (ToolPath() is not { } cli)
                {
                    return;
                }

                var result = await Task.Run(() => KeePassCli.Run(cli, ["generate", "-L", "20", "-l", "-U", "-n", "-s"], []));
                if (result.Succeeded)
                {
                    Overlay.SetInputText(result.Output.Trim());
                    generated = true;
                    Announce("Generated a password of 20 characters; Enter sets it", true);
                }
                else
                {
                    Announce(result.Why, true);
                }
            }), StaysOpen: true),
            new OverlayChoice("&Cancel", IsCancel: true),
        ])
        {
            Input = new OverlayInput($"New password for the entry {entry.Name}:", $"New password for the entry {entry.Name}", string.Empty, "Type a password, or Generate", Hidden: true),
        });
        return true;
    }

    private void SetPassword(PassNode entry, string password) => Do(async () =>
    {
        if (await Cli("edit", ["-p"], [entry.Path], [password, password]) is not null)
        {
            Announce("Password set for " + entry.Name, true);
        }
    });

    // ---- the copies and the URL (PW-5.4, PW-7) ----

    /// <summary>Ctrl+B: the selected entry's user name on the clipboard (no secret, so axPass puts it there itself).</summary>
    private void CopyUserName()
    {
        if (SelectedEntry() is not { } entry)
        {
            return;
        }

        if (!_entries.TryGetValue(entry.Path, out var fields))
        {
            Announce("Reading the entry: press it again in a moment", true);
            return;
        }

        var user = EntryFields.ValueOf(fields, EntryFields.UserName);
        if (user.Length == 0)
        {
            Announce(entry.Name + " has no user name", true);
            return;
        }

        try
        {
            Clipboard.SetText(user);
            Announce("Copied the user name of " + entry.Name, true);
        }
        catch (Exception ex) when (ex is System.Runtime.InteropServices.ExternalException or InvalidOperationException)
        {
            Fail("The clipboard could not be written: " + ex.Message);
        }
    }

    /// <summary>Ctrl+C: the password on the clipboard by the tool's <c>clip</c>, which clears it after 30 seconds (PW-5.4, PW-D4).</summary>
    private void CopyPassword()
    {
        if (SelectedEntry() is not { } entry || _password is not { } password || _database is not { } database || ToolPath() is not { } cli)
        {
            return;
        }

        Do(async () =>
        {
            var result = await Task.Run(() => KeePassCli.StartLasting(cli, KeePassCli.On("clip", database, null, entry.Path, "30"), [password], 1500));
            Announce(result is { Succeeded: false } failed ? failed.Why : $"Copied the password of {entry.Name}; the clipboard clears in 30 seconds", true);
        });
    }

    /// <summary>Ctrl+U: the entry's URL in the browser.</summary>
    private void OpenEntryUrl()
    {
        if (SelectedEntry() is not { } entry)
        {
            return;
        }

        if (_entries.TryGetValue(entry.Path, out var fields) && EntryFields.ValueOf(fields, EntryFields.Url) is { Length: > 0 } url)
        {
            OpenUrl(url);
        }
        else
        {
            Announce(_entries.ContainsKey(entry.Path) ? entry.Name + " has no URL" : "Reading the entry: press it again in a moment", true);
        }
    }

    /// <summary>The selected entry; "Select an entry" said otherwise, or "Locked".</summary>
    private PassNode? SelectedEntry()
    {
        if (_password is null)
        {
            Announce("Locked: Enter unlocks", true);
            return null;
        }

        if (SelectedRow() is { Tag: PassNode { IsGroup: false } entry })
        {
            return entry;
        }

        Announce("Select an entry first", true);
        return null;
    }

    // ---- the database ----

    /// <summary>Ctrl+O: another database from the Windows file picker; this one locks first.</summary>
    private void ChooseDatabase()
    {
        using var dialog = new OpenFileDialog
        {
            Title = "Open a KeePass database",
            Filter = "KeePass databases (*.kdbx)|*.kdbx|All files (*.*)|*.*",
            InitialDirectory = _database is not null ? Path.GetDirectoryName(_database) : Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
        };
        if (ShowSystemDialog(dialog) != DialogResult.OK)
        {
            return;
        }

        OpenDatabase(dialog.FileName);
    }

    private void OpenDatabase(string path)
    {
        _password = null;
        _tree = null;
        _entries.Clear();
        _undo.Clear();
        _database = path;
        _group = string.Empty;
        Fill(select: null);
        ShowUnlock(null);
    }

    /// <summary>File, New database: a file name from the Windows picker, then the master password twice; <c>db-create -p</c>.</summary>
    private void NewDatabase()
    {
        if (ToolPath() is not { } cli)
        {
            Announce("KeePassXC is not installed", true);
            return;
        }

        using var dialog = new SaveFileDialog
        {
            Title = "Make a KeePass database",
            Filter = "KeePass databases (*.kdbx)|*.kdbx",
            DefaultExt = "kdbx",
            OverwritePrompt = true,
            InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
        };
        if (ShowSystemDialog(dialog) != DialogResult.OK)
        {
            return;
        }

        var path = dialog.FileName;
        ShowNotice(new Notice("Master password for " + PassTree.DatabaseName(path), "The password that opens the database. Nothing brings it back if it is forgotten.",
        [
            new OverlayChoice("&Next", () =>
            {
                var first = Overlay.InputText;
                Overlay.SetInputText(string.Empty);
                ShowNotice(new Notice("Type it again", "The same master password once more.",
                [
                    new OverlayChoice("&Make", () =>
                    {
                        var again = Overlay.InputText;
                        Overlay.SetInputText(string.Empty);
                        if (again != first)
                        {
                            Announce("The two passwords differ: no database made", true);
                            return;
                        }

                        Do(async () =>
                        {
                            if (File.Exists(path))
                            {
                                File.Delete(path);
                            }

                            var result = await Task.Run(() => KeePassCli.Run(cli, KeePassCli.On("db-create", path, ["-p"]), [first, first]));
                            if (!result.Succeeded)
                            {
                                Fail(PassTree.DatabaseName(path) + " could not be made: " + result.Why);
                                return;
                            }

                            await UnlockAsync(path, first);
                        });
                    }, IsDefault: true),
                    new OverlayChoice("&Cancel", IsCancel: true),
                ])
                {
                    Input = new OverlayInput("Master password again:", $"Master password of the new database {PassTree.DatabaseName(path)}, again", string.Empty, "Type the master password again", Hidden: true),
                });
            }, IsDefault: true),
            new OverlayChoice("&Cancel", IsCancel: true),
        ])
        {
            Input = new OverlayInput("Master password:", $"Master password of the new database {PassTree.DatabaseName(path)}", string.Empty, "Type a master password", Hidden: true),
        });
    }

    /// <summary>Options, KeePassXC's tool: where <c>keepassxc-cli.exe</c> is, when KeePassXC is not where it installs.</summary>
    private void ChooseCli()
    {
        using var dialog = new OpenFileDialog
        {
            Title = "Where is keepassxc-cli.exe?",
            Filter = "keepassxc-cli.exe|keepassxc-cli.exe",
        };
        if (ShowSystemDialog(dialog) != DialogResult.OK)
        {
            return;
        }

        _settings.CliPath = dialog.FileName;
        SaveSettings();
        Announce("axPass uses " + dialog.FileName, true);
    }
}
