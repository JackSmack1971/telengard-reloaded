# Art Production Ready gate

## Purpose

This gate determines when Telengard Reloaded may transition from exploratory
visual development and placeholder/graybox client work into systematic final
production art, animation, VFX, UI-art, and production-audio batches for the
floors 1-5 vertical slice.

The gate exists to prevent expensive assets from being created against unstable
content identities, unresolved presentation contracts, or unproven UX flows.

## Hard prerequisite

`docs/gates/GODOT-PLAYABLE-SLICE.md` must pass first.

Concept art, style studies, UI wireframes, placeholder assets, and technical
visual experiments do not require this gate. Final production inventory does.

## Sources of truth

- `docs/presentation/GODOT_CLIENT_BLUEPRINT.md`
- `docs/presentation/UX_INTERACTION_BLUEPRINT.md`
- `docs/presentation/ART_DIRECTION_BLUEPRINT.md`
- `docs/presentation/ASSET_PIPELINE_BLUEPRINT.md`
- `docs/gates/GODOT-PLAYABLE-SLICE.md`
- TEL-110 through TEL-116 authored first-slice content
- the current Godot presentation/asset registry implementation

## Acceptance checklist

### Content identity is stable enough for production

- [ ] TEL-110 floors 1-5 biome/band identity is implemented and verified.
- [ ] TEL-111 first-slice monster roster is implemented and verified.
- [ ] TEL-112 encounter ecology is implemented and verified.
- [ ] TEL-113 item roster is implemented and verified.
- [ ] TEL-114 loot tables are implemented and verified.
- [ ] TEL-115 spell roster is implemented and verified.
- [ ] TEL-116 four feature definitions are implemented and verified.
- [ ] Production inventory is generated from stable IDs rather than display
      names or temporary scene-local labels.

### Playable placeholder client exists

- [ ] The Playable Godot Vertical Slice gate passed on current-enough client and
      content contracts.
- [ ] Every required production asset category has a working placeholder in the
      same lookup/presentation path intended for final assets.
- [ ] Placeholder playtesting has exposed and resolved major camera/scale/state
      contract problems.

### Production presentation contract is stable

- [ ] Renderer-safe projections expose all observable information needed to
      choose and update production visuals.
- [ ] Godot does not need hidden GameState/content internals to render the
      first-slice client.
- [ ] Content/presentation IDs resolve through a documented asset registry.
- [ ] Missing/duplicate required mappings are detectable.
- [ ] Placeholder versus production mapping state is reviewable.

### Art direction decisions are documented

For the first slice, the review packet defines:

- [ ] reference viewport and scaling strategy;
- [ ] tile size/world scale;
- [ ] camera model;
- [ ] Upper Ruins environment palette/material language;
- [ ] inn-versus-dungeon contrast;
- [ ] unknown/observed/visited/current visual language;
- [ ] player/monster silhouette and scale language;
- [ ] fountain/altar/pit/teleporter visual language;
- [ ] item/spell icon language;
- [ ] HUD typography/hierarchy/focus language;
- [ ] lighting/atmosphere rules;
- [ ] animation cadence/state vocabulary;
- [ ] VFX readability/intensity rules;
- [ ] baseline accessibility/readability notes.

The gate does not require a specific artistic style; it requires that the chosen
style constraints are explicit enough to produce consistent assets.

### Asset pipeline is defined

- [ ] Source-art versus runtime-asset locations are documented.
- [ ] Naming convention uses stable presentation/content IDs.
- [ ] Texture/import/scaling settings are documented.
- [ ] Sprite/atlas/animation conventions are documented.
- [ ] UI asset conventions are documented.
- [ ] VFX/audio organization conventions are documented.
- [ ] Binary/Git LFS policy is decided before large asset batches.
- [ ] Third-party asset license/provenance expectations are documented.
- [ ] Validation can enumerate missing first-slice mappings.

### Production asset matrix exists

The first-slice inventory records, for each relevant ID:

- [ ] category;
- [ ] required visual resource(s);
- [ ] required audio resource(s) when applicable;
- [ ] observable presentation states;
- [ ] placeholder status;
- [ ] production status;
- [ ] art-direction/reference link or identifier;
- [ ] review/approval status.

### UX is no longer structurally moving

- [ ] Required first-slice screens/overlays are represented in the playable
      client.
- [ ] Keyboard/controller focus/navigation has been exercised.
- [ ] Major panel dimensions and information hierarchy are stable enough that
      UI art can be produced without guessing the interaction model.
- [ ] Production art is not being used to hide an unresolved interaction or
      gameplay-command boundary.

## What passing this gate authorizes

After this gate passes, the task ledger may introduce focused production-asset
batches, for example:

- Upper Ruins environment tile/prop batch;
- first-slice monster art/animation batches;
- four feature production-art batch;
- first-slice item icon batch;
- first-slice spell icon/VFX batch;
- HUD/menu production-art batch;
- first-slice production-audio batch.

Each batch still follows the one-slice transaction and must not change
simulation rules merely to accommodate artwork.

## What passing does not authorize

Passing this gate does not authorize:

- art for all 50 floors;
- hundreds of monsters/items outside the representative slice;
- broad content expansion before gameplay evidence supports it;
- renderer-owned gameplay behavior;
- silent changes to content IDs or save contracts;
- bypassing normal TEL/ExecPlan/review/verification workflow.

## Failure conditions

The gate fails if:

- final asset requirements are still being guessed from prototype rectangles;
- stable first-slice content IDs do not exist;
- major required client flows still exist only in debug tooling;
- production visuals require hidden simulation data;
- asset mappings are scattered through scene code without a reviewable registry;
- scale/camera/import rules are unresolved;
- UI art would need to be redone because interaction structure is still moving;
- large binary batches are planned without a repository policy.

## Result recording

When the gate passes, record:

- date;
- commit/PR;
- Playable Godot Slice gate evidence used;
- art-direction review references;
- asset-pipeline/registry validation evidence;
- first-slice production inventory count;
- intentionally deferred deeper-band content.

Production-art TEL tickets should be created only after this evidence exists.
