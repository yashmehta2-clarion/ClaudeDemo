#!/usr/bin/env bash
# Builds and runs UserManagement.Web, then opens it in Google Chrome (macOS / Linux).
set -euo pipefail
cd "$(dirname "$0")"
PROJECT=src/UserManagement.Web/UserManagement.Web.csproj
URL=https://localhost:7180/Account/Login

dotnet dev-certs https --trust >/dev/null 2>&1 || true
dotnet build "$PROJECT" -c Debug
dotnet run --project "$PROJECT" --no-build --launch-profile https --no-launch-browser &
APP_PID=$!
trap 'kill $APP_PID 2>/dev/null || true' EXIT

for _ in $(seq 1 60); do curl -ks -o /dev/null "$URL" && break; sleep 1; done

if [[ "$(uname)" == "Darwin" ]]; then
  open -a "Google Chrome" "$URL"
elif command -v google-chrome >/dev/null; then
  google-chrome "$URL" >/dev/null 2>&1 &
else
  echo "Google Chrome not found; open $URL manually."
fi
wait $APP_PID
