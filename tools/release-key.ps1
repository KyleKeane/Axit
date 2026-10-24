<#
.SYNOPSIS
  The release signing key (docs/axit/security-audit-2026-10-24.md S-27, the user's decision): makes it, signs a
  release's zip with it, and prints its public half.

.DESCRIPTION
  An ECDSA P-256 key that lives only on the maintainer's machine, never on GitHub: its private half is kept in
  %LOCALAPPDATA%\Axit-release\release-key.bin, encrypted for the Windows account that made it (DPAPI), so that a copy
  of the file is useless elsewhere. release.ps1 signs every release's zip with it before the release is published;
  the updater (Axit.Core/Updates/ReleaseSigning) and install.cmd refuse a zip whose signature does not check against
  the public half built into them. A stolen GitHub account cannot sign. Losing the key means one release installed
  by hand with a new key's public half in it.

  Windows PowerShell 5.1 and .NET Framework only: CngKey, ECDsaCng and ProtectedData, nothing to install.

.EXAMPLE
  .\tools\release-key.ps1 new                          # once: makes the key, prints the public half for ReleaseSigning.cs
  .\tools\release-key.ps1 public                       # the public half again
  .\tools\release-key.ps1 sign publish\Axit-3.7.0-win-x64.zip    # writes Axit-3.7.0-win-x64.zip.sig next to it
  .\tools\release-key.ps1 verify publish\Axit-3.7.0-win-x64.zip  # checks the .sig against the public half
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true, Position = 0)]
    [ValidateSet('new', 'public', 'sign', 'verify')]
    [string]$Command,

    [Parameter(Position = 1)]
    [string]$File
)

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Security
Add-Type -AssemblyName System.Core
$keyFolder = Join-Path $env:LOCALAPPDATA 'Axit-release'
$keyFile = Join-Path $keyFolder 'release-key.bin'
$entropy = [Text.Encoding]::UTF8.GetBytes('Axit release signing key')

function Get-PrivateKey {
    if (-not (Test-Path -LiteralPath $keyFile)) {
        throw "No release key at $keyFile. Make one with: .\tools\release-key.ps1 new"
    }

    $blob = [Security.Cryptography.ProtectedData]::Unprotect([IO.File]::ReadAllBytes($keyFile), $entropy, 'CurrentUser')
    return [Security.Cryptography.CngKey]::Import($blob, [Security.Cryptography.CngKeyBlobFormat]::EccPrivateBlob)
}

# The public half as the apps hold it: the point's X and Y, 32 bytes each, in base64.
function Get-PublicText([Security.Cryptography.CngKey]$key) {
    $blob = $key.Export([Security.Cryptography.CngKeyBlobFormat]::EccPublicBlob)
    # BCRYPT_ECCKEY_BLOB: magic (4), key length (4), X (32), Y (32).
    return [Convert]::ToBase64String($blob, 8, 64)
}

function Get-Hash([string]$path) {
    $sha = [Security.Cryptography.SHA256]::Create()
    try {
        $stream = [IO.File]::OpenRead((Resolve-Path -LiteralPath $path).Path)
        try { return $sha.ComputeHash($stream) } finally { $stream.Dispose() }
    }
    finally { $sha.Dispose() }
}

switch ($Command) {
    'new' {
        if (Test-Path -LiteralPath $keyFile) {
            throw "A release key already exists at $keyFile. It is never replaced by this script: the apps trust its public half."
        }

        $parameters = New-Object Security.Cryptography.CngKeyCreationParameters
        $parameters.ExportPolicy = [Security.Cryptography.CngExportPolicies]::AllowPlaintextExport
        $key = [Security.Cryptography.CngKey]::Create([Security.Cryptography.CngAlgorithm]::ECDsaP256, $null, $parameters)
        $private = $key.Export([Security.Cryptography.CngKeyBlobFormat]::EccPrivateBlob)
        New-Item -ItemType Directory -Force -Path $keyFolder | Out-Null
        [IO.File]::WriteAllBytes($keyFile, [Security.Cryptography.ProtectedData]::Protect($private, $entropy, 'CurrentUser'))
        [Array]::Clear($private, 0, $private.Length)
        Write-Host "Made the release key: $keyFile (readable only by this Windows account)."
        Write-Host "Its public half, for src\Axit.Core\Updates\ReleaseSigning.cs and install.cmd:"
        Write-Output (Get-PublicText $key)
    }
    'public' {
        Write-Output (Get-PublicText (Get-PrivateKey))
    }
    'sign' {
        if (-not $File) { throw 'Name the file to sign.' }
        $key = Get-PrivateKey
        $ecdsa = New-Object Security.Cryptography.ECDsaCng($key)
        try {
            # The signature over the file's SHA-256, as r and s (32 bytes each), in base64: what ECDsa.VerifyHash reads.
            $signature = $ecdsa.SignHash((Get-Hash $File))
            $target = (Resolve-Path -LiteralPath $File).Path + '.sig'
            [IO.File]::WriteAllText($target, [Convert]::ToBase64String($signature))
            Write-Host "Signed: $target"
        }
        finally { $ecdsa.Dispose() }
    }
    'verify' {
        if (-not $File) { throw 'Name the file to check.' }
        $public = [Convert]::FromBase64String((& $PSCommandPath public))
        $blob = [byte[]](0x45, 0x43, 0x53, 0x31, 0x20, 0, 0, 0) + $public
        $ecdsa = New-Object Security.Cryptography.ECDsaCng([Security.Cryptography.CngKey]::Import($blob, [Security.Cryptography.CngKeyBlobFormat]::EccPublicBlob))
        $signature = [Convert]::FromBase64String([IO.File]::ReadAllText((Resolve-Path -LiteralPath "$File.sig").Path).Trim())
        if ($ecdsa.VerifyHash((Get-Hash $File), $signature)) { Write-Host 'The signature checks.'; exit 0 }
        Write-Host 'The signature does NOT check.'
        exit 1
    }
}
