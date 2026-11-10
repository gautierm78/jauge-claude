# Construit JaugeClaude.msix (paquet pour le Microsoft Store) à partir de l'exe compilé.
# Utilisé par GitHub Actions ; peut aussi être lancé à la main sur un PC avec le SDK Windows.
param(
  [string]$Exe = "csharp\bin\Release\net48\JaugeClaude.exe",
  [string]$Version = "3.0.0",
  [string]$Nom = "JaugeClaude",
  [string]$Editeur = "CN=JaugeClaude",
  [string]$EditeurAffiche = "Jauge Claude",
  [string]$Sortie = "JaugeClaude.msix"
)
$ErrorActionPreference = "Stop"
Add-Type -AssemblyName System.Drawing

$dossier = Join-Path $env:RUNNER_TEMP "msix"
if (-not $env:RUNNER_TEMP) { $dossier = Join-Path $env:TEMP "msix" }
Remove-Item $dossier -Recurse -Force -ErrorAction SilentlyContinue
New-Item -ItemType Directory -Force "$dossier\Assets" | Out-Null
Copy-Item $Exe "$dossier\JaugeClaude.exe"

# Logos : la pastille verte aux coins arrondis, à chaque taille demandée par le Store.
function Logo([int]$taille, [string]$fichier) {
  $bmp = New-Object System.Drawing.Bitmap $taille, $taille
  $g = [System.Drawing.Graphics]::FromImage($bmp)
  $g.SmoothingMode = "AntiAlias"
  $m = [math]::Round($taille * 0.08); $c = $taille - 2 * $m; $r = $c * 0.28
  $p = New-Object System.Drawing.Drawing2D.GraphicsPath
  $p.AddArc($m, $m, 2*$r, 2*$r, 180, 90); $p.AddArc($m+$c-2*$r, $m, 2*$r, 2*$r, 270, 90)
  $p.AddArc($m+$c-2*$r, $m+$c-2*$r, 2*$r, 2*$r, 0, 90); $p.AddArc($m, $m+$c-2*$r, 2*$r, 2*$r, 90, 90)
  $p.CloseFigure()
  $g.FillPath((New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(46,158,91))), $p)
  $police = New-Object System.Drawing.Font "Arial", ($c * 0.42), ([System.Drawing.FontStyle]::Bold), ([System.Drawing.GraphicsUnit]::Pixel)
  $f = New-Object System.Drawing.StringFormat; $f.Alignment = "Center"; $f.LineAlignment = "Center"
  $g.DrawString("%", $police, [System.Drawing.Brushes]::White, (New-Object System.Drawing.RectangleF 0, 0, $taille, $taille), $f)
  $bmp.Save("$dossier\Assets\$fichier", [System.Drawing.Imaging.ImageFormat]::Png)
  $g.Dispose(); $bmp.Dispose()
}
Logo 50  "StoreLogo.png"
Logo 150 "Square150x150Logo.png"
Logo 44  "Square44x44Logo.png"
Logo 300 "StoreLogo300.png"

# Manifeste : remplace les {{...}}. Le Store exige une version en 4 nombres finissant par 0.
$v = ($Version -replace '^v', '') -replace '[^0-9.]', ''
$parts = @($v.Split('.') | Where-Object { $_ -ne '' }) + @('0','0','0')
$v4 = "$($parts[0]).$($parts[1]).$($parts[2]).0"
(Get-Content "store\AppxManifest.xml" -Raw -Encoding UTF8).
  Replace("{{NAME}}", $Nom).Replace("{{PUBLISHER}}", $Editeur).
  Replace("{{PUBLISHER_DISPLAY}}", $EditeurAffiche).Replace("{{VERSION}}", $v4) |
  Set-Content "$dossier\AppxManifest.xml" -Encoding UTF8
Remove-Item "$dossier\Assets\StoreLogo300.png"

$makeappx = Get-ChildItem "${env:ProgramFiles(x86)}\Windows Kits\10\bin\*\x64\makeappx.exe" |
  Sort-Object FullName -Descending | Select-Object -First 1
if (-not $makeappx) { throw "makeappx.exe introuvable (installez le SDK Windows)." }
& $makeappx.FullName pack /o /d $dossier /p $Sortie
if ($LASTEXITCODE -ne 0) { throw "makeappx a échoué." }
Write-Host "Paquet créé : $Sortie (version $v4)"
