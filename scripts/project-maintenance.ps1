# Sqlx Project Maintenance Script
# For common project maintenance tasks

param(
    [Parameter()]
    [ValidateSet("test", "build", "clean", "restore", "full-test", "performance", "help")]
    [string]$Action = "help"
)

function Write-Header {
    param([string]$Title)
    Write-Host ""
    Write-Host ">> $Title" -ForegroundColor Green
    Write-Host ("=" * ($Title.Length + 3)) -ForegroundColor Green
}

function Write-Step {
    param([string]$Step)
    Write-Host "-> $Step" -ForegroundColor Yellow
}

function Write-Success {
    param([string]$Message)
    Write-Host "[OK] $Message" -ForegroundColor Green
}

function Write-Error {
    param([string]$Message)
    Write-Host "[ERROR] $Message" -ForegroundColor Red
}

function Test-Command {
    param([string]$Command)
    try {
        & $Command --version > $null 2>&1
        return $true
    }
    catch {
        return $false
    }
}

function Show-Help {
    Write-Header "Sqlx Project Maintenance Script"
    Write-Host ""
    Write-Host "Usage: .\project-maintenance.ps1 -Action <action>" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "Available actions:" -ForegroundColor Yellow
    Write-Host "  build        - Build entire solution" -ForegroundColor White
    Write-Host "  test         - Run core unit tests" -ForegroundColor White
    Write-Host "  full-test    - Run all tests (unit+integration+performance)" -ForegroundColor White
    Write-Host "  performance  - Run performance benchmarks" -ForegroundColor White
    Write-Host "  clean        - Clean build outputs" -ForegroundColor White
    Write-Host "  restore      - Restore NuGet packages" -ForegroundColor White
    Write-Host "  help         - Show this help information" -ForegroundColor White
    Write-Host ""
    Write-Host "Examples:" -ForegroundColor Cyan
    Write-Host "  .\project-maintenance.ps1 -Action build" -ForegroundColor White
    Write-Host "  .\project-maintenance.ps1 -Action full-test" -ForegroundColor White
}

function Invoke-Build {
    Write-Header "Building Solution"
    
    Write-Step "Checking .NET SDK..."
    if (-not (Test-Command "dotnet")) {
        Write-Error ".NET SDK not installed or not in PATH"
        return $false
    }
    
    Write-Step "Restoring NuGet packages..."
    dotnet restore
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Package restore failed"
        return $false
    }
    
    Write-Step "Building solution..."
    dotnet build --configuration Release --no-restore
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Build failed"
        return $false
    }
    
    Write-Success "Build completed"
    return $true
}

function Invoke-Test {
    Write-Header "运行核心测试"
    
    Write-Step "运行 Sqlx.Tests..."
    dotnet test tests/Sqlx.Tests --configuration Release --no-build --logger:console --verbosity minimal --filter "TestCategory!=Integration"
    
    if ($LASTEXITCODE -eq 0) {
        Write-Success "核心测试通过"
        return $true
    } else {
        Write-Error "核心测试失败"
        return $false
    }
}

function Invoke-FullTest {
    Write-Header "运行完整测试套件"
    
    $allPassed = $true
    
    Write-Step "1/4 运行核心单元测试..."
    dotnet test tests/Sqlx.Tests --configuration Release --no-build --logger:console --verbosity minimal --filter "TestCategory!=Integration"
    if ($LASTEXITCODE -ne 0) {
        Write-Error "核心测试失败"
        $allPassed = $false
    } else {
        Write-Success "核心测试通过"
    }
    
    Write-Step "2/4 运行集成测试..."
    dotnet test tests/Sqlx.IntegrationTests --configuration Release --no-build --logger:console --verbosity minimal
    if ($LASTEXITCODE -ne 0) {
        Write-Error "集成测试失败"
        $allPassed = $false
    } else {
        Write-Success "集成测试通过"
    }
    
    Write-Step "3/4 运行性能测试..."
    dotnet test tests/Sqlx.PerformanceTests --configuration Release --no-build --logger:console --verbosity minimal
    if ($LASTEXITCODE -ne 0) {
        Write-Error "性能测试失败"
        $allPassed = $false
    } else {
        Write-Success "性能测试通过"
    }
    
    Write-Step "4/4 运行示例测试..."
    dotnet test samples/RepositoryExample.Tests --configuration Release --no-build --logger:console --verbosity minimal
    if ($LASTEXITCODE -ne 0) {
        Write-Error "示例测试失败"
        $allPassed = $false
    } else {
        Write-Success "示例测试通过"
    }
    
    if ($allPassed) {
        Write-Success "🎉 所有测试通过！"
    } else {
        Write-Error "❌ 部分测试失败"
    }
    
    return $allPassed
}

function Invoke-Performance {
    Write-Header "运行性能基准测试"
    
    Write-Step "构建性能基准项目..."
    dotnet build samples/PerformanceBenchmark --configuration Release
    if ($LASTEXITCODE -ne 0) {
        Write-Error "性能基准构建失败"
        return $false
    }
    
    Write-Step "运行性能基准测试..."
    dotnet run --project samples/PerformanceBenchmark --configuration Release
    
    Write-Success "性能基准测试完成"
    return $true
}

function Invoke-Clean {
    Write-Header "清理构建输出"
    
    Write-Step "清理解决方案..."
    dotnet clean
    
    Write-Step "删除 bin 和 obj 目录..."
    Get-ChildItem -Path . -Recurse -Directory -Name "bin" | Remove-Item -Recurse -Force
    Get-ChildItem -Path . -Recurse -Directory -Name "obj" | Remove-Item -Recurse -Force
    
    Write-Step "删除测试结果..."
    if (Test-Path "TestResults") {
        Remove-Item "TestResults" -Recurse -Force
    }
    
    Write-Success "清理完成"
    return $true
}

function Invoke-Restore {
    Write-Header "恢复 NuGet 包"
    
    Write-Step "恢复包依赖..."
    dotnet restore
    
    if ($LASTEXITCODE -eq 0) {
        Write-Success "包恢复完成"
        return $true
    } else {
        Write-Error "包恢复失败"
        return $false
    }
}

# Main execution logic
Write-Host "Sqlx Project Maintenance Script" -ForegroundColor Magenta

switch ($Action.ToLower()) {
    "build" {
        $success = Invoke-Build
        exit $(if ($success) { 0 } else { 1 })
    }
    "test" {
        $success = Invoke-Test
        exit $(if ($success) { 0 } else { 1 })
    }
    "full-test" {
        $success = Invoke-Build
        if ($success) {
            $success = Invoke-FullTest
        }
        exit $(if ($success) { 0 } else { 1 })
    }
    "performance" {
        $success = Invoke-Performance
        exit $(if ($success) { 0 } else { 1 })
    }
    "clean" {
        $success = Invoke-Clean
        exit $(if ($success) { 0 } else { 1 })
    }
    "restore" {
        $success = Invoke-Restore
        exit $(if ($success) { 0 } else { 1 })
    }
    "help" {
        Show-Help
        exit 0
    }
    default {
        Write-Error "Unknown action: $Action"
        Show-Help
        exit 1
    }
}
