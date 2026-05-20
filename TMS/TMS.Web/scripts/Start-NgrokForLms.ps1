# Starts an ngrok tunnel to the local URLs defined in Properties/launchSettings.json (TMS.Web profile).
# Prerequisites: ngrok installed, `ngrok config add-authtoken <token>` done once, LMS running locally.
# Usage (from repo):  pwsh -File TMS.Web/scripts/Start-NgrokForLms.ps1
# Or:                 .\scripts\Start-NgrokForLms.ps1   (cwd = TMS.Web)

$ErrorActionPreference = "Stop"
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$launchPath = Join-Path $scriptDir "..\Properties\launchSettings.json" | Resolve-Path

$json = Get-Content -Raw $launchPath | ConvertFrom-Json
$applicationUrl = $json.profiles.'TMS.Web'.applicationUrl
if (-not $applicationUrl) {
    Write-Error "Could not read profiles.TMS.Web.applicationUrl from launchSettings.json"
}

$urls = $applicationUrl -split ';' | ForEach-Object { $_.Trim() } | Where-Object { $_ }
$httpsUrl = $urls | Where-Object { $_ -like 'https://*' } | Select-Object -First 1
$httpUrl = $urls | Where-Object { $_ -like 'http://*' } | Select-Object -First 1

$target = if ($httpsUrl) { $httpsUrl } else { $httpUrl }
Write-Host "Detected local LMS URL from launchSettings.json (TMS.Web profile): $target"
Write-Host "Starting ngrok (public URL will be HTTPS on the ngrok side)..."
Write-Host "Tip: after start, open http://127.0.0.1:4040 for the inspector and copy your https://*.ngrok-free.app URL."
Write-Host ""

$ngrok = Get-Command ngrok -ErrorAction SilentlyContinue
if (-not $ngrok) {
    Write-Error "ngrok not found on PATH. Install from https://ngrok.com/download and ensure ngrok is on PATH."
}

& ngrok http $target
