# Modern Telengard — Implementation Specification v0.1

## 0. Purpose

This specification translates the Modern Telengard design into systems that can be implemented incrementally with OpenAI Codex.

The governing design thesis is:

> Preserve Telengard’s danger, mystery, speed, procedural vastness, and “one more descent” compulsion. Modernize friction, readability, controls, content depth, and long-term progression.

Every major system should ultimately reinforce one question:

> **Does this make the decision to go one floor deeper more interesting?**

---

# 1. Product Definition

## 1.1 Core Game

Modern Telengard is a single-character, open-ended dungeon RPG built around repeated expeditions into a procedurally generated 50-floor dungeon.

The primary loop is:

```text
INN
 ↓
Prepare character
 ↓
Select equipment / spells / objective
 ↓
Enter dungeon
 ↓
Explore
 ↓
Fight / flee / discover / interact
 ↓
Acquire treasure
 ↓
Choose:
    DESCEND → greater danger / greater reward
    RETREAT → attempt to return safely
 ↓
Escape dungeon
 ↓
INN
 ↓
Secure wealth
Gain XP
Identify artifacts
Update discoveries
Prepare next expedition
```

This directly reflects the expedition structure defined in the design.

## 1.2 Primary Player Experience

The player should repeatedly experience:

```text
curiosity
→ uncertainty
→ discovery
→ growing confidence
→ greed
→ danger
→ decision to retreat or push deeper
→ relief or loss
```

## 1.3 Non-Negotiable Design Pillars

The implementation must preserve:

1. The dungeon feels effectively endless.
2. Going deeper is continually tempting.
3. Returning alive matters.
4. Strange dungeon features are central.
5. Player knowledge is itself progression.

These are explicitly identified as the game's most important Telengard DNA.

---

# 2. Explicit Non-Goals

The core game should not evolve toward:

- party management;
- dialogue-heavy towns;
- cinematic quest campaigns;
- base building;
- crafting-material economies;
- giant incremental skill trees;
- inventory-Tetris systems;
- MMO rarity clutter;
- linear story progression;
- chosen-one narrative structures.

These are deliberately excluded by the design because they would pull the game toward a generic fantasy roguelite.

Architecture should therefore avoid assuming any of these systems will eventually exist.

---

# 3. Architectural Principle

## 3.1 Simulation Must Be Renderer-Independent

The game simulation must contain no dependency on graphical presentation.

Required conceptual architecture:

```text
                  ┌────────────────────────┐
                  │     GAME SIMULATION    │
                  │                        │
                  │ Dungeon                │
                  │ Player                 │
                  │ Combat                 │
                  │ Items                  │
                  │ Knowledge              │
                  │ Events                 │
                  │ Progression            │
                  └───────────┬────────────┘
                              │
              ┌───────────────┼───────────────┐
              │               │               │
        Modern Renderer   Retro Renderer  Terminal Renderer
```

The design specifically calls for Modern, Retro+, and Terminal rendering modes sharing the same simulation.

### Requirement

Nothing in core simulation code may ask questions such as:

```text
Which sprite is displayed?
Which animation is playing?
What UI panel is open?
What font is being used?
Which rendering mode is active?
```

Instead the simulation emits state and domain events.

Example:

```text
MonsterSpawned
PlayerMoved
FeatureActivated
DamageResolved
ItemDiscovered
KnowledgeUpdated
PlayerDied
FloorChanged
```

Presentation systems consume those events.

---

# 4. Recommended Domain Architecture

The source design does not prescribe a programming architecture. The following structure is therefore a **proposed implementation**.

```text
/core
    simulation
    rng
    events
    time

/world
    dungeon
    generation
    floors
    regions
    tiles
    features
    hazards

/actors
    player
    monsters
    stats
    effects

/combat
    encounters
    actions
    damage
    fleeing
    threat

/items
    inventory
    equipment
    treasure
    affixes
    artifacts
    identification

/magic
    spells
    spell_effects
    experimentation

/knowledge
    journal
    observations
    confidence
    cartography

/progression
    experience
    talents
    legacy

/economy
    carried_wealth
    secured_wealth

/meta
    save
    game_modes
    profile

/presentation
    modern
    retro
    terminal

/ui
    input
    menus
    hud
```

Core systems communicate through domain interfaces rather than presentation code.

---

# 5. Runtime State Model

The simulation should expose one authoritative `GameState`.

Proposed structure:

```text
GameState
    version
    world_seed
    simulation_tick
    current_mode

    player
    expedition
    dungeon
    knowledge
    legacy
    secured_progress
    settings
```

## 5.1 PlayerState

```text
PlayerState
    id

    attributes
        strength
        intelligence
        wisdom
        constitution
        dexterity
        charisma

    level
    xp

    hp
    max_hp

    spell_power
    max_spell_power

    position
        floor
        x
        y

    inventory[]
    equipment_slots

    talents[]
    spells[]

    injuries[]
    temporary_effects[]

    light_state
    noise_state

    carried_gold

    alive
```

The six starting attributes come directly from the original-inspired character model.

Exact formulas are not defined by the design and should remain configurable rather than hard-coded into unrelated systems.

---

# 6. Expedition State

An expedition begins when the player leaves safety and ends when one of the following occurs:

```text
SUCCESS:
player reaches an inn / secure checkpoint

FAILURE:
player dies

SUSPEND:
player suspends the current session
```

Proposed state:

```text
ExpeditionState
    expedition_id

    starting_floor
    deepest_floor_reached

    start_time
    simulation_ticks

    carried_gold
    acquired_items[]

    monsters_defeated
    discoveries_made[]

    floors_visited[]
    rooms_visited

    objectives[]

    active
```

## 6.1 Securing Progress

During an expedition:

```text
loot = unsecured
gold = unsecured
new character progress = at risk according to game mode
```

After successfully reaching safety:

```text
carried wealth → secured wealth

validated discoveries → journal

expedition map knowledge → persistent map

eligible XP/progression → committed

artifacts → available for identification
```

The distinction between carried and secured wealth is a central design requirement.

---

# 7. Dungeon Coordinate Model

## Proposed implementation

Every dungeon location should have a stable coordinate:

```text
DungeonPosition
    floor: 1..50
    x: integer
    y: integer
```

The same:

```text
world_seed
floor
x
y
```

must always resolve to the same base dungeon geography.

This allows:

- deterministic generation;
- map persistence;
- legacy discoveries;
- teleporter mapping;
- saved coordinates;
- player notes;
- graves;
- future-world references.

The design calls for deterministic seeded procedural generation with persistent coordinates.

---

# 8. Random Number Architecture

Determinism is essential.

Do **not** use one global random generator for every system.

Use deterministic RNG streams.

Example:

```text
worldSeed

derive("layout", floor)
derive("features", floor)
derive("loot", floor, x, y)
derive("encounter", expeditionId, tick)
derive("monster", monsterId)
derive("feature-event", featureId, activationCount)
```

This prevents apparently unrelated code changes from rebuilding the world because one new random number was consumed somewhere.

## Requirement

Dungeon geometry generation must be reproducible from:

```text
world_seed
generator_version
location
```

Save files therefore need to preserve the generator version.

---

# 9. Procedural Dungeon Generation

The design proposes the following conceptual pipeline:

```text
layout
→ biome
→ features
→ encounters
→ loot ecology
→ secrets
```



Implement those as independent stages.

## 9.1 Stage 1 — Layout Generator

Produces:

```text
rooms
corridors
doors
walls
stairs
blocked areas
connectivity
```

### Requirements

Each generated region must:

- contain traversable space;
- contain at least one viable route between required anchors;
- avoid mandatory inaccessible critical locations;
- permit loops and alternate routes;
- not resemble a simple linear sequence.

## 9.2 Stage 2 — Biome Generator

Assigns floor-band properties such as:

```text
floor materials
ambient lighting
environmental hazards
room archetypes
monster ecology
feature weights
```

## 9.3 Stage 3 — Feature Placement

Places unusual dungeon objects:

```text
fountains
altars
pits
stairs
teleporters
cubes
thrones
elevators
button boxes
shrines
other anomalies
```

These strange fixed features are one of the strongest pieces of Telengard's identity.

## 9.4 Stage 4 — Encounter Ecology

Determines potential inhabitants or encounter weights.

Avoid permanently pre-spawning every enemy across an enormous dungeon.

Instead generate encounter possibilities from:

```text
depth
biome
danger
noise
time
room
feature
player condition
```

## 9.5 Stage 5 — Loot Ecology

Determines:

```text
treasure availability
item classes
artifact chance
curses
affixes
depth scaling
special-region loot
```

## 9.6 Stage 6 — Secrets

Generate:

```text
secret doors
hidden rooms
rare features
unusual connections
special inscriptions
buried artifacts
teleporter relationships
```

---

# 10. Dungeon Depth Bands

The 50 floors are divided into ten ecological bands.

```text
1–5     Upper Ruins
6–10    Forgotten Crypts
11–15   Flooded Deeps
16–20   Infernal Works
21–25   Fungal Abyss
26–30   Lost Kingdom
31–35   Black Labyrinth
36–40   Demon Vaults
41–45   Impossible Depths
46–50   Telengard
```

## 10.1 Band Definition

Represent each band as data.

Example:

```text
DungeonBandDefinition
    id
    display_name
    floor_min
    floor_max

    generation_profile

    monster_families[]
    feature_weights{}

    hazards[]
    ambient_rules[]

    loot_profile

    visual_theme
    audio_theme
```

## 10.2 Principle

Depth increases must introduce **new rules**, not merely larger numbers.

Examples from the design include:

```text
darkness
corruption
unstable magic
environmental hazards
elite monsters
navigation distortion
```



---

# 11. Exploration

Movement operates on a grid.

Proposed player commands:

```text
MoveNorth
MoveSouth
MoveEast
MoveWest

Interact
Search
Rest
UseItem
CastQuickSpell
OpenCommandMenu
```

## 11.1 Visibility

The player sees only nearby surroundings.

The automap records only spaces personally visited.

This preserves limited information without forcing manual graph-paper bookkeeping.

Suggested tile states:

```text
UNKNOWN
OBSERVED
VISITED
CURRENTLY_VISIBLE
```

## 11.2 Cartography Data

```text
MapKnowledge
    discovered_tiles
    discovered_connections
    landmarks
    suspected_connections
    custom_notes
    custom_pins
```

Hidden passages remain unknown until discovered.

---

# 12. Time System

The design calls for **pulse-based real time**, with menus slowing time heavily and an accessibility option for full pause.

## Proposed simulation

Use discrete simulation ticks underneath real-time presentation.

Example:

```text
SIMULATION_HZ = configurable
```

Systems update on ticks rather than frame rate.

## 12.1 Time Modes

```text
NORMAL
SLOWED
PAUSED
```

Typical behavior:

```text
exploration = NORMAL
command menu = SLOWED
accessibility full pause = PAUSED
```

Simulation speed must never depend on rendering FPS.

---

# 13. Danger System

The design specifies a hidden danger meter influenced by:

- depth;
- noise;
- light;
- wounds;
- treasure carried;
- time spent.

Proposed model:

```text
DangerState
    base_depth_danger
    time_pressure
    noise_pressure
    light_pressure
    injury_pressure
    greed_pressure

    current_danger
```

Conceptually:

```text
danger =
    depth
  + elapsed_time
  + player_noise
  + vulnerability
  + treasure_pressure
  + contextual_modifiers
```

Exact numeric weighting should live in tuning data.

## Requirement

The player should **not** see a raw danger number.

Danger should be communicated through:

```text
audio cues
environmental cues
monster activity
UI warnings
character intuition
```

---

# 14. Encounter System

Encounters should remain fast.

Target encounter duration:

```text
10–30 seconds
```

The combat design explicitly favors fast tactical decisions over separate prolonged battle screens.

## 14.1 Encounter Lifecycle

```text
SEARCHING
   ↓
CONTACT
   ↓
THREAT_ASSESSMENT
   ↓
PLAYER_ACTION
   ↓
RESOLUTION
   ↓
ENEMY_ACTION
   ↓
STATE_CHECK
   ├── enemy defeated
   ├── player escaped
   ├── player died
   └── next combat pulse
```

## 14.2 Player Actions

Minimum required verbs:

```text
Attack
Defend
Maneuver
CastSpell
UseItem
Flee
ContextualAction
```

---

# 15. Threat Assessment

Enemies should expose approximate threat categories rather than exact stats.

Required levels:

```text
TRIVIAL
DANGEROUS
DEADLY
UNKNOWN
```

This preserves out-of-depth terror while allowing the player to recognize danger.

Threat rating can consider:

```text
monster capability
player capability
health
equipment
known resistances
known vulnerabilities
environment
```

Knowledge may improve threat accuracy.

A newly encountered creature may therefore show:

```text
???
```

while a well-documented one provides a better estimate.

---

# 16. Monster Architecture

Monsters should be defined by families, traits, and behaviors rather than primarily by larger stats.

Required families include:

```text
Undead
Beasts
Demons
Humanoids
Constructs
Aberrations
```



## 16.1 Monster Definition

```text
MonsterDefinition
    id
    display_name
    family

    base_stats

    traits[]
    resistances[]
    vulnerabilities[]

    actions[]
    behaviors[]

    spawn_rules

    loot_table
```

## 16.2 Monster Instance

```text
MonsterInstance
    instance_id
    definition_id

    level
    current_hp

    temporary_effects[]
    current_behavior_state

    position
```

## 16.3 Behavior

Prefer reusable behavior tags such as:

```text
aggressive
ambusher
territorial
cowardly
pack_hunter
spellcaster
pursuer
guard
phase_shift
life_drain
```

---

# 17. Items and Treasure

Treasure must remain exciting rather than becoming a stream of minor upgrades.

The design calls for:

- item tiers;
- affixes;
- curses;
- unusual properties;
- relics;
- unidentified artifacts.

## 17.1 Item Definition

```text
ItemDefinition
    id
    display_name
    category

    base_properties

    affix_pool[]
    curse_pool[]

    rarity_rules
    depth_rules

    unidentified_name
```

## 17.2 Item Instance

```text
ItemInstance
    instance_id
    definition_id

    generated_affixes[]
    curse

    identified_state

    durability
```

Equipment degradation is part of the intended resource-pressure model.

---

# 18. Resource Pressure

Expedition pressure should come from more than hit points.

Required resource categories:

```text
HP
spell power
consumables
equipment condition
temporary injuries
light
noise
```



Each system should contribute to the decision:

```text
Can I safely descend again?
```

Avoid adding resources that generate bookkeeping without meaningfully affecting this decision.

---

# 19. Feature-Event System

This should be treated as a first-class game system rather than incidental environmental scripting.

Features include:

```text
fountains
altars
teleporters
pits
thrones
cubes
elevators
boxes
shrines
machines
unknown phenomena
```

The design calls for outcomes conditioned by:

```text
stats
items
depth
previous choices
```



## 19.1 Feature Definition

```text
FeatureDefinition
    id
    type
    presentation_key

    interaction_options[]

    outcome_table

    hint_rules

    knowledge_category
```

## 19.2 Outcome Rule

```text
FeatureOutcome
    conditions[]
    weight
    effects[]
    observations[]
```

Example conceptual fountain:

```text
AzureFountain

Possible effects:
    restore spell power
    blindness
    poison cleansing
    unknown transformation
```

The exact probabilities should not be exposed directly.

---

# 20. Learnable Feature Logic

Feature effects should not be purely arbitrary.

The design proposes visual or environmental signals that create learnable risk tables.

Possible observable attributes:

```text
color
smell
sound
temperature
surrounding markings
material
nearby corpses
magical aura
```

These attributes become inputs to the knowledge system.

Example:

```text
FountainObservation
    color = azure
    odor = ozone
    temperature = cold
```

Repeated experience should let the player infer likely outcomes.

---

# 21. Teleportation

Teleportation must initially create spatial uncertainty but become partially understandable over time.

The design specifically calls for networks whose patterns can gradually be mapped and decoded.

## Proposed model

```text
TeleportNode
    node_id
    position
    network_id

    destination_rule
```

Player knowledge:

```text
UNKNOWN:
    destination unknown

OBSERVED:
    player knows one observed destination

MAPPED:
    destination relationship confirmed
```

---

# 22. Trap Philosophy

Traps should primarily alter expeditions rather than simply subtract HP.

Possible effects specified by the design include:

```text
position
equipment
noise
status
supplies
dungeon topology
```



Examples:

```text
pit → drops player two floors

collapse → closes known route

alarm → increases danger/noise

acid trap → damages equipment

gas → creates temporary injury

teleport trap → relocates player
```

---

# 23. Knowledge System

Knowledge is a persistent progression track independent of character power.

The governing rule is:

> **Character power resets. Player knowledge accumulates.**

The journal must record what the player has actually observed rather than magically exposing hidden game data.

## 23.1 Knowledge Categories

Initial categories:

```text
Monsters
Fountains
Altars
Teleporters
Features
Spells
Regions
Hazards
Relics
```

## 23.2 Observation Model

```text
KnowledgeEntry
    subject_id

    observations[]
    sample_count

    hypotheses[]
    confidence

    confirmed_facts[]
```

Example:

```text
Azure Fountain

Observed:
✓ restored spell power
✓ caused blindness
? unknown
? unknown

Samples: 7
Confidence: 42%
```

This structure comes directly from the proposed Adventurer's Journal concept.

## 23.3 Observation Events

Knowledge should update through domain events.

Examples:

```text
MonsterDamagedBySpell
MonsterResistedSpell
FeatureActivated
FeatureProducedEffect
TeleportOccurred
TrapTriggered
ItemIdentified
SpellInteractionObserved
```

---

# 24. Knowledge Confidence

The source specifies confidence but does not define the formula.

Proposed implementation:

```text
confidence = function(
    observations,
    consistency,
    sample_count
)
```

Do not require statistically rigorous inference in version one.

Initial implementation may use deterministic thresholds:

```text
1 sample   → rumor
2–3        → suspected
4–6        → probable
7+         → high confidence
```

The underlying mechanics remain hidden.

---

# 25. Magic System

Magic should include deliberate experimentation.

The design states that spell descriptions may begin vague and become more precise through use.

## 25.1 Spell Definition

```text
SpellDefinition
    id
    name

    initial_description
    discovered_descriptions[]

    cost
    targeting_rule

    effects[]
    interactions[]
```

## 25.2 Spell Notebook

Knowledge progression may reveal:

```text
basic purpose
damage category
effective targets
resistances
feature interactions
unusual effects
```

Essential controls must never be hidden.

Mystery applies to consequences and relationships, not usability.

---

# 26. Character Creation

Required creation modes:

```text
ROLLED
POINT_ALLOCATION
DAILY_SEED
```



## 26.1 Rolled

Generate six attributes using the configured rolling rules.

Avoid allowing endless rerolling to become the objectively optimal strategy.

The exact anti-reroll solution is not defined and should be designed separately.

## 26.2 Point Allocation

Provide a fixed budget.

## 26.3 Daily Seed

Everyone using the same daily seed receives the same initial roll conditions.

---

# 27. Character Progression

The character remains classless.

Instead of traditional classes, progression uses talent constellations:

```text
Steel
Sorcery
Faith
Survival
Fortune
```



## 27.1 Talent Definition

```text
TalentDefinition
    id
    constellation

    prerequisites[]
    effects[]

    cost
```

Avoid large numbers of trivial percentage increases.

Prefer talents that alter decisions or capabilities.

Example:

```text
Poor:
+2% damage

Better:
successful defense creates an opening for a counterattack
```

---

# 28. Inn / Hub

The inn is the safety boundary and between-expedition interface.

Required functions:

```text
Rest
Bank treasure
Level up
Identify relics
Prepare loadout
Review discoveries
Select expedition objective
```



The inn should feel psychologically different from the dungeon.

It is not merely another menu screen.

---

# 29. Wealth Model

Maintain two explicit pools:

```text
carried_gold
secured_gold
```

## Dungeon

```text
loot → carried_gold
```

## Inn

```text
carried_gold → secured_gold
```

## Death

Loss depends on selected death mode.

The key design principle is that treasure does not fully belong to the player until they successfully return.

---

# 30. Death Modes

Three modes are required.

## 30.1 Classic

```text
Death:
character deleted
```

This is closest to the original spirit.

## 30.2 Legacy

Recommended default.

```text
Death:
current character dies
carried gold lost
most carried equipment lost

map discoveries remain
journal knowledge remains

some items may become heirlooms
corpse may appear in future dungeon
```



## 30.3 Adventure

```text
Death:
return to last inn
lose some unsecured treasure
retain character
```



---

# 31. Legacy System

Legacy progression should preserve knowledge without eliminating individual-run stakes.

Proposed profile:

```text
LegacyState
    persistent_map
    journal
    previous_heroes[]
    graves[]
    heirlooms[]
    discovered_rumors[]
```

Potential future-character dungeon encounters:

```text
grave
corpse
lost equipment
previous notes
heirloom
rumor
```

The design explicitly proposes maps, rumors, heirlooms, graves, and discoveries surviving dead heroes.

---

# 32. Save Model

The design requires:

```text
suspend save anywhere

strategic progress secured primarily at:
    inn
    checkpoint
```



## 32.1 Save Types

### Profile Save

Contains:

```text
settings
knowledge
legacy
secured progression
world seed
game mode
```

### Expedition Suspend

Contains:

```text
full GameState
RNG state / deterministic references
active encounter
position
inventory
temporary effects
```

## Requirement

Suspend saves must not become a way to repeatedly rewind bad outcomes.

Specific anti-save-scumming policy remains a separate design decision.

---

# 33. Save Schema Versioning

Every save should contain:

```text
save_version
simulation_version
generator_version
content_version
```

Provide migration functions:

```text
v1 → v2
v2 → v3
```

Do not tightly couple save compatibility to serialized runtime objects.

Prefer explicit save DTOs.

---

# 34. Data-Driven Content

The source does not specify a file format.

Recommended architecture is data-driven content definitions.

Examples:

```text
monsters/
items/
spells/
features/
bands/
loot_tables/
encounter_tables/
talents/
```

JSON, YAML, resource assets, ScriptableObjects, or engine-native equivalents are acceptable depending on stack.

The important requirement is:

```text
content definitions != hard-coded simulation logic
```

---

# 35. Domain Event Bus

Recommended domain events:

```text
PlayerMoved
PlayerEnteredTile

FloorEntered
FloorExited

EncounterStarted
EncounterEnded

MonsterSpawned
MonsterDamaged
MonsterKilled

SpellCast
SpellEffectResolved

ItemFound
ItemIdentified
ItemLost

FeatureDiscovered
FeatureActivated

TrapTriggered
TeleportOccurred

KnowledgeObservationAdded
KnowledgeFactConfirmed

GoldAcquired
GoldSecured

ExpeditionStarted
ExpeditionSucceeded
ExpeditionFailed

PlayerDied

GameSuspended
GameLoaded
```

This supports:

- renderer independence;
- journal updates;
- achievements;
- audio;
- analytics;
- debugging;
- legacy mechanics.

---

# 36. Command Model

Input systems should translate keyboard/controller/UI actions into game commands.

Example:

```text
UI INPUT
    ↓
MoveNorthCommand
    ↓
Simulation validates
    ↓
Simulation mutates GameState
    ↓
PlayerMoved event
    ↓
Presentation reacts
```

This makes all three rendering modes operate on identical behavior.

---

# 37. UI Requirements

The interface should be:

```text
keyboard-first
controller-first
configurable
contextual
low-clutter
```

Required concepts include:

```text
configurable hotkeys
context-sensitive commands
quick-cast spell bar
```



## Information Rule

Clearly distinguish:

```text
UNKNOWN BECAUSE MYSTERIOUS
```

from:

```text
UNKNOWN BECAUSE THE UI IS BAD
```

Known rules should be explained.

Dungeon mysteries should remain uncertain until learned.

---

# 38. Rendering Modes

## 38.1 Modern

```text
detailed 2D tiles
dynamic lighting
animation
atmospheric effects
```

## 38.2 Retro+

```text
stylized 16-bit presentation
same world state
same mechanics
```

## 38.3 Terminal

```text
ASCII / symbolic
same world state
same mechanics
```

These three modes are explicitly envisioned as alternate presentations of one simulation.

Do **not** build three versions of the game.

---

# 39. Audio Requirements

Use sound primarily for information and atmosphere.

Examples:

```text
footsteps
distant monsters
spell cues
environment drones
feature noises
danger cues
```

The inn should have a strong contrast with dungeon audio.

The design intentionally favors selective sound over constant stimulation.

---

# 40. Story Delivery

Narrative content should be archaeological rather than quest-driven.

Supported content:

```text
inscriptions
corpses
relics
journals
shrines
ruins
procedural histories
```



Avoid requiring conversations or quest NPCs for core progression.

---

# 41. Expedition Objectives

Intermediate objectives are allowed as optional motivation.

Examples specified by the design:

```text
deepest floor
discoveries
monster kills
relic recovery
survival streaks
optional challenges
```



These objectives must not turn the game into a linear quest sequence.

---

# 42. System Invariants

Codex should treat these as hard rules.

## Dungeon

```text
Same seed + generator version = same base geography.
```

## Simulation

```text
Rendering cannot mutate authoritative game state directly.
```

## Knowledge

```text
A journal fact cannot appear unless the player has acquired a qualifying observation.
```

## Economy

```text
Dungeon wealth remains unsecured until a safety boundary is reached.
```

## Legacy

```text
Persistent knowledge survives Legacy-mode character death.
```

## Combat

```text
The player must always have a path to attempt disengagement unless an explicitly defined effect prevents it.
```

## Mapping

```text
Unknown spaces cannot appear as fully mapped without discovery.
```

## Depth

```text
Progression through depth changes ecology/mechanics, not only enemy stat multipliers.
```

---

# 43. Debugging Requirements

Build developer tooling early.

Minimum debug commands:

```text
teleport floor x y

set hp
set level
give item
give gold

spawn monster
spawn feature

set danger

reveal tile
reveal floor

trigger death

inspect RNG key

dump game state
dump knowledge

save
load
```

Procedural systems become extremely difficult to diagnose without reproducibility tools.

---

# 44. Deterministic Test Mode

Provide a command such as:

```text
game --seed 12345 --deterministic
```

Automated tests should be able to:

```text
create world
move player
activate features
resolve combat
save state
reload state
compare results
```

The same scripted commands against the same seed should produce the same authoritative result.

---

# 45. Testing Strategy

## Unit Tests

Test isolated systems:

```text
damage
XP
item generation
feature outcomes
knowledge updates
RNG derivation
threat calculations
save migration
```

## Property Tests

Useful procedural invariants:

```text
generated stairs are reachable

required floor anchors are connected

generated positions are valid

no feature spawns inside invalid geometry

same seed reproduces dungeon
```

## Simulation Tests

Example:

```text
Given seed 123

Start expedition
Move N
Move N
Move E
Activate fountain
Fight monster
Return to inn

Assert:
state matches snapshot
knowledge observation added
secured gold correct
```

---

# 46. Telemetry for Balancing

If telemetry is implemented, useful aggregate measures include:

```text
average expedition duration

average deepest floor

death rate by floor

retreat rate by floor

gold carried at death

feature interaction rate

fountain interaction rate

flee attempt rate

successful escape rate

monster death causes

most avoided enemies

most-used spells

journal discovery rates
```

The most valuable metric is probably:

```text
At what point do players decide to descend versus retreat?
```

That directly measures the game's central design question.

---

# 47. Development Phases

## Phase 0 — Simulation Skeleton

Build:

```text
GameState
command system
domain event system
seeded RNG
save DTO structure
test harness
```

Definition of done:

```text
A deterministic simulation can start,
receive commands,
change state,
emit events,
save,
load,
and reproduce identical results.
```

---

## Phase 1 — Dungeon Walking Prototype

Build:

```text
floor coordinates
procedural layout
walls
corridors
stairs
player movement
visibility
fog of war
basic automap
```

Definition of done:

```text
Player can enter a seeded dungeon,
walk through it,
find stairs,
change floors,
leave,
and revisit the same geography.
```

---

## Phase 2 — Expedition Loop

Build:

```text
inn
expedition start/end
carried gold
secured gold
return-to-safety flow
basic XP
```

Definition of done:

```text
Player can descend,
collect treasure,
return,
bank it,
and begin another expedition.
```

This is the first build that proves the game's central loop.

---

## Phase 3 — Encounters

Build:

```text
monster definitions
monster spawning
threat ratings

attack
defend
flee

HP
death
```

Definition of done:

```text
Dungeon exploration creates fast encounters
with meaningful risk and the ability to flee.
```

---

## Phase 4 — Dungeon Features

Build:

```text
feature framework
fountains
altars
pits
teleporters
feature outcomes
```

Definition of done:

```text
Features can produce deterministic-but-uncertain outcomes
and materially alter an expedition.
```

---

## Phase 5 — Knowledge

Build:

```text
journal
observation events
sample counts
confidence
monster entries
feature entries
teleporter mapping
```

Definition of done:

```text
The player's actions create persistent knowledge
without revealing information they have not observed.
```

---

## Phase 6 — Character Progression

Build:

```text
attributes
levels
XP
spells
talent constellations
equipment
```

Definition of done:

```text
Characters can develop meaningfully without introducing traditional classes.
```

---

## Phase 7 — Legacy Death

Build:

```text
Classic
Legacy
Adventure

persistent knowledge
graves
heirlooms
previous heroes
```

Definition of done:

```text
All three death philosophies function correctly
without changing the underlying dungeon simulation.
```

---

## Phase 8 — Depth Ecology

Implement all ten depth bands.

Start with at least:

```text
Upper Ruins
Forgotten Crypts
Flooded Deeps
```

Then progressively add deeper ecology.

Definition of done:

```text
Moving between bands changes
monster ecology,
features,
hazards,
generation,
and expedition strategy.
```

---

## Phase 9 — Presentation Separation Proof

Build:

```text
Modern renderer

and

minimal Terminal renderer
```

Both must run the same save.

Definition of done:

```text
Save at position X in Modern mode.

Load Terminal mode.

Player is at identical position,
with identical dungeon,
inventory,
monster state,
knowledge,
and RNG outcomes.
```

This validates the architectural separation before presentation becomes expensive.

---

# 48. Recommended First Vertical Slice

Do **not** initially implement fifty floors, hundreds of monsters, and dozens of systems.

Build one representative slice:

```text
Floors 1–5

1 dungeon biome

8–12 monsters

4 dungeon features:
    fountain
    altar
    pit
    teleporter

10–15 items

6–8 spells

basic journal

carried/secured wealth

inn

Legacy death mode
```

A full playable loop should exist before broad content production.

The player must be able to:

```text
create hero
 ↓
prepare
 ↓
enter dungeon
 ↓
explore
 ↓
discover strange feature
 ↓
fight or flee
 ↓
find valuable treasure
 ↓
reach staircase
 ↓
decide whether to descend
 ↓
continue OR retreat
 ↓
possibly die
 ↓
possibly escape
 ↓
bank treasure
 ↓
learn something permanently
 ↓
start again
```

If that loop is compelling, expand content.

If it is not compelling, adding floors will not solve the problem.

---

# 49. Suggested Codex Work Breakdown

These are intentionally small enough to give Codex focused scopes.

## Foundation

```text
TEL-001 Create GameState domain model
TEL-002 Implement deterministic RNG service
TEL-003 Implement command dispatcher
TEL-004 Implement domain event bus
TEL-005 Implement save/load DTO system
TEL-006 Add deterministic simulation test harness
```

## Dungeon

```text
TEL-010 Define dungeon coordinate types
TEL-011 Implement procedural floor layout generator
TEL-012 Add connectivity validation
TEL-013 Implement tile visibility
TEL-014 Implement fog-of-war map
TEL-015 Implement stairs and floor transitions
TEL-016 Persist discovered map state
```

## Expedition

```text
TEL-020 Implement ExpeditionState
TEL-021 Implement inn state
TEL-022 Implement carried gold
TEL-023 Implement secured gold
TEL-024 Implement expedition completion
TEL-025 Implement expedition suspension
```

## Encounters

```text
TEL-030 Define monster data schema
TEL-031 Implement encounter trigger system
TEL-032 Implement combat state machine
TEL-033 Implement attack
TEL-034 Implement defend
TEL-035 Implement flee
TEL-036 Implement threat classification
TEL-037 Implement player death
```

## Features

```text
TEL-040 Create generic dungeon feature system
TEL-041 Implement weighted outcome engine
TEL-042 Implement fountain
TEL-043 Implement altar
TEL-044 Implement pit
TEL-045 Implement teleporter
```

## Knowledge

```text
TEL-050 Create KnowledgeEntry model
TEL-051 Implement journal observation pipeline
TEL-052 Implement sample counts
TEL-053 Implement confidence progression
TEL-054 Add monster knowledge
TEL-055 Add feature knowledge
TEL-056 Add teleporter mapping
```

## Items

```text
TEL-060 Define item templates
TEL-061 Implement item instances
TEL-062 Implement unidentified items
TEL-063 Implement affixes
TEL-064 Implement curses
TEL-065 Implement equipment slots
```

## Progression

```text
TEL-070 Implement XP
TEL-071 Implement levels
TEL-072 Implement spell definitions
TEL-073 Implement spell casting
TEL-074 Implement talent constellations
```

## Legacy

```text
TEL-080 Implement Classic death
TEL-081 Implement Legacy death
TEL-082 Implement Adventure death
TEL-083 Persist dead hero records
TEL-084 Implement graves
TEL-085 Implement heirlooms
```

## Presentation

```text
TEL-090 Create presentation-state adapter
TEL-091 Build Modern renderer prototype
TEL-092 Build Terminal renderer prototype
TEL-093 Verify renderer-independent save compatibility
```

---

# 50. Codex Task Template

Every Codex implementation request should use roughly this structure:

```text
TASK
Implement TEL-XXX: <name>.

DESIGN INTENT
Why this exists in the game.

CURRENT ARCHITECTURE
Relevant modules and interfaces.

REQUIREMENTS
- requirement
- requirement
- requirement

NON-GOALS
- excluded behavior
- excluded behavior

INVARIANTS
- rule that must never break
- rule that must never break

DATA MODEL
Expected entities / fields.

PUBLIC API
Expected interfaces.

EVENTS
Events emitted or consumed.

DETERMINISM
Any RNG or replay constraints.

SAVE IMPACT
Whether new state must serialize.

TESTS
Required tests.

ACCEPTANCE CRITERIA
Observable definition of done.
```

Avoid requests such as:

```text
"Build the combat system."
```

Prefer:

```text
"Implement the FleeAction resolver against the existing EncounterState,
including success calculation, failure consequences, domain events,
deterministic RNG use, serialization implications, and unit tests."
```

This keeps Codex working inside architecture rather than repeatedly redesigning the game.

---

# 51. Definition of Core Alpha

The game reaches **Core Alpha** when all of the following are true:

- [ ] A character can be created.
- [ ] A deterministic world seed can be selected.
- [ ] The player can enter the dungeon.
- [ ] Floors generate procedurally.
- [ ] Explored geography is mapped.
- [ ] Unknown geography remains hidden.
- [ ] Monsters can appear during exploration.
- [ ] The player can fight.
- [ ] The player can flee.
- [ ] Threat levels are communicated without exact monster stats.
- [ ] Treasure can be found.
- [ ] Treasure remains unsecured while underground.
- [ ] The player can descend.
- [ ] The player can return to the inn.
- [ ] Returning secures wealth.
- [ ] Fountains work through the generic feature-event system.
- [ ] Altars work through the generic feature-event system.
- [ ] Pits can alter dungeon position.
- [ ] Teleporters can alter dungeon position.
- [ ] Feature interactions produce journal observations.
- [ ] Monster interactions produce journal observations.
- [ ] Knowledge persists between Legacy characters.
- [ ] Player death correctly resolves.
- [ ] Suspend/resume works.
- [ ] Same seed reproduces base geography.
- [ ] Save/load reproduces authoritative simulation state.
- [ ] Rendering code does not own game logic.

---

# 52. Core Alpha Success Test

The strongest qualitative playtest for the implementation is not:

```text
"Did all the systems function?"
```

It is:

```text
The player finds valuable treasure.

They are injured.

They know the inn is several minutes away.

A staircase downward is directly beside them.

They hesitate.
```

If the implementation consistently produces that hesitation, the central Telengard mechanism is working.

The design describes exactly this psychological loop: the dungeon should continually suggest “maybe just one more floor,” while making survival and returning with treasure meaningful.

That should remain the primary criterion for every subsequent system.