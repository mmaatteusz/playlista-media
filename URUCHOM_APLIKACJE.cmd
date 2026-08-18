@echo off
cd /d "%~dp0"
if exist "%LOCALAPPDATA%\Programs\Playlista Media\PlaylistaMP3.exe" (
    start "" "%LOCALAPPDATA%\Programs\Playlista Media\PlaylistaMP3.exe"
    exit /b 0
)
if exist "%LOCALAPPDATA%\Programs\Playlista MP3\PlaylistaMP3.exe" (
    start "" "%LOCALAPPDATA%\Programs\Playlista MP3\PlaylistaMP3.exe"
    exit /b 0
)
if not exist "%~dp0PlaylistaMP3.exe" (
    echo Aplikacja nie jest jeszcze zainstalowana. Uruchamiam instalator...
    if exist "%~dp0Playlista_MP3_Setup.exe" (
        start "" "%~dp0Playlista_MP3_Setup.exe"
        exit /b 0
    )
    echo Nie znaleziono instalatora. Pobierz go z sekcji Releases.
    pause
    exit /b
)
start "" "%~dp0PlaylistaMP3.exe"
