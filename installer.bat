@echo off
chcp 65001 >nul
setlocal
set "DOSSIER=%~dp0"
set "SCRIPT=%DOSSIER%jauge_claude.pyw"

echo === Jauge Claude : installation de l'icone Windows ===
echo.

where pythonw >nul 2>nul
if errorlevel 1 (
  echo Python n'est pas installe.
  echo Installez-le depuis le Microsoft Store ^(rechercher "Python 3.12"^) puis relancez ce fichier.
  pause
  exit /b 1
)

echo Installation des composants pystray et pillow...
python -m pip install --user --quiet --disable-pip-version-check pystray pillow
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
echo Termine. Une pastille grise "?" apparait pres de l'horloge
echo ^(cliquez sur la fleche ^ si elle est masquee^).
echo Elle prend ses couleurs des que la jauge Tampermonkey v2.1 envoie les chiffres.
pause
