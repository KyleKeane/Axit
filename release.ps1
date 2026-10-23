<#
.SYNOPSIS
  Releases a new version of Axit, the bundle AxClaude is one app of: sets the version, commits, tags and pushes. GitHub Actions then builds, tests,
  publishes the zip and creates the GitHub release that installed copies update themselves from.

.DESCRIPTION
  Before running it, add a "## <version>" section to CHANGELOG.md: its text becomes the release notes and
  is what the update notice in the app shows. Everything else must be committed. The script then
    1. fetches origin and stops if origin/main has commits that main here does not (skipped with -NoPush),
    2. checks the "## <version>" section: it is not empty, and every bullet starts with its app or apps ("- AxFile: ",
       "- AxDown, AxJSON: ") or with "- For developers: ",
    3. runs every test project in Release, as the workflows do (test.ps1),
    4. sets <Version> in src/Axit/Axit.csproj,
    5. commits CHANGELOG.md and the project file as "Release <version>" and tags v<version>,
    6. pushes main, waits for its build to pass on GitHub (gh run watch), then pushes the tag: a tag never goes up on
       a commit whose build failed there.
  The tag starts .github/workflows/release.yml. Follow it with `gh run watch`, or on the Actions page. When it is
  done, the release is at https://github.com/KyleKeane/Axit/releases and the apps offer it as an update.

.EXAMPLE
  .\release.ps1 1.0.1
  .\release.ps1 1.0.1 -NoPush     # commit and tag only; push later with: git push origin main; git push origin v1.0.1
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true, Position = 0)]
    [string]$Version,

    [switch]$NoPush
)

$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot
$project = Join-Path $root 'src\Axit\Axit.csproj'
$changelog = Join-Path $root 'CHANGELOG.md'
$tag = "v$Version"

if ($Version -notmatch '^\d+\.\d+\.\d+$') {
    Write-Host "The version must be three numbers, like 1.0.1 (got '$Version')."
    exit 1
}

$branch = (git -C $root rev-parse --abbrev-ref HEAD).Trim()
if ($branch -ne 'main') {
    Write-Host "Release from main (you are on '$branch')."
    exit 1
}

# Nothing is released over work someone else has pushed: main here must contain everything origin/main has. The
# check belongs to the push, so -NoPush (commit and tag offline, push later) does not need origin to be reachable.
if (-not $NoPush) {
    git -C $root fetch -q origin
    if ($LASTEXITCODE -ne 0) {
        Write-Host "Could not fetch origin. Nothing was changed."
        exit $LASTEXITCODE
    }

    git -C $root merge-base --is-ancestor origin/main main
    if ($LASTEXITCODE -ne 0) {
        Write-Host "origin/main has commits that main here does not. Bring them in first (git pull --ff-only, or a merge), then run this again."
        exit 1
    }
}

$dirty = @(git -C $root status --porcelain) | Where-Object { $_ -and ($_.Substring(3) -ne 'CHANGELOG.md') }
if ($dirty.Count -gt 0) {
    Write-Host "Commit or stash these first; only CHANGELOG.md may be uncommitted:"
    $dirty | ForEach-Object { Write-Host "  $_" }
    exit 1
}

if (git -C $root tag --list $tag) {
    Write-Host "The tag $tag already exists. Pick the next version."
    exit 1
}

if (-not (Select-String -Path $changelog -Pattern ('^## ' + [regex]::Escape($Version) + '(\s|$)') -Quiet)) {
    Write-Host "CHANGELOG.md has no '## $Version' section. Add one (its text becomes the release notes), then run this again."
    exit 1
}

# The section is the release notes and the update notice: it says something, and every bullet names its app or apps,
# or is for developers, so that a reader of the notes knows where a change is (docs/axit/longevity.md 3.11). The
# apps are the folders of docs/ (axit among them, for the bundle), matched without regard to case; the notes come
# from tools/release-notes.ps1, the script the release workflow runs, which fails on an empty section.
try {
    $notes = & (Join-Path $root 'tools\release-notes.ps1') -Version $Version -Path $changelog
} catch {
    Write-Host $_.Exception.Message
    exit 1
}
$apps = @(Get-ChildItem (Join-Path $root 'docs') -Directory | ForEach-Object { $_.Name }) -join '|'
$prefix = "^- (($apps)(, ($apps))*|For developers): "
$unnamed = @($notes -split "`n" | Where-Object { $_ -match '^- ' -and $_ -notmatch $prefix })
if ($unnamed.Count -gt 0) {
    Write-Host "Every bullet of the '## $Version' section starts with its app or apps ('- AxFile: ', '- AxDown, AxJSON: ') or with '- For developers: '. These do not:"
    $unnamed | ForEach-Object { Write-Host "  $_" }
    exit 1
}

# The tests as GitHub Actions runs them: every test project, in Release (test.ps1).
Write-Host "Running the tests..."
& (Join-Path $root 'test.ps1')
if ($LASTEXITCODE -ne 0) {
    Write-Host "The tests failed. Nothing was changed."
    exit $LASTEXITCODE
}

$xml = Get-Content $project -Raw
$updated = $xml -replace '<Version>[^<]*</Version>', "<Version>$Version</Version>"
if ($updated -eq $xml -and $xml -notmatch "<Version>$([regex]::Escape($Version))</Version>") {
    Write-Host "No <Version> element was found in $project."
    exit 1
}
# Not Set-Content -Encoding utf8: in Windows PowerShell 5.1 that writes a byte order mark.
[IO.File]::WriteAllText($project, $updated, (New-Object System.Text.UTF8Encoding($false)))

git -C $root add -- CHANGELOG.md src/Axit/Axit.csproj
git -C $root commit -q -m "Release $Version"
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
git -C $root tag -a $tag -m "Axit $Version"
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
Write-Host "Committed and tagged $tag."

if ($NoPush) {
    Write-Host "Not pushed. When ready: git push origin main; git push origin $tag"
    exit 0
}

# main goes first and the tag only once main's build has passed on GitHub (build.yml, followed with gh): the tag
# starts the release build, and a tag on a commit whose build fails on the runner (an image or an action moved under
# the workflow) would be a dangling tag to delete by hand. Without gh the tag follows main at once.
git -C $root push origin main
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
if (Get-Command gh -ErrorAction SilentlyContinue) {
    Write-Host "Pushed main. Waiting for its build before pushing the tag..."
    $sha = (git -C $root rev-parse HEAD).Trim()
    $run = $null
    for ($try = 0; -not $run -and $try -lt 24; $try++) {
        Start-Sleep -Seconds 5
        $id = gh run list --workflow build.yml --commit $sha --limit 1 --json databaseId --jq '.[0].databaseId'
        if ($id -and $id -ne 'null') { $run = $id }
    }
    if (-not $run) {
        Write-Host "No build of main's $sha appeared on GitHub within two minutes. The tag was not pushed; once the build has passed: git push origin $tag"
        exit 1
    }
    gh run watch $run --exit-status
    if ($LASTEXITCODE -ne 0) {
        Write-Host "The build of main failed; the tag $tag was not pushed. Fix main, delete the tag here (git tag -d $tag) and release the next patch version."
        exit 1
    }
} else {
    Write-Host "Pushed main. gh is not installed, so the tag follows without waiting for main's build."
}

git -C $root push origin $tag
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
Write-Host "Pushed the tag. GitHub Actions is building the release: https://github.com/KyleKeane/Axit/actions"
Write-Host "Follow it with: gh run watch    (the release then appears at https://github.com/KyleKeane/Axit/releases)"
