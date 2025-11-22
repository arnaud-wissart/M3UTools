# M3UPlayer

Solution .NET 8 orchestrée avec Aspire pour héberger une API REST minimaliste et préparer l’extension vers d’autres frontends.

## Arborescence

- `M3UPlayer.sln` : solution principale.
- `src/M3UPlayer.AppHost` : orchestrateur Aspire.
- `src/M3UPlayer.ServiceDefaults` : configuration partagée (logging, résilience, discovery).
- `src/M3UPlayer.Api` : API REST minimaliste.
- `src/M3UPlayer.Core` : bibliothèque de domaine.
- `tests/M3UPlayer.Core.Tests` : tests unitaires xUnit du domaine.

## Prérequis

- .NET SDK 8.0+

## Commandes de démarrage

```bash
# Restaurer et construire
DOTNET_NOLOGO=1 dotnet build

# Lancer l’orchestrateur Aspire (l’API est démarrée par l’AppHost)
dotnet run --project src/M3UPlayer.AppHost/M3UPlayer.AppHost.csproj

# Lancer uniquement l’API
DOTNET_NOLOGO=1 dotnet run --project src/M3UPlayer.Api/M3UPlayer.Api.csproj

# Tests
DOTNET_NOLOGO=1 dotnet test
```

## API

- `GET /api/health` : retourne `{ "status": "OK", "service": "M3UPlayer.Api" }`.

## Hypothèses

- Aucun stockage persistant pour le moment.
- Aspire est utilisé uniquement pour orchestrer l’API ; les autres frontends seront ajoutés plus tard.
