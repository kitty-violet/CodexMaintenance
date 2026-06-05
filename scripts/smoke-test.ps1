$ErrorActionPreference = "Stop"

$root = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)
$exe = Join-Path $root "bin\CodexMaintenance.exe"
if (-not (Test-Path -LiteralPath $exe)) {
    & (Join-Path $root "build.ps1")
}

$temp = Join-Path ([System.IO.Path]::GetTempPath()) ("codex-maintenance-smoke-" + [Guid]::NewGuid().ToString("N"))
$app = Join-Path $temp "app"
$codexHome = Join-Path $temp "codexhome"
$backupRoot = Join-Path $temp "backups"
New-Item -ItemType Directory -Force -Path $app, $codexHome, $backupRoot | Out-Null
Copy-Item -LiteralPath $exe -Destination (Join-Path $app "CodexMaintenance.exe") -Force
Set-Content -LiteralPath (Join-Path $codexHome "config.toml") -Value "model = 'test'" -Encoding UTF8
Set-Content -LiteralPath (Join-Path $codexHome "auth.json") -Value "{}" -Encoding UTF8

Push-Location $app
try {
    $version = & .\CodexMaintenance.exe --version
    if ($LASTEXITCODE -ne 0 -or -not ($version -match "Codex Maintenance")) {
        throw "Version command failed: $version"
    }

    & .\CodexMaintenance.exe --dry-run --no-pause --codex-home $codexHome --backup-root $backupRoot --keep-backups 2
    if ($LASTEXITCODE -ne 0) {
        throw "Dry run failed."
    }
}
finally {
    Pop-Location
    Remove-Item -LiteralPath $temp -Recurse -Force -ErrorAction SilentlyContinue
}

Write-Host "Smoke test passed."
