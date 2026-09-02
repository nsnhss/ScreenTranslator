# 生成应用图标：蓝色圆角方块 + 白色"译"字
Add-Type -AssemblyName System.Drawing

$size = 256
$bmp = New-Object System.Drawing.Bitmap($size, $size)
$g = [System.Drawing.Graphics]::FromImage($bmp)
$g.SmoothingMode = 'AntiAlias'
$g.TextRenderingHint = 'AntiAlias'

# 圆角矩形路径
$radius = 48
$path = New-Object System.Drawing.Drawing2D.GraphicsPath
$d = $radius * 2
$path.AddArc(0, 0, $d, $d, 180, 90)
$path.AddArc($size - $d, 0, $d, $d, 270, 90)
$path.AddArc($size - $d, $size - $d, $d, $d, 0, 90)
$path.AddArc(0, $size - $d, $d, $d, 90, 90)
$path.CloseFigure()

# 蓝色渐变背景
$rect = New-Object System.Drawing.Rectangle(0, 0, $size, $size)
$brush = New-Object System.Drawing.Drawing2D.LinearGradientBrush(
    $rect,
    [System.Drawing.Color]::FromArgb(0x42, 0xA5, 0xF5),
    [System.Drawing.Color]::FromArgb(0x1E, 0x63, 0xD0),
    45)
$g.FillPath($brush, $path)

# 白色"译"字
$font = New-Object System.Drawing.Font('Microsoft YaHei', 120, [System.Drawing.FontStyle]::Bold)
$textBrush = [System.Drawing.Brushes]::White
$format = New-Object System.Drawing.StringFormat
$format.Alignment = 'Center'
$format.LineAlignment = 'Center'
$rectF = New-Object System.Drawing.RectangleF(0, 0, $size, $size)
$g.DrawString("译", $font, $textBrush, $rectF, $format)

$pngPath = "D:\translate\ScreenTranslator\Assets\app.png"
New-Item -ItemType Directory -Force -Path (Split-Path $pngPath) | Out-Null
$bmp.Save($pngPath, 'Png')
$g.Dispose(); $bmp.Dispose()

# PNG → ICO（Vista+ 支持 ICO 内嵌 PNG）
$pngBytes = [System.IO.File]::ReadAllBytes($pngPath)
$icoPath = "D:\translate\ScreenTranslator\Assets\app.ico"
$fs = [System.IO.File]::Create($icoPath)
$bw = New-Object System.IO.BinaryWriter($fs)
$bw.Write([uint16]0)            # reserved
$bw.Write([uint16]1)            # type: icon
$bw.Write([uint16]1)            # count
$bw.Write([byte]0)              # width: 0 = 256
$bw.Write([byte]0)              # height: 0 = 256
$bw.Write([byte]0)              # colors
$bw.Write([byte]0)              # reserved
$bw.Write([uint16]1)            # planes
$bw.Write([uint16]32)           # bit count
$bw.Write([uint32]$pngBytes.Length)
$bw.Write([uint32]22)           # offset
$bw.Write($pngBytes)
$bw.Close(); $fs.Close()

Write-Output "图标已生成: $icoPath ($($pngBytes.Length) bytes)"
