# Quick Test Script - Run tests with optimized settings
# This script provides convenient commands for different test scenarios

param(
    [Parameter(Position=0)]
    [ValidateSet('all', 'unit', 'e2e', 'mysql', 'postgres', 'sqlserver', 'sqlite')]
    [string]$Target = 'all',
    
    [Parameter(Position=1)]
    [ValidateSet('normal', 'fast')]
    [string]$Mode = 'fast',
    
    [switch]$NoBuild,
    [switch]$VerboseOutput
)

$ErrorActionPreference = "Stop"

# Colors for output
function Write-Info { Write-Host $args -ForegroundColor Cyan }
function Write-Success { Write-Host $args -ForegroundColor Green }
function Write-Warning { Write-Host $args -ForegroundColor Yellow }
function Write-Error { Write-Host $args -ForegroundColor Red }

Write-Info "=== Sqlx Test Runner ==="
Write-Info "Target: $Target"
Write-Info "Mode: $Mode"
Write-Info ""

# Select settings file based on mode
if ($Mode -eq 'fast') {
    $settingsFile = "tests/Sqlx.Tests/test.fast.runsettings"
    Write-Warning "Using FAST mode - aggressive parallelization, 15s timeout per test"
} else {
    $settingsFile = "tests/Sqlx.Tests/test.runsettings"
}

# Measure execution time
$stopwatch = [System.Diagnostics.Stopwatch]::StartNew()

try {
    $baseArgs = @(
        "test"
        "tests/Sqlx.Tests/Sqlx.Tests.csproj"
        "--settings"
        $settingsFile
    )
    
    if ($NoBuild) {
        $baseArgs += "--no-build"
    }
    
    if ($VerboseOutput) {
        $baseArgs += "--verbosity"
        $baseArgs += "detailed"
    } else {
        $baseArgs += "--verbosity"
        $baseArgs += "normal"
    }
    
    switch ($Target) {
        'all' {
            Write-Info "Running ALL tests (Unit + E2E)..."
        }
        'unit' {
            Write-Info "Running UNIT tests only (fast, no Docker required)..."
            $baseArgs += "--filter"
            $baseArgs += "FullyQualifiedName!~E2E"
        }
        'e2e' {
            Write-Info "Running E2E tests only (requires Docker)..."
            $baseArgs += "--filter"
            $baseArgs += "FullyQualifiedName~E2E"
        }
        'mysql' {
            Write-Info "Running MySQL tests only..."
            $baseArgs += "--filter"
            $baseArgs += "TestCategory=MySQL"
        }
        'postgres' {
            Write-Info "Running PostgreSQL tests only..."
            $baseArgs += "--filter"
            $baseArgs += "TestCategory=PostgreSQL"
        }
        'sqlserver' {
            Write-Info "Running SQL Server tests only..."
            $baseArgs += "--filter"
            $baseArgs += "TestCategory=SqlServer"
        }
        'sqlite' {
            Write-Info "Running SQLite tests only (fastest, no Docker)..."
            $baseArgs += "--filter"
            $baseArgs += "TestCategory=SQLite"
        }
    }
    
    & dotnet $baseArgs
    
    if ($LASTEXITCODE -ne 0) {
        throw "Test execution failed with exit code $LASTEXITCODE"
    }
    
    $stopwatch.Stop()
    $elapsed = $stopwatch.Elapsed
    
    Write-Success ""
    Write-Success "=== Test Execution Complete ==="
    Write-Success "Time: $($elapsed.Minutes)m $($elapsed.Seconds)s"
    
    if ($elapsed.TotalMinutes -gt 3) {
        Write-Warning "Tests took longer than 3 minutes. Consider using 'fast' mode or running specific test categories."
    }
    Write-Success ""
    
} catch {
    $stopwatch.Stop()
    $elapsed = $stopwatch.Elapsed
    
    Write-Error ""
    Write-Error "=== Test Execution Failed ==="
    Write-Error "Time: $($elapsed.Minutes)m $($elapsed.Seconds)s"
    Write-Error "Error: $_"
    Write-Error ""
    exit 1
}
