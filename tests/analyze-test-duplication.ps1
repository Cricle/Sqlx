# Test Duplication Analysis Script
# Scans test files and identifies naming patterns indicating duplication

$testPath = "Sqlx.Tests"
$outputFile = "Sqlx.Tests.Backup/DUPLICATION_ANALYSIS.md"

Write-Host "Analyzing test files for duplication patterns..." -ForegroundColor Cyan

# Get all test files
$testFiles = Get-ChildItem -Path $testPath -Filter "*Tests.cs" -Recurse | 
    Where-Object { $_.FullName -notlike "*\bin\*" -and $_.FullName -notlike "*\obj\*" -and $_.FullName -notlike "*\TestResults\*" }

# Group files by base name patterns
$patterns = @{
    "SetPlaceholder" = @()
    "ValuesPlaceholder" = @()
    "Dialect" = @()
    "ExpressionBlockResult" = @()
    "TypeConverter" = @()
    "SubQuery" = @()
    "SqlxQueryable" = @()
    "ResultReader" = @()
    "Performance" = @()
    "DynamicUpdate" = @()
    "SetExpression" = @()
    "Placeholder" = @()
    "SourceGenerator" = @()
}

foreach ($file in $testFiles) {
    $name = $file.Name
    
    foreach ($pattern in $patterns.Keys) {
        if ($name -like "*$pattern*") {
            $patterns[$pattern] += $file
            break
        }
    }
}

# Generate report
$report = @"
# Test Duplication Analysis Report

**Generated**: $(Get-Date -Format "yyyy-MM-dd HH:mm:ss")
**Total Test Files Analyzed**: $($testFiles.Count)

## Duplicate File Groups

"@

$totalDuplicates = 0
$duplicateGroups = 0

foreach ($pattern in $patterns.Keys | Sort-Object) {
    $files = $patterns[$pattern]
    if ($files.Count -gt 1) {
        $duplicateGroups++
        $totalDuplicates += $files.Count
        
        $report += @"

### $pattern Tests ($($files.Count) files)

"@
        foreach ($file in $files | Sort-Object Name) {
            $lines = (Get-Content $file.FullName).Count
            $report += "- ``$($file.Name)`` ($lines lines)`n"
        }
        
        $report += @"

**Merge Target**: ``Placeholders/$pattern`Tests.cs`` or appropriate category

"@
    }
}

$report += @"

## Summary Statistics

- **Duplicate Groups Identified**: $duplicateGroups
- **Total Files in Duplicate Groups**: $totalDuplicates
- **Duplication Rate**: $([math]::Round(($totalDuplicates / $testFiles.Count) * 100, 2))%
- **Potential File Reduction**: $($totalDuplicates - $duplicateGroups) files

## File Merge Mapping

"@

# Create merge mapping
$mergeMap = @{
    "SetPlaceholder" = "Placeholders/SetPlaceholderTests.cs"
    "ValuesPlaceholder" = "Placeholders/ValuesPlaceholderTests.cs"
    "Dialect" = "Dialects/DialectTests.cs"
    "ExpressionBlockResult" = "Expressions/ExpressionBlockResultTests.cs"
    "TypeConverter" = "Core/TypeConverterTests.cs"
    "SubQuery" = "QueryBuilder/SubQueryTests.cs"
    "SqlxQueryable" = "QueryBuilder/SqlxQueryableTests.cs"
    "ResultReader" = "EntityProvider/ResultReaderTests.cs"
    "Performance" = "Integration/PerformanceTests.cs"
}

foreach ($pattern in $mergeMap.Keys | Sort-Object) {
    $files = $patterns[$pattern]
    if ($files.Count -gt 0) {
        $report += @"

### $pattern Group → ``$($mergeMap[$pattern])``

Source files to merge:
"@
        foreach ($file in $files | Sort-Object Name) {
            $report += "- $($file.Name)`n"
        }
    }
}

$report += @"

## Refactoring Priority

Based on file count and estimated complexity:

1. **High Priority** (9+ files)
   - SetPlaceholder tests (9 files)
   
2. **Medium Priority** (5-8 files)
   - ValuesPlaceholder tests (6 files)
   - Dialect tests (6 files)
   
3. **Low Priority** (2-4 files)
   - ExpressionBlockResult tests (3 files)
   - TypeConverter tests (2 files)
   - SubQuery tests (2 files)
   - SqlxQueryable tests (2 files)
   - ResultReader tests (2 files)
   - Performance tests (2 files)

## Next Steps

1. Create test infrastructure (TestBase, helpers)
2. Start with high-priority SetPlaceholder tests
3. Progress through medium and low priority groups
4. Validate after each merge operation
"@

# Write report
$report | Out-File -FilePath $outputFile -Encoding UTF8

Write-Host "Analysis complete! Report saved to: $outputFile" -ForegroundColor Green
Write-Host "Duplicate groups found: $duplicateGroups" -ForegroundColor Yellow
Write-Host "Total files in duplicate groups: $totalDuplicates" -ForegroundColor Yellow
