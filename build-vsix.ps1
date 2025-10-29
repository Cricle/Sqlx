# 🏗️ Sqlx VSIX 自动构建脚本
# 功能：编译 VSIX 插件并复制到根目录

param(
    [Parameter(Mandatory=$false)]
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"

# 颜色输出函数
function Write-ColorOutput($ForegroundColor) {
    $fc = $host.UI.RawUI.ForegroundColor
    $host.UI.RawUI.ForegroundColor = $ForegroundColor
    if ($args) {
        Write-Output $args
    }
    $host.UI.RawUI.ForegroundColor = $fc
}

Write-ColorOutput Cyan "🏗️  Sqlx VSIX 构建脚本"
Write-ColorOutput Cyan ("=" * 60)

# 1. 检查环境
Write-Host "`n📋 检查构建环境..." -ForegroundColor Yellow

$vsPath = "C:\Program Files\Microsoft Visual Studio\2022\Community"
$msbuild = "$vsPath\MSBuild\Current\Bin\MSBuild.exe"

if (-not (Test-Path $msbuild)) {
    # 尝试 Professional 版本
    $vsPath = "C:\Program Files\Microsoft Visual Studio\2022\Professional"
    $msbuild = "$vsPath\MSBuild\Current\Bin\MSBuild.exe"
    
    if (-not (Test-Path $msbuild)) {
        # 尝试 Enterprise 版本
        $vsPath = "C:\Program Files\Microsoft Visual Studio\2022\Enterprise"
        $msbuild = "$vsPath\MSBuild\Current\Bin\MSBuild.exe"
        
        if (-not (Test-Path $msbuild)) {
            Write-ColorOutput Red "❌ 错误: 找不到 MSBuild.exe"
            Write-Host ""
            Write-Host "请确保已安装 Visual Studio 2022 (Community/Professional/Enterprise)"
            Write-Host "并且安装了 'Visual Studio extension development' 工作负载"
            exit 1
        }
    }
}

Write-ColorOutput Green "✅ 找到 MSBuild: $msbuild"

# 2. 切换到项目目录
$projectDir = Join-Path $PSScriptRoot "src\Sqlx.Extension"

if (-not (Test-Path $projectDir)) {
    Write-ColorOutput Red "❌ 错误: 项目目录不存在: $projectDir"
    exit 1
}

Set-Location $projectDir
Write-ColorOutput Green "✅ 项目目录: $projectDir"

# 3. 检查项目文件
$projectFile = "Sqlx.Extension.csproj"
if (-not (Test-Path $projectFile)) {
    Write-ColorOutput Red "❌ 错误: 找不到项目文件: $projectFile"
    exit 1
}

Write-ColorOutput Green "✅ 项目文件: $projectFile"

# 4. 清理旧的构建输出
Write-Host "`n🧹 清理旧的构建输出..." -ForegroundColor Yellow

if (Test-Path "bin") {
    Remove-Item "bin" -Recurse -Force -ErrorAction SilentlyContinue
    Write-ColorOutput Green "✅ 已清理 bin 目录"
}

if (Test-Path "obj") {
    Remove-Item "obj" -Recurse -Force -ErrorAction SilentlyContinue
    Write-ColorOutput Green "✅ 已清理 obj 目录"
}

# 5. 还原 NuGet 包
Write-Host "`n📦 还原 NuGet 包..." -ForegroundColor Yellow

$restoreOutput = dotnet restore 2>&1 | Out-String

if ($LASTEXITCODE -ne 0) {
    Write-ColorOutput Red "❌ NuGet 包还原失败"
    Write-Host $restoreOutput
    exit 1
}

Write-ColorOutput Green "✅ NuGet 包还原成功"

# 6. 构建项目
Write-Host "`n🔨 开始构建 ($Configuration 配置)..." -ForegroundColor Yellow
Write-Host ""

$buildArgs = @(
    $projectFile,
    "/t:Rebuild",
    "/p:Configuration=$Configuration",
    "/v:minimal",
    "/nologo"
)

Write-ColorOutput Gray "命令: MSBuild $($buildArgs -join ' ')"
Write-Host ""

$buildOutput = & $msbuild $buildArgs 2>&1

if ($LASTEXITCODE -ne 0) {
    Write-ColorOutput Red "`n❌ 构建失败!"
    Write-Host ""
    Write-ColorOutput Yellow "错误详情:"
    Write-Host $buildOutput
    exit 1
}

Write-ColorOutput Green "`n✅ 构建成功!"

# 7. 检查生成的 VSIX 文件
Write-Host "`n📦 检查生成的文件..." -ForegroundColor Yellow

$vsixPath = "bin\$Configuration\Sqlx.Extension.vsix"

if (-not (Test-Path $vsixPath)) {
    Write-ColorOutput Red "❌ 错误: VSIX 文件未生成"
    Write-Host "预期位置: $vsixPath"
    exit 1
}

$vsixFile = Get-Item $vsixPath
$vsixSizeMB = [math]::Round($vsixFile.Length / 1MB, 2)

Write-ColorOutput Green "✅ VSIX 文件已生成"
Write-Host "   位置: $vsixPath"
Write-Host "   大小: $vsixSizeMB MB"
Write-Host "   时间: $($vsixFile.LastWriteTime)"

# 8. 复制 VSIX 到根目录
Write-Host "`n📋 复制 VSIX 到根目录..." -ForegroundColor Yellow

$rootDir = Split-Path (Split-Path $projectDir -Parent) -Parent
$outputFileName = "Sqlx.Extension-v0.1.0-$Configuration.vsix"
$outputPath = Join-Path $rootDir $outputFileName

# 如果文件已存在，先删除
if (Test-Path $outputPath) {
    Remove-Item $outputPath -Force
}

Copy-Item $vsixPath $outputPath -Force

if (Test-Path $outputPath) {
    Write-ColorOutput Green "✅ VSIX 已复制到根目录"
    Write-Host "   位置: $outputPath"
    Write-Host "   文件名: $outputFileName"
} else {
    Write-ColorOutput Red "❌ 复制失败"
    exit 1
}

# 9. 验证 VSIX 内容（可选）
Write-Host "`n🔍 验证 VSIX 内容..." -ForegroundColor Yellow

# 复制一份并解压查看
$tempZip = Join-Path $env:TEMP "Sqlx.Extension.zip"
$tempExtract = Join-Path $env:TEMP "Sqlx.Extension.Content"

Copy-Item $outputPath $tempZip -Force

if (Test-Path $tempExtract) {
    Remove-Item $tempExtract -Recurse -Force
}

try {
    Expand-Archive -Path $tempZip -DestinationPath $tempExtract -Force
    
    $manifestPath = Join-Path $tempExtract "extension.vsixmanifest"
    $dllPath = Join-Path $tempExtract "Sqlx.Extension.dll"
    $licensePath = Join-Path $tempExtract "License.txt"
    
    $allGood = $true
    
    if (Test-Path $manifestPath) {
        Write-ColorOutput Green "   ✅ extension.vsixmanifest"
    } else {
        Write-ColorOutput Red "   ❌ extension.vsixmanifest 缺失"
        $allGood = $false
    }
    
    if (Test-Path $dllPath) {
        Write-ColorOutput Green "   ✅ Sqlx.Extension.dll"
    } else {
        Write-ColorOutput Red "   ❌ Sqlx.Extension.dll 缺失"
        $allGood = $false
    }
    
    if (Test-Path $licensePath) {
        Write-ColorOutput Green "   ✅ License.txt"
    } else {
        Write-ColorOutput Red "   ❌ License.txt 缺失"
        $allGood = $false
    }
    
    # 清理临时文件
    Remove-Item $tempZip -Force -ErrorAction SilentlyContinue
    Remove-Item $tempExtract -Recurse -Force -ErrorAction SilentlyContinue
    
    if (-not $allGood) {
        Write-ColorOutput Red "`n⚠️  警告: VSIX 内容不完整"
    }
    
} catch {
    Write-ColorOutput Yellow "   ⚠️  无法验证内容: $_"
}

# 10. 生成校验和（可选）
Write-Host "`n🔐 生成校验和..." -ForegroundColor Yellow

$hash = Get-FileHash -Path $outputPath -Algorithm SHA256
Write-Host "   SHA256: $($hash.Hash)"

# 保存校验和到文件
$hashFile = "$outputPath.sha256"
$hash.Hash | Out-File -FilePath $hashFile -Encoding ASCII -NoNewline
Write-ColorOutput Green "   ✅ 校验和已保存: $hashFile"

# 11. 总结
Write-Host ""
Write-ColorOutput Cyan ("=" * 60)
Write-ColorOutput Green "🎉 构建完成！"
Write-ColorOutput Cyan ("=" * 60)

Write-Host ""
Write-Host "📦 VSIX 文件信息:"
Write-Host "   文件名: $outputFileName"
Write-Host "   位置: $outputPath"
Write-Host "   大小: $vsixSizeMB MB"
Write-Host "   配置: $Configuration"
Write-Host ""

Write-Host "🚀 下一步操作:"
Write-Host "   1. 安装测试:" -ForegroundColor Cyan
Write-Host "      双击: $outputFileName"
Write-Host ""
Write-Host "   2. 发布到 Marketplace:" -ForegroundColor Cyan
Write-Host "      上传: $outputFileName"
Write-Host ""
Write-Host "   3. 创建 GitHub Release:" -ForegroundColor Cyan
Write-Host "      附加: $outputFileName"
Write-Host "      附加: $outputFileName.sha256"
Write-Host ""

Write-ColorOutput Green "✅ 所有步骤完成！"

# 返回原目录
Set-Location $PSScriptRoot

