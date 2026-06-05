$ErrorActionPreference = "Stop"

$root = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)
& (Join-Path $root "build.ps1")
& (Join-Path $root "scripts\smoke-test.ps1")

$exe = Join-Path $root "bin\CodexMaintenance.exe"
$guiExe = Join-Path $root "bin\CodexMaintenanceGui.exe"
$versionLine = & $exe --version
if (-not ($versionLine -match "(\d+\.\d+\.\d+)")) {
    throw "Could not determine version from: $versionLine"
}
$version = $Matches[1]

$artifacts = Join-Path $root "artifacts"
$staging = Join-Path $artifacts "CodexMaintenance"
$zip = Join-Path $artifacts ("CodexMaintenance-v" + $version + ".zip")

if (Test-Path -LiteralPath $staging) { Remove-Item -LiteralPath $staging -Recurse -Force }
New-Item -ItemType Directory -Force -Path $staging | Out-Null

$files = @(
    ".gitattributes",
    ".gitignore",
    "build.ps1",
    "CHANGELOG.md",
    "CodexMaintenance.config.example",
    "CodexMaintenance-menu.cmd",
    "CONTRIBUTING.md",
    "LICENSE",
    "README.md",
    "README.zh-CN.md",
    "SECURITY.md"
)
foreach ($file in $files) {
    Copy-Item -LiteralPath (Join-Path $root $file) -Destination (Join-Path $staging $file) -Force
}
Copy-Item -LiteralPath $exe -Destination (Join-Path $staging "CodexMaintenance.exe") -Force
Copy-Item -LiteralPath $guiExe -Destination (Join-Path $staging "CodexMaintenanceGui.exe") -Force
Copy-Item -LiteralPath (Join-Path $root "assets") -Destination (Join-Path $staging "assets") -Recurse -Force
Copy-Item -LiteralPath (Join-Path $root "src") -Destination (Join-Path $staging "src") -Recurse -Force
Copy-Item -LiteralPath (Join-Path $root "docs") -Destination (Join-Path $staging "docs") -Recurse -Force
Copy-Item -LiteralPath (Join-Path $root "scripts") -Destination (Join-Path $staging "scripts") -Recurse -Force

if (Test-Path -LiteralPath $zip) { Remove-Item -LiteralPath $zip -Force }
Compress-Archive -Path $staging -DestinationPath $zip -Force
Write-Host "Packaged: $zip"
