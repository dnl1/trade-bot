#!/bin/bash
# Trade Bot — deploy script (run on the homelab server)
# Usage: ./scripts/deploy.sh [--no-build]
set -euo pipefail

DEPLOY_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$DEPLOY_DIR"

echo "==> Trade Bot Deploy — $(date '+%Y-%m-%d %H:%M:%S')"

# Verify .env exists
if [ ! -f .env ]; then
  echo "ERROR: .env not found. Copy .env.example and fill in your credentials."
  exit 1
fi

# Create logs folder if missing
mkdir -p logs

# Pull latest code
echo "==> git pull"
git pull --ff-only

if [[ "${1:-}" != "--no-build" ]]; then
  echo "==> Building Docker image"
  docker compose build --no-cache trade-bot
fi

echo "==> Starting services"
docker compose up -d

echo "==> Waiting for bot to become healthy (max 2 min)..."
for i in $(seq 1 24); do
  STATUS=$(docker inspect trade-bot --format='{{.State.Health.Status}}' 2>/dev/null || echo "starting")
  if [ "$STATUS" = "healthy" ]; then
    echo "==> Bot is healthy!"
    break
  fi
  echo "    ($i/24) status: $STATUS"
  sleep 5
done

echo ""
echo "==> Final status:"
docker compose ps

echo ""
echo "==> Last 20 log lines:"
docker compose logs --tail=20 trade-bot
