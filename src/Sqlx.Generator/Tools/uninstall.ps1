# Sqlx.Generator NuGet Package Uninstall Script
# This script runs when the Sqlx.Generator NuGet package is uninstalled

param($installPath, $toolsPath, $package, $project)

Write-Host "Uninstalling Sqlx.Generator source generator..." -ForegroundColor Yellow

try {
    Write-Host "✓ Sqlx.Generator source generator has been removed from your project." -ForegroundColor Green
    Write-Host "" 
    Write-Host "Note: Generated files may remain in obj/Generated/ until next clean build." -ForegroundColor Gray
    Write-Host "To completely remove generated code, run: dotnet clean" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "Thank you for using Sqlx.Generator!" -ForegroundColor Blue
}
catch {
    Write-Warning "Uninstallation completed with warnings: $($_.Exception.Message)"
}

Write-Host "Sqlx.Generator uninstallation completed." -ForegroundColor Green

