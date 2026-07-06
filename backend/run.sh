#!/bin/bash
# Charger les variables d'environnement depuis .env
if [ -f ../.env ]; then
    export $(grep -v '^#' ../.env | xargs)
fi

dotnet run --project src/Api
