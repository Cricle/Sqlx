# -----------------------------------------------------------------------
# Sqlx 开发环境设置脚本
# 快速设置开发环境
# -----------------------------------------------------------------------

param(
    [switch]$InstallTools,
    [switch]$Help
)

if ($Help) {
    Write-Host "🛠️  Sqlx 开发环境设置" -ForegroundColor Green
    Write-Host ""
    Write-Host "用法: .\scripts\setup-dev.ps1 [选项]"
    Write-Host ""
    Write-Host "选项:"
    Write-Host "  -InstallTools    安装开发工具"
    Write-Host "  -Help           显示此帮助"
    Write-Host ""
    exit 0
}

$ErrorActionPreference = "Stop"

function Write-Step($message) {
    Write-Host "🔧 $message" -ForegroundColor Cyan
}

function Write-Success($message) {
    Write-Host "✅ $message" -ForegroundColor Green
}

function Write-Warning($message) {
    Write-Host "⚠️  $message" -ForegroundColor Yellow
}

Write-Host "🛠️  Sqlx 开发环境设置" -ForegroundColor Green
Write-Host ""

# 1. 检查 .NET SDK
Write-Step "检查 .NET SDK..."
try {
    $dotnetVersion = & dotnet --version
    Write-Success ".NET SDK 版本: $dotnetVersion"
} catch {
    Write-Host "❌ .NET SDK 未安装" -ForegroundColor Red
    Write-Host "请访问 https://dotnet.microsoft.com/download 安装 .NET 8.0 SDK" -ForegroundColor Yellow
    exit 1
}

# 2. 检查必需的 .NET 版本
$requiredVersions = @("6.0", "8.0")
foreach ($version in $requiredVersions) {
    try {
        $runtimes = & dotnet --list-runtimes | Select-String "Microsoft.NETCore.App $version"
        if ($runtimes) {
            Write-Success ".NET $version 运行时已安装"
        } else {
            Write-Warning ".NET $version 运行时未找到，某些测试可能失败"
        }
    } catch {
        Write-Warning "无法检查 .NET $version 运行时"
    }
}

# 3. 还原依赖
Write-Step "还原项目依赖..."
& dotnet restore
if ($LASTEXITCODE -eq 0) {
    Write-Success "依赖还原完成"
} else {
    Write-Host "❌ 依赖还原失败" -ForegroundColor Red
    exit 1
}

# 4. 验证构建
Write-Step "验证项目构建..."
& dotnet build --configuration Debug --no-restore
if ($LASTEXITCODE -eq 0) {
    Write-Success "项目构建成功"
} else {
    Write-Host "❌ 项目构建失败" -ForegroundColor Red
    exit 1
}

# 5. 运行快速测试
Write-Step "运行快速测试验证..."
& dotnet test tests/Sqlx.Tests/Sqlx.Tests.csproj --configuration Debug --no-build --filter "TestCategory!=Integration&TestCategory!=Performance" --verbosity quiet
if ($LASTEXITCODE -eq 0) {
    Write-Success "快速测试通过"
} else {
    Write-Warning "某些测试失败，但这可能是正常的"
}

# 6. 安装开发工具 (可选)
if ($InstallTools) {
    Write-Step "安装开发工具..."
    
    # 安装覆盖率工具
    try {
        & dotnet tool install --global dotnet-reportgenerator-globaltool
        Write-Success "ReportGenerator 工具已安装"
    } catch {
        Write-Warning "ReportGenerator 工具安装失败或已存在"
    }
    
    # 安装格式化工具
    try {
        & dotnet tool install --global dotnet-format
        Write-Success "dotnet-format 工具已安装"
    } catch {
        Write-Warning "dotnet-format 工具安装失败或已存在"
    }
    
    # 安装性能分析工具
    try {
        & dotnet tool install --global dotnet-trace
        Write-Success "dotnet-trace 工具已安装"
    } catch {
        Write-Warning "dotnet-trace 工具安装失败或已存在"
    }
}

# 7. 创建开发配置文件
Write-Step "创建开发配置文件..."
$devConfig = @"
{
  "sqlx": {
    "development": {
      "enableDiagnostics": true,
      "logLevel": "Debug",
      "connectionStrings": {
        "sqlite": "Data Source=:memory:",
        "testDb": "Data Source=test.db"
      }
    }
  }
}
"@

$devConfigPath = "appsettings.Development.json"
if (-not (Test-Path $devConfigPath)) {
    $devConfig | Out-File -FilePath $devConfigPath -Encoding UTF8
    Write-Success "开发配置文件已创建: $devConfigPath"
} else {
    Write-Warning "开发配置文件已存在: $devConfigPath"
}

# 8. 设置 Git hooks (如果在 Git 仓库中)
if (Test-Path ".git") {
    Write-Step "设置 Git hooks..."
    
    $preCommitHook = @"
#!/bin/sh
# Sqlx pre-commit hook
echo "🔧 Running pre-commit checks..."

# Format code
dotnet format --no-restore --verify-no-changes --verbosity quiet
if [ `$? -ne 0 ]; then
    echo "❌ Code formatting issues found. Run 'dotnet format' to fix."
    exit 1
fi

# Run quick tests
dotnet test --configuration Debug --no-build --filter "TestCategory!=Integration&TestCategory!=Performance" --verbosity quiet
if [ `$? -ne 0 ]; then
    echo "❌ Quick tests failed. Fix issues before committing."
    exit 1
fi

echo "✅ Pre-commit checks passed"
"@
    
    $hookPath = ".git/hooks/pre-commit"
    if (-not (Test-Path $hookPath)) {
        $preCommitHook | Out-File -FilePath $hookPath -Encoding ASCII
        # 在 Windows 上设置执行权限 (如果可能)
        try {
            & git update-index --chmod=+x $hookPath
        } catch {
            # 忽略权限设置错误
        }
        Write-Success "Git pre-commit hook 已设置"
    } else {
        Write-Warning "Git pre-commit hook 已存在"
    }
}

# 9. 完成设置
Write-Host ""
Write-Host "🎉 开发环境设置完成！" -ForegroundColor Green
Write-Host ""
Write-Host "下一步:" -ForegroundColor Yellow
Write-Host "  1. 运行完整构建: .\scripts\build.ps1" -ForegroundColor Gray
Write-Host "  2. 运行示例项目: cd samples\GettingStarted && dotnet run" -ForegroundColor Gray
Write-Host "  3. 查看文档: docs\QUICK_START.md" -ForegroundColor Gray
Write-Host "  4. 开始开发: 编辑 src\Sqlx\ 中的文件" -ForegroundColor Gray
Write-Host ""
Write-Host "有用的命令:" -ForegroundColor Yellow
Write-Host "  dotnet build                    # 构建项目" -ForegroundColor Gray
Write-Host "  dotnet test                     # 运行测试" -ForegroundColor Gray
Write-Host "  dotnet format                   # 格式化代码" -ForegroundColor Gray
Write-Host "  .\scripts\build.ps1 -Help      # 查看构建选项" -ForegroundColor Gray

