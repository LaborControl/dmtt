# Script to apply EF Core migrations to Azure PostgreSQL
# This will update the Azure database schema to include nullable supplier identifiers

Write-Host "Starting Azure Database Migration..." -ForegroundColor Green
Write-Host ""

# Set the connection string from appsettings.json
$env:ASPNETCORE_ENVIRONMENT = "Production"

# Navigate to API directory
Set-Location -Path $PSScriptRoot

# Display current migration status
Write-Host "Checking current migration status..." -ForegroundColor Yellow
dotnet ef migrations list

Write-Host ""
Write-Host "Applying migrations to Azure PostgreSQL..." -ForegroundColor Yellow

# Apply migrations to Azure database
dotnet ef database update --verbose

if ($LASTEXITCODE -eq 0) {
    Write-Host ""
    Write-Host "✅ Migration applied successfully to Azure PostgreSQL!" -ForegroundColor Green
    Write-Host ""
    Write-Host "You can now test creating a supplier order in the application." -ForegroundColor Cyan
} else {
    Write-Host ""
    Write-Host "❌ Migration failed. Please check the error messages above." -ForegroundColor Red
    Write-Host ""
    Write-Host "Common issues:" -ForegroundColor Yellow
    Write-Host "  - Network connectivity to Azure" -ForegroundColor White
    Write-Host "  - Incorrect connection string in appsettings.json" -ForegroundColor White
    Write-Host "  - PostgreSQL firewall rules blocking connection" -ForegroundColor White
    Write-Host "  - SSL/TLS certificate issues" -ForegroundColor White
}

Write-Host ""
Write-Host "Script completed." -ForegroundColor Green
