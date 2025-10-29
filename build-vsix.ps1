# Sqlx VSIX Build Script
# Build VSIX extension and copy to root directory

param(
    [Parameter(Mandatory=$false)]
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"

# Color output function
function Write-ColorOutput($ForegroundColor) {
    $fc = $host.UI.RawUI.ForegroundColor
    $host.UI.RawUI.ForegroundColor = $ForegroundColor
    if ($args) {
        Write-Output $args
    }
    $host.UI.RawUI.ForegroundColor = $fc
}

Write-ColorOutput Cyan "==========================="
Write-ColorOutput Cyan "  Sqlx VSIX Build Script"
Write-ColorOutput Cyan "==========================="

# 1. Check environment
Write-Host "`n[1/7] Checking build environment..." -ForegroundColor Yellow

$vsPath = "C:\Program Files\Microsoft Visual Studio\2022\Community"
$msbuild = "$vsPath\MSBuild\Current\Bin\MSBuild.exe"

if (-not (Test-Path $msbuild)) {
    # Try Professional
    $vsPath = "C:\Program Files\Microsoft Visual Studio\2022\Professional"
    $msbuild = "$vsPath\MSBuild\Current\Bin\MSBuild.exe"
    
    if (-not (Test-Path $msbuild)) {
        # Try Enterprise
        $vsPath = "C:\Program Files\Microsoft Visual Studio\2022\Enterprise"
        $msbuild = "$vsPath\MSBuild\Current\Bin\MSBuild.exe"
        
        if (-not (Test-Path $msbuild)) {
            Write-ColorOutput Red "[ERROR] MSBuild.exe not found"
            Write-Host ""
            Write-Host "Please ensure Visual Studio 2022 is installed"
            Write-Host "with 'Visual Studio extension development' workload"
            exit 1
        }
    }
}

Write-ColorOutput Green "[OK] Found MSBuild: $msbuild"

# 2. Navigate to project directory
$projectDir = Join-Path $PSScriptRoot "src\Sqlx.Extension"

if (-not (Test-Path $projectDir)) {
    Write-ColorOutput Red "[ERROR] Project directory not found: $projectDir"
    exit 1
}

Set-Location $projectDir
Write-ColorOutput Green "[OK] Project directory: $projectDir"

# 3. Check project file
$projectFile = "Sqlx.Extension.csproj"
if (-not (Test-Path $projectFile)) {
    Write-ColorOutput Red "[ERROR] Project file not found: $projectFile"
    exit 1
}

Write-ColorOutput Green "[OK] Project file: $projectFile"

# 4. Clean old build output
Write-Host "`n[2/7] Cleaning old build output..." -ForegroundColor Yellow

if (Test-Path "bin") {
    Remove-Item "bin" -Recurse -Force -ErrorAction SilentlyContinue
    Write-ColorOutput Green "[OK] Cleaned bin directory"
}

if (Test-Path "obj") {
    Remove-Item "obj" -Recurse -Force -ErrorAction SilentlyContinue
    Write-ColorOutput Green "[OK] Cleaned obj directory"
}

# 5. Restore NuGet packages
Write-Host "`n[3/7] Restoring NuGet packages..." -ForegroundColor Yellow

$restoreOutput = dotnet restore 2>&1 | Out-String

if ($LASTEXITCODE -ne 0) {
    Write-ColorOutput Red "[ERROR] NuGet restore failed"
    Write-Host $restoreOutput
    exit 1
}

Write-ColorOutput Green "[OK] NuGet packages restored"

# 6. Build project
Write-Host "`n[4/7] Building project ($Configuration configuration)..." -ForegroundColor Yellow
Write-Host ""

$buildArgs = @(
    $projectFile,
    "/t:Rebuild",
    "/p:Configuration=$Configuration",
    "/v:minimal",
    "/nologo"
)

Write-ColorOutput Gray "Command: MSBuild $($buildArgs -join ' ')"
Write-Host ""

$buildOutput = & $msbuild $buildArgs 2>&1

if ($LASTEXITCODE -ne 0) {
    Write-ColorOutput Red "`n[ERROR] Build failed!"
    Write-Host ""
    Write-ColorOutput Yellow "Error details:"
    Write-Host $buildOutput
    Set-Location $PSScriptRoot
    exit 1
}

Write-ColorOutput Green "`n[OK] Build succeeded!"

# 7. Check generated VSIX file
Write-Host "`n[5/7] Checking generated files..." -ForegroundColor Yellow

$vsixPath = "bin\$Configuration\Sqlx.Extension.vsix"

if (-not (Test-Path $vsixPath)) {
    Write-ColorOutput Red "[ERROR] VSIX file not generated"
    Write-Host "Expected location: $vsixPath"
    Set-Location $PSScriptRoot
    exit 1
}

$vsixFile = Get-Item $vsixPath
$vsixSizeMB = [math]::Round($vsixFile.Length / 1MB, 2)

Write-ColorOutput Green "[OK] VSIX file generated"
Write-Host "   Location: $vsixPath"
Write-Host "   Size: $vsixSizeMB MB"
Write-Host "   Time: $($vsixFile.LastWriteTime)"

# 8. Copy VSIX to root directory
Write-Host "`n[6/7] Copying VSIX to root directory..." -ForegroundColor Yellow

$rootDir = Split-Path (Split-Path $projectDir -Parent) -Parent
$outputFileName = "Sqlx.Extension-v0.1.0-$Configuration.vsix"
$outputPath = Join-Path $rootDir $outputFileName

# Remove if exists
if (Test-Path $outputPath) {
    Remove-Item $outputPath -Force
}

Copy-Item $vsixPath $outputPath -Force

if (Test-Path $outputPath) {
    Write-ColorOutput Green "[OK] VSIX copied to root directory"
    Write-Host "   Location: $outputPath"
    Write-Host "   Filename: $outputFileName"
} else {
    Write-ColorOutput Red "[ERROR] Copy failed"
    Set-Location $PSScriptRoot
    exit 1
}

# 9. Generate checksum
Write-Host "`n[7/7] Generating checksum..." -ForegroundColor Yellow

$hash = Get-FileHash -Path $outputPath -Algorithm SHA256
Write-Host "   SHA256: $($hash.Hash)"

$hashFile = "$outputPath.sha256"
$hash.Hash | Out-File -FilePath $hashFile -Encoding ASCII -NoNewline
Write-ColorOutput Green "   [OK] Checksum saved: $hashFile"

# 10. Summary
Write-Host ""
Write-ColorOutput Cyan "==========================="
Write-ColorOutput Green "  BUILD COMPLETED!"
Write-ColorOutput Cyan "==========================="

Write-Host ""
Write-Host "VSIX File Information:"
Write-Host "   Filename: $outputFileName"
Write-Host "   Location: $outputPath"
Write-Host "   Size: $vsixSizeMB MB"
Write-Host "   Config: $Configuration"
Write-Host ""

Write-Host "Next Steps:"
Write-Host "   1. Install: Double-click $outputFileName" -ForegroundColor Cyan
Write-Host "   2. Test: Press F5 in Visual Studio" -ForegroundColor Cyan
Write-Host "   3. Publish: Upload to Marketplace" -ForegroundColor Cyan
Write-Host ""

Write-ColorOutput Green "[SUCCESS] All steps completed!"

# Return to original directory
Set-Location $PSScriptRoot
