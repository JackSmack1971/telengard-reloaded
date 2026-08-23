# Changelog

All notable changes to this project will be documented in this file.

## [Unreleased]

### Added

- Added a renderer-independent deterministic simulation foundation for seeded dungeon exploration, expeditions, encounters, combat, features, items, magic, and progression
- Added explicit versioned save/load DTOs and migrations for restoring simulation state across sessions
- Added the Modern renderer prototype frame/cue projection and optional Godot visual prototype
- Added Adventure death handling that returns the retained character to the inn while discarding expedition-carried gold and acquired loot
- Added Legacy dead-hero records with explicit save/load persistence and version-10 migration support
- Added Legacy grave markers with explicit save/load persistence and version-11 migration support
- Added Legacy heirloom records with explicit save/load persistence and version-12 migration support
- Added an immutable renderer-facing presentation-state adapter that filters undiscovered features and internal monster details
- Added role-aware coverage reporting and safely scoped mutation-tooling arguments
- Added the renderer-independent character-creation command boundary for ROLLED, POINT_ALLOCATION, and DAILY_SEED providers
- Added configurable deterministic rolled character creation through the simulation boundary
- Added configurable point-allocation character creation with deterministic validation through the simulation boundary
- Added configurable daily-seed character creation with deterministic, cross-player attribute generation
- Added renderer-independent initial game setup with deterministic world-seed selection and ready-at-inn state
- Added renderer-independent treasure acquisition with unsecured expedition loot and deterministic content loot-table selection
- Added the deterministic terminal renderer prototype with ASCII/symbolic state and safe event-cue output
- Added validated Legacy character replacement that preserves persistent knowledge and Legacy history across death
- Added a canonical audit-status ledger with deterministic generated playbook and P0-gate projections and stale-output validation
- Added a deterministic Core Alpha vertical-slice integration proof covering success, Legacy failure/restart, save reload, knowledge, and wealth boundaries
- Added deterministic headless developer debug scripts with simulation-routed state setup, stable JSON Lines output, and save/load replay comparison
- Added renderer-independent save compatibility proof across the Modern and Terminal presentation boundaries
- Added the versioned Upper Ruins dungeon band definition for representative floors 1–5
