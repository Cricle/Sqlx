# Sqlx v2.0.0 发布脚本 (PowerShell)
# 用途：一键发布到 NuGet.org

$ErrorActionPreference = "Stop"
$VERSION = "2.0.0"

Write-Host "🚀 Sqlx v$VERSION 发布脚本" -ForegroundColor Cyan
Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Cyan
Write-Host ""

# 1. 检查环境
Write-Host "1️⃣ 检查环境..." -ForegroundColor Yellow
try {
    $dotnetVersion = dotnet --version
    Write-Host "✅ dotnet CLI: $dotnetVersion" -ForegroundColor Green
} catch {
    Write-Host "❌ 错误: 未找到 dotnet CLI" -ForegroundColor Red
    exit 1
}
Write-Host ""

# 2. 清理
Write-Host "2️⃣ 清理旧的构建文件..." -ForegroundColor Yellow
dotnet clean -c Release > $null 2>&1
Write-Host "✅ 清理完成" -ForegroundColor Green
Write-Host ""

# 3. 运行测试
Write-Host "3️⃣ 运行测试..." -ForegroundColor Yellow
$testOutput = dotnet test tests/Sqlx.Tests --logger "console;verbosity=minimal" 2>&1 | Out-String
$testResult = $testOutput -split "`n" | Where-Object { $_ -match "通过" } | Select-Object -Last 1
Write-Host $testResult
if ($testOutput -notmatch "失败:     0") {
    Write-Host "❌ 错误: 测试失败，取消发布" -ForegroundColor Red
    exit 1
}
Write-Host "✅ 测试通过" -ForegroundColor Green
Write-Host ""

# 4. 构建 Release
Write-Host "4️⃣ 构建 Release..." -ForegroundColor Yellow
dotnet build -c Release > $null 2>&1
if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ 错误: 构建失败" -ForegroundColor Red
    exit 1
}
Write-Host "✅ 构建成功" -ForegroundColor Green
Write-Host ""

# 5. 打包
Write-Host "5️⃣ 打包 NuGet..." -ForegroundColor Yellow
dotnet pack -c Release > $null 2>&1
if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ 错误: 打包失败" -ForegroundColor Red
    exit 1
}
Write-Host "✅ Sqlx.$VERSION.nupkg" -ForegroundColor Green
Write-Host "✅ Sqlx.Generator.$VERSION.nupkg" -ForegroundColor Green
Write-Host ""

# 6. 检查包
Write-Host "6️⃣ 检查包文件..." -ForegroundColor Yellow
$sqlxPkg = "src\Sqlx\bin\Release\Sqlx.$VERSION.nupkg"
$generatorPkg = "src\Sqlx.Generator\bin\Release\Sqlx.Generator.$VERSION.nupkg"

if (-not (Test-Path $sqlxPkg)) {
    Write-Host "❌ 错误: 未找到 Sqlx 包" -ForegroundColor Red
    exit 1
}
if (-not (Test-Path $generatorPkg)) {
    Write-Host "❌ 错误: 未找到 Sqlx.Generator 包" -ForegroundColor Red
    exit 1
}
Write-Host "✅ 包文件检查通过" -ForegroundColor Green
Write-Host ""

# 7. 创建 Git 标签
Write-Host "7️⃣ 创建 Git 标签..." -ForegroundColor Yellow
$createTag = Read-Host "是否创建 Git 标签 v$VERSION? (y/n)"
if ($createTag -eq "y" -or $createTag -eq "Y") {
    git tag -a "v$VERSION" -m "Release v$VERSION - 100% Test Coverage"
    Write-Host "✅ 标签已创建" -ForegroundColor Green

    $pushTag = Read-Host "是否推送标签到远程? (y/n)"
    if ($pushTag -eq "y" -or $pushTag -eq "Y") {
        git push origin "v$VERSION"
        Write-Host "✅ 标签已推送" -ForegroundColor Green
    }
} else {
    Write-Host "⏭️  跳过标签创建" -ForegroundColor Gray
}
Write-Host ""

# 8. 发布到 NuGet
Write-Host "8️⃣ 发布到 NuGet.org..." -ForegroundColor Yellow
$publish = Read-Host "是否发布到 NuGet.org? (y/n)"
if ($publish -eq "y" -or $publish -eq "Y") {
    $nugetKey = Read-Host "请输入 NuGet API Key" -AsSecureString
    $BSTR = [System.Runtime.InteropServices.Marshal]::SecureStringToBSTR($nugetKey)
    $apiKey = [System.Runtime.InteropServices.Marshal]::PtrToStringAuto($BSTR)
    Write-Host ""

    Write-Host "📦 发布 Sqlx..." -ForegroundColor Cyan
    dotnet nuget push $sqlxPkg --api-key $apiKey --source https://api.nuget.org/v3/index.json

    Write-Host "📦 发布 Sqlx.Generator..." -ForegroundColor Cyan
    dotnet nuget push $generatorPkg --api-key $apiKey --source https://api.nuget.org/v3/index.json

    Write-Host "✅ 发布完成！" -ForegroundColor Green
} else {
    Write-Host "⏭️  跳过 NuGet 发布" -ForegroundColor Gray
    Write-Host ""
    Write-Host "手动发布命令:" -ForegroundColor Cyan
    Write-Host "  dotnet nuget push `"$sqlxPkg`" --api-key YOUR_KEY --source https://api.nuget.org/v3/index.json"
    Write-Host "  dotnet nuget push `"$generatorPkg`" --api-key YOUR_KEY --source https://api.nuget.org/v3/index.json"
}
Write-Host ""

# 9. 完成
Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Cyan
Write-Host "🎉 Sqlx v$VERSION 发布流程完成！" -ForegroundColor Green
Write-Host ""
Write-Host "下一步:"
Write-Host "  ✅ 创建 GitHub Release"
Write-Host "  ✅ 发布社区公告"
Write-Host "  ✅ 更新项目网站"
Write-Host ""
Write-Host "详见: NEXT_STEPS.md"
Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Cyan

