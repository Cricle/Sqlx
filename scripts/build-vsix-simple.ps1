#!/usr/bin/env pwsh

<#
.SYNOPSIS
    Sqlx Visual Studio Extension (VSIX) 简化编译脚本

.DESCRIPTION
    这是一个简化版的VSIX编译脚本，专注于核心编译功能。
    适合在任何支持PowerShell Core的平台上运行。

.PARAMETER Configuration
    编译配置 ('Debug' 或 'Release')。默认: Release

.PARAMETER Clean
    编译前是否清理。默认: false

.EXAMPLE
    ./build-vsix-simple.ps1

.EXAMPLE
    ./build-vsix-simple.ps1 -Configuration Debug -Clean
#>

param(
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Release',
    [switch]$Clean
)

# 基本设置
$ErrorActionPreference = 'Stop'

# 路径设置
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Definition
$RootDir = Split-Path -Parent $ScriptDir
$SolutionFile = Join-Path $RootDir "Sqlx.VisualStudio.sln"
$VsixFile = Join-Path $RootDir "src\Sqlx.VisualStudio\bin\$Configuration\net472\Sqlx.VisualStudio.vsix"

Write-Host "🚀 Sqlx VSIX 编译脚本 (简化版)" -ForegroundColor Magenta
Write-Host "配置: $Configuration" -ForegroundColor Cyan

# 检查解决方案文件
if (-not (Test-Path $SolutionFile)) {
    Write-Host "❌ 错误: 找不到解决方案文件" -ForegroundColor Red
    Write-Host "   路径: $SolutionFile" -ForegroundColor Yellow
    exit 1
}
Write-Host "✅ 解决方案文件验证通过" -ForegroundColor Green

# 检查 dotnet
try {
    $version = dotnet --version
    Write-Host "✅ .NET SDK 版本: $version" -ForegroundColor Green
} catch {
    Write-Host "❌ 错误: 找不到 .NET SDK" -ForegroundColor Red
    exit 1
}

try {
    # 清理 (如果需要)
    if ($Clean) {
        Write-Host "🔄 清理中..." -ForegroundColor Cyan
        dotnet clean $SolutionFile --configuration $Configuration --verbosity minimal
        Write-Host "✅ 清理完成" -ForegroundColor Green
    }

    # 恢复包
    Write-Host "🔄 恢复包中..." -ForegroundColor Cyan
    dotnet restore $SolutionFile --verbosity minimal
    Write-Host "✅ 包恢复完成" -ForegroundColor Green

    # 编译
    Write-Host "🔄 编译中 ($Configuration)..." -ForegroundColor Cyan
    dotnet build $SolutionFile --configuration $Configuration --no-restore --verbosity minimal
    Write-Host "✅ 编译完成" -ForegroundColor Green

    # 检查VSIX文件
    if (Test-Path $VsixFile) {
        $fileInfo = Get-Item $VsixFile
        $sizeKB = [math]::Round($fileInfo.Length / 1KB, 2)

        Write-Host ""
        Write-Host "🎉 VSIX编译成功!" -ForegroundColor Green
        Write-Host "📁 文件位置: $VsixFile" -ForegroundColor Yellow
        Write-Host "📊 文件大小: $sizeKB KB" -ForegroundColor Yellow
        Write-Host "🕒 修改时间: $($fileInfo.LastWriteTime)" -ForegroundColor Yellow
        Write-Host ""
        Write-Host "安装方法:" -ForegroundColor Cyan
        Write-Host "• 双击VSIX文件安装" -ForegroundColor White
        Write-Host "• 或在VS中: 扩展 > 管理扩展 > 从磁盘安装" -ForegroundColor White

    } else {
        Write-Host "❌ 错误: VSIX文件未找到" -ForegroundColor Red
        Write-Host "   预期位置: $VsixFile" -ForegroundColor Yellow
        exit 1
    }

} catch {
    Write-Host "❌ 编译失败: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "✨ 编译完成!" -ForegroundColor Green
