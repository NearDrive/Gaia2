# Contribuir

## Regles de PR
- PRs petits (1 tema) i sense scope creep.
- Descripció amb checklist de DoD.
- CI verd abans de merge.

## Estàndards mínims
- Tests xUnit per features (quan comencem features).
- No introdueix dependències exòtiques sense justificació a docs/DECISIONS.md.

## Comandes típiques (CI)
- `dotnet build`
- `dotnet test`
- `dotnet run --project src/App.Headless`
