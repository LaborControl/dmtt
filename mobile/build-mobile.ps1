# Script de build automatisé EAS - Application Mobile Labor Control
# Usage: .\build-mobile.ps1

param(
    [string]$Profile = "preview",
    [string]$Platform = "android",
    [switch]$Wait = $true
)

$projectPath = "c:\Dev\LC\Mobile\LaborControlApp"
$timestamp = Get-Date -Format "yyyy-MM-dd_HH-mm-ss"

Write-Host "╔════════════════════════════════════════════════════════════╗" -ForegroundColor Cyan
Write-Host "║  🚀 EAS Build - Labor Control Mobile Application          ║" -ForegroundColor Cyan
Write-Host "╚════════════════════════════════════════════════════════════╝" -ForegroundColor Cyan
Write-Host ""
Write-Host "📋 Configuration:" -ForegroundColor Yellow
Write-Host "   Platform: $Platform"
Write-Host "   Profile:  $Profile"
Write-Host "   Timestamp: $timestamp"
Write-Host ""

# Vérifier que le répertoire existe
if (-not (Test-Path $projectPath)) {
    Write-Host "❌ Erreur: Répertoire non trouvé: $projectPath" -ForegroundColor Red
    exit 1
}

cd $projectPath

# Vérifier authentification EAS
Write-Host "🔐 Vérification authentification EAS..." -ForegroundColor Cyan
$whoami = npx eas whoami 2>&1
if ($whoami -like "*not logged in*" -or $LASTEXITCODE -ne 0) {
    Write-Host "❌ Non authentifié auprès d'EAS" -ForegroundColor Red
    Write-Host ""
    Write-Host "📝 Pour vous connecter, exécutez:" -ForegroundColor Yellow
    Write-Host "   npx eas login" -ForegroundColor White
    Write-Host ""
    Write-Host "💡 Ou créez un compte gratuit: https://expo.dev/signup" -ForegroundColor Cyan
    exit 1
}

Write-Host "✅ Authentifié en tant que: $whoami" -ForegroundColor Green
Write-Host ""

# Vérifier que node_modules existe
if (-not (Test-Path "node_modules")) {
    Write-Host "📦 Installation des dépendances..." -ForegroundColor Cyan
    npm install
    if ($LASTEXITCODE -ne 0) {
        Write-Host "❌ Erreur lors de l'installation des dépendances" -ForegroundColor Red
        exit 1
    }
}

# Lancer le build
Write-Host "🔨 Démarrage du build EAS..." -ForegroundColor Cyan
Write-Host "   Cela peut prendre 5-10 minutes..." -ForegroundColor Gray
Write-Host ""

$buildArgs = @("build", "--platform", $Platform, "--profile", $Profile)
if ($Wait) {
    $buildArgs += "--wait"
}

npx eas @buildArgs

if ($LASTEXITCODE -ne 0) {
    Write-Host ""
    Write-Host "❌ Le build a échoué" -ForegroundColor Red
    Write-Host ""
    Write-Host "💡 Conseils de dépannage:" -ForegroundColor Yellow
    Write-Host "   1. Vérifiez votre connexion Internet"
    Write-Host "   2. Consultez les logs: npx eas build:list"
    Write-Host "   3. Nettoyez et relancez:"
    Write-Host "      rm -r node_modules"
    Write-Host "      npm install"
    Write-Host "      npx eas build --platform $Platform --profile $Profile --wait"
    exit 1
}

Write-Host ""
Write-Host "╔════════════════════════════════════════════════════════════╗" -ForegroundColor Green
Write-Host "║  ✅ BUILD TERMINÉ AVEC SUCCÈS!                            ║" -ForegroundColor Green
Write-Host "╚════════════════════════════════════════════════════════════╝" -ForegroundColor Green
Write-Host ""
Write-Host "📱 Prochaines étapes:" -ForegroundColor Cyan
Write-Host "   1. Consultez le lien de téléchargement:"
Write-Host "      https://expo.dev/builds" -ForegroundColor White
Write-Host ""
Write-Host "   2. Partagez l'APK avec les testeurs:"
Write-Host "      📥 Lien direct de téléchargement (valide 30 jours)"
Write-Host ""
Write-Host "   3. Installation sur Android:"
Write-Host "      • Télécharger l'APK depuis le lien"
Write-Host "      • Paramètres → Sécurité → Autoriser sources inconnues"
Write-Host "      • Installer l'APK"
Write-Host ""
Write-Host "💡 Pour automatiser les builds futurs:" -ForegroundColor Yellow
Write-Host "   .\build-mobile.ps1 -Profile preview -Platform android"
Write-Host ""
