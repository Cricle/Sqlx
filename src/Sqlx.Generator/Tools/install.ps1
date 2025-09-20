# Sqlx.Generator NuGet Package Install Script
# This script runs when the Sqlx.Generator NuGet package is installed

param($installPath, $toolsPath, $package, $project)

Write-Host "Installing Sqlx.Generator source generator..." -ForegroundColor Green

try {
    # Check if this is a supported project type
    if ($project.Kind -eq "{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}" -or  # C# Project
        $project.Kind -eq "{9A19103F-16F7-4668-BE54-9A1E7A4F7556}") {   # SDK-style C# Project
        
        Write-Host "✓ Sqlx.Generator has been successfully added to your project." -ForegroundColor Green
        Write-Host "✓ Source generation will occur automatically during build." -ForegroundColor Green
        Write-Host "" 
        Write-Host "Quick Start:" -ForegroundColor Cyan
        Write-Host "  1. Add [Sqlx] attribute to your methods" -ForegroundColor Gray
        Write-Host "  2. Build your project to generate code" -ForegroundColor Gray
        Write-Host "  3. Check the generated files in obj/Generated/" -ForegroundColor Gray
        Write-Host ""
        Write-Host "For more information, visit: https://github.com/your-repo/Sqlx" -ForegroundColor Blue
        
    } else {
        Write-Warning "Sqlx.Generator is designed for C# projects. Current project type may not be supported."
    }
}
catch {
    Write-Warning "Installation completed with warnings: $($_.Exception.Message)"
}

Write-Host "Sqlx.Generator installation completed." -ForegroundColor Green

