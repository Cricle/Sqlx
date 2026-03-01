# 分析实际源代码的重复性（排除生成文件和编译输出）
$testFiles = Get-ChildItem -Path "tests/Sqlx.Tests" -Recurse -Filter "*.cs" -File | 
    Where-Object { 
        $_.FullName -notmatch '\\obj\\' -and 
        $_.FullName -notmatch '\\bin\\' -and
        $_.Name -notmatch '\.g\.cs$' -and
        $_.Name -notmatch 'AssemblyInfo\.cs$' -and
        $_.Name -notmatch 'GlobalUsings\.g\.cs$'
    }

Write-Host "=== 实际源代码文件统计 ===" -ForegroundColor Cyan
Write-Host "总文件数: $($testFiles.Count)"

# 统计测试方法数量
$totalTestMethods = 0
$testMethodCounts = @{}

foreach ($file in $testFiles) {
    $content = Get-Content $file.FullName -Raw
    if ($content) {
        $testMethods = [regex]::Matches($content, '\[TestMethod\]')
        $testMethodCounts[$file.Name] = $testMethods.Count
        $totalTestMethods += $testMethods.Count
    }
}

Write-Host "总测试方法数: $totalTestMethods"
Write-Host ""

# 查找测试方法最多的文件
Write-Host "=== 测试方法数量最多的文件（Top 15） ===" -ForegroundColor Yellow
$testMethodCounts.GetEnumerator() | 
    Where-Object { $_.Value -gt 0 } | 
    Sort-Object Value -Descending | 
    Select-Object -First 15 | 
    ForEach-Object {
        Write-Host "$($_.Key): $($_.Value) 个测试方法"
    }

# 分析重复的测试模式
Write-Host "`n=== 重复测试模式分析 ===" -ForegroundColor Cyan

$dialectTestFiles = $testFiles | Where-Object { 
    $content = Get-Content $_.FullName -Raw
    $content -match 'DataRow.*SqlDialect' -or $content -match 'TestMethod.*Dialect'
}

Write-Host "`n包含 Dialect 测试的文件: $($dialectTestFiles.Count) 个"
$dialectTestFiles | Select-Object -First 10 | ForEach-Object {
    $content = Get-Content $_.FullName -Raw
    $dataRowCount = ([regex]::Matches($content, 'DataRow')).Count
    Write-Host "  $($_.Name): $dataRowCount 个 DataRow"
}

# 查找可能可以合并的测试文件
Write-Host "`n=== 可能可以合并的测试文件 ===" -ForegroundColor Yellow

$fileGroups = $testFiles | Group-Object { 
    $name = $_.BaseName
    # 移除常见后缀
    $name = $name -replace 'Tests$', ''
    $name = $name -replace 'E2ETests$', ''
    $name = $name -replace 'StrictTests$', ''
    $name = $name -replace 'DialectTests$', ''
    $name = $name -replace 'IntegrationTests$', ''
    $name = $name -replace 'AdvancedTests$', ''
    $name = $name -replace 'ComprehensiveTests$', ''
    $name
}

$potentialMerges = $fileGroups | Where-Object { $_.Count -gt 1 }
Write-Host "发现 $($potentialMerges.Count) 组可能可以合并的文件"

$potentialMerges | Select-Object -First 10 | ForEach-Object {
    Write-Host "`n组: $($_.Name) ($($_.Count) 个文件)"
    $_.Group | ForEach-Object {
        $methodCount = $testMethodCounts[$_.Name]
        Write-Host "  - $($_.Name) ($methodCount 个测试方法)"
    }
}

# 统计 E2E 测试
Write-Host "`n=== E2E 测试统计 ===" -ForegroundColor Cyan
$e2eFiles = $testFiles | Where-Object { $_.FullName -match '\\E2E\\' }
Write-Host "E2E 测试文件数: $($e2eFiles.Count)"

$e2eByCategory = $e2eFiles | Group-Object { 
    $path = $_.DirectoryName
    if ($path -match '\\E2E\\([^\\]+)') {
        $matches[1]
    } else {
        "Other"
    }
}

$e2eByCategory | Sort-Object Count -Descending | ForEach-Object {
    Write-Host "  $($_.Name): $($_.Count) 个文件"
}
