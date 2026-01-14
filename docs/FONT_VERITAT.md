# Projecte: Agents Evolutius 2D — Font de la Veritat

## 1. Resum
Volem crear un experiment/simulador 2D on una població d’agents aprèn a sobreviure mitjançant evolució generació a generació. Cada agent està controlat per una xarxa neuronal (NN) i rep informació de l’entorn principalment a través d’un con de visió. L’objectiu principal és observar **propietats emergents** (estratègies, hàbits, exploració, rutes, cooperació/competència, ús d’interaccions, etc.) sense codificar comportaments.

La simulació s’ha de poder executar **headless** (sense render) per maximitzar rendiment i escalar fins a **5k+ agents** en el futur. El render és només per observació i depuració.

---

## 2. Objectius

### 2.1 Objectiu principal
- **Emergència**: generar i observar comportaments no dissenyats explícitament, sorgits de la pressió selectiva i la percepció limitada.

### 2.2 Objectius secundaris
- **Escalabilitat**: començar amb 100–200 agents i escalar progressivament fins a 5k+.
- **Observabilitat**: disposar d’un “viewer” amb tiles i sprites per inspeccionar episodis, més endavant amb efectes visuals.
- **Repetibilitat**: experiments reproduïbles (seeds i configuració persistida).
- **Extensibilitat**: entorn, sensors i accions extensibles sense reescriure el core.

### 2.3 No-objectius (de moment)
- No fer un “joc final” comercial: és un **experiment**.
- No dependre d’un motor/scene-graph per a la simulació.
- No fer un sistema de percepció “perfecte” (ha de ser limitat i parcial).

---

## 3. Decisions clau (principis de disseny)

1. **2D top-down**.
2. **Món basat en grid** com a font de veritat (tile-based), amb agents en posició contínua (Vector2).
3. **Simulació separada del render**:
   - Core de simulació headless.
   - Viewer opcional que consumeix snapshots.
4. **Percepció sense categories semàntiques**:
   - No “aigua/terra/perill” com a inputs.
   - La NN ha d’aprendre a interpretar el que veu.
5. **Representació sensorial per embeddings**:
   - Cada tipus d’element té una “signatura” vectorial petita i arbitrària.
   - El significat emergeix per associació amb recompensa/supervivència.
6. **Control de bloat** si usem NEAT:
   - Penalització/cost per complexitat (energia) + límits durs + ajust dinàmic.

---

## 4. Tecnologia recomanada

### 4.1 Llenguatge
- **C# (.NET 8/9)** per al core de simulació i evolució.

### 4.2 Viewer (observació)
- Viewer “code-only” amb **tiles + sprites**.
- Requisit: ha de ser opcional i no afectar el rendiment de la simulació.

*(La llibreria concreta del viewer es manté flexible; la decisió final es fixa quan s’implementi el primer prototip de viewer.)*

---

## 5. Arquitectura del sistema

### 5.1 Components
- **Core.Sim**
  - Grid world (chunks si cal)
  - Agents (estat físic i biològic)
  - Sensors (con de visió)
  - Accions i interaccions
  - Fitness/recompenses
  - Scheduler de ticks

- **Core.Evo**
  - Representació de genome/pesos
  - Mutació / crossover / selecció
  - Speciation (si NEAT)
  - Control de bloat

- **Persistence**
  - Checkpoints de població/genomes
  - Mètriques d’experiments
  - Config + seeds

- **Viewer** (opcional)
  - Render de tiles i agents
  - Controls de càmera
  - Overlays debug (con de visió, energia, etc.)

### 5.2 Flux bàsic
1. Inicialitzar entorn + població inicial.
2. Simular episodis (ticks) fins criteri (temps, morts, objectiu, etc.).
3. Calcular fitness i descriptors de comportament.
4. Seleccionar/reproduir/mutar.
5. Guardar checkpoints i mètriques.
6. (Opcional) Renderitzar snapshots per observació.

---

## 6. Món i física (simple, ràpida)

### 6.1 Grid
- Grid d’enteros/bytes (tile ids) o estructures compactes.
- Possibilitat de **chunks** per escalar mapes.

### 6.2 Moviment agent
- Posició contínua (Vector2) però col·lisions resoltes contra el grid:
  - Evitar física generalista.
  - Resolució simple (slide/stop) segons el tile.

### 6.3 Contacte
- “Contacte” = l’agent està dins un radi/overlap amb cel·les o objectes interactuables.
- El contacte habilita la fase d’**interacció**.

---

## 7. Percepció: con de visió amb embeddings

### 7.1 Objectiu de la percepció
- Inputs limitats, consistents, i sense semàntica “masticada”.
- “Visió borrosa” llunyana però sense col·lapsar identitats en un sol escalar.

### 7.2 Embeddings
- Cada tipus d’element visible té un vector **E ∈ R^d** (d petit, ex. 8–16).
- Els embeddings són:
  - deterministes (seed) i persistits,
  - no ordenats ni interpretables per humans.

### 7.3 Sampling del con
- Con definit per:
  - angle FOV,
  - distància màxima,
  - orientació (rotació agent).
- Mostreig amb un nombre fix de rays o sectors (ex. 9–21).
- Raycast **sobre grid** (DDA / grid traversal), no sobre motor de física.

### 7.4 Format d’inputs (proposta base)
Per cada ray/sector:
- distància normalitzada (0..1)
- embedding de l’impacte (d valors)

**InputRay = [dist, e0..e(d-1)]**

Input total aproximat: **numRays * (1 + d)**.

### 7.5 Visió “borrosa”
- Lluny: es pot usar una mitjana ponderada d’embeddings trobats en una banda.
- A prop: embedding del primer impacte (més “nítid”).

---

## 8. Accions disponibles

### 8.1 Accions de locomoció (cada tick)
1. **Rotació** (canvi d’orientació del con)
2. **Velocitat endavant** contínua, rang: **[-Vmax, +Vmax]**

### 8.2 Acció d’interacció
3. **Interactuar**: només té efecte quan hi ha contacte.

### 8.3 Selecció d’interaccions (sense categories)
Quan l’agent està en contacte amb alguna cosa, existeix un conjunt d’**opcions d’acció**. Per evitar “ids semàntics”:
- Cada acció possible té un embedding **A ∈ R^k**.
- La NN produeix una preferència **P ∈ R^k**.
- Es tria l’acció disponible amb màxima similitud entre P i cada A (p.ex. dot-product).

Això permet aprendre associacions objecte→acció sense llistes rígides d’ids amb significat humà.

---

## 9. Xarxes neuronals i evolució

### 9.1 Topologia
La prioritat és observar propietats emergents i permetre **complexificació estructural**. Per tant, el projecte utilitza **NEAT (topologia variable)** com a enfocament **obligatori**.

**Decisió del projecte (fixa):** farem servir **NEAT sí o sí**. No és opcional.

Això implica:
- mutacions estructurals (afegir nodes/connexions) i de pesos,
- compatibilitat/speciation,
- possibilitat de recurrència per memòria,
- control de bloat i límits per mantenir rendiment.

### 9.2 Diversitat (per emergència)
L’emergència es potencia mantenint diversitat. Mecanismes possibles:
- Speciation (NEAT)
- Novelty/Quality-Diversity (fase posterior si cal)

### 9.3 Anti-bloat (NEAT)
Objectiu: evitar que la complexitat creixi sense millora.

Mecanismes combinats:
1. **Límits durs** (caps) de nodes i connexions.
2. **Cost energètic per complexitat** dins la simulació:
   - cost per tick proporcional a nodes/connexions.
3. **Penalització dinàmica** de complexitat al fitness si creix la mida mitjana sense guanys.

### 9.4 Memòria
Per comportaments seqüencials, es permet recurrència (NEAT) o un estat intern (si topologia fixa).

---

## 10. Persistència (font de veritat de l’experiment)

### 10.1 Checkpoints
Guardar periòdicament:
- genomes/pesos (inclosa topologia si NEAT)
- configuració (paràmetres) + seed
- mapping d’embeddings (tipus→vector)

Requisit: format ràpid i compacte.

### 10.2 Mètriques
Registrar:
- fitness per generació (mitjana, p50, p90, millor)
- mida mitjana de xarxa (nodes/connexions)
- descriptors de comportament (si n’hi ha)
- causes de mort/fracàs
- paràmetres de run

Requisit: consultable per analitzar i comparar runs.

---

## 11. Rendiment i escalabilitat

### 11.1 Principis
- Headless per entrenament.
- Viewer desacoblat.
- Estructures de dades compactes (evitar objectes per agent si escala).

### 11.2 Controls per escalar
- Actualitzar visió cada N ticks (configurable).
- Limitar rays i dimensions d’embedding.
- Caps de complexitat de la NN.

Objectiu final: viabilitat de 5k agents en entorns realistes (pot requerir optimització incremental).

---

## 12. Roadmap (alt nivell)

1. **MVP Headless**
   - grid + moviment + contacte
   - con de visió (DDA) + embeddings
   - NN forward + loop d’episodi

2. **Evolució mínima**
   - selecció + mutació + generacions
   - fitness bàsic (sobrevivència)
   - checkpoints + mètriques

3. **Viewer**
   - tiles + sprites + càmera
   - overlays debug

4. **Emergència i diversitat**
   - anti-bloat complet
   - novelty/QD si cal

5. **Escalat**
   - optimitzacions per 1k → 5k agents
   - profiling i ajustos de paràmetres

---

## 13. Paràmetres inicials (a fixar quan implementem)
- numRays (ex. 15)
- dimensió embedding d (ex. 8 o 12)
- FOV i distància màxima
- taxa d’actualització de visió (cada 1–5 ticks)
- Vmax
- mida màxima NEAT (caps nodes/connexions)
- definició d’energia i costos
- criteri de final d’episodi i fitness

---

## 14. Glossari
- **Embedding**: vector numèric petit que representa una “signatura” sensorial sense semàntica humana.
- **Headless**: execució sense render.
- **Bloat**: creixement de complexitat de la xarxa sense guany proporcional en rendiment.
- **QD (Quality-Diversity)**: tècniques que busquen moltes solucions diferents i bones, no només la millor.
