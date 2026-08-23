---
version: alpha
name: Telengard Reloaded — Deep Ward
description: "A modern visual reinterpretation of a 1982 dungeon crawl: void-black oppression, phosphor-green vigilance, and the fragile amber warmth of the Inn. Preserves what the original communicated, not how 8-bit hardware communicated it."
colors:
  void-primary: "#0A0B0D"
  void-elevated: "#14161A"
  void-recessed: "#050506"
  surface-inn: "#1C1712"
  surface-inn-elevated: "#26201A"
  ward: "#4CE38A"
  ward-hover: "#3BC975"
  ward-dim: "#1F5C3C"
  ember: "#E3A052"
  ember-hover: "#C98A3F"
  misty: "#8C8FE0"
  danger: "#E5484D"
  danger-hover: "#C93C40"
  caution: "#E0B84C"
  text-high: "#F2F4F2"
  text-muted: "#8B9490"
  text-on-ward: "#08130D"
  text-on-ember: "#1A0F04"
  divider: "#242830"
  tile-visited: "#2A3A33"
typography:
  display-large:
    fontFamily: "Cinzel Decorative, serif"
    fontSize: 3.5rem
    fontWeight: 700
    lineHeight: 1.05
    letterSpacing: "0.02em"
  headline:
    fontFamily: "Cinzel, serif"
    fontSize: 1.5rem
    fontWeight: 600
    lineHeight: 1.2
    letterSpacing: "0.01em"
  body:
    fontFamily: "Inter, sans-serif"
    fontSize: 1rem
    fontWeight: 400
    lineHeight: 1.5
  label-caps:
    fontFamily: "Inter, sans-serif"
    fontSize: 0.75rem
    fontWeight: 600
    letterSpacing: "0.12em"
  stat-mono:
    fontFamily: "JetBrains Mono, monospace"
    fontSize: 0.9375rem
    fontWeight: 500
    lineHeight: 1.3
    fontFeature: "'tnum' 1"
  combat-log:
    fontFamily: "JetBrains Mono, monospace"
    fontSize: 0.8125rem
    fontWeight: 400
    lineHeight: 1.6
    fontFeature: "'tnum' 1"
spacing:
  none: 0px
  xs: 4px
  sm: 8px
  md: 16px
  lg: 32px
  xl: 64px
  tile: 48px
  hud-gutter: 24px
rounded:
  none: 0px
  xs: 2px
  sm: 4px
  md: 8px
  full: 9999px
components:
  button-primary:
    backgroundColor: "{colors.ward}"
    textColor: "{colors.text-on-ward}"
    typography: "{typography.label-caps}"
    rounded: "{rounded.xs}"
    padding: "{spacing.sm}"
    height: "40px"
  button-primary-hover:
    backgroundColor: "{colors.ward-hover}"
    textColor: "{colors.text-on-ward}"
  button-primary-disabled:
    backgroundColor: "{colors.void-elevated}"
    textColor: "{colors.text-muted}"
  button-secondary-inn:
    backgroundColor: "{colors.ember}"
    textColor: "{colors.text-on-ember}"
    typography: "{typography.label-caps}"
    rounded: "{rounded.xs}"
    padding: "{spacing.sm}"
    height: "40px"
  button-secondary-inn-hover:
    backgroundColor: "{colors.ember-hover}"
    textColor: "{colors.text-on-ember}"
  panel-hud:
    backgroundColor: "{colors.void-elevated}"
    textColor: "{colors.text-high}"
    typography: "{typography.body}"
    rounded: "{rounded.none}"
    padding: "{spacing.md}"
  panel-hud-inn:
    backgroundColor: "{colors.surface-inn-elevated}"
    textColor: "{colors.text-high}"
    typography: "{typography.body}"
    rounded: "{rounded.none}"
    padding: "{spacing.md}"
  panel-combat-log:
    backgroundColor: "{colors.void-recessed}"
    textColor: "{colors.text-muted}"
    typography: "{typography.combat-log}"
    rounded: "{rounded.none}"
    padding: "{spacing.sm}"
  stat-readout:
    backgroundColor: "{colors.void-elevated}"
    textColor: "{colors.text-high}"
    typography: "{typography.stat-mono}"
    rounded: "{rounded.xs}"
    padding: "{spacing.xs}"
  badge-status:
    backgroundColor: "{colors.void-elevated}"
    textColor: "{colors.text-high}"
    typography: "{typography.label-caps}"
    rounded: "{rounded.full}"
    padding: "{spacing.xs}"
  badge-status-danger:
    backgroundColor: "{colors.danger}"
    textColor: "{colors.text-high}"
  bar-track:
    backgroundColor: "{colors.void-recessed}"
    rounded: "{rounded.full}"
    height: "8px"
  bar-fill-hp:
    backgroundColor: "{colors.ward}"
    rounded: "{rounded.full}"
    height: "8px"
  bar-fill-hp-critical:
    backgroundColor: "{colors.danger}"
    rounded: "{rounded.full}"
    height: "8px"
  bar-fill-sp:
    backgroundColor: "{colors.misty}"
    rounded: "{rounded.full}"
    height: "8px"
  tile-current:
    backgroundColor: "{colors.void-primary}"
    textColor: "{colors.ward}"
  tile-visited:
    backgroundColor: "{colors.tile-visited}"
  tile-unseen:
    backgroundColor: "{colors.void-recessed}"
  tile-feature-benign:
    textColor: "{colors.ember}"
  tile-feature-teleport:
    textColor: "{colors.misty}"
  tile-feature-danger:
    textColor: "{colors.danger}"
  modal-inn:
    backgroundColor: "{colors.surface-inn}"
    textColor: "{colors.text-high}"
    typography: "{typography.body}"
    rounded: "{rounded.sm}"
    padding: "{spacing.lg}"
  tooltip:
    backgroundColor: "{colors.void-elevated}"
    textColor: "{colors.text-high}"
    typography: "{typography.label-caps}"
    rounded: "{rounded.xs}"
    padding: "{spacing.xs}"
---

## Overview

Telengard Reloaded is a modern visual reinterpretation of Daniel Lawrence's 1982 dungeon crawl — not a graphical reproduction of it. The original ran on hardware that could only *imply* dread: a black CRT void, a handful of composite-artifacted primaries, and dense monospace stat blocks standing in for everything the machine couldn't draw. Players filled the rest in themselves. That's the thing worth keeping.

The guiding rule for every decision in this file: **preserve what the original was communicating, not necessarily how it communicated it.** Low resolution, character-cell graphics, and restricted palettes are not requirements — they're the reason certain feelings existed (oppressive darkness, sparse information, real-time dread), and this system reproduces the feelings with contemporary tools instead of the constraints.

Three things structure every surface in the game:

- **Oppressive darkness as the default state.** The dungeon is `{colors.void-primary}` — a near-black cold void — almost everywhere. Light, color, and information are earned by exploration, not given away. This is the spiritual successor to the original's black screen: emptiness *is* the atmosphere, not a placeholder for it.
- **Sparse, meaningful signal.** One accent color (`{colors.ward}`, a cold phosphor-green) does essentially all interactive and "the player is here / this is active" signaling in the dungeon. When something glows green, it matters. This mirrors the CRT phosphor legibility of the original without literally rendering scanlines.
- **Dungeon vs. Inn as the game's only real palette shift.** Everywhere else in this file, single-accent discipline holds. The Inn is the deliberate, documented exception: it swaps `{colors.void-primary}` for a warm umber-black (`{colors.surface-inn}`) and lights it with `{colors.ember}` instead of `{colors.ward}`. This is not a second competing brand accent — Inn UI never uses `{colors.ward}` for its own actions, and Dungeon UI never uses `{colors.ember}`. The contrast between the two *is* the game telling you that you are, or are not, safe.

This system targets a Godot 4 .NET presentation client (currently mid-migration from a GDScript prototype, `ModernRenderer.gd`, toward the declared production path). Godot does not own gameplay state — it consumes these tokens purely for presentation: dungeon tile rendering at a 48×48 logical grid (`{spacing.tile}`), HUD/stat panels, combat log, and Inn/town UI, driven from Godot `Theme` resources and C# view-model bindings rather than CSS. The token math (hex values, DTCG structure, WCAG verification) is identical regardless of consumer — this file is the same contract whether it's read by a Tailwind exporter or a Godot theme importer.

## Colors

The palette is built from three void-black surface steps, one signaling accent, one contextual "safety" accent reserved exclusively for the Inn, and a small set of functional gameplay-state colors.

**Void surfaces (Dungeon default):**
- **Void Primary** (`#0A0B0D`): The base of every dungeon screen. This is the "screen is mostly off" feeling the C64 original had by hardware necessity — here it's a deliberate design choice. Never place body text directly on unlit void; text lives on `{colors.void-elevated}` or higher.
- **Void Elevated** (`#14161A`): The one-step-up surface for HUD panels, stat readouts, and tooltips. This is where the player actually reads things.
- **Void Recessed** (`#050506`): Deeper than primary — used for unseen/fog-of-war tiles and the combat log backdrop. This is *below* the floor the player stands on, informationally: things you haven't discovered yet.

**Inn surfaces (the sanctioned exception):**
- **Surface Inn** (`#1C1712`): Warm umber-black base, used only inside town/Inn screens. It should never bleed into dungeon UI, and dungeon `{colors.void-primary}` should never appear in the Inn. The swap itself is the safety cue.
- **Surface Inn Elevated** (`#26201A`): Inn's equivalent of `{colors.void-elevated}` — stat panels, rest dialogs, shop lists.

**Ward** (`#4CE38A`): The single dungeon accent — phosphor-green, evocative of a monochrome monitor and of glowing runes/wards in the fiction. Drives every primary interactive element in the dungeon: buttons, the player's tile marker, active/selected states, HP bar fill above the critical threshold. If it's green, it's either the player or something the player can act on right now.

**Ember** (`#E3A052`): The Inn's equivalent signal color — warm torchlight amber. Drives Inn buttons, rest prompts, and shop interactions. Documented exception to single-accent discipline: this is a *contextual* accent bound to one screen family, not a second brand color competing with Ward for attention across the whole app.

**Misty** (`#8C8FE0`): A soft violet-gray reserved for teleportation — a direct nod to the original's "grey misty cubes." Used for the SP (spell point) bar fill and any teleport-related tile markers or effects. Never used for standard interaction; it means "space is not stable here."

**Functional states** (used identically in both Dungeon and Inn contexts, since danger and caution are universal):
- **Danger** (`#E5484D`): Critical HP, death warnings, active monster threat markers.
- **Caution** (`#E0B84C`): Traps, low resources, non-critical warnings.

**Text:**
- **Text High** (`#F2F4F2`): Primary reading text on any void or Inn surface.
- **Text Muted** (`#8B9490`): Secondary/label text, combat log body copy.
- **Text on Ward** / **Text on Ember**: Near-black variants used only for text sitting directly on filled accent surfaces (buttons), never on plain backgrounds.

## Typography

Three families carry three distinct jobs, and none of them should bleed into each other's territory.

- **Cinzel Decorative** (`{typography.display-large}`) is reserved for the biggest ceremonial moments only: the title screen, "LEVEL 12" descent banners, character death screens. It should feel carved, permanent, slightly archaic — it is *never* used for anything the player reads more than once per session.
- **Cinzel** (`{typography.headline}`) handles section headers inside panels — "CHARACTER," "INVENTORY," "THE DEADLY DRAGON INN." Same carved-stone family as display, smaller and quieter, so headers still feel like part of the world rather than a UI chrome font.
- **Inter** (`{typography.body}`, `{typography.label-caps}`) is the actual interface workhorse — flavor text, tavern dialogue, button labels, tooltips. It should read as unmistakably modern and clean against the carved-stone headers; that contrast is intentional and does the same job the original's plain PETSCII text did against its graphics.
- **JetBrains Mono** (`{typography.stat-mono}`, `{typography.combat-log}`) is mandatory for every numeric HUD value — HP, SP, gold, coordinates, damage numbers in the combat log. Tabular numerals (`'tnum' 1`) are required on both roles without exception: numbers in a stat block or a scrolling combat log that don't align vertically break the "instrument panel" feeling this game needs.
- Max line length in any prose panel (flavor text, tavern dialogue, item descriptions) is **65 characters** — this is a direct carryover from the original's terminal-width text discipline, and it still reads better in a narrow HUD sidebar than a wide unconstrained block.
- Combat log entries are left-aligned, monospace, and should visually resemble a terminal scrollback — this is one of the few places a literal retro echo (not just the *feeling* of one) is appropriate, because it's functionally a log, not a rendered world.

## Layout & Spacing

- Base structural rhythm runs on an **8px grid** (`{spacing.xs}` through `{spacing.xl}`), consistent with standard interface rhythm regardless of the game's dungeon-grid math.
- The dungeon tile itself is tokenized separately as `{spacing.tile}` (48px logical) to match the current Godot renderer's tile scale — treat this as a hard layout constant referenced by the tile/grid renderer, not as part of the general UI spacing scale.
- HUD panels (stat block, combat log, minimap) dock to fixed screen edges with `{spacing.hud-gutter}` (24px) of breathing room from the viewport edge — enough that dungeon geometry never visually collides with UI chrome.
- The dungeon viewport itself has **no maximum width constraint** — it should fill available space, since the sense of scale (200×200 tiles per level, 50+ levels) is part of what the original communicated through sheer size. HUD and panel content, by contrast, is capped and centered where it appears in modal/dialog contexts (Inn menus, character sheet) to stay legible.
- Inn/town screens use more generous spacing than dungeon HUD (favor `{spacing.lg}` between menu groups over `{spacing.md}`) — this is deliberate: the Inn should *feel* less cramped and less urgent than the dungeon's dense instrument-panel HUD.

## Elevation & Depth

Depth is conveyed exclusively through absolute background color shifts between the three void steps (and their Inn equivalents) — never through drop shadows, glows-as-shadows, or blur. This isn't just stylistic preference; it's the same principle that makes the dungeon/Inn palette swap legible as *safety information* rather than decoration, and mixing in simulated lighting would muddy that signal.

- **Void Primary → Void Elevated → Void Recessed** is the complete depth stack for the dungeon. A HUD panel (`{colors.void-elevated}`) sits "above" the dungeon floor (`{colors.void-primary}`) purely because it's a lighter step; the combat log and fog-of-war tiles sit "below" (`{colors.void-recessed}`) because they're darker still. No `box-shadow`, no `drop-shadow`, under any circumstance.
- **Surface Inn → Surface Inn Elevated** mirrors the same two-step logic inside town/Inn screens.
- Depth should *increase in oppressiveness* the deeper the player descends: as dungeon level number increases, HUD panel backgrounds may shift a few percent darker/lower-contrast within the existing token set (interpolating toward `{colors.void-recessed}`) to reinforce "further from safety" — this is a prose-level animation/theming behavior, not a new token; implement it as a runtime tint applied to the existing `panel-hud` background, not as new hardcoded hex values.
- Glow effects (a soft `rgba` bloom behind the player's tile marker or an active Ward-accented element, for instance) are permitted as prose-documented visual effects layered by the renderer, but are never baseline component tokens — they're implemented as an additive glow shader/particle pass in Godot, referencing `{colors.ward}` as its source color, not as a new frontmatter token.

## Shapes

Shape language is severe and mostly unrounded — this is a dungeon carved from stone, not a friendly consumer app.

- `{rounded.none}` is the default for all structural surfaces: HUD panels, the combat log, tile backgrounds, modal dialogs' outer frame. Corridors and walls in the tile renderer are hard-edged; nothing about the dungeon geometry should read as soft.
- `{rounded.xs}` (2px) is reserved for small interactive controls — buttons, tooltips, individual stat readouts — just enough to distinguish "clickable/readable chip" from "structural wall of the world" without softening the overall tone.
- `{rounded.full}` is reserved exclusively for status pills (`badge-status`) and resource bars (HP/SP tracks and fills) — the one place a rounded, almost organic shape is appropriate, since these represent living, fluctuating quantities rather than dungeon architecture.
- Inn/town surfaces may use `{rounded.sm}` (8px) on their modal frame only — very slightly softer than the dungeon's `{rounded.none}`, reinforcing "safety" the same way the color swap does, without breaking the game's overall hard-edged identity.

## Components

- **Buttons:** `button-primary` (Ward-green fill, near-black text, `{rounded.xs}`) is the only primary action button style used inside the dungeon — inventory actions, combat choices, menu confirms. `button-secondary-inn` is its Inn-context equivalent (Ember fill) and must never appear inside dungeon screens; the two button families are not interchangeable skins of each other, they're contextually exclusive.
- **HUD stat panel:** `panel-hud` holds the character's core numbers (HP, SP, level, gold, coordinates) using `stat-readout` for each individual value in `{typography.stat-mono}` with tabular numerals. This panel is always visible during dungeon exploration and should never be dismissible — it's the player's only lifeline of information in an otherwise dark screen, mirroring the original's always-on stat line.
- **Combat log:** `panel-combat-log` is a scrolling, monospace, `{colors.void-recessed}`-backed panel using `{colors.text-muted}` for historical entries and `{colors.text-high}` or a functional color (`{colors.danger}`, `{colors.caution}`) for the most recent/relevant line. New entries should visually "arrive" at the bottom, terminal-style.
- **Resource bars:** `bar-track` (recessed, pill) with `bar-fill-hp` (Ward-green) that swaps to `bar-fill-hp-critical` (Danger-red) below a defined critical HP threshold (recommend 20% of max HP), and `bar-fill-sp` (Misty-violet) for spell points. The HP color swap is a hard state change, not a gradient — the player should feel the moment it happens.
- **Tile states:** `tile-current` (player's cell — Ward-green marker on void), `tile-visited` (dim explored-but-empty floor), `tile-unseen` (fog of war, indistinguishable from `{colors.void-recessed}`), and `tile-feature-*` variants for altars/fountains/thrones (Ember), teleport cubes (Misty), and known dangers (Danger). Unseen tiles must be genuinely indistinguishable from surrounding void — no visual hint that something exists there before it's discovered.
- **Badges:** `badge-status` (pill, neutral) for buffs/conditions like "Protected" or "Invisible"; `badge-status-danger` for debuffs like "Poisoned" or "Cursed." Both use `{typography.label-caps}` for maximum legibility at small size.
- **Modals:** `modal-inn` is the only modal treatment in the game (rest, save, shop, character creation) — always Inn-toned regardless of whether it's triggered from a dungeon context (e.g., an emergency Scroll of Recall), reinforcing that entering any modal dialog is itself a small pocket of safety.
- **Tooltips:** Small, `{rounded.xs}`, `{colors.void-elevated}` background, `{typography.label-caps}` — used for item/monster/feature identification on hover or focus, never for critical information the player needs without hovering.

## Do's and Don'ts

- **Do** let the dungeon viewport stay almost entirely `{colors.void-primary}` at all times — if a screen feels "busy" or "full," something has gone wrong; emptiness is the design, not a loading state.
- **Do** treat the Dungeon/Inn palette swap as a hard boundary. A screen is either fully Dungeon-toned or fully Inn-toned; there is no in-between or gradient transition between the two token sets.
- **Do** use tabular numerals (`'tnum' 1`) on every single numeric HUD value without exception — misaligned digits in the stat block or combat log undermine the "instrument panel" feeling immediately.
- **Do** keep `{colors.misty}` exclusive to teleportation and spell points — it should never become a generic "purple UI accent" used for unrelated decoration.
- **Don't** use `box-shadow`, `drop-shadow`, `filter: blur()`-as-elevation, or any simulated lighting to indicate depth or hierarchy. Depth is background color shift only, full stop.
- **Don't** apply `{rounded.full}` or `{rounded.sm}` to dungeon structural surfaces (panels, tiles, the combat log frame). Roundness is reserved for bars/badges and the Inn modal frame only.
- **Don't** let `{colors.ward}` and `{colors.ember}` appear as competing accents on the same screen. If a UI element needs to reference both a dungeon action and an Inn action in one view, restructure the layout — don't blend the palettes.
- **Don't** literally reproduce CRT scanlines, pixel-grid snapping, color-cycling, or composite-video artifacting anywhere outside the combat log's intentionally terminal-flavored treatment. The goal is the feeling of the original's atmosphere, not a skeuomorphic hardware simulation layered over modern art.
- **Don't** give fog-of-war/`tile-unseen` cells any visual distinction from surrounding void — no faint outline, no "something is here" hint. Undiscovered means undiscovered.
- **Don't** soften the HP-critical state transition into a gradient or animation that delays the color swap — the moment HP crosses the critical threshold, `bar-fill-hp-critical` should be immediate and unambiguous.
