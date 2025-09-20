# Sqlx Demo Script
Write-Host "=== Sqlx SQL Template Engine Demo ===" -ForegroundColor Cyan
Write-Host ""

# Check solution file
if (Test-Path "Sqlx.sln") {
    Write-Host "Solution file found" -ForegroundColor Green
} else {
    Write-Host "Solution file not found" -ForegroundColor Red
    exit 1
}

# Show project structure
Write-Host "`nProject Structure:" -ForegroundColor Yellow
Write-Host "  Core Libraries: 2" -ForegroundColor White
Write-Host "  VS Extension: 1" -ForegroundColor White  
Write-Host "  Test Projects: 2" -ForegroundColor White
Write-Host "  Sample Projects: 3" -ForegroundColor White
Write-Host "  Tools: 1" -ForegroundColor White

# Show features
Write-Host "`nKey Features:" -ForegroundColor Yellow
Write-Host "  Advanced SQL Template Engine" -ForegroundColor Green
Write-Host "  Visual Studio Integration" -ForegroundColor Green
Write-Host "  Compile-time Code Generation" -ForegroundColor Green
Write-Host "  Template Validation Tools" -ForegroundColor Green
Write-Host "  Rich Documentation" -ForegroundColor Green

# Build check
Write-Host "`nBuild Status:" -ForegroundColor Yellow
& dotnet build Sqlx.sln --verbosity minimal
if ($LASTEXITCODE -eq 0) {
    Write-Host "Build successful!" -ForegroundColor Green
} else {
    Write-Host "Build failed" -ForegroundColor Red
}

# Run a sample
Write-Host "`nRunning SqlxDemo sample:" -ForegroundColor Yellow
& dotnet run --project samples\SqlxDemo\SqlxDemo.csproj --no-build

Write-Host "`nDemo completed!" -ForegroundColor Green

