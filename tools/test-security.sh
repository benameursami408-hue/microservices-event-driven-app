#!/usr/bin/env bash
set -euo pipefail

BASE_URL="${BASE_URL:-http://localhost:5005}"
CORRELATION_ID="security-smoke-$(date +%s)"

echo "Checking security headers on $BASE_URL/health/ready"
headers="$(curl -fsSI -H "X-Correlation-ID: $CORRELATION_ID" "$BASE_URL/health/ready")"
echo "$headers" | grep -qi "x-correlation-id" || { echo "Missing X-Correlation-ID"; exit 1; }
echo "$headers" | grep -qi "x-content-type-options" || { echo "Missing X-Content-Type-Options"; exit 1; }
echo "$headers" | grep -qi "x-frame-options" || { echo "Missing X-Frame-Options"; exit 1; }
echo "$headers" | grep -qi "referrer-policy" || { echo "Missing Referrer-Policy"; exit 1; }

echo "Checking unauthenticated API request returns 401 or 403"
status="$(curl -s -o /dev/null -w "%{http_code}" "$BASE_URL/api/admin/users")"
case "$status" in
  401|403) echo "Protected endpoint returned $status as expected" ;;
  *) echo "Expected 401 or 403, got $status"; exit 1 ;;
esac

echo "Security smoke test passed."
