Add-Type -AssemblyName System.Drawing
$root = Join-Path (Split-Path $PSScriptRoot -Parent) 'Assets\PKBall'
$vector = Join-Path $root 'Art\Vector'
$bitmap = Join-Path $root 'Resources\Icons'
New-Item -ItemType Directory -Force -Path $vector, $bitmap | Out-Null

$icons = @(
    @{ id='spear'; color='#f7bb59'; shape='<path d="M32 55 69 18 79 9 74 28 42 62"/><path d="M22 72 34 60 45 71 32 83Z"/>' },
    @{ id='crossbow'; color='#70d7f6'; shape='<path d="M16 32 Q50 8 84 32 L72 40 Q50 27 28 40Z"/><path d="M50 25V80M36 49H64M50 24 78 30"/>' },
    @{ id='shield'; color='#88bcff'; shape='<path d="M50 12 78 24 76 58 Q69 78 50 87 Q31 78 24 58 L22 24Z"/><path d="m37 50 10 11 18-24"/>' },
    @{ id='wheel'; color='#92edca'; shape='<circle cx="50" cy="50" r="31"/><circle cx="50" cy="50" r="10"/><path d="M50 19v21m0 20v21M19 50h21m20 0h21M28 28l15 15m14 14 15 15M72 28 57 43M43 57 28 72"/>' },
    @{ id='armor'; color='#b8c8e6'; shape='<path d="M30 18 42 11h16l12 7 13 16-12 13-6-7v42H35V40l-6 7-12-13Z"/><path d="M35 40h30M50 18v55"/>' },
    @{ id='ram'; color='#ff9a7b'; shape='<path d="M13 56 58 24 86 18 80 45 37 75Z"/><path d="M17 70 33 54l14 15-17 17Z"/>' },
    @{ id='injector'; color='#d6a8fa'; shape='<path d="M36 20 70 54 59 65 25 31Z"/><path d="m70 54 9 9M21 27l9-9m-4 44-12 12m31-31 12-12"/>' },
    @{ id='core'; color='#ffe47c'; shape='<path d="M50 10 74 27 83 55 63 82H37L17 55l9-28Z"/><circle cx="50" cy="50" r="16"/><path d="M50 34v32M34 50h32"/>' }
)

foreach ($icon in $icons) {
    $svg = @"
<svg xmlns="http://www.w3.org/2000/svg" width="100" height="100" viewBox="0 0 100 100">
  <rect width="100" height="100" rx="19" fill="#12232f"/>
  <circle cx="50" cy="50" r="43" fill="#1c3c4b" stroke="$($icon.color)" stroke-width="2"/>
  <g fill="none" stroke="$($icon.color)" stroke-width="6" stroke-linejoin="round" stroke-linecap="round">$($icon.shape)</g>
</svg>
"@
    [IO.File]::WriteAllText((Join-Path $vector ($icon.id + '.svg')), $svg, [Text.Encoding]::UTF8)
    $bmp = New-Object System.Drawing.Bitmap 256,256
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $g.ScaleTransform(2.56,2.56)
    $g.Clear([System.Drawing.ColorTranslator]::FromHtml('#12232f'))
    $brush = New-Object System.Drawing.SolidBrush ([System.Drawing.ColorTranslator]::FromHtml('#1c3c4b'))
    $pen = New-Object System.Drawing.Pen ([System.Drawing.ColorTranslator]::FromHtml($icon.color)), 5
    $pen.StartCap = $pen.EndCap = [System.Drawing.Drawing2D.LineCap]::Round
    $g.FillEllipse($brush,7,7,86,86)
    $g.DrawEllipse($pen,7,7,86,86)
    switch ($icon.id) {
        'spear' { $g.DrawLine($pen,26,75,75,19); $g.DrawLine($pen,75,19,68,39); $g.DrawLine($pen,26,75,40,65) }
        'crossbow' { $g.DrawArc($pen,17,17,66,35,180,180); $g.DrawLine($pen,50,28,50,82); $g.DrawLine($pen,29,40,71,40); $g.DrawLine($pen,50,28,79,31) }
        'shield' { $g.DrawLines($pen,[System.Drawing.Point[]]@((New-Object System.Drawing.Point(50,13)),(New-Object System.Drawing.Point(78,25)),(New-Object System.Drawing.Point(74,60)),(New-Object System.Drawing.Point(50,86)),(New-Object System.Drawing.Point(26,60)),(New-Object System.Drawing.Point(22,25)),(New-Object System.Drawing.Point(50,13)))); $g.DrawLines($pen,[System.Drawing.Point[]]@((New-Object System.Drawing.Point(35,49)),(New-Object System.Drawing.Point(46,61)),(New-Object System.Drawing.Point(66,36)))) }
        'wheel' { $g.DrawEllipse($pen,20,20,60,60); $g.DrawEllipse($pen,40,40,20,20); foreach($a in 0,45,90,135){$rad=$a*[Math]::PI/180;$dx=[Math]::Cos($rad)*29;$dy=[Math]::Sin($rad)*29;$g.DrawLine($pen,[float](50-$dx),[float](50-$dy),[float](50+$dx),[float](50+$dy))} }
        'armor' { $g.DrawLines($pen,[System.Drawing.Point[]]@((New-Object System.Drawing.Point(20,34)),(New-Object System.Drawing.Point(32,18)),(New-Object System.Drawing.Point(44,12)),(New-Object System.Drawing.Point(56,12)),(New-Object System.Drawing.Point(68,18)),(New-Object System.Drawing.Point(80,34)),(New-Object System.Drawing.Point(69,46)),(New-Object System.Drawing.Point(65,40)),(New-Object System.Drawing.Point(65,82)),(New-Object System.Drawing.Point(35,82)),(New-Object System.Drawing.Point(35,40)),(New-Object System.Drawing.Point(31,46)),(New-Object System.Drawing.Point(20,34))));$g.DrawLine($pen,50,18,50,72) }
        'ram' { $g.DrawLine($pen,17,67,82,20); $g.DrawLine($pen,82,20,75,44); $g.DrawLine($pen,75,44,32,78); $g.DrawLine($pen,17,67,32,78) }
        'injector' { $g.DrawRectangle($pen,31,29,38,29);$g.DrawLine($pen,21,18,79,76);$g.DrawLine($pen,16,77,31,62);$g.DrawLine($pen,70,55,83,68) }
        'core' { $g.DrawEllipse($pen,27,27,46,46);$g.DrawEllipse($pen,39,39,22,22);$g.DrawLine($pen,50,17,50,28);$g.DrawLine($pen,50,72,50,83);$g.DrawLine($pen,17,50,28,50);$g.DrawLine($pen,72,50,83,50) }
    }
    $bmp.Save((Join-Path $bitmap ($icon.id + '.png')),[System.Drawing.Imaging.ImageFormat]::Png)
    $pen.Dispose(); $brush.Dispose(); $g.Dispose(); $bmp.Dispose()
}
