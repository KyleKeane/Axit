<#
.SYNOPSIS
  Builds Axit and starts AxDown: run.ps1 with -App down. Every other argument goes through unchanged. Without a file
  it opens docs\axdown\tutorial.md, the guided tour of AxDown with every Markdown structure to practise on.

.EXAMPLE
  .\run-axdown.ps1                    # the tutorial file
  .\run-axdown.ps1 C:\notes\todo.md   # that file
  .\run-axdown.ps1 -NoBuild           # the tutorial file, without building
#>
$arguments = @($args)
if (-not ($arguments | Where-Object { $_ -notlike '-*' })) {
    $arguments += Join-Path $PSScriptRoot 'docs\axdown\tutorial.md'
}
& "$PSScriptRoot\run.ps1" -App down @arguments
exit $LASTEXITCODE
