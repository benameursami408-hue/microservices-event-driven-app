$ErrorActionPreference = "Stop"
$Root = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)
Set-Location $Root

$projects = @(
  "src/services/AuthService/AuthService.Tests/AuthService.Tests.csproj",
  "src/services/ReclamationService/ReclamationService.Tests/ReclamationService.Tests.csproj",
  "src/services/InterventionService/InterventionService.Tests/InterventionService.Tests.csproj",
  "src/services/NotificationService/NotificationService.Tests/NotificationService.Tests.csproj"
)

foreach ($project in $projects) {
  Write-Host "==> dotnet test $project"
  dotnet test $project --configuration Release
}
