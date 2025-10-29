# Sqlx.Extension 诊断和修复脚本
# 用于解决 CS0246 引用错误

Write-Host "=" * 70 -ForegroundColor Cyan
Write-Host "Sqlx.Extension 诊断和修复工具" -ForegroundColor Cyan
Write-Host "=" * 70 -ForegroundColor Cyan
Write-Host ""

# 1. 检查 Visual Studio 安装
Write-Host "步骤 1: 检查 Visual Studio 2022 安装..." -ForegroundColor Yellow

$vsWhere = "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe"
if (Test-Path $vsWhere) {
    $vsInstall = & $vsWhere -latest -property installationPath
    Write-Host "✓ Visual Studio 2022 已安装: $vsInstall" -ForegroundColor Green
    
    # 检查 VSSDK 工作负载
    $vssdkPath = Join-Path $vsInstall "VSSDK"
    if (Test-Path $vssdkPath) {
        Write-Host "✓ Visual Studio SDK 已安装" -ForegroundColor Green
    } else {
        Write-Host "✗ Visual Studio SDK 未安装！" -ForegroundColor Red
        Write-Host "  请运行 Visual Studio Installer 并安装 'Visual Studio extension development' 工作负载" -ForegroundColor Yellow
        Write-Host ""
        Write-Host "按任意键打开 Visual Studio Installer..." -ForegroundColor Yellow
        $null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
        Start-Process "$env:ProgramFiles(x86)\Microsoft Visual Studio\Installer\vs_installer.exe"
        exit 1
    }
} else {
    Write-Host "✗ 未找到 Visual Studio 2022！" -ForegroundColor Red
    exit 1
}

Write-Host ""

# 2. 清理输出目录
Write-Host "步骤 2: 清理输出目录..." -ForegroundColor Yellow

$projectDir = Split-Path -Parent $MyInvocation.MyCommand.Path
Push-Location $projectDir

$binDir = Join-Path $projectDir "bin"
$objDir = Join-Path $projectDir "obj"

if (Test-Path $binDir) {
    Remove-Item $binDir -Recurse -Force -ErrorAction SilentlyContinue
    Write-Host "✓ 已删除 bin 目录" -ForegroundColor Green
}

if (Test-Path $objDir) {
    Remove-Item $objDir -Recurse -Force -ErrorAction SilentlyContinue
    Write-Host "✓ 已删除 obj 目录" -ForegroundColor Green
}

Write-Host ""

# 3. 清理 NuGet 缓存
Write-Host "步骤 3: 清理 NuGet 缓存..." -ForegroundColor Yellow

try {
    dotnet nuget locals all --clear | Out-Null
    Write-Host "✓ NuGet 缓存已清理" -ForegroundColor Green
} catch {
    Write-Host "! 无法清理 NuGet 缓存（可选步骤）" -ForegroundColor Yellow
}

Write-Host ""

# 4. 检查并删除 .vs 文件夹
Write-Host "步骤 4: 清理 Visual Studio 缓存..." -ForegroundColor Yellow

$solutionDir = Resolve-Path (Join-Path $projectDir "..\..")
$vsDir = Join-Path $solutionDir ".vs"

if (Test-Path $vsDir) {
    Remove-Item $vsDir -Recurse -Force -ErrorAction SilentlyContinue
    Write-Host "✓ 已删除 .vs 目录" -ForegroundColor Green
} else {
    Write-Host "• .vs 目录不存在，跳过" -ForegroundColor Gray
}

Write-Host ""

# 5. 检查项目文件
Write-Host "步骤 5: 验证项目配置..." -ForegroundColor Yellow

$csprojPath = Join-Path $projectDir "Sqlx.Extension.csproj"
if (Test-Path $csprojPath) {
    $content = Get-Content $csprojPath -Raw
    
    # 检查关键包引用
    $requiredPackages = @(
        "Microsoft.VisualStudio.SDK",
        "Microsoft.VSSDK.BuildTools",
        "Microsoft.VisualStudio.Shell.15.0",
        "Microsoft.CodeAnalysis.CSharp.Workspaces"
    )
    
    $allFound = $true
    foreach ($package in $requiredPackages) {
        if ($content -match $package) {
            Write-Host "✓ 找到包引用: $package" -ForegroundColor Green
        } else {
            Write-Host "✗ 缺少包引用: $package" -ForegroundColor Red
            $allFound = $false
        }
    }
    
    if (-not $allFound) {
        Write-Host ""
        Write-Host "项目文件缺少必要的包引用！" -ForegroundColor Red
        Write-Host "请执行: git pull origin main" -ForegroundColor Yellow
        exit 1
    }
} else {
    Write-Host "✗ 未找到项目文件！" -ForegroundColor Red
    exit 1
}

Write-Host ""

# 6. 还原 NuGet 包
Write-Host "步骤 6: 还原 NuGet 包..." -ForegroundColor Yellow

# 查找 MSBuild
$msbuildPath = & $vsWhere -latest -requires Microsoft.Component.MSBuild -find MSBuild\**\Bin\MSBuild.exe | Select-Object -First 1

if ($msbuildPath -and (Test-Path $msbuildPath)) {
    Write-Host "使用 MSBuild: $msbuildPath" -ForegroundColor Gray
    
    Write-Host "正在还原 NuGet 包..." -ForegroundColor Gray
    & $msbuildPath $csprojPath /t:Restore /v:minimal /nologo
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "✓ NuGet 包还原成功" -ForegroundColor Green
    } else {
        Write-Host "✗ NuGet 包还原失败" -ForegroundColor Red
        Write-Host "请在 Visual Studio 中手动还原包" -ForegroundColor Yellow
    }
} else {
    Write-Host "! 未找到 MSBuild，请在 Visual Studio 中还原包" -ForegroundColor Yellow
}

Write-Host ""

# 7. 提供下一步指导
Write-Host "=" * 70 -ForegroundColor Cyan
Write-Host "诊断完成！" -ForegroundColor Cyan
Write-Host "=" * 70 -ForegroundColor Cyan
Write-Host ""

Write-Host "下一步操作：" -ForegroundColor Yellow
Write-Host "1. 打开 Visual Studio 2022" -ForegroundColor White
Write-Host "2. 打开解决方案: $solutionDir\Sqlx.sln" -ForegroundColor White
Write-Host "3. 等待 NuGet 包还原完成（查看右下角状态栏）" -ForegroundColor White
Write-Host "4. 右键点击 Sqlx.Extension 项目 -> 重新生成" -ForegroundColor White
Write-Host "5. 检查输出窗口是否有错误" -ForegroundColor White
Write-Host ""

Write-Host "如果仍有 CS0246 错误：" -ForegroundColor Yellow
Write-Host "• 在 Visual Studio 中: 工具 -> 选项 -> NuGet 包管理器" -ForegroundColor White
Write-Host "• 点击 '清除所有 NuGet 缓存'" -ForegroundColor White
Write-Host "• 关闭并重新打开 Visual Studio" -ForegroundColor White
Write-Host ""

Write-Host "按任意键退出..." -ForegroundColor Gray
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")

Pop-Location

