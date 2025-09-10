# Quick Test Script for Sqlx Project

Write-Host "Sqlx Quick Test Script" -ForegroundColor Green
Write-Host "======================" -ForegroundColor Green

Write-Host ""
Write-Host "Building solution..." -ForegroundColor Yellow
dotnet build --configuration Release

if ($LASTEXITCODE -eq 0) {
    Write-Host "[OK] Build successful" -ForegroundColor Green
    
    Write-Host ""
    Write-Host "Running core tests..." -ForegroundColor Yellow
    dotnet test tests/Sqlx.Tests --configuration Release --no-build --logger:console --verbosity minimal --filter "TestCategory!=Integration"
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "[OK] Core tests passed" -ForegroundColor Green
        
        Write-Host ""
        Write-Host "Running integration tests..." -ForegroundColor Yellow
        dotnet test tests/Sqlx.IntegrationTests --configuration Release --no-build --logger:console --verbosity minimal
        
        if ($LASTEXITCODE -eq 0) {
            Write-Host "[OK] Integration tests passed" -ForegroundColor Green
            Write-Host ""
            Write-Host "All tests passed successfully!" -ForegroundColor Green
        } else {
            Write-Host "[ERROR] Integration tests failed" -ForegroundColor Red
        }
    } else {
        Write-Host "[ERROR] Core tests failed" -ForegroundColor Red
    }
} else {
    Write-Host "[ERROR] Build failed" -ForegroundColor Red
}

Write-Host ""
Write-Host "Test run completed." -ForegroundColor Cyan
