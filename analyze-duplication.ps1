# 分析测试代码重复性
$testFiles = Get-ChildItem -Path "tests/Sqlx.Tests" -Recurse -Filter "*.cs" -File

$duplicatePatterns = @{}
$testMethodCounts = @{}

foreach ($file in $testFiles) {
    $content = Get-Content $file.FullName -Raw
    
    # 统计测试方法数量
    $testMethods = [regex]::Matches($content, '\[TestMethod\]')
    $testMethodCounts[$file.Name] = $testMethods.Count
    
    # 查找重复的测试模式
    $patterns = @(
        'SqlDialect\.SqlServer',
        'SqlDialect\.MySql',
        'SqlDialect\.PostgreSql',
        'SqlDialect\.SQLite',
        'SqlDialect\.Oracle',
        'SqlDialect\.DB2',
        'DataRow.*SqlDialect',
        'TestInitialize',
        'TestCleanup',
        'ClassInitialize',
        'ClassCleanup'
    )
    
    foreach ($pattern in $patterns) {
        $matches = [regex]::Matches($content, $pattern)
        if ($matches.Count -gt 0) {
            if (-not $duplicatePatterns.ContainsKey($pattern)) {
                $duplicatePatterns[$pattern] = @()
            }
            $duplicatePatterns[$pattern] += @{
                File = $file.Name
                Count = $matches.Count
            }
        }
    }
}

Write-Host "`n=== 测试方法数量最多的文件 ===" -ForegroundColor Cyan
$testMethodCounts.GetEnumerator() | 
    Where-Object { $_.Value -gt 20 } | 
    Sort-Object Value -Descending | 
    Select-Object -First 20 | 
    ForEach-Object {
        Write-Host "$($_.Key): $($_.Value) 个测试方法"
    }

Write-Host "`n=== 重复模式分析 ===" -ForegroundColor Cyan
foreach ($pattern in $duplicatePatterns.Keys) {
    $total = ($duplicatePatterns[$pattern] | Measure-Object -Property Count -Sum).Sum
    $fileCount = $duplicatePatterns[$pattern].Count
    Write-Host "`n模式: $pattern"
    Write-Host "  出现在 $fileCount 个文件中，总计 $total 次"
    if ($fileCount -gt 10) {
        Write-Host "  前10个文件:" -ForegroundColor Yellow
        $duplicatePatterns[$pattern] | 
            Sort-Object Count -Descending | 
            Select-Object -First 10 | 
            ForEach-Object {
                Write-Host "    $($_.File): $($_.Count) 次"
            }
    }
}

# 查找可能的重复测试类
Write-Host "`n=== 查找相似的测试文件 ===" -ForegroundColor Cyan
$fileGroups = $testFiles | Group-Object { 
    $name = $_.BaseName
    # 移除常见后缀
    $name = $name -replace 'Tests$', ''
    $name = $name -replace 'E2ETests$', ''
    $name = $name -replace 'StrictTests$', ''
    $name = $name -replace 'DialectTests$', ''
    $name = $name -replace 'IntegrationTests$', ''
    $name
}

$fileGroups | Where-Object { $_.Count -gt 1 } | ForEach-Object {
    Write-Host "`n组: $($_.Name)"
    $_.Group | ForEach-Object {
        Write-Host "  - $($_.Name)"
    }
}
