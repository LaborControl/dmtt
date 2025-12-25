#!/bin/bash
# Script to apply EF Core migrations to Azure PostgreSQL
# This will update the Azure database schema to include nullable supplier identifiers

echo -e "\033[0;32mStarting Azure Database Migration...\033[0m"
echo ""

# Set the connection string from appsettings.json
export ASPNETCORE_ENVIRONMENT="Production"

# Navigate to API directory
cd "$(dirname "$0")"

# Display current migration status
echo -e "\033[0;33mChecking current migration status...\033[0m"
dotnet ef migrations list

echo ""
echo -e "\033[0;33mApplying migrations to Azure PostgreSQL...\033[0m"

# Apply migrations to Azure database
dotnet ef database update --verbose

if [ $? -eq 0 ]; then
    echo ""
    echo -e "\033[0;32m✅ Migration applied successfully to Azure PostgreSQL!\033[0m"
    echo ""
    echo -e "\033[0;36mYou can now test creating a supplier order in the application.\033[0m"
else
    echo ""
    echo -e "\033[0;31m❌ Migration failed. Please check the error messages above.\033[0m"
    echo ""
    echo -e "\033[0;33mCommon issues:\033[0m"
    echo -e "\033[0;37m  - Network connectivity to Azure\033[0m"
    echo -e "\033[0;37m  - Incorrect connection string in appsettings.json\033[0m"
    echo -e "\033[0;37m  - PostgreSQL firewall rules blocking connection\033[0m"
    echo -e "\033[0;37m  - SSL/TLS certificate issues\033[0m"
fi

echo ""
echo -e "\033[0;32mScript completed.\033[0m"
