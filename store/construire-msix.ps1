# Construit JaugeClaude.msix (paquet pour le Microsoft Store) à partir de l'exe compilé.
# Utilisé par GitHub Actions ; peut aussi être lancé à la main sur un PC avec le SDK Windows.
param(
  [string]$Exe = "csharp\bin\Release\net48\JaugeClaude.exe",
  [string]$Version = "3.0.0",
  [string]$Nom = "JaugeClaude",
  [string]$Editeur = "CN=JaugeClaude",
  [string]$EditeurAffiche = "Jauge Claude",
  [string]$NomAffiche = "",
  [string]$Sortie = "JaugeClaude.msix"
)
$ErrorActionPreference = "Stop"
# Nom réservé dans l'Espace partenaires, avec l'apostrophe typographique (U+2019).
# Écrit avec [char] pour ne pas dépendre de l'encodage du fichier ou de la ligne de commande.
if (-not $NomAffiche) { $NomAffiche = "Jauge d" + [char]0x2019 + "utilisation pour Claude" }
Add-Type -AssemblyName System.Drawing

$dossier = Join-Path $env:RUNNER_TEMP "msix"
if (-not $env:RUNNER_TEMP) { $dossier = Join-Path $env:TEMP "msix" }
Remove-Item $dossier -Recurse -Force -ErrorAction SilentlyContinue
New-Item -ItemType Directory -Force "$dossier\Assets" | Out-Null
Copy-Item $Exe "$dossier\JaugeClaude.exe"

# Logos : l'étoile arrondie verte de l'application (même forme que dans Program.cs), sans chiffre.
function Logo([int]$taille, [string]$fichier) {
  $bmp = New-Object System.Drawing.Bitmap $taille, $taille
  $g = [System.Drawing.Graphics]::FromImage($bmp)
  $g.SmoothingMode = "AntiAlias"
  $k = $taille / 24.0; $f = 0.62
  function P($x, $y) { New-Object System.Drawing.PointF ([single]($x * $k)), ([single]($y * $k)) }
  function C($x, $y) { P (12 + ($x - 12) * $f) (12 + ($y - 12) * $f) }
  $e = New-Object System.Drawing.Drawing2D.GraphicsPath
  $e.AddBezier((P 12 0), (C 0 0), (C 0 0), (P 0 12))
  $e.AddBezier((P 0 12), (C 0 24), (C 0 24), (P 12 24))
  $e.AddBezier((P 12 24), (C 24 24), (C 24 24), (P 24 12))
  $e.AddBezier((P 24 12), (C 24 0), (C 24 0), (P 12 0))
  $e.CloseFigure()
  $g.FillPath((New-Object System.Drawing.SolidBrush ([System.Drawing.ColorTranslator]::FromHtml("#22C55E"))), $e)
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
  Replace("{{PUBLISHER_DISPLAY}}", $EditeurAffiche).Replace("{{DISPLAY_NAME}}", [System.Security.SecurityElement]::Escape($NomAffiche)).Replace("{{VERSION}}", $v4) |
  Set-Content "$dossier\AppxManifest.xml" -Encoding UTF8
Remove-Item "$dossier\Assets\StoreLogo300.png"

$makeappx = Get-ChildItem "${env:ProgramFiles(x86)}\Windows Kits\10\bin\*\x64\makeappx.exe" |
  Sort-Object FullName -Descending | Select-Object -First 1
if (-not $makeappx) { throw "makeappx.exe introuvable (installez le SDK Windows)." }
& $makeappx.FullName pack /o /d $dossier /p $Sortie
if ($LASTEXITCODE -ne 0) { throw "makeappx a échoué." }
Write-Host "Paquet créé : $Sortie (version $v4)"
