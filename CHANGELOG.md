# Changelog

## 1.2.0 - 2026-09-25 (fork)

- Re-acquire memory: a bot that actually spotted the player skips the delay if it re-sees them within `ReacquireMemory` seconds (default 10), closing the peek-in/peek-out reset.
- Bots with night vision in use (`BotOwner.NightVision.UsingNow`) are not delayed.
- Combat bypass: no delay for `CombatBypass` seconds (default 8) after `EnemyInfo.LastGetHitTime` / `LastDoHitTime`.
- SAIN compatibility: when `me.sol.sain` is loaded, delays are multiplied by `SainDelayMultiplier` (default 0.6) because SAIN applies its own darkness gain-sight modifier.
- Muzzle flash: postfix on `Player.OnMakingShot` for the local player; unsuppressed shots force BRIGHT and suppressed shots force at least DIM for `MuzzleFlashDuration` (default 1.5 s).
- Visible lasers (`vis_0*` device modes) raise exposure to at least DIM; IR devices are still ignored.
- Movement: sprinting adds `MovementWeight` (default 0.10) to exposure, standing still removes half of it.
- Light rescans use the configurable `LightRefreshInterval` (default 10 s, unchanged); diagnostics log each scan's light count and duration.

## 1.1.0 - 2026-09-11

- Ported to SPT 4.1.5 (fork).
- Verified the entire EFT surface against a real SPT 4.1.5 `Assembly-CSharp.dll`: `EnemyInfo.SetVisible(bool value)` — including the parameter name Harmony binds by — plus `EnemyInfo.Distance`/`.Person`/`.Owner`, `TacticalComboVisualController.LightMod`, `PlayerBones.WeaponRoot`, `LaserBeam`, `Player.FirearmController`, `EFT.EnvironmentEffect`, and `EFT.Weather`. Nothing was renamed, so no gameplay or patch code changed.
- Raised the BepInEx dependency from `com.SPT.core` 4.0.13 to 4.1.0.
- Defaulted `SptVersion` in the release packager to 4.1.5.
- Added an `EnsureRealSptReflection` build check: building against a placeholder `spt-reflection.dll` (version 1.0.0.0) compiles but produces a plugin the launcher rejects as "built for SPT 1.0.0", so the build now fails with an explanation instead.
- Defaulted `SptRoot` so a plain `dotnet build -c Release` works without a local props file; `-p:SptRoot=`, `SPT_ROOT`, and `VeilSight.local.props` still override it.
- Rewrote the README in Korean, keeping attribution to the original author.

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
