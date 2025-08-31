# Sqlx NuGet包自动化发布脚本
# 用法: .\push-nuget.ps1 [-Version "1.0.0"] [-ApiKey "your-api-key"] [-Source "https://api.nuget.org/v3/index.json"]

param(
    [Parameter(Mandatory=$false)]
    [string]$Version = "",
    
    [Parameter(Mandatory=$false)]
    [string]$ApiKey = "",
    
    [Parameter(Mandatory=$false)]
    [string]$Source = "https://api.nuget.org/v3/index.json",
    
    [Parameter(Mandatory=$false)]
    [switch]$DryRun = $false,
    
    [Parameter(Mandatory=$false)]
    [switch]$SkipTests = $false
)

# 颜色输出函数
function Write-ColorOutput($ForegroundColor) {
    $fc = $host.UI.RawUI.ForegroundColor
    $host.UI.RawUI.ForegroundColor = $ForegroundColor
    if ($args) {
        Write-Output $args
    } else {
        $input | Write-Output
    }
    $host.UI.RawUI.ForegroundColor = $fc
}

function Write-Success { Write-ColorOutput Green $args }
function Write-Warning { Write-ColorOutput Yellow $args }
function Write-Error { Write-ColorOutput Red $args }
function Write-Info { Write-ColorOutput Cyan $args }

# 检查dotnet命令是否可用
function Test-DotnetCommand {
    try {
        dotnet --version | Out-Null
        return $true
    } catch {
        Write-Error "错误: 未找到dotnet命令。请确保已安装.NET SDK。"
        return $false
    }
}

# 获取项目版本
function Get-ProjectVersion {
    param([string]$ProjectPath)
    
    if ($Version -ne "") {
        return $Version
    }
    
    try {
        $content = Get-Content $ProjectPath -Raw
        if ($content -match '<Version>(.*?)</Version>') {
            return $matches[1]
        } elseif ($content -match '<PackageVersion>(.*?)</PackageVersion>') {
            return $matches[1]
        } else {
            $currentDate = Get-Date -Format "yyyy.MM.dd"
            $buildNumber = Get-Date -Format "HHmm"
            return "$currentDate.$buildNumber"
        }
    } catch {
        Write-Warning "无法从项目文件获取版本，使用默认版本"
        return "1.0.0"
    }
}

# 清理输出目录
function Clear-OutputDirectories {
    Write-Info "清理输出目录..."
    
    $dirsToClean = @("bin", "obj", "*.nupkg")
    foreach ($dir in $dirsToClean) {
        Get-ChildItem -Path . -Recurse -Name $dir | ForEach-Object {
            $fullPath = Join-Path $PWD $_
            if (Test-Path $fullPath) {
                Remove-Item $fullPath -Recurse -Force
                Write-Info "已删除: $fullPath"
            }
        }
    }
}

# 运行测试
function Run-Tests {
    if ($SkipTests) {
        Write-Warning "跳过测试 (使用了 -SkipTests 参数)"
        return $true
    }
    
    Write-Info "运行单元测试..."
    
    $testProjects = Get-ChildItem -Path . -Recurse -Name "*.Tests.csproj"
    if ($testProjects.Count -eq 0) {
        Write-Warning "未找到测试项目"
        return $true
    }
    
    foreach ($testProject in $testProjects) {
        Write-Info "运行测试: $testProject"
        $result = dotnet test $testProject --configuration Release --verbosity minimal
        if ($LASTEXITCODE -ne 0) {
            Write-Error "测试失败: $testProject"
            return $false
        }
    }
    
    Write-Success "所有测试通过!"
    return $true
}

# 构建项目
function Build-Project {
    param([string]$ProjectPath, [string]$PackageVersion)
    
    Write-Info "构建项目: $ProjectPath (版本: $PackageVersion)"
    
    $buildArgs = @(
        "build"
        $ProjectPath
        "--configuration", "Release"
        "--verbosity", "minimal"
        "/p:PackageVersion=$PackageVersion"
        "/p:Version=$PackageVersion"
    )
    
    & dotnet @buildArgs
    
    if ($LASTEXITCODE -ne 0) {
        Write-Error "构建失败: $ProjectPath"
        return $false
    }
    
    Write-Success "构建成功: $ProjectPath"
    return $true
}

# 打包项目
function Pack-Project {
    param([string]$ProjectPath, [string]$PackageVersion)
    
    Write-Info "打包项目: $ProjectPath"
    
    $packArgs = @(
        "pack"
        $ProjectPath
        "--configuration", "Release"
        "--output", "."
        "--verbosity", "minimal"
        "/p:PackageVersion=$PackageVersion"
        "/p:Version=$PackageVersion"
        "--no-build"
    )
    
    & dotnet @packArgs
    
    if ($LASTEXITCODE -ne 0) {
        Write-Error "打包失败: $ProjectPath"
        return $false
    }
    
    Write-Success "打包成功: $ProjectPath"
    return $true
}

# 推送到NuGet
function Push-ToNuGet {
    param([string]$PackagePath, [string]$NuGetApiKey, [string]$NuGetSource)
    
    if ($DryRun) {
        Write-Warning "模拟运行: 将推送 $PackagePath 到 $NuGetSource"
        return $true
    }
    
    if ($NuGetApiKey -eq "") {
        $NuGetApiKey = Read-Host "请输入NuGet API Key" -AsSecureString
        $NuGetApiKey = [System.Runtime.InteropServices.Marshal]::PtrToStringAuto([System.Runtime.InteropServices.Marshal]::SecureStringToBSTR($NuGetApiKey))
    }
    
    Write-Info "推送包到NuGet: $PackagePath"
    
    $pushArgs = @(
        "nuget", "push"
        $PackagePath
        "--api-key", $NuGetApiKey
        "--source", $NuGetSource
        "--verbosity", "minimal"
    )
    
    & dotnet @pushArgs
    
    if ($LASTEXITCODE -ne 0) {
        Write-Error "推送失败: $PackagePath"
        return $false
    }
    
    Write-Success "推送成功: $PackagePath"
    return $true
}

# 主函数
function Main {
    Write-Info "=== Sqlx NuGet包发布脚本 ==="
    Write-Info "源码目录: $PWD"
    Write-Info "目标源: $Source"
    
    # 检查环境
    if (-not (Test-DotnetCommand)) {
        exit 1
    }
    
    # 查找主项目文件
    $projectFile = "Sqlx/Sqlx.csproj"
    if (-not (Test-Path $projectFile)) {
        Write-Error "未找到项目文件: $projectFile"
        exit 1
    }
    
    # 获取版本号
    $packageVersion = Get-ProjectVersion $projectFile
    Write-Info "包版本: $packageVersion"
    
    try {
        # 清理
        Clear-OutputDirectories
        
        # 运行测试
        if (-not (Run-Tests)) {
            exit 1
        }
        
        # 构建
        if (-not (Build-Project $projectFile $packageVersion)) {
            exit 1
        }
        
        # 打包
        if (-not (Pack-Project $projectFile $packageVersion)) {
            exit 1
        }
        
        # 查找生成的包
        $packagePath = "Sqlx.$packageVersion.nupkg"
        if (-not (Test-Path $packagePath)) {
            Write-Error "未找到生成的包: $packagePath"
            exit 1
        }
        
        # 推送到NuGet
        if (-not (Push-ToNuGet $packagePath $ApiKey $Source)) {
            exit 1
        }
        
        Write-Success "✅ NuGet包发布成功!"
        Write-Info "包名: Sqlx"
        Write-Info "版本: $packageVersion"
        Write-Info "源: $Source"
        
    } catch {
        Write-Error "发布过程中发生错误: $($_.Exception.Message)"
        exit 1
    }
}

# 运行主函数
Main



