Add-Type -AssemblyName System.Drawing
$base = Join-Path (Split-Path $PSScriptRoot -Parent) 'Assets\PKBall'
$out = Join-Path $base 'Resources\Art'
$vector = Join-Path $base 'Art\Characters'
New-Item -ItemType Directory -Force -Path $out,$vector | Out-Null
$styles = @(
    @{id='player'; body='#20b4cb'; rim='#baf5f8'; eyes='#103b54'; mark='#ffffff'; shape='player'},
    @{id='rival'; body='#ee7655'; rim='#ffd0aa'; eyes='#5a243c'; mark='#fff3d9'; shape='rival'},
    @{id='monster'; body='#8064bd'; rim='#cdb9f1'; eyes='#311b59'; mark='#e8f7a4'; shape='monster'}
)
foreach($style in $styles){
    $bmp = New-Object System.Drawing.Bitmap 256,256
    $g=[System.Drawing.Graphics]::FromImage($bmp)
    $g.SmoothingMode=[System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $g.Clear([System.Drawing.Color]::Transparent)
    $body=New-Object System.Drawing.SolidBrush ([System.Drawing.ColorTranslator]::FromHtml($style.body))
    $rim=New-Object System.Drawing.Pen ([System.Drawing.ColorTranslator]::FromHtml($style.rim)), 12
    $dark=New-Object System.Drawing.SolidBrush ([System.Drawing.ColorTranslator]::FromHtml($style.eyes))
    $mark=New-Object System.Drawing.SolidBrush ([System.Drawing.ColorTranslator]::FromHtml($style.mark))
    $svgExtras=''
    if($style.shape -eq 'monster'){
        $left=[System.Drawing.Point[]]@((New-Object System.Drawing.Point(45,83)),(New-Object System.Drawing.Point(38,27)),(New-Object System.Drawing.Point(94,55)))
        $right=[System.Drawing.Point[]]@((New-Object System.Drawing.Point(162,55)),(New-Object System.Drawing.Point(218,27)),(New-Object System.Drawing.Point(211,83)))
        $g.FillPolygon($body,$left);$g.FillPolygon($body,$right)
        $g.DrawPolygon($rim,$left);$g.DrawPolygon($rim,$right)
        $svgExtras='<path d="M45 83 38 27 94 55M162 55 218 27 211 83" fill="none" stroke="'+$style.rim+'" stroke-width="10"/>'
    }
    $g.FillEllipse($body,30,30,196,196)
    $g.DrawEllipse($rim,30,30,196,196)
    if($style.shape -eq 'player'){
        $g.FillEllipse($dark,80,113,22,29);$g.FillEllipse($dark,154,113,22,29)
        $g.FillPolygon($mark,[System.Drawing.Point[]]@((New-Object System.Drawing.Point(128,52)),(New-Object System.Drawing.Point(104,88)),(New-Object System.Drawing.Point(152,88))))
        $svgExtras='<circle cx="91" cy="128" r="12" fill="'+$style.eyes+'"/><circle cx="165" cy="128" r="12" fill="'+$style.eyes+'"/><path d="M128 52 104 88 152 88Z" fill="white"/>'
    }elseif($style.shape -eq 'rival'){
        $g.FillRectangle($dark,75,113,31,23);$g.FillRectangle($dark,150,113,31,23)
        $g.FillPolygon($mark,[System.Drawing.Point[]]@((New-Object System.Drawing.Point(128,49)),(New-Object System.Drawing.Point(111,85)),(New-Object System.Drawing.Point(145,85))))
        $svgExtras='<rect x="75" y="113" width="31" height="23" fill="'+$style.eyes+'"/><rect x="150" y="113" width="31" height="23" fill="'+$style.eyes+'"/><path d="M128 49 111 85 145 85Z" fill="'+$style.mark+'"/>'
    }else{
        $g.FillEllipse($dark,72,109,36,40);$g.FillEllipse($dark,148,109,36,40)
        $g.FillPolygon($mark,[System.Drawing.Point[]]@((New-Object System.Drawing.Point(128,54)),(New-Object System.Drawing.Point(100,90)),(New-Object System.Drawing.Point(156,90))))
        $svgExtras+='<ellipse cx="90" cy="129" rx="18" ry="20" fill="'+$style.eyes+'"/><ellipse cx="166" cy="129" rx="18" ry="20" fill="'+$style.eyes+'"/><path d="M128 54 100 90 156 90Z" fill="'+$style.mark+'"/>'
    }
    $pen=New-Object System.Drawing.Pen ([System.Drawing.ColorTranslator]::FromHtml($style.eyes)), 7
    $g.DrawArc($pen,91,130,74,47,15,150)
    $bmp.Save((Join-Path $out ($style.id+'.png')),[System.Drawing.Imaging.ImageFormat]::Png)
    $svg='<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 256 256"><circle cx="128" cy="128" r="98" fill="'+$style.body+'" stroke="'+$style.rim+'" stroke-width="12"/>'+$svgExtras+'<path d="M92 166 Q128 193 164 166" fill="none" stroke="'+$style.eyes+'" stroke-width="7" stroke-linecap="round"/></svg>'
    [IO.File]::WriteAllText((Join-Path $vector ($style.id+'.svg')),$svg,[Text.Encoding]::UTF8)
    $pen.Dispose();$body.Dispose();$rim.Dispose();$dark.Dispose();$mark.Dispose();$g.Dispose();$bmp.Dispose()
}
