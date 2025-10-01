#Requires -Version 5.1

<#
.SYNOPSIS
    编译Sqlx Visual Studio扩展(VSIX)的脚本

.DESCRIPTION
    这个脚本用于编译Sqlx Visual Studio扩展，生成.vsix文件用于分发。
    支持Debug和Release两种配置，并提供详细的编译信息。

.PARAMETER Configuration
    编译配置，可以是 'Debug' 或 'Release'。默认为 'Release'。

.PARAMETER Clean
    是否在编译前清理输出目录。

.PARAMETER Verbose
    是否显示详细的编译输出。

.EXAMPLE
    .\build-vsix.ps1
    使用Release配置编译VSIX

.EXAMPLE
    .\build-vsix.ps1 -Configuration Debug -Clean -Verbose
    使用Debug配置，清理后编译，并显示详细输出
#>

param(
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Release',

    [switch]$Clean,

    [switch]$Verbose
)

# 脚本配置
$ErrorActionPreference = 'Stop'
$ProgressPreference = 'SilentlyContinue'

# 路径配置
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Definition
$RootDir = Split-Path -Parent $ScriptDir
$VsixSolutionFile = Join-Path $RootDir "Sqlx.VisualStudio.sln"
$VsixProjectFile = Join-Path $RootDir "src\Sqlx.VisualStudio\Sqlx.VisualStudio.csproj"
$OutputDir = Join-Path $RootDir "src\Sqlx.VisualStudio\bin\$Configuration\net472"
$VsixFile = Join-Path $OutputDir "Sqlx.VisualStudio.vsix"

# 颜色输出函数
function Write-ColorText {
    param(
        [string]$Text,
        [ConsoleColor]$ForegroundColor = 'White'
    )
    $originalColor = $Host.UI.RawUI.ForegroundColor
    $Host.UI.RawUI.ForegroundColor = $ForegroundColor
    Write-Host $Text
    $Host.UI.RawUI.ForegroundColor = $originalColor
}

function Write-Step {
    param([string]$Message)
    Write-ColorText "🔄 $Message" -ForegroundColor Cyan
}

function Write-Success {
    param([string]$Message)
    Write-ColorText "✅ $Message" -ForegroundColor Green
}

function Write-Error {
    param([string]$Message)
    Write-ColorText "❌ $Message" -ForegroundColor Red
}

function Write-Warning {
    param([string]$Message)
    Write-ColorText "⚠️  $Message" -ForegroundColor Yellow
}

# 检查必需的工具
function Test-Prerequisites {
    Write-Step "检查必需的工具和环境..."

    # 检查.NET SDK
    try {
        $dotnetVersion = dotnet --version
        Write-Success ".NET SDK 版本: $dotnetVersion"
    }
    catch {
        Write-Error ".NET SDK 未找到。请安装 .NET SDK。"
        exit 1
    }

    # 检查MSBuild (通过VS或Build Tools)
    $msbuildPath = ""
    $vswhere = "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe"

    if (Test-Path $vswhere) {
        $vsInstallPath = & $vswhere -latest -products * -requires Microsoft.Component.MSBuild -property installationPath
        if ($vsInstallPath) {
            $msbuildPath = Join-Path $vsInstallPath "MSBuild\Current\Bin\MSBuild.exe"
            if (-not (Test-Path $msbuildPath)) {
                $msbuildPath = Join-Path $vsInstallPath "MSBuild\15.0\Bin\MSBuild.exe"
            }
        }
    }

    if (-not $msbuildPath -or -not (Test-Path $msbuildPath)) {
        Write-Warning "未找到Visual Studio MSBuild，尝试使用dotnet build..."
        return $null
    }

    Write-Success "MSBuild 路径: $msbuildPath"
    return $msbuildPath
}

# 验证项目文件
function Test-ProjectFiles {
    Write-Step "验证项目文件..."

    if (-not (Test-Path $VsixSolutionFile)) {
        Write-Error "找不到VS插件解决方案文件: $VsixSolutionFile"
        exit 1
    }
    Write-Success "解决方案文件: $VsixSolutionFile"

    if (-not (Test-Path $VsixProjectFile)) {
        Write-Error "找不到VS插件项目文件: $VsixProjectFile"
        exit 1
    }
    Write-Success "项目文件: $VsixProjectFile"

    # 检查VSIX清单文件
    $manifestFile = Join-Path $RootDir "src\Sqlx.VisualStudio\source.extension.vsixmanifest"
    if (-not (Test-Path $manifestFile)) {
        Write-Error "找不到VSIX清单文件: $manifestFile"
        exit 1
    }
    Write-Success "VSIX清单文件: $manifestFile"
}

# 清理输出目录
function Clear-OutputDirectory {
    if ($Clean) {
        Write-Step "清理输出目录..."
        try {
            if (Test-Path $OutputDir) {
                Remove-Item $OutputDir -Recurse -Force
                Write-Success "输出目录已清理: $OutputDir"
            }

            # 清理解决方案
            & dotnet clean $VsixSolutionFile --configuration $Configuration --verbosity minimal
            Write-Success "解决方案清理完成"
        }
        catch {
            Write-Warning "清理过程中出现警告: $_"
        }
    }
}

# 恢复NuGet包
function Restore-Packages {
    Write-Step "恢复NuGet包..."
    try {
        & dotnet restore $VsixSolutionFile --verbosity minimal
        if ($LASTEXITCODE -ne 0) {
            throw "包恢复失败"
        }
        Write-Success "NuGet包恢复完成"
    }
    catch {
        Write-Error "包恢复失败: $_"
        exit 1
    }
}

# 编译项目
function Build-Project {
    param([string]$MsBuildPath)

    Write-Step "编译VS扩展项目 (配置: $Configuration)..."

    try {
        if ($MsBuildPath) {
            # 使用MSBuild编译（推荐用于VSIX项目）
            $buildArgs = @(
                $VsixSolutionFile
                "/p:Configuration=$Configuration"
                "/p:Platform=`"Any CPU`""
                "/p:DeployExtension=false"
                "/p:CreateVsixContainer=true"
                "/m"
                "/nologo"
            )

            if ($Verbose) {
                $buildArgs += "/v:normal"
            } else {
                $buildArgs += "/v:minimal"
            }

            Write-Host "执行命令: $MsBuildPath $($buildArgs -join ' ')"
            & $MsBuildPath $buildArgs

        } else {
            # 使用dotnet build作为备选方案
            $buildArgs = @(
                "build"
                $VsixSolutionFile
                "--configuration"
                $Configuration
                "--no-restore"
            )

            if ($Verbose) {
                $buildArgs += "--verbosity", "normal"
            } else {
                $buildArgs += "--verbosity", "minimal"
            }

            Write-Host "执行命令: dotnet $($buildArgs -join ' ')"
            & dotnet @buildArgs
        }

        if ($LASTEXITCODE -ne 0) {
            throw "编译失败，退出代码: $LASTEXITCODE"
        }

        Write-Success "项目编译成功"
    }
    catch {
        Write-Error "编译失败: $_"
        exit 1
    }
}

# 验证VSIX文件
function Test-VsixFile {
    Write-Step "验证VSIX文件..."

    if (-not (Test-Path $VsixFile)) {
        Write-Error "VSIX文件未生成: $VsixFile"

        # 列出输出目录内容以便调试
        Write-Host "输出目录内容:"
        if (Test-Path $OutputDir) {
            Get-ChildItem $OutputDir -Recurse | ForEach-Object {
                Write-Host "  $($_.FullName)"
            }
        } else {
            Write-Host "  输出目录不存在"
        }

        exit 1
    }

    $vsixInfo = Get-Item $VsixFile
    Write-Success "VSIX文件生成成功!"
    Write-Host "  文件路径: $($vsixInfo.FullName)"
    Write-Host "  文件大小: $([math]::Round($vsixInfo.Length / 1KB, 2)) KB"
    Write-Host "  修改时间: $($vsixInfo.LastWriteTime)"

    return $vsixInfo
}

# 显示安装说明
function Show-InstallationInstructions {
    param($VsixInfo)

    Write-ColorText "`n📦 VSIX编译完成！" -ForegroundColor Green
    Write-Host ""
    Write-Host "安装方法："
    Write-Host "1. 双击VSIX文件进行安装："
    Write-ColorText "   $($VsixInfo.FullName)" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "2. 或在Visual Studio中："
    Write-Host "   - 扩展 → 管理扩展 → 从磁盘安装"
    Write-Host "   - 选择生成的VSIX文件"
    Write-Host ""
    Write-Host "3. 或使用命令行："
    Write-ColorText "   vsixinstaller `"$($VsixInfo.FullName)`"" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "功能："
    Write-Host "  • SQL悬浮提示 - 鼠标悬停查看SQL内容"
    Write-Host "  • 支持 [Sqlx]、[SqlTemplate]、[ExpressionToSql] 特性"
    Write-Host "  • 实时代码分析和缓存"
    Write-Host ""
}

# 主函数
function Main {
    Write-ColorText "🚀 Sqlx Visual Studio Extension 编译脚本" -ForegroundColor Magenta
    Write-Host "配置: $Configuration"
    Write-Host "清理: $(if($Clean){'是'}else{'否'})"
    Write-Host "详细输出: $(if($Verbose){'是'}else{'否'})"
    Write-Host ""

    try {
        # 步骤1：检查先决条件
        $msbuildPath = Test-Prerequisites

        # 步骤2：验证项目文件
        Test-ProjectFiles

        # 步骤3：清理（如果需要）
        Clear-OutputDirectory

        # 步骤4：恢复包
        Restore-Packages

        # 步骤5：编译项目
        Build-Project -MsBuildPath $msbuildPath

        # 步骤6：验证输出
        $vsixInfo = Test-VsixFile

        # 步骤7：显示安装说明
        Show-InstallationInstructions -VsixInfo $vsixInfo

        Write-ColorText "`n🎉 编译成功完成！" -ForegroundColor Green

    }
    catch {
        Write-Error "脚本执行失败: $_"
        exit 1
    }
}

# 执行主函数
Main
