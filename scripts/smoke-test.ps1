$ErrorActionPreference = "Stop"

$root = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)
$exe = Join-Path $root "bin\CodexMaintenance.exe"
$guiExe = Join-Path $root "bin\CodexMaintenanceGui.exe"
if (-not (Test-Path -LiteralPath $exe) -or -not (Test-Path -LiteralPath $guiExe)) {
    & (Join-Path $root "build.ps1")
}
if (-not (Test-Path -LiteralPath $guiExe)) {
    throw "GUI executable was not built: $guiExe"
}

$temp = Join-Path ([System.IO.Path]::GetTempPath()) ("codex-maintenance-smoke-" + [Guid]::NewGuid().ToString("N"))
$app = Join-Path $temp "app"
$otherCwd = Join-Path $temp "other-cwd"
$codexHome = Join-Path $temp "codexhome"
$backupRoot = Join-Path $temp "backups"
New-Item -ItemType Directory -Force -Path $app, $otherCwd, $codexHome, $backupRoot | Out-Null
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

    $configRelativeApp = Join-Path $temp "config-relative-app"
    $configRelativeBackup = Join-Path $configRelativeApp "backups"
    New-Item -ItemType Directory -Force -Path $configRelativeApp | Out-Null
    Copy-Item -LiteralPath $exe -Destination (Join-Path $configRelativeApp "CodexMaintenance.exe") -Force
    $config = @(
        "CodexHome=$codexHome",
        "BackupRoot=backups",
        "KeepBackups=2",
        "VacuumThresholdMb=20.0"
    )
    Set-Content -LiteralPath (Join-Path $configRelativeApp "CodexMaintenance.config") -Value $config -Encoding UTF8

    Push-Location $otherCwd
    try {
        $relativeOutput = & (Join-Path $configRelativeApp "CodexMaintenance.exe") --dry-run --no-pause 2>&1
        if ($LASTEXITCODE -ne 0) {
            throw "Config-relative dry run failed: $relativeOutput"
        }
        if (($relativeOutput -join "`n") -notmatch [Regex]::Escape($configRelativeBackup)) {
            throw "BackupRoot=backups did not resolve beside the config file."
        }
    }
    finally {
        Pop-Location
    }
}
finally {
    Pop-Location
    Remove-Item -LiteralPath $temp -Recurse -Force -ErrorAction SilentlyContinue
}

Write-Host "Smoke test passed."
