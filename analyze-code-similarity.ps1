# 分析代码相似度和重复模式
$testFiles = Get-ChildItem -Path "tests/Sqlx.Tests" -Recurse -Filter "*.cs" -File | 
    Where-Object { 
        $_.FullName -notmatch '\\obj\\' -and 
        $_.FullName -notmatch '\\bin\\' -and
        $_.Name -notmatch '\.g\.cs$' -and
        $_.Name -notmatch 'AssemblyInfo\.cs$' -and
        $_.Name -notmatch 'GlobalUsings\.g\.cs$'
    }

Write-Host "=== 分析测试代码相似模式 ===" -ForegroundColor Cyan
Write-Host ""

# 1. 统计使用 DataRow 的测试（可能可以参数化）
Write-Host "1. DataRow 参数化测试分析" -ForegroundColor Yellow
$dataRowFiles = @{}
foreach ($file in $testFiles) {
    $content = Get-Content $file.FullName -Raw
    if ($content) {
        $dataRowMatches = [regex]::Matches($content, '\[DataRow\(')
        if ($dataRowMatches.Count -gt 0) {
            $dataRowFiles[$file.Name] = $dataRowMatches.Count
        }
    }
}

$dataRowFiles.GetEnumerator() | 
    Sort-Object Value -Descending | 
    Select-Object -First 15 | 
    ForEach-Object {
        Write-Host "  $($_.Key): $($_.Value) 个 DataRow"
    }

Write-Host "`n总计: $($dataRowFiles.Count) 个文件使用 DataRow，共 $(($dataRowFiles.Values | Measure-Object -Sum).Sum) 个"

# 2. 统计重复的测试设置代码
Write-Host "`n2. 测试设置代码分析" -ForegroundColor Yellow
$setupPatterns = @{
    'TestInitialize' = 0
    'TestCleanup' = 0
    'ClassInitialize' = 0
    'ClassCleanup' = 0
    'new SqlBuilder' = 0
    'new SqlxContext' = 0
    'DbConnection' = 0
}

foreach ($file in $testFiles) {
    $content = Get-Content $file.FullName -Raw
    if ($content) {
        foreach ($pattern in $setupPatterns.Keys) {
            $matches = [regex]::Matches($content, [regex]::Escape($pattern))
            $setupPatterns[$pattern] += $matches.Count
        }
    }
}

$setupPatterns.GetEnumerator() | 
    Sort-Object Value -Descending | 
    ForEach-Object {
        Write-Host "  $($_.Key): $($_.Value) 次"
    }

# 3. 统计相似的断言模式
Write-Host "`n3. 断言模式分析" -ForegroundColor Yellow
$assertPatterns = @{
    'Assert.AreEqual' = 0
    'Assert.IsTrue' = 0
    'Assert.IsFalse' = 0
    'Assert.IsNotNull' = 0
    'Assert.IsNull' = 0
    'Assert.ThrowsException' = 0
    'Assert.Contains' = 0
}

foreach ($file in $testFiles) {
    $content = Get-Content $file.FullName -Raw
    if ($content) {
        foreach ($pattern in $assertPatterns.Keys) {
            $matches = [regex]::Matches($content, [regex]::Escape($pattern))
            $assertPatterns[$pattern] += $matches.Count
        }
    }
}

$assertPatterns.GetEnumerator() | 
    Sort-Object Value -Descending | 
    ForEach-Object {
        Write-Host "  $($_.Key): $($_.Value) 次"
    }

# 4. 查找可能的辅助方法机会
Write-Host "`n4. 重复代码模式（可提取为辅助方法）" -ForegroundColor Yellow

$helperOpportunities = @{
    'SqlDialect.SqlServer' = 0
    'SqlDialect.MySql' = 0
    'SqlDialect.PostgreSql' = 0
    'SqlDialect.SQLite' = 0
    'new TestEntity' = 0
    'new TestUser' = 0
    'ToList()' = 0
    'FirstOrDefault()' = 0
}

foreach ($file in $testFiles) {
    $content = Get-Content $file.FullName -Raw
    if ($content) {
        foreach ($pattern in $helperOpportunities.Keys) {
            $matches = [regex]::Matches($content, [regex]::Escape($pattern))
            $helperOpportunities[$pattern] += $matches.Count
        }
    }
}

$helperOpportunities.GetEnumerator() | 
    Sort-Object Value -Descending | 
    ForEach-Object {
        Write-Host "  $($_.Key): $($_.Value) 次"
    }

# 5. 统计测试类的平均大小
Write-Host "`n5. 测试文件大小分析" -ForegroundColor Yellow
$fileSizes = @{}
foreach ($file in $testFiles) {
    $lines = (Get-Content $file.FullName).Count
    $fileSizes[$file.Name] = $lines
}

$avgSize = ($fileSizes.Values | Measure-Object -Average).Average
$maxSize = ($fileSizes.Values | Measure-Object -Maximum).Maximum
$minSize = ($fileSizes.Values | Measure-Object -Minimum).Minimum

Write-Host "  平均行数: $([math]::Round($avgSize, 0))"
Write-Host "  最大行数: $maxSize"
Write-Host "  最小行数: $minSize"

Write-Host "`n  最大的测试文件（Top 10）:"
$fileSizes.GetEnumerator() | 
    Sort-Object Value -Descending | 
    Select-Object -First 10 | 
    ForEach-Object {
        Write-Host "    $($_.Key): $($_.Value) 行"
    }

# 6. 建议
Write-Host "`n=== 减少重复性的建议 ===" -ForegroundColor Green
Write-Host "1. 考虑创建测试基类或辅助类来共享常见的设置代码"
Write-Host "2. 使用 DataRow 参数化测试来减少重复的测试方法"
Write-Host "3. 提取常见的测试数据创建逻辑到工厂方法"
Write-Host "4. 考虑将大型测试文件拆分为更小的、更专注的测试类"
Write-Host "5. 创建自定义断言方法来封装常见的验证逻辑"
