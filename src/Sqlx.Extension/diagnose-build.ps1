# 诊断 VSIX 构建问题

Write-Host "🔍 Sqlx.Extension 构建诊断" -ForegroundColor Cyan
Write-Host "=" * 60

# 1. 检查环境
Write-Host "`n📋 环境检查" -ForegroundColor Yellow
Write-Host "-" * 60

# 检查 Visual Studio
$vsPath = "C:\Program Files\Microsoft Visual Studio\2022\Community"
if (Test-Path $vsPath) {
    Write-Host "✅ Visual Studio 2022 Community: 已安装" -ForegroundColor Green
    $msbuild = "$vsPath\MSBuild\Current\Bin\MSBuild.exe"
    if (Test-Path $msbuild) {
        Write-Host "✅ MSBuild: 找到" -ForegroundColor Green
    } else {
        Write-Host "❌ MSBuild: 未找到" -ForegroundColor Red
    }
} else {
    Write-Host "❌ Visual Studio 2022: 未找到" -ForegroundColor Red
}

# 检查 .NET Framework
$frameworkPath = "C:\Windows\Microsoft.NET\Framework64\v4.0.30319"
if (Test-Path $frameworkPath) {
    Write-Host "✅ .NET Framework 4.x: 已安装" -ForegroundColor Green
} else {
    Write-Host "⚠️  .NET Framework: 请检查版本" -ForegroundColor Yellow
}

# 2. 检查项目文件
Write-Host "`n📁 项目文件检查" -ForegroundColor Yellow
Write-Host "-" * 60

$files = @(
    "Sqlx.Extension.csproj",
    "source.extension.vsixmanifest",
    "License.txt",
    "Properties\AssemblyInfo.cs",
    "Sqlx.ExtensionPackage.cs"
)

foreach ($file in $files) {
    if (Test-Path $file) {
        Write-Host "✅ $file" -ForegroundColor Green
    } else {
        Write-Host "❌ $file - 缺失" -ForegroundColor Red
    }
}

# 3. 检查源代码文件
Write-Host "`n📝 源代码文件检查" -ForegroundColor Yellow
Write-Host "-" * 60

$sourceFiles = @(
    "SyntaxColoring\SqlTemplateClassifier.cs",
    "SyntaxColoring\SqlTemplateClassifierProvider.cs",
    "SyntaxColoring\SqlClassificationDefinitions.cs",
    "QuickActions\GenerateRepositoryCodeAction.cs",
    "QuickActions\AddCrudMethodsCodeAction.cs",
    "Diagnostics\SqlTemplateParameterAnalyzer.cs",
    "Diagnostics\SqlTemplateParameterCodeFixProvider.cs"
)

foreach ($file in $sourceFiles) {
    if (Test-Path $file) {
        Write-Host "✅ $file" -ForegroundColor Green
    } else {
        Write-Host "❌ $file - 缺失" -ForegroundColor Red
    }
}

# 4. 检查 NuGet 包
Write-Host "`n📦 NuGet 包检查" -ForegroundColor Yellow
Write-Host "-" * 60

Write-Host "正在检查包还原状态..."
$restoreResult = dotnet restore 2>&1 | Out-String

if ($LASTEXITCODE -eq 0) {
    Write-Host "✅ NuGet 包还原成功" -ForegroundColor Green
} else {
    Write-Host "❌ NuGet 包还原失败" -ForegroundColor Red
    Write-Host $restoreResult -ForegroundColor Red
}

# 5. 尝试构建（如果有 MSBuild）
Write-Host "`n🔨 构建测试" -ForegroundColor Yellow
Write-Host "-" * 60

if (Test-Path $msbuild) {
    Write-Host "尝试使用 MSBuild 构建..."
    Write-Host "命令: msbuild Sqlx.Extension.csproj /t:Rebuild /p:Configuration=Debug /v:minimal"
    Write-Host ""

    $buildOutput = & $msbuild Sqlx.Extension.csproj /t:Rebuild /p:Configuration=Debug /v:minimal 2>&1

    if ($LASTEXITCODE -eq 0) {
        Write-Host "✅ 构建成功!" -ForegroundColor Green

        # 检查 VSIX 文件
        if (Test-Path "bin\Debug\Sqlx.Extension.vsix") {
            $size = (Get-Item "bin\Debug\Sqlx.Extension.vsix").Length / 1MB
            Write-Host "✅ VSIX 文件已生成: $([math]::Round($size, 2)) MB" -ForegroundColor Green
        }
    } else {
        Write-Host "❌ 构建失败!" -ForegroundColor Red
        Write-Host "`n构建输出（最后 50 行）:" -ForegroundColor Yellow
        $buildOutput | Select-Object -Last 50
    }
} else {
    Write-Host "⚠️  无法找到 MSBuild，请在 Visual Studio 中构建" -ForegroundColor Yellow
}

# 6. 总结
Write-Host "`n📊 诊断总结" -ForegroundColor Yellow
Write-Host "=" * 60

Write-Host "`n如果构建失败，请检查："
Write-Host "1. 是否在 Visual Studio 2022 中打开项目？" -ForegroundColor Cyan
Write-Host "2. 是否安装了 'Visual Studio extension development' 工作负载？" -ForegroundColor Cyan
Write-Host "3. 查看上面的构建输出中的具体错误信息" -ForegroundColor Cyan
Write-Host "4. 将错误信息提供给开发者以获取帮助" -ForegroundColor Cyan

Write-Host "`n📝 将此输出复制并提供给开发者" -ForegroundColor Green
Write-Host "=" * 60

