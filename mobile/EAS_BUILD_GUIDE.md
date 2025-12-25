# Guide EAS Build - Tests de l'Application Mobile

## Configuration actuelle ✅

Votre projet est **déjà configuré** pour EAS Build :
- ✅ `eas.json` présent avec profils (development, preview, production)
- ✅ `app.json` avec projectId EAS : `39ebcd7f-dc23-4e9c-a103-9946f0b83a28`
- ✅ Package NFC configuré : `react-native-nfc-manager`
- ✅ Permissions Android NFC activées

## Prérequis

### 1. Compte Expo (gratuit)
```bash
# Si pas encore connecté
npx eas login
# Ou créer un compte : https://expo.dev/signup
```

### 2. Vérifier l'authentification
```bash
npx eas whoami
# Doit afficher votre email Expo
```

## Créer un Build Android Preview

### Étape 1 : Build APK pour tests internes

```bash
cd c:\Dev\LC\Mobile\LaborControlApp

# Build APK (pas d'installation Play Store requise)
npx eas build --platform android --profile preview
```

**Durée** : 5-10 minutes (première fois peut être plus long)

### Étape 2 : Récupérer le lien de téléchargement

À la fin du build, EAS affiche :
```
✅ Build finished
📱 APK URL: https://expo.dev/artifacts/eas/...
```

**Copier ce lien** → À partager aux testeurs

### Étape 3 : Installer sur Android

**Pour les testeurs** :

1. **Sur smartphone Android** :
   - Ouvrir le lien dans le navigateur
   - Télécharger l'APK
   - Aller dans Paramètres → Sécurité → Autoriser installations depuis sources inconnues
   - Installer l'APK

2. **Ou via ADB** (si développeur) :
   ```bash
   adb install -r app-release.apk
   ```

## Automatiser les builds

### Script PowerShell pour builds réguliers

Créer `build-mobile.ps1` :

```powershell
# Script de build automatisé
$projectPath = "c:\Dev\LC\Mobile\LaborControlApp"
$timestamp = Get-Date -Format "yyyy-MM-dd_HH-mm"

Write-Host "🚀 Démarrage build EAS Android Preview..." -ForegroundColor Green
Write-Host "Timestamp: $timestamp" -ForegroundColor Cyan

cd $projectPath

# Vérifier authentification
$whoami = npx eas whoami 2>&1
if ($whoami -like "*not logged in*") {
    Write-Host "❌ Non authentifié. Exécutez: npx eas login" -ForegroundColor Red
    exit 1
}

# Lancer le build
npx eas build --platform android --profile preview --wait

Write-Host "✅ Build terminé!" -ForegroundColor Green
Write-Host "Consultez https://expo.dev/builds pour le lien de téléchargement" -ForegroundColor Cyan
```

**Utilisation** :
```powershell
.\build-mobile.ps1
```

## Profils de build disponibles

### Preview (RECOMMANDÉ pour tests)
```json
{
  "preview": {
    "distribution": "internal",
    "android": {
      "buildType": "apk"
    }
  }
}
```
- ✅ APK directement téléchargeable
- ✅ Pas de Play Store
- ✅ Gratuit (30 builds/mois)
- ✅ Valide 30 jours

### Production
```json
{
  "production": {
    "autoIncrement": true
  }
}
```
- Pour soumission Google Play Store
- Nécessite compte développeur Google

## Dépannage

### Erreur : "Not logged in"
```bash
npx eas login
# Entrer email/password Expo
```

### Erreur : "Project not found"
```bash
# Vérifier projectId dans app.json
npx eas project:info
```

### Build échoue
```bash
# Nettoyer et relancer
rm -r node_modules
npm install
npx eas build --platform android --profile preview --wait
```

### Voir les logs du build
```bash
# Affiche les logs en temps réel
npx eas build --platform android --profile preview --wait
```

## Partager avec testeurs

### Lien de téléchargement direct

Après chaque build, partager :
```
📱 Télécharger l'APK : https://expo.dev/artifacts/eas/...
```

**Validité** : 30 jours

### QR Code (optionnel)

EAS génère aussi un QR code pour scanner directement depuis le téléphone.

## Intégration CI/CD (futur)

Pour automatiser les builds à chaque commit :

```yaml
# .github/workflows/build-mobile.yml
name: Build Mobile APK

on:
  push:
    branches: [main, develop]
    paths:
      - 'Mobile/LaborControlApp/**'

jobs:
  build:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3
      - uses: actions/setup-node@v3
        with:
          node-version: '18'
      - run: npm install -g eas-cli
      - run: eas login --non-interactive
        env:
          EAS_TOKEN: ${{ secrets.EAS_TOKEN }}
      - run: eas build --platform android --profile preview --wait
        working-directory: Mobile/LaborControlApp
```

## Ressources

- 📚 [Documentation EAS Build](https://docs.expo.dev/build/introduction/)
- 🔗 [Expo Dashboard](https://expo.dev/builds)
- 📱 [React Native NFC Manager](https://github.com/revtel/react-native-nfc-manager)

---

**Prochaine étape** : Exécuter `npx eas build --platform android --profile preview` pour créer le premier APK de test ! 🚀
