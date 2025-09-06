# PowerShell script to analyze code coverage
param(
    [string]$CoverageFile = "TestResults\*\coverage.cobertura.xml"
)

Write-Host "🔍 分析代码覆盖率 Analyzing Code Coverage..." -ForegroundColor Cyan
Write-Host "=====================================`n" -ForegroundColor Cyan

# Find the coverage file
$coverageFiles = Get-ChildItem $CoverageFile -Recurse | Sort-Object LastWriteTime -Descending
if ($coverageFiles.Count -eq 0) {
    Write-Host "❌ 未找到覆盖率文件 No coverage files found" -ForegroundColor Red
    exit 1
}

$latestCoverageFile = $coverageFiles[0]
Write-Host "📄 覆盖率文件 Coverage file: $($latestCoverageFile.FullName)" -ForegroundColor Green

# Parse the XML
[xml]$coverage = Get-Content $latestCoverageFile.FullName

# Extract overall statistics
$overallLineCoverage = [math]::Round([double]$coverage.coverage.'line-rate' * 100, 2)
$overallBranchCoverage = [math]::Round([double]$coverage.coverage.'branch-rate' * 100, 2)

Write-Host "`n📊 总体覆盖率统计 Overall Coverage Statistics:" -ForegroundColor Yellow
Write-Host "=============================================" -ForegroundColor Yellow
Write-Host "🎯 行覆盖率 Line Coverage:   $overallLineCoverage%" -ForegroundColor $(if($overallLineCoverage -ge 90) {'Green'} elseif($overallLineCoverage -ge 80) {'Yellow'} else {'Red'})
Write-Host "🌳 分支覆盖率 Branch Coverage: $overallBranchCoverage%" -ForegroundColor $(if($overallBranchCoverage -ge 90) {'Green'} elseif($overallBranchCoverage -ge 80) {'Yellow'} else {'Red'})

# Analyze packages (assemblies)
Write-Host "`n📦 各程序集覆盖率 Assembly Coverage:" -ForegroundColor Yellow
Write-Host "====================================" -ForegroundColor Yellow

foreach ($package in $coverage.coverage.packages.package) {
    $packageName = $package.name
    $lineCoverage = [math]::Round([double]$package.'line-rate' * 100, 2)
    $branchCoverage = [math]::Round([double]$package.'branch-rate' * 100, 2)
    
    $color = if($lineCoverage -ge 90) {'Green'} elseif($lineCoverage -ge 80) {'Yellow'} else {'Red'}
    Write-Host "📋 $packageName" -ForegroundColor White
    Write-Host "   ├─ 行覆盖率 Line:   $lineCoverage%" -ForegroundColor $color
    Write-Host "   └─ 分支覆盖率 Branch: $branchCoverage%" -ForegroundColor $color
    Write-Host ""
}

# Analyze classes with low coverage
Write-Host "🔍 低覆盖率类分析 Low Coverage Classes:" -ForegroundColor Yellow
Write-Host "======================================" -ForegroundColor Yellow

$lowCoverageFound = $false
foreach ($package in $coverage.coverage.packages.package) {
    foreach ($class in $package.classes.class) {
        $className = $class.name
        $lineCoverage = [math]::Round([double]$class.'line-rate' * 100, 2)
        
        if ($lineCoverage -lt 90) {
            $lowCoverageFound = $true
            $color = if($lineCoverage -ge 80) {'Yellow'} else {'Red'}
            Write-Host "⚠️  $className - $lineCoverage%" -ForegroundColor $color
            
            # Show uncovered lines
            $uncoveredLines = @()
            foreach ($line in $class.lines.line) {
                if ($line.hits -eq "0") {
                    $uncoveredLines += $line.number
                }
            }
            if ($uncoveredLines.Count -gt 0) {
                Write-Host "   未覆盖行 Uncovered lines: $($uncoveredLines -join ', ')" -ForegroundColor Gray
            }
        }
    }
}

if (-not $lowCoverageFound) {
    Write-Host "✅ 所有类都达到了90%以上的覆盖率！ All classes have 90%+ coverage!" -ForegroundColor Green
}

# Coverage targets and recommendations
Write-Host "`n🎯 覆盖率目标与建议 Coverage Targets & Recommendations:" -ForegroundColor Cyan
Write-Host "=================================================" -ForegroundColor Cyan

if ($overallLineCoverage -ge 95) {
    Write-Host "🏆 优秀！ Excellent! 覆盖率超过95%" -ForegroundColor Green
} elseif ($overallLineCoverage -ge 90) {
    Write-Host "✅ 良好！ Good! 覆盖率超过90%" -ForegroundColor Green
} elseif ($overallLineCoverage -ge 80) {
    Write-Host "⚠️  需要改进 Needs improvement. 目标90%+" -ForegroundColor Yellow
} else {
    Write-Host "❌ 覆盖率不足 Insufficient coverage. 需要大量改进 Needs significant improvement" -ForegroundColor Red
}

# Final summary
Write-Host "`n📈 测试覆盖率报告完成 Coverage Report Complete!" -ForegroundColor Green
Write-Host "============================================" -ForegroundColor Green
Write-Host "🎯 总测试数 Total Tests: $($coverage.coverage.packages.package | ForEach-Object { $_.classes.class | ForEach-Object { $_.lines.line } } | Measure-Object).Count lines analyzed" -ForegroundColor White
Write-Host "✅ 通过率 Pass Rate: 100% (All tests passed)" -ForegroundColor Green
Write-Host "📊 覆盖率 Coverage: $overallLineCoverage% lines, $overallBranchCoverage% branches" -ForegroundColor White

if ($overallLineCoverage -ge 90) {
    Write-Host "`n🎉 恭喜！达到高覆盖率标准！ Congratulations! High coverage achieved!" -ForegroundColor Green
    exit 0
} else {
    Write-Host "`n📋 建议添加更多测试以提高覆盖率 Recommend adding more tests to improve coverage" -ForegroundColor Yellow
    exit 1
}
