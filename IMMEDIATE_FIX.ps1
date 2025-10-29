# ========================================
# Sqlx.Extension 立即修复脚本
# 用于解决 CS0246 错误
# ========================================

Write-Host ""
Write-Host "==============================================" -ForegroundColor Cyan
Write-Host "  Sqlx.Extension CS0246 错误 - 立即修复" -ForegroundColor Cyan
Write-Host "==============================================" -ForegroundColor Cyan
Write-Host ""

$projectRoot = $PSScriptRoot
$extensionDir = Join-Path $projectRoot "src\Sqlx.Extension"

# 步骤 1: 检查项目目录
Write-Host "[1/6] 检查项目目录..." -ForegroundColor Yellow
if (-not (Test-Path $extensionDir)) {
    Write-Host "错误: 找不到项目目录: $extensionDir" -ForegroundColor Red
    exit 1
}
Write-Host "✓ 项目目录存在" -ForegroundColor Green
Write-Host ""

# 步骤 2: 清理输出目录
Write-Host "[2/6] 清理输出目录 (bin, obj)..." -ForegroundColor Yellow
@("bin", "obj") | ForEach-Object {
    $dir = Join-Path $extensionDir $_
    if (Test-Path $dir) {
        Remove-Item $dir -Recurse -Force -ErrorAction SilentlyContinue
        Write-Host "  ✓ 已删除 $_" -ForegroundColor Green
    } else {
        Write-Host "  • $_ 不存在，跳过" -ForegroundColor Gray
    }
}
Write-Host ""

# 步骤 3: 删除 .vs 缓存
Write-Host "[3/6] 清理 Visual Studio 缓存..." -ForegroundColor Yellow
$vsDir = Join-Path $projectRoot ".vs"
if (Test-Path $vsDir) {
    Remove-Item $vsDir -Recurse -Force -ErrorAction SilentlyContinue
    Write-Host "  ✓ 已删除 .vs 目录" -ForegroundColor Green
} else {
    Write-Host "  • .vs 不存在，跳过" -ForegroundColor Gray
}
Write-Host ""

# 步骤 4: 清理 NuGet 缓存
Write-Host "[4/6] 清理 NuGet 缓存..." -ForegroundColor Yellow
try {
    $output = dotnet nuget locals all --clear 2>&1
    Write-Host "  ✓ NuGet 缓存已清理" -ForegroundColor Green
} catch {
    Write-Host "  ! 警告: 无法清理 NuGet 缓存" -ForegroundColor Yellow
}
Write-Host ""

# 步骤 5: 删除全局 NuGet 包缓存中的 VS SDK 包
Write-Host "[5/6] 清理 Visual Studio SDK 包缓存..." -ForegroundColor Yellow
$nugetCache = Join-Path $env:USERPROFILE ".nuget\packages"
$vsPackages = @(
    "microsoft.visualstudio.sdk",
    "microsoft.visualstudio.shell.15.0",
    "microsoft.visualstudio.shell.framework",
    "microsoft.visualstudio.text.ui",
    "microsoft.codeanalysis.csharp.workspaces"
)

foreach ($pkg in $vsPackages) {
    $pkgDir = Join-Path $nugetCache $pkg
    if (Test-Path $pkgDir) {
        Remove-Item $pkgDir -Recurse -Force -ErrorAction SilentlyContinue
        Write-Host "  ✓ 已删除 $pkg 缓存" -ForegroundColor Green
    }
}
Write-Host ""

# 步骤 6: 提示下一步
Write-Host "[6/6] 下一步操作" -ForegroundColor Yellow
Write-Host ""
Write-Host "清理完成！现在请执行以下步骤：" -ForegroundColor Green
Write-Host ""
Write-Host "1. 打开 Visual Studio 2022" -ForegroundColor White
Write-Host ""
Write-Host "2. 打开解决方案文件：" -ForegroundColor White
Write-Host "   $projectRoot\Sqlx.sln" -ForegroundColor Cyan
Write-Host ""
Write-Host "3. 等待 NuGet 包自动还原" -ForegroundColor White
Write-Host "   (查看右下角状态栏，会显示 '正在还原...')" -ForegroundColor Gray
Write-Host "   (可能需要 2-5 分钟)" -ForegroundColor Gray
Write-Host ""
Write-Host "4. 如果没有自动还原，请手动还原：" -ForegroundColor White
Write-Host "   • 右键点击解决方案" -ForegroundColor Gray
Write-Host "   • 选择 '还原 NuGet 包'" -ForegroundColor Gray
Write-Host ""
Write-Host "5. 清理解决方案：" -ForegroundColor White
Write-Host "   • 菜单: 生成 → 清理解决方案" -ForegroundColor Gray
Write-Host ""
Write-Host "6. 重新生成：" -ForegroundColor White
Write-Host "   • 右键点击 'Sqlx.Extension' 项目" -ForegroundColor Gray
Write-Host "   • 选择 '重新生成'" -ForegroundColor Gray
Write-Host ""
Write-Host "7. 检查输出窗口是否有错误" -ForegroundColor White
Write-Host ""

Write-Host "==============================================" -ForegroundColor Cyan
Write-Host ""

Write-Host "如果仍有错误，请在 Visual Studio 中执行：" -ForegroundColor Yellow
Write-Host ""
Write-Host "方法 A - 使用包管理器控制台：" -ForegroundColor White
Write-Host "  1. 工具 → NuGet 包管理器 → 包管理器控制台" -ForegroundColor Gray
Write-Host "  2. 执行: Update-Package -reinstall -Project Sqlx.Extension" -ForegroundColor Cyan
Write-Host ""
Write-Host "方法 B - 手动重新安装关键包：" -ForegroundColor White
Write-Host "  1. 工具 → NuGet 包管理器 → 管理解决方案的 NuGet 包" -ForegroundColor Gray
Write-Host "  2. 找到 Sqlx.Extension 项目" -ForegroundColor Gray
Write-Host "  3. 卸载并重新安装这些包：" -ForegroundColor Gray
Write-Host "     - Microsoft.VisualStudio.SDK" -ForegroundColor Cyan
Write-Host "     - Microsoft.VisualStudio.Shell.15.0" -ForegroundColor Cyan
Write-Host "     - Microsoft.CodeAnalysis.CSharp.Workspaces" -ForegroundColor Cyan
Write-Host ""

Write-Host "按任意键继续..." -ForegroundColor Gray
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")

