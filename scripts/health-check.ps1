#!/usr/bin/env pwsh
# Sqlx Project Health Check Script
# 项目健康检查工具

<#
.SYNOPSIS
    Sqlx项目完整健康检查

.DESCRIPTION
    检查项目的各个方面，包括代码、文档、构建、测试等

.EXAMPLE
    .\health-check.ps1
#>

param(
    [switch]$Verbose,
    [switch]$FixIssues
)

$ErrorActionPreference = "Continue"
$script:issuesFound = 0
$script:checksTotal = 0
$script:checksPassed = 0

function Write-Header {
    param([string]$Text)
    Write-Host "`n========================================" -ForegroundColor Cyan
    Write-Host "  $Text" -ForegroundColor Cyan
    Write-Host "========================================`n" -ForegroundColor Cyan
}

function Write-CheckResult {
    param(
        [string]$CheckName,
        [bool]$Passed,
        [string]$Details = ""
    )
    
    $script:checksTotal++
    
    if ($Passed) {
        $script:checksPassed++
        Write-Host "[✓] " -ForegroundColor Green -NoNewline
        Write-Host "$CheckName" -ForegroundColor White
        if ($Details -and $Verbose) {
            Write-Host "    $Details" -ForegroundColor Gray
        }
    } else {
        $script:issuesFound++
        Write-Host "[✗] " -ForegroundColor Red -NoNewline
        Write-Host "$CheckName" -ForegroundColor White
        if ($Details) {
            Write-Host "    $Details" -ForegroundColor Yellow
        }
    }
}

function Test-FileExists {
    param([string]$Path, [string]$Description)
    $exists = Test-Path $Path
    Write-CheckResult -CheckName "File: $Description" -Passed $exists -Details $Path
    return $exists
}

function Test-DirectoryExists {
    param([string]$Path, [string]$Description)
    $exists = Test-Path $Path -PathType Container
    Write-CheckResult -CheckName "Directory: $Description" -Passed $exists -Details $Path
    return $exists
}

# ============================================
# Start Health Check
# ============================================

Write-Host @"
   _____       _       
  / ____|     | |      
 | (___   __ _| |_  __ 
  \___ \ / _`` | \ \/ / 
  ____) | (_| | |>  <  
 |_____/ \__, |_/_/\_\ 
            | |        
            |_|        

Sqlx Project Health Check
"@ -ForegroundColor Cyan

Write-Host "Starting health check..." -ForegroundColor White
Write-Host "Time: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')`n" -ForegroundColor Gray

# ============================================
# 1. Project Structure
# ============================================
Write-Header "1. Project Structure"

Test-FileExists "Sqlx.sln" "Solution file"
Test-FileExists "README.md" "Main README"
Test-FileExists "LICENSE.txt" "License file"
Test-FileExists "Directory.Build.props" "Central build props"
Test-FileExists "Directory.Packages.props" "Central packages"

Test-DirectoryExists "src/Sqlx" "Core library"
Test-DirectoryExists "src/Sqlx.Generator" "Source generator"
Test-DirectoryExists "src/Sqlx.Extension" "VS Extension"
Test-DirectoryExists "tests/Sqlx.Tests" "Unit tests"
Test-DirectoryExists "tests/Sqlx.Benchmarks" "Benchmarks"
Test-DirectoryExists "samples" "Sample projects"
Test-DirectoryExists "docs" "Documentation"

# ============================================
# 2. Core Library Files
# ============================================
Write-Header "2. Core Library Files"

Test-FileExists "src/Sqlx/Sqlx.csproj" "Core project file"
Test-FileExists "src/Sqlx/ICrudRepository.cs" "ICrudRepository interface"
Test-FileExists "src/Sqlx/IQueryRepository.cs" "IQueryRepository interface"
Test-FileExists "src/Sqlx/IBatchRepository.cs" "IBatchRepository interface"
Test-FileExists "src/Sqlx/ParameterizedSql.cs" "ParameterizedSql class"
Test-FileExists "src/Sqlx/PagedResult.cs" "PagedResult class"

# ============================================
# 3. Generator Files
# ============================================
Write-Header "3. Source Generator Files"

Test-FileExists "src/Sqlx.Generator/Sqlx.Generator.csproj" "Generator project"
Test-FileExists "src/Sqlx.Generator/CSharpGenerator.cs" "Main generator"
Test-FileExists "src/Sqlx.Generator/AbstractGenerator.cs" "Abstract generator"

# ============================================
# 4. VS Extension Files
# ============================================
Write-Header "4. Visual Studio Extension Files"

Test-FileExists "src/Sqlx.Extension/Sqlx.Extension.csproj" "Extension project"
Test-FileExists "src/Sqlx.Extension/source.extension.vsixmanifest" "VSIX manifest"
Test-FileExists "src/Sqlx.Extension/SqlxExtensionPackage.cs" "Extension package"
Test-FileExists "src/Sqlx.Extension/SqlxExtension.vsct" "Command definitions"
Test-FileExists "src/Sqlx.Extension/License.txt" "Extension license"

# Tool Windows
Test-FileExists "src/Sqlx.Extension/ToolWindows/SqlPreviewWindow.cs" "SQL Preview window"
Test-FileExists "src/Sqlx.Extension/ToolWindows/GeneratedCodeWindow.cs" "Generated Code window"
Test-FileExists "src/Sqlx.Extension/ToolWindows/QueryTesterWindow.cs" "Query Tester window"
Test-FileExists "src/Sqlx.Extension/ToolWindows/RepositoryExplorerWindow.cs" "Repository Explorer"

# Classifiers
Test-FileExists "src/Sqlx.Extension/Classifiers/SqlTemplateClassifier.cs" "SQL classifier"
Test-FileExists "src/Sqlx.Extension/Classifiers/SqlTemplateClassifierProvider.cs" "Classifier provider"

# ============================================
# 5. Documentation Files
# ============================================
Write-Header "5. Documentation Files"

$docFiles = @(
    "README.md",
    "INDEX.md",
    "INSTALL.md",
    "TUTORIAL.md",
    "FAQ.md",
    "TROUBLESHOOTING.md",
    "MIGRATION_GUIDE.md",
    "PERFORMANCE.md",
    "QUICK_REFERENCE.md",
    "CHANGELOG.md",
    "CONTRIBUTING.md",
    "HOW_TO_RELEASE.md"
)

foreach ($doc in $docFiles) {
    Test-FileExists $doc "Doc: $doc"
}

Test-FileExists "docs/QUICK_START_GUIDE.md" "Quick start guide"
Test-FileExists "docs/API_REFERENCE.md" "API reference"
Test-FileExists "docs/BEST_PRACTICES.md" "Best practices"
Test-FileExists "docs/web/index.html" "GitHub Pages"

# ============================================
# 6. Build Scripts
# ============================================
Write-Header "6. Build Scripts"

Test-FileExists "scripts/build.ps1" "Build script"
Test-FileExists "src/Sqlx.Extension/build-vsix.ps1" "VSIX build script"
Test-FileExists "src/Sqlx.Extension/build-vsix.bat" "VSIX build batch"
Test-FileExists "release.ps1" "Release script (PowerShell)"
Test-FileExists "release.sh" "Release script (Bash)"

# ============================================
# 7. .NET SDK and Tools
# ============================================
Write-Header "7. .NET SDK and Tools"

try {
    $dotnetVersion = dotnet --version
    Write-CheckResult -CheckName ".NET SDK installed" -Passed $true -Details "Version: $dotnetVersion"
} catch {
    Write-CheckResult -CheckName ".NET SDK installed" -Passed $false -Details "dotnet command not found"
}

try {
    $msbuildPath = Get-Command msbuild -ErrorAction SilentlyContinue
    if ($msbuildPath) {
        Write-CheckResult -CheckName "MSBuild available" -Passed $true -Details $msbuildPath.Source
    } else {
        Write-CheckResult -CheckName "MSBuild available" -Passed $false -Details "msbuild not in PATH"
    }
} catch {
    Write-CheckResult -CheckName "MSBuild available" -Passed $false
}

# ============================================
# 8. Git Status
# ============================================
Write-Header "8. Git Status"

try {
    $gitStatus = git status --porcelain
    $isClean = [string]::IsNullOrWhiteSpace($gitStatus)
    Write-CheckResult -CheckName "Working directory clean" -Passed $isClean -Details $(if ($isClean) { "No uncommitted changes" } else { "Uncommitted changes found" })
    
    $branch = git branch --show-current
    Write-CheckResult -CheckName "Git branch" -Passed $true -Details "Current: $branch"
    
    $unpushed = git log origin/$branch..$branch --oneline 2>$null
    $hasUnpushed = -not [string]::IsNullOrWhiteSpace($unpushed)
    Write-CheckResult -CheckName "All commits pushed" -Passed (-not $hasUnpushed) -Details $(if ($hasUnpushed) { "Unpushed commits found" } else { "All commits pushed" })
} catch {
    Write-CheckResult -CheckName "Git repository" -Passed $false -Details "Git not available or not a repository"
}

# ============================================
# 9. Project Build Test
# ============================================
Write-Header "9. Build Test"

Write-Host "Testing dotnet restore..." -ForegroundColor Gray
try {
    $restoreOutput = dotnet restore 2>&1
    $restoreSuccess = $LASTEXITCODE -eq 0
    Write-CheckResult -CheckName "dotnet restore" -Passed $restoreSuccess -Details $(if ($restoreSuccess) { "Success" } else { "Failed" })
} catch {
    Write-CheckResult -CheckName "dotnet restore" -Passed $false -Details $_.Exception.Message
}

Write-Host "Testing dotnet build..." -ForegroundColor Gray
try {
    $buildOutput = dotnet build -c Release --no-restore 2>&1
    $buildSuccess = $LASTEXITCODE -eq 0
    Write-CheckResult -CheckName "dotnet build" -Passed $buildSuccess -Details $(if ($buildSuccess) { "Success" } else { "Failed" })
} catch {
    Write-CheckResult -CheckName "dotnet build" -Passed $false -Details $_.Exception.Message
}

# ============================================
# 10. Test Execution
# ============================================
Write-Header "10. Tests"

if (Test-Path "tests/Sqlx.Tests/Sqlx.Tests.csproj") {
    Write-Host "Running tests..." -ForegroundColor Gray
    try {
        $testOutput = dotnet test tests/Sqlx.Tests/Sqlx.Tests.csproj --no-build --verbosity quiet 2>&1
        $testSuccess = $LASTEXITCODE -eq 0
        Write-CheckResult -CheckName "Unit tests" -Passed $testSuccess -Details $(if ($testSuccess) { "All tests passed" } else { "Some tests failed" })
    } catch {
        Write-CheckResult -CheckName "Unit tests" -Passed $false -Details $_.Exception.Message
    }
} else {
    Write-CheckResult -CheckName "Unit tests" -Passed $false -Details "Test project not found"
}

# ============================================
# 11. Documentation Integrity
# ============================================
Write-Header "11. Documentation Integrity"

# Check for broken internal links (basic check)
$readmeContent = Get-Content "README.md" -Raw -ErrorAction SilentlyContinue
if ($readmeContent) {
    $linksToCheck = @(
        @{ Link = "INDEX.md"; Name = "INDEX.md link" },
        @{ Link = "INSTALL.md"; Name = "INSTALL.md link" },
        @{ Link = "TUTORIAL.md"; Name = "TUTORIAL.md link" },
        @{ Link = "FAQ.md"; Name = "FAQ.md link" }
    )
    
    foreach ($linkCheck in $linksToCheck) {
        $hasLink = $readmeContent -match $linkCheck.Link
        Write-CheckResult -CheckName $linkCheck.Name -Passed $hasLink
    }
}

# Check INDEX.md completeness
if (Test-Path "INDEX.md") {
    $indexContent = Get-Content "INDEX.md" -Raw
    $hasNavigation = $indexContent -match "完整文档导航索引"
    Write-CheckResult -CheckName "INDEX.md has navigation" -Passed $hasNavigation
}

# ============================================
# 12. Version Consistency
# ============================================
Write-Header "12. Version Consistency"

if (Test-Path "Directory.Build.props") {
    $buildProps = [xml](Get-Content "Directory.Build.props")
    $version = $buildProps.Project.PropertyGroup.Version
    Write-CheckResult -CheckName "Version defined" -Passed (-not [string]::IsNullOrEmpty($version)) -Details "Version: $version"
}

if (Test-Path "VERSION") {
    Write-CheckResult -CheckName "VERSION file exists" -Passed $true
}

# ============================================
# Summary
# ============================================
Write-Header "Summary"

$passRate = if ($script:checksTotal -gt 0) { 
    [math]::Round(($script:checksPassed / $script:checksTotal) * 100, 1) 
} else { 
    0 
}

Write-Host "Total Checks:  " -NoNewline
Write-Host $script:checksTotal -ForegroundColor Cyan

Write-Host "Passed:        " -NoNewline
Write-Host $script:checksPassed -ForegroundColor Green

Write-Host "Failed:        " -NoNewline
Write-Host ($script:checksTotal - $script:checksPassed) -ForegroundColor $(if ($script:issuesFound -eq 0) { "Green" } else { "Red" })

Write-Host "Pass Rate:     " -NoNewline
Write-Host "$passRate%" -ForegroundColor $(if ($passRate -ge 95) { "Green" } elseif ($passRate -ge 80) { "Yellow" } else { "Red" })

Write-Host ""

if ($script:issuesFound -eq 0) {
    Write-Host "✓ All checks passed! Project is healthy." -ForegroundColor Green
    exit 0
} else {
    Write-Host "✗ Found $script:issuesFound issue(s). Please review." -ForegroundColor Yellow
    exit 1
}

