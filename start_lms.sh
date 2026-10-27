#!/bin/bash

# ==========================================================
# LMS EduDigi - Platform Startup Script
# ==========================================================

export PATH=/Users/sandeepkumar/local_env/dotnet:$PATH
export DOTNET_ROLL_FORWARD=Major
export ASPNETCORE_ENVIRONMENT=Development
export CI=true

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
cd "$SCRIPT_DIR"

mkdir -p "$SCRIPT_DIR/logs"

PORT=5022

echo "🔄 Checking if LMS is already running on port $PORT..."
PID=$(lsof -ti:$PORT 2>/dev/null || true)
if [ -n "$PID" ]; then
    echo "Stopping existing instance on port $PORT (PID: $PID)..."
    kill -9 $PID 2>/dev/null || true
    sleep 1
fi

echo "📦 Pre-building LMS Solution..."
dotnet build TMS/TMS.sln -v q --nologo

echo "🚀 Starting LMS EduDigi Web on http://localhost:$PORT..."
nohup dotnet run --no-build --project TMS/TMS.Web/TMS.Web.csproj --urls "http://localhost:$PORT" > logs/lms.log 2>&1 &

echo "⏳ Waiting for LMS to initialize..."
sleep 5

if lsof -i :$PORT >/dev/null 2>&1; then
    echo "=========================================================="
    echo "✅ LMS EduDigi is running at: http://localhost:$PORT"
    echo "=========================================================="
    lsof -i :$PORT
else
    echo "⚠️ Service didn't start within 5 seconds. Check logs/lms.log for details."
fi
