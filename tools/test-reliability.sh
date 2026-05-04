#!/usr/bin/env bash
set -euo pipefail

services=(5001 5002 5003 5004 5005)
for port in "${services[@]}"; do
  echo "Checking http://localhost:${port}/health/ready"
  curl -fsS "http://localhost:${port}/health/ready" >/dev/null
  echo "OK ${port}"
done

correlation_id="phase6-smoke-$(date +%s)"
echo "Checking X-Correlation-ID roundtrip through gateway: ${correlation_id}"
headers=$(curl -fsSI -H "X-Correlation-ID: ${correlation_id}" http://localhost:5005/health/ready || true)
echo "$headers" | grep -i "X-Correlation-ID: ${correlation_id}" >/dev/null

echo "Phase 6 reliability smoke checks passed."
