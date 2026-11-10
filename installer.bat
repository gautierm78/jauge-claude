@echo off
chcp 65001 >nul
setlocal
set "DOSSIER=%~dp0"
set "SCRIPT=%DOSSIER%jauge_claude.pyw"

echo === Jauge Claude : installation de l'application ===
echo.

where pythonw >nul 2>nul
if errorlevel 1 (
  echo Python n'est pas installe.
  echo Installez-le depuis le Microsoft Store ^(rechercher "Python 3.12"^) puis relancez ce fichier.
  pause
  exit /b 1
)

echo Installation des composants pystray, pillow et pywebview...
python -m pip install --user --quiet --disable-pip-version-check --no-warn-script-location pystray pillow pywebview
if errorlevel 1 (
  echo L'installation des composants a echoue.
  pause
  exit /b 1
)

echo Ajout au demarrage de Windows...
powershell -NoProfile -Command ^
  "$s=(New-Object -ComObject WScript.Shell).CreateShortcut([Environment]::GetFolderPath('Startup')+'\Jauge Claude.lnk');" ^
  "$s.TargetPath=(Get-Command pythonw).Source; $s.Arguments='\"%SCRIPT%\"'; $s.WorkingDirectory='%DOSSIER%'; $s.Save()"

echo Lancement...
start "" pythonw "%SCRIPT%"

echo.
echo Termine. Une pastille apparait pres de l'horloge
echo ^(cliquez sur la fleche ^ si elle est masquee^).
echo Au premier lancement, une fenetre claude.ai s'ouvre : connectez-vous une fois,
echo puis fermez-la. Ensuite tout est automatique.
pause
