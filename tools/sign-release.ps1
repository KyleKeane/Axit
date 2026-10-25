<#
.SYNOPSIS
  Signs a draft release with the release key and publishes it (docs/axit/SPEC.md AX-3.4; security audit S-27).

.DESCRIPTION
  release.ps1 calls it after pushing the tag; it can be run alone for a draft already on GitHub. It
    1. waits for the tag's release workflow to finish (it leaves the release as a draft with the zip and install.cmd),
    2. downloads the draft's zip into a fresh folder and checks the build provenance GitHub attested for it (that this
       repository's release workflow built it),
    3. signs the zip's SHA-256 with the release key (tools/release-key.ps1), which lives only on this machine,
       checks the signature against the public half the apps hold, and uploads "<zip>.sig" to the draft,
    4. publishes the release. Only then do the apps see it, and they install it only when the signature checks.

.EXAMPLE
  .\tools\sign-release.ps1 3.7.0
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true, Position = 0)]
    [string]$Version
)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$tag = "v$Version"
$repo = 'KyleKeane/Axit'
if ($Version -notmatch '^\d+\.\d+\.\d+$') { throw "The version must be three numbers, like 1.0.1 (got '$Version')." }

Write-Host "Waiting for the release workflow of $tag..."
$run = $null
for ($try = 0; -not $run -and $try -lt 24; $try++) {
    $id = gh run list --repo $repo --workflow release.yml --branch $tag --limit 1 --json databaseId --jq '.[0].databaseId'
    if ($id -and $id -ne 'null') { $run = $id } else { Start-Sleep -Seconds 5 }
}
if (-not $run) { throw "No release workflow for $tag appeared within two minutes." }
gh run watch $run --repo $repo --exit-status
if ($LASTEXITCODE -ne 0) { throw "The release workflow of $tag failed; nothing was signed or published." }

$draft = gh release view $tag --repo $repo --json isDraft --jq .isDraft
if (-not $draft) { throw "No release for $tag was found on GitHub: look at the draft's tag name there (gh release list)." }
if ($draft -ne 'true') { throw "Release $tag is not a draft: it was published without its signature. Look at it on GitHub before anything else." }

$folder = Join-Path $env:TEMP ("axit-sign-" + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $folder | Out-Null
try {
    $name = "Axit-$Version-win-x64.zip"
    gh release download $tag --repo $repo --pattern $name --dir $folder
    if ($LASTEXITCODE -ne 0) { throw "The draft's $name could not be downloaded." }
    $zip = Join-Path $folder $name

    gh attestation verify $zip --repo $repo | Out-Host
    if ($LASTEXITCODE -ne 0) { throw "GitHub has no provenance from this repository's release workflow for $name; nothing was signed." }

    & (Join-Path $PSScriptRoot 'release-key.ps1') sign $zip | Out-Host
    & (Join-Path $PSScriptRoot 'release-key.ps1') verify $zip | Out-Host
    if ($LASTEXITCODE -ne 0) { throw 'The signature just made does not check against the public half; nothing was published.' }

    gh release upload $tag "$zip.sig" --repo $repo --clobber
    if ($LASTEXITCODE -ne 0) { throw 'The signature could not be uploaded.' }
    gh release edit $tag --repo $repo --draft=false
    if ($LASTEXITCODE -ne 0) { throw 'The release could not be published.' }
    Write-Host "Signed and published ${tag}: https://github.com/$repo/releases/tag/$tag"
}
finally {
    Remove-Item -LiteralPath $folder -Recurse -Force -ErrorAction SilentlyContinue
}
