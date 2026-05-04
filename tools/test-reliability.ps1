$ErrorActionPreference = "Stop"

$ports = @(5001, 5002, 5003, 5004, 5005)
foreach ($port in $ports) {
    Write-Host "Checking http://localhost:$port/health/ready"
    Invoke-RestMethod -Uri "http://localhost:$port/health/ready" | Out-Null
    Write-Host "OK $port"
}

$correlationId = "phase6-smoke-$(Get-Date -Format yyyyMMddHHmmss)"
Write-Host "Checking X-Correlation-ID roundtrip through gateway: $correlationId"
$response = Invoke-WebRequest -Uri "http://localhost:5005/health/ready" -Headers @{ "X-Correlation-ID" = $correlationId }
if ($response.Headers["X-Correlation-ID"] -ne $correlationId) {
    throw "Expected X-Correlation-ID header '$correlationId' but got '$($response.Headers["X-Correlation-ID"])'"
}

Write-Host "Phase 6 reliability smoke checks passed."
