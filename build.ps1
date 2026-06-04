$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$src = Join-Path $root "src\CodexMaintenance.cs"
$outDir = Join-Path $root "bin"
$out = Join-Path $outDir "CodexMaintenance.exe"

$candidates = @(
    "$env:WINDIR\Microsoft.NET\Framework64\v4.0.30319\csc.exe",
    "$env:WINDIR\Microsoft.NET\Framework\v4.0.30319\csc.exe"
)

$csc = $candidates | Where-Object { Test-Path -LiteralPath $_ } | Select-Object -First 1
if (-not $csc) {
    throw "csc.exe was not found. Install .NET Framework developer tools or add csc.exe to PATH."
}

New-Item -ItemType Directory -Force -Path $outDir | Out-Null

& $csc /nologo /optimize+ /target:exe /out:$out $src
if ($LASTEXITCODE -ne 0) {
    throw "Build failed."
}

Write-Host "Built: $out"
