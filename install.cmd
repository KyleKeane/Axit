@echo off
rem Installs Axit for the current user: double-click it, no administrator rights, no PowerShell policy to change.
rem Next to install.ps1 (the extracted zip, or the installed folder) it runs that script and passes its arguments on,
rem for example: install.cmd -Uninstall. On its own, downloaded from the releases page, it finds the latest release
rem through the page's own address (releases/latest leads to the release's page, whose address ends in the tag; not
rem the GitHub API, which allows a network address sixty requests an hour), downloads the zip by its name and installs
rem it, into a folder of a random name, only after the zip's signature checks against Axit's release key (its public half is in the line below; docs/axit/security-audit-2026-10-24.md S-28). PowerShell runs with the execution policy bypassed for this one run only, over TLS 1.2 or later.
rem Everything is one block: cmd.exe reads a block whole before running it, so this file may be deleted while it runs
rem (Uninstall from Settings, Apps runs it from the folder that the uninstall removes).
setlocal
(
    if exist "%~dp0install.ps1" (
        powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0install.ps1" %*
    ) else (
        echo Getting the latest Axit release from GitHub...
        powershell -NoProfile -ExecutionPolicy Bypass -Command "$ErrorActionPreference = 'Stop'; [Net.ServicePointManager]::SecurityProtocol = [Net.ServicePointManager]::SecurityProtocol -bor 3072; $page = Invoke-WebRequest 'https://github.com/KyleKeane/Axit/releases/latest' -UseBasicParsing; $where = $page.BaseResponse.ResponseUri.AbsoluteUri; if ($where -notmatch '/tag/v[0-9]') { throw ('No release was found: the latest-release address led to ' + $where) }; $tag = ($where -split '/tag/')[-1]; $name = 'Axit-' + $tag.Substring(1) + '-win-x64.zip'; $dir = Join-Path $env:TEMP ('Axit-install-' + [guid]::NewGuid().ToString('N')); New-Item -ItemType Directory $dir | Out-Null; $zip = Join-Path $dir $name; $base = 'https://github.com/KyleKeane/Axit/releases/download/' + $tag + '/' + $name; Write-Host ('Downloading ' + $name + '...'); Invoke-WebRequest $base -OutFile $zip -UseBasicParsing; $sig = [Convert]::FromBase64String((Invoke-WebRequest ($base + '.sig') -UseBasicParsing).Content.Trim()); $sha = [Security.Cryptography.SHA256]::Create(); $s = [IO.File]::OpenRead($zip); $hash = $sha.ComputeHash($s); $s.Dispose(); $key = [Security.Cryptography.CngKey]::Import(([byte[]](0x45,0x43,0x53,0x31,0x20,0,0,0) + [Convert]::FromBase64String('vAMJ2gn+w4fa1hWGA69TxbBQ+Ob4YYnTMKAwzQAhlXlffPSVVyb5pll/0Z93QQDGHVIq9gBOA2mLlOJ8Sx/k6w==')), [Security.Cryptography.CngKeyBlobFormat]::EccPublicBlob); if (-not (New-Object Security.Cryptography.ECDsaCng($key)).VerifyHash($hash, $sig)) { Remove-Item $dir -Recurse -Force; throw 'The downloaded zip is not signed by Axit''s release key; nothing was installed.' }; Write-Host 'The release''s signature checks.'; Expand-Archive $zip $dir -Force; & (Join-Path $dir 'install.ps1'); $code = $LASTEXITCODE; Set-Location $env:TEMP; Remove-Item $dir -Recurse -Force -ErrorAction SilentlyContinue; exit $code"
    )
    if errorlevel 1 (
        echo.
        echo That did not work. The messages above say why.
    )
    echo.
    pause
    rem Leave without reading this file again: (goto) ends the batch at once and quietly, even once it is deleted.
    (goto) 2>nul
)
