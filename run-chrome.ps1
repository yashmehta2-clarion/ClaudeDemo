<#
  Builds and runs UserManagement.Web, then opens it in Google Chrome.
  Usage (from the repository root):  powershell -ExecutionPolicy Bypass -File .\run-chrome.ps1
#>
$ErrorActionPreference = 'Stop'
$project = Join-Path $PSScriptRoot 'src\UserManagement.Web\UserManagement.Web.csproj'
$url     = 'https://localhost:7180/Account/Login'

# Trust the ASP.NET Core development HTTPS certificate (prompts only the first time).
dotnet dev-certs https --trust | Out-Null

dotnet build $project -c Debug
if ($LASTEXITCODE -ne 0) { throw 'Build failed.' }

$app = Start-Process dotnet -ArgumentList @('run', '--project', "`"$project`"", '--no-build', '--launch-profile', 'https', '--no-launch-browser') -PassThru -NoNewWindow

# Wait until the site answers, then open Chrome.
for ($i = 0; $i -lt 60; $i++) {
    try { Invoke-WebRequest $url -UseBasicParsing -TimeoutSec 2 | Out-Null; break } catch { Start-Sleep -Seconds 1 }
}

$chromeCandidates = @(
    "$env:ProgramFiles\Google\Chrome\Application\chrome.exe",
    "${env:ProgramFiles(x86)}\Google\Chrome\Application\chrome.exe",
    "$env:LocalAppData\Google\Chrome\Application\chrome.exe"
)
$chrome = $chromeCandidates | Where-Object { $_ -and (Test-Path $_) } | Select-Object -First 1
if ($chrome) { Start-Process $chrome $url } else { Write-Warning "Google Chrome not found; open $url manually."; Start-Process $url }

Write-Host "App running (PID $($app.Id)). Press Ctrl+C to stop."
Wait-Process -Id $app.Id
