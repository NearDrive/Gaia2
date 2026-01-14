# Roadmap de desenvolupament (guiat amb Codex)

Aquest document descriu el **roadmap** recomanat per desenvolupar el projecte des d’un repo buit a GitHub, **guiant Codex** perquè implementi el sistema de la forma més eficient i consistent possible.

## Principis no negociables (guardrails)

- **Simulació desacoblada del render**: primer *headless* (CLI), viewer després.
- **Món tile-based (grid) com a font de veritat**, agents amb posició contínua (`Vector2`).
- **Inputs sense categories semàntiques**: la NN rep **embeddings** (no “water”, “food”, etc.).
- **NEAT obligatori** (no opcional), amb suport per recurrència (memòria interna).
- **Anti-bloat**: xarxes més complexes han de tenir cost/penalització.
- **Checkpoints + seeds + configs** per reproduïbilitat; persistència també de l’EmbeddingRegistry.

---

## Com treballar amb Codex (operativa)

### Regles d’or per cada tasca
1) **PR petita**: un tema per PR (idealment < ~400 línies) i sense “scope creep”.
2) **Definition of Done (DoD) explícit**: què queda funcional i com es prova.
3) **No trencar guardrails**: headless primer, viewer separat, embeddings sense semàntica, NEAT sí o sí.
4) **Sempre runnable**: cada milestone ha de deixar un `App.Headless` executable amb paràmetres.
5) **Determinisme**: seed i config sempre visibles i guardats a la sortida del run.

### Plantilla d’issue per Codex (recomanada)
- **Objectiu** (1–2 frases)
- **Abast** (què entra / què no entra)
- **DoD** (tests, comandes, outputs esperats)
- **Fitxers/claus** (carpetes o projectes tocats)
- **Notes** (guardrails que apliquen)

---

## Fase 0 — Bootstrap del repo (1–2 PRs)

### PR-00: Estructura base + CI
**Objectiu:** convertir el repo en una base estable per iterar ràpid.

**Deliverables**
- Solució `.sln` amb projectes:
  - `src/Core.Sim/` (simulació)
  - `src/Core.Evo/` (NEAT i evolució)
  - `src/Persistence/` (checkpoints, registries)
  - `src/App.Headless/` (runner CLI)
  - `tests/Core.*.Tests/` (xUnit)
- GitHub Actions: `build + test` a cada push/PR
- `.editorconfig`, `Directory.Build.props` (nullable, warnings, analyzers)

**DoD**
- `dotnet test` passa
- `dotnet run --project src/App.Headless` compila (encara que faci “Hello”)

### PR-01: Guardrails i docs del projecte
**Deliverables**
- `docs/FONT_VERITAT.md` (o referència interna)
- `docs/DECISIONS.md` amb “Principis no negociables”
- `CONTRIBUTING.md` amb regla de PRs petites + DoD

**DoD**
- Docs existents i clares
- Cap codi funcional nou (només infraestructura)

---

## Fase 1 — MVP Headless (Sim mínima verificable) (4 PRs)

### PR-02: Core loop + determinisme
**Deliverables**
- `SimulationConfig` (seed, dt, ticks/episodi)
- RNG centralitzat
- `Simulation.Step()` desacoblat del render

**DoD**
- Un run headless de 10.000 ticks amb seed fixa dona resultats repetibles

### PR-03: Grid World mínim
**Deliverables**
- `GridWorld` amb tiles compactes (ex: `byte`/`ushort`)
- utilitats per consultar tile per posició
- (opcional) estructura preparada per chunks (sense implementar-ho complet encara)

**DoD**
- Tests bàsics de lectura/escriptura de tiles

### PR-04: Agents + moviment + col·lisió
**Deliverables**
- `Agent` amb posició contínua (`Vector2`)
- moviment simple + resolució col·lisió contra tiles sòlids
- contacte bàsic (overlap/radi vs cel·les)

**DoD**
- Test d’un mapa petit on l’agent no pot travessar sòlids
- Headless imprimeix estadístiques (agents vius, posicions mitjanes, etc.)

---

## Fase 2 — Percepció (con de visió + embeddings) (2 PRs)

### PR-05: EmbeddingRegistry
**Objectiu:** mapping determinista `tileId/objectTypeId -> float[d]`

**Deliverables**
- `EmbeddingRegistry` amb dimensió `d` configurable
- persistència del mapping (JSON al principi)
- API per obtenir embedding d’un tile o objecte

**DoD**
- El mateix tileId dona el mateix embedding entre runs (amb el mateix fitxer/seed)

### PR-06: VisionCone (rays) amb DDA
**Deliverables**
- Raycast grid (DDA / grid traversal)
- `numRays`, `FOV`, `maxDist`, `visionEveryNTicks`
- Input per ray: `[distNorm, e0..e(d-1)]`

**DoD**
- Test unitari que valida impactes esperats en un mapa controlat
- Input size estable (documentada)

---

## Fase 3 — Accions + interaccions sense semàntica (2 PRs)

### PR-07: Model d’acció (outputs)
**Deliverables**
- Decodificació outputs NN → `rotationDelta`, `forwardSpeed`, `actionPreferenceVector[k]`
- “No-action” sempre possible

**DoD**
- L’agent es pot moure sense trencar la sim

### PR-08: Interaction Options (embedding d’accions)
**Deliverables**
- Quan hi ha contacte, generar llista d’opcions amb `ActionEmbedding[k]`
- Selecció per similarity (dot-product/cosine) amb `actionPreferenceVector`
- Implementar 1–2 interaccions mínimes (ex: “beure” i “no-res”), però **no exposar cap etiqueta a la NN**

**DoD**
- Tests: la llista d’opcions és determinista i seleccionable

---

## Fase 4 — Fitness + episodis + mètriques (2 PRs)

### PR-09: Needs mínims + supervivència
**Deliverables**
- energia que baixa per tick
- mort a 0
- reward: temps viu + penalitzacions bàsiques

**DoD**
- En mapes amb recursos, alguns agents poden viure més que altres (variància)

### PR-10: Metrics i output de runs
**Deliverables**
- mètriques per generació i per episodi: `best/avg/p50/p90`
- registre d’experiment (seed + config + commit)
- output a CSV/JSONL a `runs/<timestamp>/`

**DoD**
- Cada run genera carpeta amb resultats i config

---

## Fase 5 — NEAT (obligatori) (4 PRs)

### PR-11: Genome + innovation numbers
**Deliverables**
- representació nodes/connexions, innovations
- forward pass amb suport de recurrència (mínim: permetre connexions recurrents i resoldre amb passos interns curts)

**DoD**
- Es pot instanciar una xarxa des d’un genome i inferir outputs

### PR-12: Mutacions + crossover
**Deliverables**
- mutació de pesos
- afegir connexió / afegir node
- crossover bàsic

**DoD**
- Tests: genome muta i conserva invariants (no connexions invalides)

### PR-13: Speciation + selecció
**Deliverables**
- distància de compatibilitat
- assignació a espècies
- selecció/reproducció per espècie

**DoD**
- En 2–3 generacions apareixen diverses espècies en poblacions simples

### PR-14: Anti-bloat
**Deliverables**
- caps durs: `maxNodes`, `maxConnections`
- cost energètic per complexitat dins la sim (per tick) **o** penalització al fitness
- penalització dinàmica si creix mida mitjana sense millora

**DoD**
- La mida de xarxa no creix sense control en runs llargs

---

## Fase 6 — Persistència “de veritat” (1–2 PRs)

### PR-15: Checkpoints
**Deliverables**
- `Checkpoint` inclou:
  - config + seed
  - població (genomes)
  - estat de NEAT (innovations, espècies)
  - `EmbeddingRegistry`
- format inicial: JSON (+ compressió opcional) o binari

**DoD**
- Es pot pausar i reprendre un run obtenint continuïtat (mateixa seed/config)

---

## Fase 7 — Viewer (opcional i desacoblat) (2 PRs)

> **Només quan el headless ja és sòlid.**

### PR-16: Snapshot API
**Deliverables**
- `WorldSnapshot` amb tiles visibles, agents, debug (rays)
- headless pot “dump” snapshots cada N ticks

**DoD**
- Snapshot estable i versionable (schema documentat)

### PR-17: Viewer mínim
**Deliverables**
- app de visualització (llibreria a triar)
- render tiles + agents + overlay del con de visió

**DoD**
- Reproduir un episodi des de snapshots o stream

---

## Fase 8 — Escalat 1k → 5k agents (iteratiu)

**Objectiu:** performance real sense trencar el comportament.

- evitar allocations per tick, usar arrays/structs
- visió cada N ticks i rays precomputats si cal
- profiling i micro-benchmarks al runner
- regressions: “perf smoke test” a CI (opcional)

**DoD**
- Run amb 1k agents estable; després 2k; després 5k, amb mètriques i sense GC spikes exagerats

---

## Checklist final (per “release” d’experiment)
- [ ] Headless reproducible (seed/config)
- [ ] Checkpoints funcionen
- [ ] Embeddings persistits i estables
- [ ] NEAT amb especiació + anti-bloat actiu
- [ ] Metrics exportades per comparar runs
- [ ] (Opcional) Viewer desacoblat amb snapshots
