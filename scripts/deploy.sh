#!/usr/bin/env bash
#
# One-command deploy for LoanTracker (native install on the VPS).
# Builds the API (self-contained linux-x64) and the React frontend locally,
# uploads them, and restarts the API service. No Docker, no .NET/Node on the server.
#
# Usage:   bash scripts/deploy.sh
# Requires: dotnet SDK + node/npm locally, and the SSH deploy key.
#
set -euo pipefail

SERVER="${LT_SERVER:-root@213.210.37.67}"
KEY="${LT_KEY:-$HOME/.ssh/loantracker_deploy}"
SSH="ssh -i $KEY -o BatchMode=yes -o StrictHostKeyChecking=accept-new"
SCP="scp -i $KEY -o BatchMode=yes -o StrictHostKeyChecking=accept-new"

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
OUT="$ROOT/.deploy"
rm -rf "$OUT" && mkdir -p "$OUT"

echo "==> Building API (self-contained linux-x64)..."
dotnet publish "$ROOT/backend/src/LoanTracker.Api/LoanTracker.Api.csproj" \
  -c Release -r linux-x64 --self-contained true -o "$OUT/publish" >/dev/null

echo "==> Building frontend..."
( cd "$ROOT/frontend" && npm run build >/dev/null )

echo "==> Packaging..."
tar --force-local -czf "$OUT/api.tar.gz" -C "$OUT/publish" .
tar --force-local -czf "$OUT/web.tar.gz" -C "$ROOT/frontend/dist" .

echo "==> Uploading..."
$SCP "$OUT/api.tar.gz" "$OUT/web.tar.gz" "$SERVER:/tmp/"

echo "==> Deploying on server..."
$SSH "$SERVER" 'bash -s' <<'REMOTE'
set -e
rm -rf /opt/loantracker/api && mkdir -p /opt/loantracker/api
tar xzf /tmp/api.tar.gz -C /opt/loantracker/api
chmod +x /opt/loantracker/api/LoanTracker.Api
rm -rf /var/www/html/loantracker && mkdir -p /var/www/html/loantracker
tar xzf /tmp/web.tar.gz -C /var/www/html/loantracker
chown -R www-data:www-data /opt/loantracker /var/www/html/loantracker
systemctl restart loantracker-api
sleep 4
echo "service: $(systemctl is-active loantracker-api)"
code=$(curl -s -o /dev/null -w '%{http_code}' -X POST http://127.0.0.1:5090/api/auth/login -H 'Content-Type: application/json' -d '{"username":"x","password":"y"}')
echo "auth endpoint reachable (expect 401): HTTP $code"
rm -f /tmp/api.tar.gz /tmp/web.tar.gz
REMOTE

echo "==> Done. https://motiwala.pratishthabridal.com"
