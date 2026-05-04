$ErrorActionPreference = "Stop"
$BaseUrl = if ($env:BASE_URL) { $env:BASE_URL } else { "http://localhost:5005" }
$CorrelationId = "security-smoke-$([DateTimeOffset]::UtcNow.ToUnixTimeSeconds())"

Write-Host "Checking security headers on $BaseUrl/health/ready"
$response = Invoke-WebRequest -Uri "$BaseUrl/health/ready" -Headers @{ "X-Correlation-ID" = $CorrelationId } -Method Head
$headers = $response.Headers

foreach ($required in @("X-Correlation-ID", "X-Content-Type-Options", "X-Frame-Options", "Referrer-Policy")) {
    if (-not $headers.ContainsKey($required)) {
        throw "Missing required header: $required"
    }
}

Write-Host "Checking unauthenticated API request returns 401 or 403"
try {
    Invoke-WebRequest -Uri "$BaseUrl/api/admin/users" -Method Get | Out-Null
    throw "Expected protected endpoint to reject unauthenticated request"
} catch {
    $statusCode = $_.Exception.Response.StatusCode.value__
    if ($statusCode -ne 401 -and $statusCode -ne 403) {
        throw "Expected 401 or 403, got $statusCode"
    }
}

Write-Host "Security smoke test passed."
