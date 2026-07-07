#!/bin/bash
# Charger les variables d'environnement depuis .env
if [ -f ../.env ]; then
    export $(grep -v '^#' ../.env | xargs)
    echo "Variables .env chargées"
fi

dotnet run --project src/Api
