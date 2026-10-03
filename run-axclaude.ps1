<#
.SYNOPSIS
  Builds Axit and starts AxClaude: run.ps1 with -App claude. Every other argument goes through unchanged.

.EXAMPLE
  .\run-axclaude.ps1                       # the current folder
  .\run-axclaude.ps1 C:\src\myproject -New # a new conversation in that folder
  .\run-axclaude.ps1 -NoBuild              # skip the build
#>
& "$PSScriptRoot\run.ps1" -App claude @args
exit $LASTEXITCODE
