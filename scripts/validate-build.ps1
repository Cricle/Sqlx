# -----------------------------------------------------------------------
# Build validation script for Sqlx solution
# This script validates that all projects in the solution can build successfully
# -----------------------------------------------------------------------

param(
    [string]$Configuration = "Debug",
    [switch]$SkipTests,
    [switch]$Verbose
)

# Set error action preference
$ErrorActionPreference = "Stop"

# Colors for output
$Green = "Green"
$Red = "Red"
$Yellow = "Yellow"
$Cyan = "Cyan"

function Write-ColoredOutput {
    param([string]$Message, [string]$Color = "White")
    Write-Host $Message -ForegroundColor $Color
}

function Test-ProjectExists {
    param([string]$ProjectPath)
    
    if (Test-Path $ProjectPath) {
        Write-ColoredOutput "✅ Found: $ProjectPath" $Green
        return $true
    } else {
        Write-ColoredOutput "❌ Missing: $ProjectPath" $Red
        return $false
    }
}

function Build-Project {
    param([string]$ProjectPath, [string]$ProjectName)
    
    Write-ColoredOutput "`n🔨 Building $ProjectName..." $Cyan
    
    try {
        $buildArgs = @("build", $ProjectPath, "-c", $Configuration, "--no-restore")
        if ($Verbose) {
            $buildArgs += "-v", "detailed"
        }
        
        & dotnet @buildArgs
        
        if ($LASTEXITCODE -eq 0) {
            Write-ColoredOutput "✅ $ProjectName built successfully" $Green
            return $true
        } else {
            Write-ColoredOutput "❌ $ProjectName build failed" $Red
            return $false
        }
    }
    catch {
        Write-ColoredOutput "❌ Error building ${ProjectName}: $($_.Exception.Message)" $Red
        return $false
    }
}

function Run-Tests {
    param([string]$TestProjectPath, [string]$ProjectName)
    
    Write-ColoredOutput "`n🧪 Running tests for $ProjectName..." $Cyan
    
    try {
        & dotnet test $TestProjectPath -c $Configuration --no-build --logger "console;verbosity=normal"
        
        if ($LASTEXITCODE -eq 0) {
            Write-ColoredOutput "✅ $ProjectName tests passed" $Green
            return $true
        } else {
            Write-ColoredOutput "❌ $ProjectName tests failed" $Red
            return $false
        }
    }
    catch {
        Write-ColoredOutput "❌ Error running tests for ${ProjectName}: $($_.Exception.Message)" $Red
        return $false
    }
}

# Main validation process
Write-ColoredOutput "🚀 === Sqlx Solution Build Validation ===" $Cyan
Write-ColoredOutput "Configuration: $Configuration" $Yellow
Write-ColoredOutput "Skip Tests: $SkipTests" $Yellow
Write-ColoredOutput ""

# Check if solution file exists
$solutionFile = "Sqlx.sln"
if (-not (Test-Path $solutionFile)) {
    Write-ColoredOutput "❌ Solution file not found: $solutionFile" $Red
    exit 1
}

Write-ColoredOutput "✅ Solution file found: $solutionFile" $Green

# Define all projects to validate
$projects = @(
    @{ Name = "Sqlx"; Path = "src\Sqlx\Sqlx.csproj"; Type = "Library" },
    @{ Name = "Sqlx.Generator"; Path = "src\Sqlx.Generator\Sqlx.Generator.csproj"; Type = "Library" },
    @{ Name = "Sqlx.VisualStudio.Extension"; Path = "src\Sqlx.VisualStudio.Extension\Sqlx.VisualStudio.Extension.csproj"; Type = "VSIX" },
    @{ Name = "Sqlx.Tests"; Path = "tests\Sqlx.Tests\Sqlx.Tests.csproj"; Type = "Test" },
    @{ Name = "Sqlx.Generator.Tests"; Path = "tests\Sqlx.Generator.Tests\Sqlx.Generator.Tests.csproj"; Type = "Test" },
    @{ Name = "SqlxDemo"; Path = "samples\SqlxDemo\SqlxDemo.csproj"; Type = "Executable" },
    @{ Name = "AdvancedTemplateDemo"; Path = "samples\AdvancedTemplateDemo\AdvancedTemplateDemo.csproj"; Type = "Executable" },
    @{ Name = "IntegrationShowcase"; Path = "samples\IntegrationShowcase\IntegrationShowcase.csproj"; Type = "Executable" },
    @{ Name = "SqlxTemplateValidator"; Path = "tools\SqlxTemplateValidator\SqlxTemplateValidator.csproj"; Type = "Tool" }
)

# Check project existence
Write-ColoredOutput "`n📁 Checking project files..." $Cyan
$allProjectsExist = $true
foreach ($project in $projects) {
    if (-not (Test-ProjectExists $project.Path)) {
        $allProjectsExist = $false
    }
}

if (-not $allProjectsExist) {
    Write-ColoredOutput "`n❌ Some projects are missing. Cannot proceed with build validation." $Red
    exit 1
}

# Restore packages
Write-ColoredOutput "`n📦 Restoring NuGet packages..." $Cyan
try {
    & dotnet restore $solutionFile
    if ($LASTEXITCODE -eq 0) {
        Write-ColoredOutput "✅ Package restore completed" $Green
    } else {
        Write-ColoredOutput "❌ Package restore failed" $Red
        exit 1
    }
}
catch {
    Write-ColoredOutput "❌ Error during package restore: $($_.Exception.Message)" $Red
    exit 1
}

# Build all projects
Write-ColoredOutput "`n🔨 Building all projects..." $Cyan
$buildResults = @()

foreach ($project in $projects) {
    # Skip VSIX projects for now as they have special requirements
    if ($project.Type -eq "VSIX") {
        Write-ColoredOutput "⏭️ Skipping VSIX project: $($project.Name) (requires VS Build Tools)" $Yellow
        continue
    }
    
    $success = Build-Project $project.Path $project.Name
    $buildResults += @{ Name = $project.Name; Success = $success; Type = $project.Type }
}

# Run tests if requested
$testResults = @()
if (-not $SkipTests) {
    Write-ColoredOutput "`n🧪 Running tests..." $Cyan
    
    $testProjects = $projects | Where-Object { $_.Type -eq "Test" }
    foreach ($testProject in $testProjects) {
        $success = Run-Tests $testProject.Path $testProject.Name
        $testResults += @{ Name = $testProject.Name; Success = $success }
    }
}

# Summary
Write-ColoredOutput "`n📊 === Build Validation Summary ===" $Cyan

$totalProjects = $buildResults.Count
$successfulBuilds = ($buildResults | Where-Object { $_.Success }).Count
$failedBuilds = $totalProjects - $successfulBuilds

Write-ColoredOutput "📈 Build Results:" $Yellow
Write-ColoredOutput "  Total Projects: $totalProjects" $Yellow
Write-ColoredOutput "  Successful: $successfulBuilds" $Green
Write-ColoredOutput "  Failed: $failedBuilds" $(if ($failedBuilds -eq 0) { $Green } else { $Red })

if ($testResults.Count -gt 0) {
    $totalTests = $testResults.Count
    $successfulTests = ($testResults | Where-Object { $_.Success }).Count
    $failedTests = $totalTests - $successfulTests
    
    Write-ColoredOutput "`n🧪 Test Results:" $Yellow
    Write-ColoredOutput "  Total Test Projects: $totalTests" $Yellow
    Write-ColoredOutput "  Passed: $successfulTests" $Green
    Write-ColoredOutput "  Failed: $failedTests" $(if ($failedTests -eq 0) { $Green } else { $Red })
}

# Detailed results
if ($Verbose -or $failedBuilds -gt 0) {
    Write-ColoredOutput "`n📋 Detailed Results:" $Yellow
    foreach ($result in $buildResults) {
        $status = if ($result.Success) { "✅ PASS" } else { "❌ FAIL" }
        $color = if ($result.Success) { $Green } else { $Red }
        Write-ColoredOutput "  $status $($result.Name) [$($result.Type)]" $color
    }
    
    if ($testResults.Count -gt 0) {
        Write-ColoredOutput "`n🧪 Test Details:" $Yellow
        foreach ($result in $testResults) {
            $status = if ($result.Success) { "✅ PASS" } else { "❌ FAIL" }
            $color = if ($result.Success) { $Green } else { $Red }
            Write-ColoredOutput "  $status $($result.Name)" $color
        }
    }
}

# Final result
$overallSuccess = ($failedBuilds -eq 0) -and (($testResults.Count -eq 0) -or (($testResults | Where-Object { -not $_.Success }).Count -eq 0))

if ($overallSuccess) {
    Write-ColoredOutput "`n🎉 All validations passed successfully!" $Green
    exit 0
} else {
    Write-ColoredOutput "`n❌ Some validations failed. Please check the errors above." $Red
    exit 1
}

