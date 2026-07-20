# Changelog

## 1.0.1 - 2026-07-19

- Fixed cross-extractor compatibility. The 1.0.0 ZIP’s directory entries have zero attributes and omit two parent-directory records.
- Added canonical forward-slash paths, explicit parent-directory records, and directory attributes to release archives.

## 1.0.0 - 2026-07-19

- Added live natural and artificial exposure classification with validated DARK, DIM, and BRIGHT boundaries.
- Added separate DIM and DARK distance-scaled visibility delays, caps, and a close-range bypass.
- Added Forgiving and Standard balance presets without coupling VeilSight to a specific AI mod.
- Added a compact configurable amber exposure meter with boundary-aware score mapping, responsive smoothing, and exposure-dependent brightness.
- Added visible weapon-light detection while ignoring laser-only modes.
- Added current-light filtering, corrected spotlight cone evaluation, and a bounded runner-up check when the strongest nearby light is obstructed.
- Added live ambient sampling and separate ambient, direct-light, and nearby-light contributions.
- Added dedicated handling for Labs, Factory day/night, and Labyrinth while preserving normal-map behavior; Factory's purely visual angled ceiling-light shafts remain a documented map-authoring limitation.
- Added stale-data, unavailable-source, exception, and disabled-state fail-open behavior.
- Added stable raid and enemy-pair lifecycle handling, including preserved progress across DIM/DARK transitions.
- Added configuration ranges, release-safe diagnostic defaults, external SPT build-path configuration, and conditional deployment.
- Validated daylight, dusk, night, indoor, bunker, artificial-light, weapon-light, destroyed-light, and supported weatherless-map behavior through controlled SPT 4.0.13 raids.
