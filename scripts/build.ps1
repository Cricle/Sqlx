# Sqlx 构建脚本

param(
    [string]$Configuration = "Release",
    [switch]$SkipTests,
    [switch]$Help
)

if ($Help) {
    Write-Host "🚀 Sqlx 构建脚本" -ForegroundColor Green
    Write-Host ""
    Write-Host "用法: .\scripts\build.ps1 [选项]"
    Write-Host ""
    Write-Host "选项:"
    Write-Host "  -Configuration <config>  构建配置 (Debug|Release) [默认: Release]"
    Write-Host "  -SkipTests              跳过测试"
    Write-Host "  -Help                   显示此帮助"
    Write-Host ""
    Write-Host "示例:"
    Write-Host "  .\scripts\build.ps1                      # 完整构建"
    Write-Host "  .\scripts\build.ps1 -Configuration Debug # Debug 构建"
    Write-Host "  .\scripts\build.ps1 -SkipTests           # 跳过测试"
    exit 0
}

$ErrorActionPreference = "Stop"

Write-Host ""
Write-Host "=====================================" -ForegroundColor Cyan
Write-Host "🚀 Sqlx 构建开始" -ForegroundColor Green
Write-Host "=====================================" -ForegroundColor Cyan
Write-Host "配置: $Configuration" -ForegroundColor Gray
Write-Host ""

try {
    # 1. 清理
    Write-Host "🧹 清理..." -ForegroundColor Cyan
    & dotnet clean --configuration $Configuration > $null
    Write-Host "   ✅ 清理完成" -ForegroundColor Green

    # 2. 还原
    Write-Host "📦 还原依赖..." -ForegroundColor Cyan
    & dotnet restore
    if ($LASTEXITCODE -ne 0) { throw "还原失败" }
    Write-Host "   ✅ 依赖还原完成" -ForegroundColor Green

    # 3. 构建
    Write-Host "🔨 构建项目..." -ForegroundColor Cyan
    & dotnet build --no-restore --configuration $Configuration
    if ($LASTEXITCODE -ne 0) { throw "构建失败" }
    Write-Host "   ✅ 构建完成" -ForegroundColor Green

    # 4. 测试
    if (-not $SkipTests) {
        Write-Host "🧪 运行测试..." -ForegroundColor Cyan
        & dotnet test --no-build --configuration $Configuration
        if ($LASTEXITCODE -ne 0) { throw "测试失败" }
        Write-Host "   ✅ 测试通过" -ForegroundColor Green
    } else {
        Write-Host "⏭️  跳过测试" -ForegroundColor Yellow
    }

    # 5. 完成
    Write-Host ""
    Write-Host "=====================================" -ForegroundColor Cyan
    Write-Host "🎉 构建成功！" -ForegroundColor Green
    Write-Host "=====================================" -ForegroundColor Cyan
    Write-Host ""

} catch {
    Write-Host ""
    Write-Host "=====================================" -ForegroundColor Red
    Write-Host "❌ 构建失败: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "=====================================" -ForegroundColor Red
    Write-Host ""
    exit 1
}
