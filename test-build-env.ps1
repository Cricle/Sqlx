# Simple environment test script

Write-Host "=== Testing Build Environment ===" -ForegroundColor Cyan
Write-Host ""

# Test 1: PowerShell Version
Write-Host "1. PowerShell Version:" -ForegroundColor Yellow
$PSVersionTable.PSVersion
Write-Host ""

# Test 2: Visual Studio 2022
Write-Host "2. Visual Studio 2022:" -ForegroundColor Yellow
$vsPaths = @(
    "C:\Program Files\Microsoft Visual Studio\2022\Community",
    "C:\Program Files\Microsoft Visual Studio\2022\Professional",
    "C:\Program Files\Microsoft Visual Studio\2022\Enterprise"
)

$found = $false
foreach ($path in $vsPaths) {
    if (Test-Path $path) {
        Write-Host "   [OK] Found: $path" -ForegroundColor Green
        $msbuild = Join-Path $path "MSBuild\Current\Bin\MSBuild.exe"
        if (Test-Path $msbuild) {
            Write-Host "   [OK] MSBuild: $msbuild" -ForegroundColor Green
            $found = $true
        }
    }
}

if (-not $found) {
    Write-Host "   [ERROR] Visual Studio 2022 not found" -ForegroundColor Red
}
Write-Host ""

# Test 3: Project Files
Write-Host "3. Project Files:" -ForegroundColor Yellow
$projectDir = "src\Sqlx.Extension"
if (Test-Path $projectDir) {
    Write-Host "   [OK] Project directory exists" -ForegroundColor Green
    $projectFile = Join-Path $projectDir "Sqlx.Extension.csproj"
    if (Test-Path $projectFile) {
        Write-Host "   [OK] Project file exists" -ForegroundColor Green
    } else {
        Write-Host "   [ERROR] Project file not found" -ForegroundColor Red
    }
} else {
    Write-Host "   [ERROR] Project directory not found" -ForegroundColor Red
}
Write-Host ""

# Test 4: dotnet CLI
Write-Host "4. .NET CLI:" -ForegroundColor Yellow
try {
    $dotnetVersion = dotnet --version 2>&1
    Write-Host "   [OK] dotnet version: $dotnetVersion" -ForegroundColor Green
} catch {
    Write-Host "   [ERROR] dotnet not found" -ForegroundColor Red
}
Write-Host ""

Write-Host "=== Test Complete ===" -ForegroundColor Cyan
Write-Host ""
Write-Host "If you see any [ERROR] messages above, those need to be fixed first."

