# PowerShell脚本用于批量修复测试问题
Write-Host "开始修复Sqlx项目的测试问题..." -ForegroundColor Green

# 运行测试并捕获输出
$testResult = dotnet test --verbosity normal --logger "console;verbosity=detailed" 2>&1

# 分析失败的测试
$failedTests = @()
$testResult | ForEach-Object {
    if ($_ -match "error TESTERROR.*?(\w+\.\w+).*?错误消息: (.+)") {
        $failedTests += @{
            TestName = $matches[1]
            Error = $matches[2]
        }
    }
}

Write-Host "发现 $($failedTests.Count) 个失败的测试:" -ForegroundColor Yellow
foreach ($test in $failedTests) {
    Write-Host "  - $($test.TestName): $($test.Error)" -ForegroundColor Red
}

# 分类失败原因
$sqlTemplateIssues = $failedTests | Where-Object { $_.TestName -like "*SqlTemplate*" }
$crudIssues = $failedTests | Where-Object { $_.TestName -like "*Crud*" -or $_.TestName -like "*Update*" -or $_.TestName -like "*Insert*" }
$dialectIssues = $failedTests | Where-Object { $_.TestName -like "*Dialect*" -or $_.TestName -like "*SqlServer*" -or $_.TestName -like "*MySQL*" }

Write-Host "`n问题分类:" -ForegroundColor Cyan
Write-Host "  SQL模板问题: $($sqlTemplateIssues.Count)" -ForegroundColor Yellow
Write-Host "  CRUD操作问题: $($crudIssues.Count)" -ForegroundColor Yellow  
Write-Host "  数据库方言问题: $($dialectIssues.Count)" -ForegroundColor Yellow

Write-Host "`n建议的修复策略:" -ForegroundColor Green
Write-Host "1. 检查SQL生成是否符合预期格式" -ForegroundColor White
Write-Host "2. 调整测试断言以匹配实际实现行为" -ForegroundColor White
Write-Host "3. 修复参数化查询的生成逻辑" -ForegroundColor White
Write-Host "4. 确保数据库方言的正确应用" -ForegroundColor White

