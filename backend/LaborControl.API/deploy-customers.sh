#!/bin/bash
# Script de déploiement rapide vers Azure

echo "📦 Publication du backend..."
cd "C:\Dev\LC\Backend\LaborControl.API"
dotnet publish -c Release -o ./publish

echo "🗜️ Création du ZIP..."
cd publish
zip -r ../deploy-customers.zip .

echo "☁️ Déploiement vers Azure..."
cd ..
az webapp deployment source config-zip --resource-group LaborControl-RG --name laborcontrol-api --src deploy-customers.zip

echo "✅ Déploiement terminé!"
