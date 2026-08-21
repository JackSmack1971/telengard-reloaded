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
- Added a canonical audit-status ledger with deterministic generated playbook and P0-gate projections and stale-output validation
