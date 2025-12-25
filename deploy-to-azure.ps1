# Azure Deployment Script for Labor Control DMTT API
$ErrorActionPreference = 'Continue'

Write-Host "Getting Azure access token..."
$token = az account get-access-token --query accessToken -o tsv

if (-not $token) {
    Write-Host "Failed to get access token. Please run 'az login' first."
    exit 1
}

$headers = @{
    'Authorization' = "Bearer $token"
    'Content-Type' = 'application/json'
}

# Get publishing credentials
Write-Host "Getting publishing credentials..."
$uri = 'https://management.azure.com/subscriptions/bc5d760a-d624-4f08-84b2-48f79b07ea51/resourceGroups/laborcontrol-dmtt-rg/providers/Microsoft.Web/sites/laborcontrol-dmtt-api/config/publishingcredentials/list?api-version=2022-03-01'

try {
    $creds = Invoke-RestMethod -Uri $uri -Method POST -Headers $headers -TimeoutSec 60
    $user = $creds.properties.publishingUserName
    $pass = $creds.properties.publishingPassword
    Write-Host "Got credentials for: $user"
}
catch {
    Write-Host "Failed to get credentials: $_"
    exit 1
}

# Deploy via Kudu ZIP deploy using WebClient
Write-Host "Deploying to Azure..."

$zipPath = 'c:\Users\jcpas\labor-control-dmtt\backend\LaborControl.API\deploy-linux.zip'
$kuduUri = 'https://laborcontrol-dmtt-api.scm.azurewebsites.net/api/zipdeploy'

try {
    Write-Host "Uploading $zipPath using WebClient..."

    # Create WebClient with credentials
    $webClient = New-Object System.Net.WebClient
    $webClient.Credentials = New-Object System.Net.NetworkCredential($user, $pass)

    # Read zip file
    $fileBytes = [System.IO.File]::ReadAllBytes($zipPath)
    Write-Host "File size: $($fileBytes.Length) bytes"

    # Upload
    $webClient.Headers.Add("Content-Type", "application/octet-stream")
    $response = $webClient.UploadData($kuduUri, "POST", $fileBytes)
    $responseText = [System.Text.Encoding]::UTF8.GetString($response)

    Write-Host "Deployment response: $responseText"
    Write-Host "Deployment completed successfully!"
}
catch [System.Net.WebException] {
    $statusCode = [int]$_.Exception.Response.StatusCode
    $reader = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
    $responseBody = $reader.ReadToEnd()
    Write-Host "Deployment failed with status $statusCode : $responseBody"
    exit 1
}
catch {
    Write-Host "Deployment failed: $_"
    exit 1
}

Write-Host "Done!"
