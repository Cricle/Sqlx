# -----------------------------------------------------------------------
# Sqlx 统一构建脚本
# 简化开发和 CI/CD 流程
# -----------------------------------------------------------------------

param(
    [string]$Configuration = "Release",
    [string]$Version = "",
    [switch]$SkipTests,
    [switch]$SkipPack,
    [switch]$Verbose,
    [switch]$Help
)

if ($Help) {
    Write-Host "🚀 Sqlx 构建脚本" -ForegroundColor Green
    Write-Host ""
    Write-Host "用法: .\scripts\build.ps1 [选项]"
    Write-Host ""
    Write-Host "选项:"
    Write-Host "  -Configuration <config>  构建配置 (Debug|Release) [默认: Release]"
    Write-Host "  -Version <version>       版本号 (例如: 1.2.3)"
    Write-Host "  -SkipTests              跳过测试"
    Write-Host "  -SkipPack               跳过打包"
    Write-Host "  -Verbose                详细输出"
    Write-Host "  -Help                   显示此帮助"
    Write-Host ""
    Write-Host "示例:"
    Write-Host "  .\scripts\build.ps1                          # 完整构建"
    Write-Host "  .\scripts\build.ps1 -Configuration Debug     # Debug 构建"
    Write-Host "  .\scripts\build.ps1 -SkipTests               # 跳过测试"
    Write-Host "  .\scripts\build.ps1 -Version 1.2.3          # 指定版本"
    exit 0
}

# 设置错误处理
$ErrorActionPreference = "Stop"

# 颜色输出函数
function Write-Step($message) {
    Write-Host "🔧 $message" -ForegroundColor Cyan
}

function Write-Success($message) {
    Write-Host "✅ $message" -ForegroundColor Green
}

function Write-Error($message) {
    Write-Host "❌ $message" -ForegroundColor Red
}

function Write-Warning($message) {
    Write-Host "⚠️  $message" -ForegroundColor Yellow
}

# 开始构建
Write-Host "🚀 Sqlx 构建开始" -ForegroundColor Green
Write-Host "配置: $Configuration" -ForegroundColor Gray

try {
    # 1. 清理
    Write-Step "清理构建输出..."
    if (Test-Path "artifacts") {
        Remove-Item -Recurse -Force "artifacts"
    }
    New-Item -ItemType Directory -Force "artifacts" | Out-Null
    
    # 2. 还原依赖
    Write-Step "还原 NuGet 包..."
    $restoreArgs = @("restore")
    if ($Verbose) { $restoreArgs += "--verbosity", "detailed" }
    & dotnet @restoreArgs
    if ($LASTEXITCODE -ne 0) { throw "还原失败" }
    Write-Success "依赖还原完成"
    
    # 3. 构建
    Write-Step "构建项目..."
    $buildArgs = @("build", "--no-restore", "--configuration", $Configuration)
    if ($Version) {
        $buildArgs += "-p:Version=$Version"
        $buildArgs += "-p:AssemblyVersion=$Version"
        $buildArgs += "-p:FileVersion=$Version"
    }
    if ($Verbose) { $buildArgs += "--verbosity", "detailed" }
    
    & dotnet @buildArgs
    if ($LASTEXITCODE -ne 0) { throw "构建失败" }
    Write-Success "构建完成"
    
    # 4. 运行测试
    if (-not $SkipTests) {
        Write-Step "运行测试..."
        $testArgs = @(
            "test", 
            "--no-build", 
            "--configuration", $Configuration,
            "--collect:XPlat Code Coverage",
            "--results-directory", "artifacts/coverage"
        )
        if ($Verbose) { $testArgs += "--verbosity", "detailed" }
        
        & dotnet @testArgs
        if ($LASTEXITCODE -ne 0) { throw "测试失败" }
        Write-Success "测试通过"
        
        # 生成覆盖率报告
        if (Get-Command "reportgenerator" -ErrorAction SilentlyContinue) {
            Write-Step "生成覆盖率报告..."
            & reportgenerator -reports:"artifacts/coverage/**/*.xml" -targetdir:"artifacts/coverage/report" -reporttypes:"Html;Badges"
            Write-Success "覆盖率报告生成完成: artifacts/coverage/report"
        }
    } else {
        Write-Warning "跳过测试"
    }
    
    # 5. 打包
    if (-not $SkipPack) {
        Write-Step "创建 NuGet 包..."
        $packArgs = @(
            "pack", "src/Sqlx/Sqlx.csproj",
            "--no-build",
            "--configuration", $Configuration,
            "--output", "artifacts"
        )
        if ($Version) {
            $packArgs += "-p:PackageVersion=$Version"
        }
        if ($Verbose) { $packArgs += "--verbosity", "detailed" }
        
        & dotnet @packArgs
        if ($LASTEXITCODE -ne 0) { throw "打包失败" }
        Write-Success "NuGet 包创建完成"
    } else {
        Write-Warning "跳过打包"
    }
    
    # 6. 构建示例项目
    Write-Step "构建示例项目..."
    $sampleProjects = @(
        "samples/RepositoryExample/RepositoryExample.csproj",
        "samples/GettingStarted/GettingStarted.csproj"
    )
    
    foreach ($project in $sampleProjects) {
        if (Test-Path $project) {
            Write-Host "  构建 $project..." -ForegroundColor Gray
            & dotnet build $project --configuration $Configuration --no-restore
            if ($LASTEXITCODE -ne 0) { Write-Warning "示例项目 $project 构建失败" }
        }
    }
    Write-Success "示例项目构建完成"
    
    # 7. 总结
    Write-Host ""
    Write-Host "🎉 构建完成！" -ForegroundColor Green
    Write-Host "输出目录: artifacts/" -ForegroundColor Gray
    
    if (Test-Path "artifacts/*.nupkg") {
        $packages = Get-ChildItem "artifacts/*.nupkg"
        Write-Host "NuGet 包:" -ForegroundColor Gray
        foreach ($pkg in $packages) {
            Write-Host "  📦 $($pkg.Name)" -ForegroundColor Gray
        }
    }
    
    if (Test-Path "artifacts/coverage/report") {
        Write-Host "覆盖率报告: artifacts/coverage/report/index.html" -ForegroundColor Gray
    }
    
} catch {
    Write-Error "构建失败: $($_.Exception.Message)"
    exit 1
}

