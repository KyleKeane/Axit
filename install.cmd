@echo off
rem Installs Axit for the current user: double-click it, no administrator rights, no PowerShell policy to change.
rem Next to install.ps1 (the extracted zip, or the installed folder) it runs that script and passes its arguments on,
rem for example: install.cmd -Uninstall. On its own, downloaded from the releases page, it finds the latest release
rem through the page's own address (releases/latest leads to the release's page, whose address ends in the tag; not
rem the GitHub API, which allows a network address sixty requests an hour), downloads the zip by its name and installs
rem it. PowerShell runs with the execution policy bypassed for this one run only, and TLS is whatever Windows offers.
rem Everything is one block: cmd.exe reads a block whole before running it, so this file may be deleted while it runs
rem (Uninstall from Settings, Apps runs it from the folder that the uninstall removes).
setlocal
(
    if exist "%~dp0install.ps1" (
        powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0install.ps1" %*
    ) else (
        echo Getting the latest Axit release from GitHub...
        powershell -NoProfile -ExecutionPolicy Bypass -Command "$ErrorActionPreference = 'Stop'; [Net.ServicePointManager]::SecurityProtocol = 'SystemDefault'; $page = Invoke-WebRequest 'https://github.com/KyleKeane/Axit/releases/latest' -UseBasicParsing; $where = $page.BaseResponse.ResponseUri.AbsoluteUri; if ($where -notmatch '/tag/v[0-9]') { throw ('No release was found: the latest-release address led to ' + $where) }; $tag = ($where -split '/tag/')[-1]; $name = 'Axit-' + $tag.Substring(1) + '-win-x64.zip'; $dir = Join-Path $env:TEMP 'Axit-install'; Remove-Item $dir -Recurse -Force -ErrorAction SilentlyContinue; New-Item -ItemType Directory $dir | Out-Null; $zip = Join-Path $dir $name; Write-Host ('Downloading ' + $name + '...'); Invoke-WebRequest ('https://github.com/KyleKeane/Axit/releases/download/' + $tag + '/' + $name) -OutFile $zip -UseBasicParsing; Expand-Archive $zip $dir -Force; & (Join-Path $dir 'install.ps1'); $code = $LASTEXITCODE; Set-Location $env:TEMP; Remove-Item $dir -Recurse -Force -ErrorAction SilentlyContinue; exit $code"
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
