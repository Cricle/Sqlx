# Sqlx 项目报告文件清理脚本
# 用于清理开发过程中生成的临时报告文件

Write-Host "🧹 Sqlx 项目清理脚本" -ForegroundColor Green
Write-Host "===============================================" -ForegroundColor Green

# 定义要清理的报告文件模式
$reportPatterns = @(
    "*ACHIEVEMENT_REPORT*.md",
    "*COVERAGE_IMPROVEMENT*.md", 
    "*CONTINUOUS_IMPROVEMENT*.md",
    "*DETAILED_COVERAGE*.md",
    "*ENHANCED_*.md",
    "*FINAL_COMPILATION*.md",
    "*FINAL_COMPREHENSIVE*.md",
    "*FINAL_COVERAGE*.md",
    "*FINAL_TEST_PROGRESS*.md",
    "*FUNCTION_VERIFICATION*.md",
    "*RESTRUCTURE_SUMMARY*.md",
    "*ULTIMATE_*.md",
    "*UNIT_TEST_COVERAGE*.md",
    "TEST_COVERAGE_REPORT.md",
    "COMPLETE_PROJECT_SUMMARY.md"
)

# 保留的重要文件
$keepFiles = @(
    "README.md",
    "CONTRIBUTING.md", 
    "PROJECT_FINAL_STATUS.md",
    "FINAL_TEST_ENHANCEMENT_REPORT.md",
    "ADVANCED_USAGE_GUIDE.md"
)

Write-Host "🔍 扫描项目根目录中的报告文件..." -ForegroundColor Yellow

$filesToDelete = @()
foreach ($pattern in $reportPatterns) {
    $files = Get-ChildItem -Path "." -Name $pattern -File
    foreach ($file in $files) {
        if ($keepFiles -notcontains $file) {
            $filesToDelete += $file
        }
    }
}

if ($filesToDelete.Count -eq 0) {
    Write-Host "✅ 没有找到需要清理的文件" -ForegroundColor Green
    exit 0
}

Write-Host "📋 找到以下文件可以清理:" -ForegroundColor Cyan
foreach ($file in $filesToDelete) {
    Write-Host "  • $file" -ForegroundColor White
}

Write-Host ""
$confirm = Read-Host "是否继续清理这些文件? (y/N)"

if ($confirm -eq 'y' -or $confirm -eq 'Y') {
    Write-Host "🗑️  开始清理文件..." -ForegroundColor Yellow
    
    $deletedCount = 0
    foreach ($file in $filesToDelete) {
        try {
            Remove-Item $file -Force
            Write-Host "  ✓ 已删除: $file" -ForegroundColor Green
            $deletedCount++
        }
        catch {
            Write-Host "  ✗ 删除失败: $file - $($_.Exception.Message)" -ForegroundColor Red
        }
    }
    
    Write-Host ""
    Write-Host "🎉 清理完成! 共删除 $deletedCount 个文件" -ForegroundColor Green
    Write-Host ""
    Write-Host "📚 保留的重要文档:" -ForegroundColor Cyan
    foreach ($file in $keepFiles) {
        if (Test-Path $file) {
            Write-Host "  • $file" -ForegroundColor White
        }
    }
}
else {
    Write-Host "❌ 取消清理操作" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "💡 提示: 重要的项目文档已保留，包括:" -ForegroundColor Blue
Write-Host "  • README.md - 项目主要文档" -ForegroundColor White
Write-Host "  • CONTRIBUTING.md - 贡献指南" -ForegroundColor White  
Write-Host "  • PROJECT_FINAL_STATUS.md - 最终状态报告" -ForegroundColor White
Write-Host "  • FINAL_TEST_ENHANCEMENT_REPORT.md - 测试增强报告" -ForegroundColor White
Write-Host "  • docs/ - 完整的项目文档" -ForegroundColor White
