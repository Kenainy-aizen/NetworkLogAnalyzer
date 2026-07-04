# Étape 1 — Build
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /app

# Copier les fichiers de projet et restaurer les dépendances
COPY NetworkLogAnalyzer.sln .
COPY src/Api/Api.csproj src/Api/
COPY src/Storage/Storage.csproj src/Storage/
COPY src/Parser/Parser.csproj src/Parser/
COPY src/Collector/Collector.csproj src/Collector/
COPY src/Analyzer/Analyzer.csproj src/Analyzer/

RUN dotnet restore

# Copier tout le code et builder
COPY src/ src/
RUN dotnet publish src/Api/Api.csproj -c Release -o /app/publish

# Étape 2 — Runtime (image plus légère)
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

# Copier l'application buildée
COPY --from=build /app/publish .

# Port exposé
EXPOSE 5000

# Variables d'environnement
ENV ASPNETCORE_URLS=http://+:5000
ENV ASPNETCORE_ENVIRONMENT=Production

ENTRYPOINT ["dotnet", "Api.dll"]
