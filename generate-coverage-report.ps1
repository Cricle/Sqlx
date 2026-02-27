# 生成代码覆盖率报告
# Generate code coverage report

Write-Host "=== 生成代码覆盖率报告 ===" -ForegroundColor Cyan
Write-Host ""

# 清理旧的测试结果
Write-Host "清理旧的测试结果..." -ForegroundColor Yellow
if (Test-Path "./TestResults") {
    Remove-Item -Path "./TestResults" -Recurse -Force
}
if (Test-Path "./CoverageReport") {
    Remove-Item -Path "./CoverageReport" -Recurse -Force
}

# 编译项目
Write-Host "编译项目..." -ForegroundColor Yellow
dotnet build tests/Sqlx.Tests/Sqlx.Tests.csproj --configuration Release --verbosity minimal
if ($LASTEXITCODE -ne 0) {
    Write-Host "编译失败!" -ForegroundColor Red
    exit 1
}

# 运行单元测试并收集覆盖率（排除E2E测试）
Write-Host "运行单元测试并收集覆盖率..." -ForegroundColor Yellow
dotnet test tests/Sqlx.Tests/Sqlx.Tests.csproj `
    --configuration Release `
    --filter "FullyQualifiedName!~E2E" `
    --collect:"XPlat Code Coverage" `
    --results-directory ./TestResults `
    --no-build `
    --verbosity minimal

if ($LASTEXITCODE -ne 0) {
    Write-Host "测试失败!" -ForegroundColor Red
    exit 1
}

# 查找覆盖率文件
$coverageFile = Get-ChildItem -Path "./TestResults" -Filter "coverage.cobertura.xml" -Recurse | Select-Object -First 1

if ($null -eq $coverageFile) {
    Write-Host "未找到覆盖率文件!" -ForegroundColor Red
    exit 1
}

Write-Host "覆盖率文件: $($coverageFile.FullName)" -ForegroundColor Green

# 检查是否安装了 reportgenerator
$reportGeneratorInstalled = $null -ne (Get-Command "reportgenerator" -ErrorAction SilentlyContinue)

if ($reportGeneratorInstalled) {
    Write-Host "生成HTML覆盖率报告..." -ForegroundColor Yellow
    reportgenerator `
        -reports:"$($coverageFile.FullName)" `
        -targetdir:"./CoverageReport" `
        -reporttypes:"Html;TextSummary"
    
    Write-Host ""
    Write-Host "覆盖率报告已生成: ./CoverageReport/index.html" -ForegroundColor Green
    
    # 显示摘要
    if (Test-Path "./CoverageReport/Summary.txt") {
        Write-Host ""
        Write-Host "=== 覆盖率摘要 ===" -ForegroundColor Cyan
        Get-Content "./CoverageReport/Summary.txt"
    }
} else {
    Write-Host ""
    Write-Host "未安装 reportgenerator 工具" -ForegroundColor Yellow
    Write-Host "可以使用以下命令安装:" -ForegroundColor Yellow
    Write-Host "  dotnet tool install -g dotnet-reportgenerator-globaltool" -ForegroundColor White
    Write-Host ""
    Write-Host "覆盖率数据文件: $($coverageFile.FullName)" -ForegroundColor Green
}

Write-Host ""
Write-Host "完成!" -ForegroundColor Green
