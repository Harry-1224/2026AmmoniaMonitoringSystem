Add-Type -AssemblyName System.Drawing

$outDir = Join-Path (Get-Location) "output"
if (-not (Test-Path $outDir)) {
    New-Item -ItemType Directory -Path $outDir | Out-Null
}

$outPath = Join-Path $outDir "character_museum_cars_scene.png"
$w = 1024
$h = 768
$bmp = New-Object System.Drawing.Bitmap $w, $h
$g = [System.Drawing.Graphics]::FromImage($bmp)
$g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
$g.TextRenderingHint = [System.Drawing.Text.TextRenderingHint]::AntiAliasGridFit

function Brush([string]$hex) {
    return New-Object System.Drawing.SolidBrush ([System.Drawing.ColorTranslator]::FromHtml($hex))
}

function Pen([string]$hex, [float]$width = 1) {
    $p = New-Object System.Drawing.Pen ([System.Drawing.ColorTranslator]::FromHtml($hex), $width)
    $p.StartCap = [System.Drawing.Drawing2D.LineCap]::Round
    $p.EndCap = [System.Drawing.Drawing2D.LineCap]::Round
    return $p
}

function T([string]$codes) {
    $chars = @()
    foreach ($code in $codes.Split(" ", [System.StringSplitOptions]::RemoveEmptyEntries)) {
        $chars += [char]([Convert]::ToInt32($code, 16))
    }
    return -join $chars
}

function FillRoundRect($graphics, $brush, [float]$x, [float]$y, [float]$width, [float]$height, [float]$radius) {
    $path = New-Object System.Drawing.Drawing2D.GraphicsPath
    $d = $radius * 2
    $path.AddArc($x, $y, $d, $d, 180, 90)
    $path.AddArc($x + $width - $d, $y, $d, $d, 270, 90)
    $path.AddArc($x + $width - $d, $y + $height - $d, $d, $d, 0, 90)
    $path.AddArc($x, $y + $height - $d, $d, $d, 90, 90)
    $path.CloseFigure()
    $graphics.FillPath($brush, $path)
    $path.Dispose()
}

function DrawRoundRect($graphics, $pen, [float]$x, [float]$y, [float]$width, [float]$height, [float]$radius) {
    $path = New-Object System.Drawing.Drawing2D.GraphicsPath
    $d = $radius * 2
    $path.AddArc($x, $y, $d, $d, 180, 90)
    $path.AddArc($x + $width - $d, $y, $d, $d, 270, 90)
    $path.AddArc($x + $width - $d, $y + $height - $d, $d, $d, 0, 90)
    $path.AddArc($x, $y + $height - $d, $d, $d, 90, 90)
    $path.CloseFigure()
    $graphics.DrawPath($pen, $path)
    $path.Dispose()
}

function Poly($graphics, $brush, [int[][]]$pts) {
    $points = @()
    foreach ($p in $pts) {
        $points += New-Object System.Drawing.Point $p[0], $p[1]
    }
    $graphics.FillPolygon($brush, $points)
}

function DrawCar($graphics, [int]$x, [int]$y, [int]$scale, [string]$body, [string]$accent) {
    $s = $scale / 100.0
    $black = Brush "#171717"
    $darkPen = Pen "#202020" (3 * $s)
    $chrome = Brush "#d9e0df"
    $cream = Brush "#fff0c6"
    $glass = Brush "#9bd5f0"
    $shadow = Brush "#2f394080"

    $graphics.FillEllipse($shadow, $x + 10 * $s, $y + 106 * $s, 235 * $s, 35 * $s)
    FillRoundRect $graphics (Brush $body) ($x + 36 * $s) ($y + 60 * $s) (185 * $s) (56 * $s) (16 * $s)
    Poly $graphics (Brush $body) @(
        @(($x + 72 * $s), ($y + 61 * $s)),
        @(($x + 101 * $s), ($y + 26 * $s)),
        @(($x + 167 * $s), ($y + 28 * $s)),
        @(($x + 198 * $s), ($y + 64 * $s))
    )
    DrawRoundRect $graphics (Pen "#222222" (2 * $s)) ($x + 36 * $s) ($y + 60 * $s) (185 * $s) (56 * $s) (16 * $s)
    $graphics.DrawLine((Pen "#2b2b2b" (3 * $s)), $x + 123 * $s, $y + 32 * $s, $x + 123 * $s, $y + 86 * $s)
    FillRoundRect $graphics $glass ($x + 82 * $s) ($y + 34 * $s) (78 * $s) (32 * $s) (6 * $s)
    FillRoundRect $graphics $glass ($x + 164 * $s) ($y + 38 * $s) (30 * $s) (28 * $s) (5 * $s)
    $graphics.DrawRectangle((Pen "#222222" (2 * $s)), $x + 82 * $s, $y + 34 * $s, 78 * $s, 32 * $s)
    $graphics.DrawRectangle((Pen "#222222" (2 * $s)), $x + 164 * $s, $y + 38 * $s, 30 * $s, 28 * $s)
    FillRoundRect $graphics (Brush $accent) ($x + 48 * $s) ($y + 80 * $s) (78 * $s) (20 * $s) (8 * $s)
    $graphics.FillEllipse($black, $x + 48 * $s, $y + 94 * $s, 48 * $s, 48 * $s)
    $graphics.FillEllipse($chrome, $x + 58 * $s, $y + 104 * $s, 28 * $s, 28 * $s)
    $graphics.FillEllipse($black, $x + 172 * $s, $y + 94 * $s, 48 * $s, 48 * $s)
    $graphics.FillEllipse($chrome, $x + 182 * $s, $y + 104 * $s, 28 * $s, 28 * $s)
    $graphics.FillEllipse($cream, $x + 205 * $s, $y + 76 * $s, 18 * $s, 18 * $s)
    $graphics.FillEllipse($cream, $x + 30 * $s, $y + 77 * $s, 18 * $s, 18 * $s)
    $graphics.DrawLine($darkPen, $x + 18 * $s, $y + 113 * $s, $x + 235 * $s, $y + 113 * $s)
}

# Background: polished car museum gallery.
$g.Clear([System.Drawing.ColorTranslator]::FromHtml("#eef2f2"))
$g.FillRectangle((Brush "#d8e0e3"), 0, 0, $w, 188)
$g.FillRectangle((Brush "#f3f4f1"), 0, 188, $w, 345)
$floorPath = New-Object System.Drawing.Drawing2D.GraphicsPath
$floorPath.AddPolygon(@(
    (New-Object System.Drawing.Point 0, 484),
    (New-Object System.Drawing.Point $w, 450),
    (New-Object System.Drawing.Point $w, $h),
    (New-Object System.Drawing.Point 0, $h)
))
$g.FillPath((Brush "#8d9da2"), $floorPath)
$floorPath.Dispose()

for ($i = 0; $i -lt 11; $i++) {
    $x = $i * 103
    $g.DrawLine((Pen "#c3ccd0" 2), $x, 496, $x - 210, $h)
}
for ($i = 0; $i -lt 7; $i++) {
    $y = 520 + $i * 42
    $g.DrawLine((Pen "#a9b5b8" 1.4), 0, $y, $w, $y - 23)
}

# Ceiling ducts, red pipes, and lamps from the reference museum mood.
for ($i = 0; $i -lt 7; $i++) {
    $y = 30 + $i * 28
    $g.DrawLine((Pen "#26333b" 5), 0, $y, $w, $y + 14)
}
foreach ($y in @(70, 132)) {
    $g.DrawLine((Pen "#b91d29" 6), 0, $y, $w, $y - 12)
}
foreach ($y in @(44, 102)) {
    $g.DrawLine((Pen "#d6dadb" 20), -40, $y, $w + 60, $y + 26)
    $g.DrawLine((Pen "#f8faf9" 5), -40, $y - 5, $w + 60, $y + 21)
}
for ($i = 0; $i -lt 12; $i++) {
    $lx = 55 + $i * 86
    $ly = 88 + (($i % 2) * 54)
    $g.DrawLine((Pen "#2f3638" 2), $lx, $ly - 36, $lx, $ly)
    $lampPts = @(
        @(($lx - 14), $ly),
        @(($lx + 14), $ly),
        @(($lx + 6), ($ly + 16)),
        @(($lx - 6), ($ly + 16))
    )
    Poly $g (Brush "#202629") $lampPts
    $g.FillEllipse((Brush "#fff7c8"), $lx - 8, $ly + 12, 16, 8)
}

# Wall mural panels.
for ($i = 0; $i -lt 4; $i++) {
    $px = 328 + $i * 170
    FillRoundRect $g (Brush "#e4e7e4") $px 218 145 204 6
    $g.DrawLine((Pen "#b7c0c0" 2), $px + 22, 392, $px + 118, 248)
    $g.DrawLine((Pen "#c4cccc" 2), $px + 46, 392, $px + 132, 265)
    $g.DrawRectangle((Pen "#c5cccc" 1.5), $px, 218, 145, 204)
}

# Exhibition signage and posts.
FillRoundRect $g (Brush "#f7f7f4") 40 256 230 128 5
$fontSmall = New-Object System.Drawing.Font "Malgun Gothic", 12
$fontTiny = New-Object System.Drawing.Font "Malgun Gothic", 8
$g.DrawString((T "D074 B798 C2DD 0020 CC28 B7C9 0020 C804 C2DC"), (New-Object System.Drawing.Font "Malgun Gothic", 14, ([System.Drawing.FontStyle]::Bold)), (Brush "#313638"), 62, 278)
$g.DrawString((T "CD08 AE30 0020 C790 B3D9 CC28 C758 0020 D615 D0DC C640 0020 C0C9 C0C1 002C"), $fontSmall, (Brush "#5d6668"), 62, 310)
$g.DrawString((T "C55E CABD 0020 B77C B514 C5D0 C774 D130 C640 0020 B465 ADFC 0020 D39C B354 B97C 0020 C0B4 D3B4 BCF4 C138 C694 002E"), $fontSmall, (Brush "#5d6668"), 62, 333)

foreach ($p in @(@(190,477), @(340,462), @(500,452), @(687,448), @(850,444))) {
    FillRoundRect $g (Brush "#d4ece8") $p[0] $p[1] 40 88 4
    $g.DrawLine((Pen "#f5f6f2" 5), $p[0] + 20, $p[1], $p[0] + 20, $p[1] - 48)
}
$g.DrawCurve((Pen "#f6f1e9" 5), @(
    (New-Object System.Drawing.Point 180, 432),
    (New-Object System.Drawing.Point 330, 420),
    (New-Object System.Drawing.Point 510, 414),
    (New-Object System.Drawing.Point 695, 410),
    (New-Object System.Drawing.Point 870, 406)
))

# Cars in perspective.
DrawCar $g 94 350 72 "#111820" "#eac37f"
DrawCar $g 245 346 88 "#1592c8" "#f0c891"
DrawCar $g 450 334 98 "#c99d4c" "#e87932"
DrawCar $g 700 324 112 "#f3c273" "#e7a856"

# Soft floor reflections.
$reflectionBrush = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(34, 245, 251, 252))
foreach ($r in @(@(210,560,140,26), @(430,560,180,30), @(690,565,220,34), @(80,555,120,22))) {
    $g.FillEllipse($reflectionBrush, $r[0], $r[1], $r[2], $r[3])
}

# Chibi character body.
$outline = Pen "#20171d" 5
$bodyBrush = Brush "#fff9f5"
$pink = Brush "#f29ac1"
$pinkDark = Pen "#9f5078" 3
$eye = Brush "#b9a7f1"
$blush = Brush "#f6a0b5"
$g.FillEllipse((Brush "#3e4b4f55"), 64, 626, 275, 48)
FillRoundRect $g $bodyBrush 98 472 225 158 64
$g.DrawEllipse($outline, 117, 473, 192, 154)
$g.FillEllipse($bodyBrush, 78, 522, 58, 92)
$g.DrawEllipse($outline, 78, 522, 58, 92)
$g.FillEllipse($bodyBrush, 270, 526, 62, 94)
$g.DrawEllipse($outline, 270, 526, 62, 94)
$g.FillEllipse($bodyBrush, 117, 596, 54, 58)
$g.DrawEllipse($outline, 117, 596, 54, 58)
$g.FillEllipse($bodyBrush, 241, 592, 58, 62)
$g.DrawEllipse($outline, 241, 592, 58, 62)
$g.FillEllipse($bodyBrush, 79, 407, 185, 146)
$g.DrawEllipse($outline, 79, 407, 185, 146)

# Hair mass and locks.
Poly $g $pink @(@(84,455), @(108,389), @(177,365), @(246,397), @(267,470), @(238,440), @(228,507), @(191,463), @(166,523), @(137,461), @(108,503))
$g.DrawPolygon($pinkDark, @(
    (New-Object System.Drawing.Point 84,455),
    (New-Object System.Drawing.Point 108,389),
    (New-Object System.Drawing.Point 177,365),
    (New-Object System.Drawing.Point 246,397),
    (New-Object System.Drawing.Point 267,470),
    (New-Object System.Drawing.Point 238,440),
    (New-Object System.Drawing.Point 228,507),
    (New-Object System.Drawing.Point 191,463),
    (New-Object System.Drawing.Point 166,523),
    (New-Object System.Drawing.Point 137,461),
    (New-Object System.Drawing.Point 108,503)
))
foreach ($line in @(@(142,384,125,453), @(174,374,170,448), @(203,386,216,455), @(236,414,232,471))) {
    $g.DrawBezier((Pen "#9f5078" 2.5), $line[0], $line[1], $line[0]-8, $line[1]+24, $line[2]+8, $line[3]-20, $line[2], $line[3])
}

# Face.
$g.FillEllipse($eye, 119, 455, 38, 42)
$g.FillEllipse($eye, 188, 455, 38, 42)
$g.DrawEllipse((Pen "#3b304a" 3), 119, 455, 38, 42)
$g.DrawEllipse((Pen "#3b304a" 3), 188, 455, 38, 42)
$g.FillEllipse((Brush "#ffffff"), 130, 463, 10, 10)
$g.FillEllipse((Brush "#ffffff"), 199, 463, 10, 10)
$g.DrawArc((Pen "#202020" 4), 158, 493, 18, 18, 20, 140)
$g.DrawArc((Pen "#202020" 4), 176, 493, 18, 18, 20, 140)
$g.FillEllipse($blush, 102, 496, 28, 12)
$g.FillEllipse($blush, 220, 496, 28, 12)

# Hair rose and ribbon.
$g.FillEllipse((Brush "#ed83b1"), 232, 379, 54, 54)
$g.DrawEllipse((Pen "#783b5d" 3), 232, 379, 54, 54)
$g.DrawArc((Pen "#783b5d" 3), 244, 391, 30, 30, 10, 310)
$g.DrawLine((Pen "#783b5d" 3), 258, 382, 258, 432)
Poly $g (Brush "#f3e9f4") @(@(282,410), @(325,392), @(300,444))
Poly $g (Brush "#ffffff") @(@(283,411), @(331,436), @(298,449))
$g.DrawPolygon((Pen "#4d3547" 3), @((New-Object System.Drawing.Point 282,410),(New-Object System.Drawing.Point 325,392),(New-Object System.Drawing.Point 300,444)))
$g.DrawPolygon((Pen "#4d3547" 3), @((New-Object System.Drawing.Point 283,411),(New-Object System.Drawing.Point 331,436),(New-Object System.Drawing.Point 298,449)))

# Pointer and speech bubble.
$g.DrawLine((Pen "#282828" 5), 298, 482, 454, 424)
$g.FillEllipse((Brush "#282828"), 446, 418, 16, 16)
FillRoundRect $g (Brush "#ffffff") 382 228 397 112 20
DrawRoundRect $g (Pen "#2a2a2a" 3) 382 228 397 112 20
Poly $g (Brush "#ffffff") @(@(430,337), @(458,337), @(432,370))
$g.DrawLine((Pen "#2a2a2a" 3), 430, 337, 432, 370)
$g.DrawLine((Pen "#2a2a2a" 3), 458, 337, 432, 370)
$fontBubble = New-Object System.Drawing.Font "Malgun Gothic", 24, ([System.Drawing.FontStyle]::Bold)
$g.DrawString((T "C5EC AE30 0020 BC30 CE58 B41C 0020 CC28 B7C9 B4E4 C740"), $fontBubble, (Brush "#1f282c"), 413, 252)
$g.DrawString((T "CD08 AE30 0020 D074 B798 C2DD 0020 C790 B3D9 CC28 C608 C694 0021"), $fontBubble, (Brush "#1f282c"), 413, 292)
$g.DrawString((T "B77C B514 C5D0 C774 D130 002C 0020 B465 ADFC 0020 D39C B354 002C 0020 BAA9 C7AC 0020 C9C0 BD95 C744 0020 C0B4 D3B4 BCF4 C138 C694 002E"), $fontSmall, (Brush "#445057"), 425, 348)

# Small callout tags near cars.
foreach ($tag in @(@(285,315,(T "CCAD C0C9 0020 C138 B2E8")), @(510,304,(T "D669 B3D9 0020 C7A5 C2DD")), @(770,292,(T "BAA9 C7AC 0020 C9C0 BD95")))) {
    FillRoundRect $g (Brush "#ffffff") $tag[0] $tag[1] 86 28 6
    DrawRoundRect $g (Pen "#475156" 1.5) $tag[0] $tag[1] 86 28 6
    $tagText = $tag[2]
    $tagX = $tag[0] + 10
    $tagY = $tag[1] + 7
    $g.DrawString($tagText, $fontTiny, (Brush "#2f383b"), $tagX, $tagY)
}

$g.Dispose()
$bmp.Save($outPath, [System.Drawing.Imaging.ImageFormat]::Png)
$bmp.Dispose()
Write-Output $outPath
