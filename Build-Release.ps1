[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

if ($env:OS -ne "Windows_NT") {
    throw "Ten skrypt buduje aplikację Windows i musi zostać uruchomiony w systemie Windows."
}

$projectDirectory = Split-Path -Parent $MyInvocation.MyCommand.Path
$buildDirectory = Join-Path $projectDirectory "build"
$frameworkRoots = @(
    (Join-Path $env:WINDIR "Microsoft.NET\Framework64\v4.0.30319"),
    (Join-Path $env:WINDIR "Microsoft.NET\Framework\v4.0.30319")
)
$compiler = $null
foreach ($frameworkRoot in $frameworkRoots) {
    $candidate = Join-Path $frameworkRoot "csc.exe"
    if (Test-Path -LiteralPath $candidate) {
        $compiler = $candidate
        break
    }
}
if (-not $compiler) {
    throw "Nie znaleziono kompilatora .NET Framework 4.x (csc.exe)."
}

New-Item -ItemType Directory -Path $buildDirectory -Force | Out-Null

$applicationOutput = Join-Path $buildDirectory "PlaylistaMP3.exe"
$setupOutput = Join-Path $buildDirectory "Playlista_MP3_Setup.exe"
$rootSetupOutput = Join-Path $projectDirectory "Playlista_MP3_Setup.exe"

$applicationArguments = @(
    "/nologo",
    "/target:winexe",
    "/platform:x64",
    "/optimize+",
    "/win32icon:$projectDirectory\PlaylistaMP3.ico",
    "/win32manifest:$projectDirectory\PlaylistaMP3.app.manifest",
    "/out:$applicationOutput",
    "/reference:System.dll",
    "/reference:System.Core.dll",
    "/reference:System.Drawing.dll",
    "/reference:System.Windows.Forms.dll",
    "/reference:System.Configuration.dll",
    (Join-Path $projectDirectory "PlaylistaMP3.cs"),
    (Join-Path $projectDirectory "PlaylistaMP3.ModernUI.cs")
)

Write-Host "Budowanie aplikacji Playlista Media..." -ForegroundColor Cyan
& $compiler @applicationArguments
if ($LASTEXITCODE -ne 0) {
    throw "Budowanie aplikacji nie powiodło się (kod $LASTEXITCODE)."
}

Write-Host "Test uruchomieniowy aplikacji..." -ForegroundColor Cyan
$selfTest = Start-Process -FilePath $applicationOutput -ArgumentList "/self-test" -Wait -PassThru
if ($selfTest.ExitCode -ne 0) {
    $startupLog = Join-Path $env:LOCALAPPDATA "PlaylistaMP3\logs\startup.log"
    throw "Test uruchomieniowy aplikacji nie powiódł się (kod $($selfTest.ExitCode)). Dziennik: $startupLog"
}

$setupArguments = @(
    "/nologo",
    "/target:winexe",
    "/platform:x64",
    "/optimize+",
    "/win32icon:$projectDirectory\PlaylistaMP3.ico",
    "/win32manifest:$projectDirectory\PlaylistaMP3.Setup.manifest",
    "/out:$setupOutput",
    "/reference:System.dll",
    "/reference:System.Core.dll",
    "/reference:System.Drawing.dll",
    "/reference:System.Windows.Forms.dll",
    "/reference:System.IO.Compression.dll",
    "/reference:System.IO.Compression.FileSystem.dll",
    "/resource:$applicationOutput,PlaylistaMP3.Payload.exe",
    "/resource:$projectDirectory\README.md,PlaylistaMP3.Readme.md",
    "/resource:$projectDirectory\LICENSE.txt,PlaylistaMP3.License.txt",
    "/resource:$projectDirectory\THIRD_PARTY_NOTICES.md,PlaylistaMP3.ThirdParty.md",
    (Join-Path $projectDirectory "PlaylistaMP3.Setup.cs")
)

Write-Host "Budowanie instalatora..." -ForegroundColor Cyan
& $compiler @setupArguments
if ($LASTEXITCODE -ne 0) {
    throw "Budowanie instalatora nie powiodło się (kod $LASTEXITCODE)."
}

Write-Host "Test uruchomieniowy instalatora..." -ForegroundColor Cyan
$setupSelfTest = Start-Process -FilePath $setupOutput -ArgumentList "/self-test" -Wait -PassThru
if ($setupSelfTest.ExitCode -ne 0) {
    $installerLog = Join-Path $env:LOCALAPPDATA "PlaylistaMP3\logs\installer.log"
    throw "Test uruchomieniowy instalatora nie powiódł się (kod $($setupSelfTest.ExitCode)). Dziennik: $installerLog"
}

Copy-Item -LiteralPath $setupOutput -Destination $rootSetupOutput -Force
$hash = (Get-FileHash -Algorithm SHA256 -LiteralPath $rootSetupOutput).Hash.ToLowerInvariant()

Write-Host "Gotowe: $rootSetupOutput" -ForegroundColor Green
Write-Host "SHA-256: $hash" -ForegroundColor Green
