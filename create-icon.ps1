Add-Type -AssemblyName System.Drawing

function New-DevPadBitmap([int]$size) {
    $bmp = New-Object System.Drawing.Bitmap($size, $size, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $g   = [System.Drawing.Graphics]::FromImage($bmp)
    $g.SmoothingMode      = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $g.TextRenderingHint  = [System.Drawing.Text.TextRenderingHint]::AntiAliasGridFit
    $g.InterpolationMode  = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
    $g.Clear([System.Drawing.Color]::Transparent)

    # Rounded-rectangle background  (#0D6EFD bright blue)
    $bg   = [System.Drawing.Color]::FromArgb(255, 13, 110, 253)
    $bgBr = New-Object System.Drawing.SolidBrush($bg)
    $r    = [int]($size * 0.18)
    $path = New-Object System.Drawing.Drawing2D.GraphicsPath
    $path.AddArc(0,          0,          $r*2, $r*2, 180, 90)
    $path.AddArc($size-$r*2, 0,          $r*2, $r*2, 270, 90)
    $path.AddArc($size-$r*2, $size-$r*2, $r*2, $r*2, 0,   90)
    $path.AddArc(0,          $size-$r*2, $r*2, $r*2, 90,  90)
    $path.CloseFigure()
    $g.FillPath($bgBr, $path)

    # Draw "</>" centred  (VS Code keyword blue)
    $fontSize = [float]($size * 0.31)
    $font     = New-Object System.Drawing.Font("Consolas", $fontSize, [System.Drawing.FontStyle]::Bold, [System.Drawing.GraphicsUnit]::Pixel)
    $textBr   = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(255, 255, 255, 255))
    $sf       = New-Object System.Drawing.StringFormat
    $sf.Alignment     = [System.Drawing.StringAlignment]::Center
    $sf.LineAlignment = [System.Drawing.StringAlignment]::Center
    $g.DrawString("</>", $font, $textBr, [System.Drawing.RectangleF]::new(0, 0, $size, $size), $sf)

    $font.Dispose(); $textBr.Dispose(); $bgBr.Dispose(); $path.Dispose(); $g.Dispose()
    return $bmp
}

# Build PNG streams at 4 resolutions
$sizes   = @(256, 48, 32, 16)
$streams = foreach ($s in $sizes) {
    $ms  = New-Object System.IO.MemoryStream
    $bmp = New-DevPadBitmap $s
    $bmp.Save($ms, [System.Drawing.Imaging.ImageFormat]::Png)
    $bmp.Dispose()
    $ms
}

# Write ICO file  (ICONDIR + ICONDIRENTRYs + PNG blobs)
$out    = "C:\Users\Deniz Nuran\DevPad\Assets\devpad.ico"
$stream = [System.IO.File]::Create($out)
$w      = New-Object System.IO.BinaryWriter($stream)

# ICONDIR header
$w.Write([uint16]0)              # reserved
$w.Write([uint16]1)              # type = ICO
$w.Write([uint16]$sizes.Count)  # image count

# ICONDIRENTRYs  (16 bytes each)
$offset = 6 + 16 * $sizes.Count
for ($i = 0; $i -lt $sizes.Count; $i++) {
    $s    = $sizes[$i]
    $data = $streams[$i].ToArray()
    $dim  = if ($s -eq 256) { 0 } else { $s }   # 0 encodes 256 in ICO spec
    $w.Write([byte]$dim)        # width
    $w.Write([byte]$dim)        # height
    $w.Write([byte]0)           # palette colours
    $w.Write([byte]0)           # reserved
    $w.Write([uint16]1)         # colour planes
    $w.Write([uint16]32)        # bits per pixel
    $w.Write([uint32]$data.Length)
    $w.Write([uint32]$offset)
    $offset += $data.Length
}

# Image data
foreach ($ms in $streams) { $w.Write($ms.ToArray()) }
$w.Close(); $stream.Close()

Write-Host "Created: $out"
