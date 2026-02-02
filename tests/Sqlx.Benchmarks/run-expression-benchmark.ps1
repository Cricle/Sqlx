#!/usr/bin/env pwsh
# Run ExpressionBlockResult benchmark

Write-Host "Running ExpressionBlockResult Benchmark..." -ForegroundColor Cyan
Write-Host ""

# Build the project
Write-Host "Building project..." -ForegroundColor Yellow
dotnet build -c Release

if ($LASTEXITCODE -ne 0) {
    Write-Host "Build failed!" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "Running benchmark..." -ForegroundColor Yellow
Write-Host ""

# Run the specific benchmark
dotnet run -c Release --project . --filter "*ExpressionBlockResult*"

Write-Host ""
Write-Host "Benchmark completed!" -ForegroundColor Green
Write-Host "Results saved to BenchmarkDotNet.Artifacts/results/" -ForegroundColor Cyan
