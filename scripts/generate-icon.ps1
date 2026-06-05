$ErrorActionPreference = "Stop"

Add-Type -AssemblyName System.Drawing

$root = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)
$assets = Join-Path $root "assets"
New-Item -ItemType Directory -Force -Path $assets | Out-Null

function New-RoundedRectPath([float]$x, [float]$y, [float]$w, [float]$h, [float]$r) {
    $path = New-Object System.Drawing.Drawing2D.GraphicsPath
    $d = $r * 2
    $path.AddArc($x, $y, $d, $d, 180, 90)
    $path.AddArc($x + $w - $d, $y, $d, $d, 270, 90)
    $path.AddArc($x + $w - $d, $y + $h - $d, $d, $d, 0, 90)
    $path.AddArc($x, $y + $h - $d, $d, $d, 90, 90)
    $path.CloseFigure()
    return $path
}

function New-IconPng([int]$size, [string]$path) {
    $bmp = New-Object System.Drawing.Bitmap $size, $size, ([System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $g.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
    $g.Clear([System.Drawing.Color]::Transparent)

    $scale = $size / 256.0
    $rect = New-Object System.Drawing.RectangleF (12*$scale), (12*$scale), (232*$scale), (232*$scale)
    $bgPath = New-RoundedRectPath $rect.X $rect.Y $rect.Width $rect.Height (48*$scale)
    $bgBrush = New-Object System.Drawing.Drawing2D.LinearGradientBrush $rect, ([System.Drawing.Color]::FromArgb(255, 37, 99, 235)), ([System.Drawing.Color]::FromArgb(255, 124, 58, 237)), 45
    $g.FillPath($bgBrush, $bgPath)

    $glowBrush = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(48, 255, 255, 255))
    $g.FillEllipse($glowBrush, 38*$scale, 28*$scale, 142*$scale, 94*$scale)

    $shadowBrush = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(70, 15, 23, 42))
    $g.FillEllipse($shadowBrush, 62*$scale, 188*$scale, 132*$scale, 24*$scale)

    # Database cylinder.
    $dbBody = New-Object System.Drawing.RectangleF (63*$scale), (82*$scale), (96*$scale), (92*$scale)
    $dbBrush = New-Object System.Drawing.Drawing2D.LinearGradientBrush $dbBody, ([System.Drawing.Color]::FromArgb(255, 239, 246, 255)), ([System.Drawing.Color]::FromArgb(255, 191, 219, 254)), 90
    $dbPen = New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(255, 219, 234, 254)), (5*$scale)
    $g.FillRectangle($dbBrush, $dbBody)
    $g.DrawRectangle($dbPen, $dbBody.X, $dbBody.Y, $dbBody.Width, $dbBody.Height)
    $topRect = New-Object System.Drawing.RectangleF (63*$scale), (64*$scale), (96*$scale), (36*$scale)
    $bottomRect = New-Object System.Drawing.RectangleF (63*$scale), (156*$scale), (96*$scale), (36*$scale)
    $g.FillEllipse($dbBrush, $topRect)
    $g.DrawEllipse($dbPen, $topRect)
    $g.FillEllipse((New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(255, 147, 197, 253))), $bottomRect)
    $g.DrawEllipse($dbPen, $bottomRect)

    $linePen = New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(255, 96, 165, 250)), (5*$scale)
    $g.DrawArc($linePen, 63*$scale, 104*$scale, 96*$scale, 36*$scale, 0, 180)
    $g.DrawArc($linePen, 63*$scale, 130*$scale, 96*$scale, 36*$scale, 0, 180)

    # Broom handle.
    $handlePen = New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(255, 255, 255, 255)), (13*$scale)
    $handlePen.StartCap = [System.Drawing.Drawing2D.LineCap]::Round
    $handlePen.EndCap = [System.Drawing.Drawing2D.LineCap]::Round
    $g.DrawLine($handlePen, 147*$scale, 65*$scale, 194*$scale, 174*$scale)

    $handleGlowPen = New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(150, 191, 219, 254)), (5*$scale)
    $handleGlowPen.StartCap = [System.Drawing.Drawing2D.LineCap]::Round
    $handleGlowPen.EndCap = [System.Drawing.Drawing2D.LineCap]::Round
    $g.DrawLine($handleGlowPen, 149*$scale, 67*$scale, 191*$scale, 168*$scale)

    # Broom head.
    $broomPath = New-Object System.Drawing.Drawing2D.GraphicsPath
    $points = @(
        (New-Object System.Drawing.PointF (170*$scale), (153*$scale)),
        (New-Object System.Drawing.PointF (214*$scale), (169*$scale)),
        (New-Object System.Drawing.PointF (200*$scale), (211*$scale)),
        (New-Object System.Drawing.PointF (157*$scale), (191*$scale))
    )
    $broomPath.AddPolygon($points)
    $broomBrush = New-Object System.Drawing.Drawing2D.LinearGradientBrush (New-Object System.Drawing.RectangleF (150*$scale), (150*$scale), (66*$scale), (64*$scale)), ([System.Drawing.Color]::FromArgb(255, 253, 224, 71)), ([System.Drawing.Color]::FromArgb(255, 245, 158, 11)), 90
    $g.FillPath($broomBrush, $broomPath)
    $g.DrawPath((New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(255, 255, 247, 237)), (4*$scale)), $broomPath)

    $bristlePen = New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(210, 146, 64, 14)), (3*$scale)
    foreach ($x in 166,178,190,202) {
        $g.DrawLine($bristlePen, $x*$scale, 172*$scale, ($x-10)*$scale, 197*$scale)
    }

    # Check mark.
    $checkPen = New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(255, 34, 197, 94)), (11*$scale)
    $checkPen.StartCap = [System.Drawing.Drawing2D.LineCap]::Round
    $checkPen.EndCap = [System.Drawing.Drawing2D.LineCap]::Round
    $g.DrawLines($checkPen, @(
        (New-Object System.Drawing.PointF (82*$scale), (143*$scale)),
        (New-Object System.Drawing.PointF (103*$scale), (164*$scale)),
        (New-Object System.Drawing.PointF (139*$scale), (120*$scale))
    ))

    # Sparkles.
    $sparkBrush = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(255, 255, 255, 255))
    foreach ($spark in @(@(55,55,8), @(199,58,6), @(211,105,5))) {
        $cx=$spark[0]*$scale; $cy=$spark[1]*$scale; $r=$spark[2]*$scale
        $g.FillEllipse($sparkBrush, $cx-$r, $cy-$r, $r*2, $r*2)
        $g.FillRectangle($sparkBrush, $cx-($r/3), $cy-($r*2.2), ($r*2/3), $r*4.4)
        $g.FillRectangle($sparkBrush, $cx-($r*2.2), $cy-($r/3), $r*4.4, ($r*2/3))
    }

    $g.Dispose()
    $bmp.Save($path, [System.Drawing.Imaging.ImageFormat]::Png)
    $bmp.Dispose()
}

function Write-Ico([string[]]$pngPaths, [string]$icoPath) {
    $entries = @()
    foreach ($pngPath in $pngPaths) {
        $bytes = [System.IO.File]::ReadAllBytes($pngPath)
        $image = [System.Drawing.Image]::FromFile($pngPath)
        $entries += [pscustomobject]@{ Width=$image.Width; Height=$image.Height; Bytes=$bytes }
        $image.Dispose()
    }

    $stream = New-Object System.IO.FileStream($icoPath, [System.IO.FileMode]::Create, [System.IO.FileAccess]::Write)
    try {
        $writer = New-Object System.IO.BinaryWriter($stream)
        $writer.Write([UInt16]0)
        $writer.Write([UInt16]1)
        $writer.Write([UInt16]$entries.Count)
        $offset = 6 + (16 * $entries.Count)
        foreach ($entry in $entries) {
            $widthByte = if ($entry.Width -ge 256) { 0 } else { $entry.Width }
            $heightByte = if ($entry.Height -ge 256) { 0 } else { $entry.Height }
            $writer.Write([byte]$widthByte)
            $writer.Write([byte]$heightByte)
            $writer.Write([byte]0)
            $writer.Write([byte]0)
            $writer.Write([UInt16]1)
            $writer.Write([UInt16]32)
            $writer.Write([UInt32]$entry.Bytes.Length)
            $writer.Write([UInt32]$offset)
            $offset += $entry.Bytes.Length
        }
        foreach ($entry in $entries) {
            $writer.Write($entry.Bytes)
        }
        $writer.Dispose()
    }
    finally {
        $stream.Dispose()
    }
}

$sizes = @(256,128,64,48,32,16)
$pngs = @()
foreach ($size in $sizes) {
    $png = Join-Path $assets ("CodexMaintenance-{0}.png" -f $size)
    New-IconPng $size $png
    $pngs += $png
}
Write-Ico $pngs (Join-Path $assets "CodexMaintenance.ico")
Write-Host "Generated icon: $(Join-Path $assets 'CodexMaintenance.ico')"

