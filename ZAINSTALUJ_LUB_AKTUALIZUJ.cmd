@echo off
title Playlista Media - instalacja
cd /d "%~dp0"
if exist "%~dp0Playlista_MP3_Setup.exe" (
    start "" "%~dp0Playlista_MP3_Setup.exe"
    exit /b 0
)
echo Nie znaleziono Playlista_MP3_Setup.exe.
echo Pobierz instalator z sekcji Releases albo uruchom Build-Release.ps1.
echo.
pause
