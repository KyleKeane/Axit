<#
.SYNOPSIS
  The apps of the bundle, read off Dispatch.Apps in src\Axit.Core\Dispatch.cs, for the scripts that run before a
  build or without one: run.ps1, tools\make-icon.ps1 and tools\new-app.ps1 (docs/axit/app-kit.md AK-4.1).

.DESCRIPTION
  Dot-source it, then call Get-AxitApps with the repository's root. Each app is an object with Name (as shown,
  "axGIT"), Verb ("git"), Project ("AxGit"), Group, Opens (Nothing, Folders, Files, FilesAndFolders) and Icon
  (Letters, Rgb), in the order of Dispatch.Apps. It reads the lines as Dispatch.cs writes them: each entry starts
  with new("name", Constant, "Project", AppGroup.X, Opens.Y on a line of its own, and its Icon = new("Ab", r, g, b)
  follows before the next entry. The installer runs without the source and reads the same list from the program,
  Axit.exe apps.

.EXAMPLE
  . .\tools\apps.ps1; Get-AxitApps . | Format-Table Name, Verb, Project
#>

function Get-AxitApps([string]$Root) {
    $file = Join-Path $Root 'src\Axit.Core\Dispatch.cs'
    $text = [System.IO.File]::ReadAllText((Resolve-Path -LiteralPath $file).ProviderPath)

    # The verbs are constants: public const string AxGit = "git";
    $verbs = @{}
    foreach ($match in [regex]::Matches($text, 'public const string (\w+) = "([^"]*)";')) {
        $verbs[$match.Groups[1].Value] = $match.Groups[2].Value
    }

    $start = $text.IndexOf('public static readonly IReadOnlyList<App> Apps =')
    if ($start -lt 0) { throw "Dispatch.Apps was not found in $file." }
    $end = $text.IndexOf('];', $start)
    $block = $text.Substring($start, $end - $start)
    $entries = [regex]::Matches($block, '(?m)^\s*new\("(?<name>[^"]+)", (?<verb>\w+), "(?<project>\w+)", AppGroup\.(?<group>\w+), Opens\.(?<opens>\w+)')
    if ($entries.Count -eq 0) { throw "No app was found in Dispatch.Apps in $file." }

    for ($i = 0; $i -lt $entries.Count; $i++) {
        $entry = $entries[$i]
        $next = if ($i + 1 -lt $entries.Count) { $entries[$i + 1].Index } else { $block.Length }
        $body = $block.Substring($entry.Index, $next - $entry.Index)
        $icon = [regex]::Match($body, 'Icon = new\("(?<letters>[^"]+)", (?<r>\d+), (?<g>\d+), (?<b>\d+)\)')
        $constant = $entry.Groups['verb'].Value
        if (-not $verbs.ContainsKey($constant)) { throw "The verb constant $constant of $($entry.Groups['name'].Value) was not found in $file." }
        [pscustomobject]@{
            Name = $entry.Groups['name'].Value
            Verb = $verbs[$constant]
            Constant = $constant
            Project = $entry.Groups['project'].Value
            Group = $entry.Groups['group'].Value
            Opens = $entry.Groups['opens'].Value
            Icon = if ($icon.Success) {
                [pscustomobject]@{ Letters = $icon.Groups['letters'].Value; Rgb = @([int]$icon.Groups['r'].Value, [int]$icon.Groups['g'].Value, [int]$icon.Groups['b'].Value) }
            } else { $null }
        }
    }
}
