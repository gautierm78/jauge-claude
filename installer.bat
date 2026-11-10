@echo off
chcp 65001 >nul
setlocal
rem Installe la Jauge Claude dans %LOCALAPPDATA%\JaugeClaude (fonctionne même si ce fichier est seul).
set "SOURCE=%~dp0"
set "DEST=%LOCALAPPDATA%\JaugeClaude"
set "SCRIPT=%DEST%\jauge_claude.pyw"
set "URL=https://raw.githubusercontent.com/gautierm78/jauge-claude/application-autonome/jauge_claude.pyw"

echo === Jauge Claude : installation de l'application ===
echo.

where pythonw >nul 2>nul
if errorlevel 1 (
  echo Python n'est pas installe.
  echo Installez-le depuis le Microsoft Store ^(rechercher "Python 3.12"^) puis relancez ce fichier.
  pause
  exit /b 1
)

if not exist "%DEST%" mkdir "%DEST%"

echo Arret d'une ancienne version eventuelle...
powershell -NoProfile -Command "Get-CimInstance Win32_Process -Filter \"Name='pythonw.exe'\" | Where-Object { $_.CommandLine -like '*jauge_claude.pyw*' } | ForEach-Object { Stop-Process -Id $_.ProcessId -Force }" >nul 2>nul

if exist "%SOURCE%jauge_claude.pyw" (
  echo Copie du programme...
  copy /y "%SOURCE%jauge_claude.pyw" "%SCRIPT%" >nul
) else (
  echo Telechargement du programme...
  powershell -NoProfile -Command "Invoke-WebRequest -UseBasicParsing '%URL%' -OutFile '%SCRIPT%'"
)
if not exist "%SCRIPT%" (
  echo Impossible d'obtenir jauge_claude.pyw. Verifiez la connexion internet.
  pause
  exit /b 1
)

echo Installation des composants pystray, pillow et pywebview ^(1 a 2 minutes^)...
python -m pip install --user --disable-pip-version-check --no-warn-script-location --progress-bar on pystray pillow pywebview
if errorlevel 1 (
  echo L'installation des composants a echoue.
  pause
  exit /b 1
)

echo Ajout au demarrage de Windows...
powershell -NoProfile -Command ^
  "$s=(New-Object -ComObject WScript.Shell).CreateShortcut([Environment]::GetFolderPath('Startup')+'\Jauge Claude.lnk');" ^
  "$s.TargetPath=(Get-Command pythonw).Source; $s.Arguments='\"%SCRIPT%\"'; $s.WorkingDirectory='%DEST%'; $s.Save()"

echo Verification du demarrage...
python -c "import pystray, PIL, webview" 2>"%DEST%\erreur.txt"
if errorlevel 1 (
  echo Un composant ne se charge pas :
  type "%DEST%\erreur.txt"
  pause
  exit /b 1
)

echo Lancement...
start "" pythonw "%SCRIPT%"

echo.
echo Termine. Une pastille apparait pres de l'horloge
echo ^(cliquez sur la fleche ^^ si elle est masquee^).
echo Au premier lancement, une fenetre claude.ai s'ouvre : connectez-vous une fois,
echo puis fermez-la. Ensuite tout est automatique.
echo En cas de probleme, le detail est dans %DEST%\erreur.txt
pause
