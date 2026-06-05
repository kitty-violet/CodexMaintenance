$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$srcDir = Join-Path $root "src"
$cliSrc = Join-Path $srcDir "CodexMaintenance.cs"
$guiSrc = Join-Path $srcDir "CodexMaintenanceGui.cs"
$outDir = Join-Path $root "bin"
$cliOut = Join-Path $outDir "CodexMaintenance.exe"
$guiOut = Join-Path $outDir "CodexMaintenanceGui.exe"
$icon = Join-Path $root "assets\CodexMaintenance.ico"

$candidates = @(
    "$env:WINDIR\Microsoft.NET\Framework64\v4.0.30319\csc.exe",
    "$env:WINDIR\Microsoft.NET\Framework\v4.0.30319\csc.exe"
)

$csc = $candidates | Where-Object { Test-Path -LiteralPath $_ } | Select-Object -First 1
if (-not $csc) {
    throw "csc.exe was not found. Install .NET Framework developer tools or add csc.exe to PATH."
}

New-Item -ItemType Directory -Force -Path $outDir | Out-Null

& $csc /nologo /optimize+ /target:exe /out:$cliOut $cliSrc
if ($LASTEXITCODE -ne 0) {
    throw "CLI build failed."
}

if (-not (Test-Path -LiteralPath $icon)) {
    powershell.exe -NoProfile -File (Join-Path $root "scripts\generate-icon.ps1")
}

& $csc /nologo /optimize+ /target:winexe /reference:System.Windows.Forms.dll /reference:System.Drawing.dll /win32icon:$icon /out:$guiOut $guiSrc
if ($LASTEXITCODE -ne 0) {
    throw "GUI build failed."
}

Copy-Item -LiteralPath $cliOut -Destination (Join-Path $root "CodexMaintenance.exe") -Force
Copy-Item -LiteralPath $guiOut -Destination (Join-Path $root "CodexMaintenanceGui.exe") -Force

Write-Host "Built: $cliOut"
Write-Host "Built: $guiOut"

