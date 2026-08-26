# Build and compile Inno Setup installer for Bible Study App
param(
    [string]$Target = "win-arm64"
)

Write-Host "=======================================================" -ForegroundColor Cyan
Write-Host "  Bible Study App - Compile & Package Installer Wizard " -ForegroundColor Cyan
Write-Host "  Waitaminute Digital (Native ARM64 / x64 Windows)     " -ForegroundColor Cyan
Write-Host "=======================================================" -ForegroundColor Cyan
Write-Host ""

Write-Host "[1/3] Publishing .NET MAUI Release Bundle for $Target..." -ForegroundColor Yellow
dotnet publish LumenScriptura.csproj `
    -c Release `
    -f net10.0-windows10.0.19041.0 `
    -r $Target `
    --self-contained true `
    -p:UseMonoRuntime=false `
    -p:WindowsPackageType=MSIX `
    -p:GenerateAppxPackageOnBuild=true `
    -p:AppxPackageSigningEnabled=false `
    -o "./artifacts/$Target"

if ($LASTEXITCODE -ne 0) {
    Write-Error "Dotnet publish failed with exit code $LASTEXITCODE"
    exit $LASTEXITCODE
}

Write-Host ""
Write-Host "[2/3] Compiling Inno Setup Installer..." -ForegroundColor Yellow

$iscc = Get-Command iscc -ErrorAction SilentlyContinue
if (-not $iscc) {
    $candidates = @(
        "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe",
        "${env:ProgramFiles}\Inno Setup 6\ISCC.exe",
        "${env:LOCALAPPDATA}\Programs\Inno Setup 6\ISCC.exe"
    )
    foreach ($cand in $candidates) {
        if (Test-Path $cand) {
            $iscc = $cand
            break
        }
    }
}

if ($iscc) {
    & $iscc installer.iss
    if ($LASTEXITCODE -eq 0) {
        Write-Host ""
        Write-Host "[3/3] Build Complete!" -ForegroundColor Green
        Write-Host "Installer created at: ./artifacts/installer/BibleStudyApp-Setup.exe" -ForegroundColor Green
    }
} else {
    Write-Warning "Inno Setup compiler (iscc.exe) was not found in PATH or standard Program Files."
    Write-Host "To install Inno Setup, run: choco install innosetup -y (or winget install JRSoftware.InnoSetup)"
    Write-Host "Your raw binaries are compiled and ready in: ./artifacts/$Target" -ForegroundColor Cyan
}
