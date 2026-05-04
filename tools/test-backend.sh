#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT_DIR"

projects=(
  "src/services/AuthService/AuthService.Tests/AuthService.Tests.csproj"
  "src/services/ReclamationService/ReclamationService.Tests/ReclamationService.Tests.csproj"
  "src/services/InterventionService/InterventionService.Tests/InterventionService.Tests.csproj"
  "src/services/NotificationService/NotificationService.Tests/NotificationService.Tests.csproj"
)

for project in "${projects[@]}"; do
  echo "==> dotnet test $project"
  dotnet test "$project" --configuration Release
  echo
 done
