@echo off
title Playlista Media - aktualizacja narzedzi
cd /d "%~dp0"
if exist "%~dp0Playlista_MP3_Setup.exe" (
    start "" "%~dp0Playlista_MP3_Setup.exe" /tools
    exit /b 0
)
echo Nie znaleziono Playlista_MP3_Setup.exe.
echo Uruchom ponownie pelny instalator Playlista Media.
echo.
pause
