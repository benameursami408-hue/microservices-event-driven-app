$ErrorActionPreference = "Stop"
$Root = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)

& "$Root/tools/test-frontend.ps1"
& "$Root/tools/test-backend.ps1"
