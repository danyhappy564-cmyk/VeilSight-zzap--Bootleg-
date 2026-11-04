<p align="center">
  <a href="README.md"><img alt="한국어" src="https://img.shields.io/badge/%ED%95%9C%EA%B5%AD%EC%96%B4-%ED%95%9C%EA%B5%AD%EC%96%B4%EB%A1%9C%20%EB%B3%B4%EA%B8%B0-6e7681?style=for-the-badge"></a>
  <a href="README.en.md"><img alt="English (this page)" src="https://img.shields.io/badge/English-This%20page-1f6feb?style=for-the-badge"></a>
</p>

### ⚠️ IMPORTANT NOTICE / DISCLAIMER

- **Original Author:** Sherrifpowpow
- **Original Repository:** VeilSight
- **Original Link:** https://github.com/Sherrifpowpow/VeilSight
- **License:** MIT
- **This Port By:** R_F (danyhappy564-cmyk) — unofficial, AI-assisted port. Not affiliated with or endorsed by the original author.

1. **Reflection & Take-Downs:** I deeply reflect on the ECOT incident. As an AI-assisted "vibe coder," I will immediately delete files if the original authors ask.
2. **No Re-Distribution:** These ported builds are unverified, temporary fixes. Please do NOT re-upload or share them anywhere else.
3. **Do Not Pester Original Authors:** Never report bugs or pester original modders regarding issues from my unofficial ports.
4. **Full Credit & Respect:** I will always credit original creators on GitHub and prioritize their decisions above all else.
5. **Support Original Creators:** Instead of using my ports, please visit the original authors' Forge pages to leave kind words or tips.

---

## Changelog

The full changelog (7 entries) is in the [`changelog/`](changelog/) folder, 10 per file (higher number = newer, latest: [`CHANGELOG-001.md`](changelog/CHANGELOG-001.md)). Per-release summaries: [Releases](../../releases).

---

# VeilSight (SPT 4.1 port)

**Enemies take longer to notice you when you hide in darkness.**

Measures ambient brightness in real time. In dark areas, it delays the point at which bots
confirm that they have seen you, with the delay scaling by distance. Stepping into the light
or turning on a flashlight removes the advantage immediately. You do not become invisible;
**confirmed detection is simply delayed by a few seconds**.

> **Original author · Original**
> **Sherrifpowpow** — MIT License
>
> This repository is a **fork** of the original above. Through 1.1.0, it was **only an SPT 4.1 port**;
> **from 1.2.0 onward, this fork adds its own rules and features** (see below).
> Set all newly added values to 0/off in F12 to restore the original behavior.

---

## Additions and fixes in v1.2.0

All changes affect the rules deciding whether to delay **the moment a bot confirms it has seen you**
(`EnemyInfo.SetVisible`, the game function that toggles a bot's "enemy visible" state).
The only newly hooked game function is `Player.OnMakingShot` (runs when the player fires); it only records shot times.

| Feature | What changes | How to disable |
|---|---|---|
| **Reacquisition memory** | Once a bot has actually seen you, it sees you **immediately** if it spots you again within 10 seconds of losing sight. Previously, repeatedly peeking out of darkness could grant another 2–4 seconds each time | `Awareness / ReacquireMemory` = 0 |
| **Bots with night vision** | Bots with NVGs **switched on** receive no darkness delay | `Awareness / NightVisionBypass` = false |
| **In combat** | No delay for 8 seconds after you hit that bot or it hits you | `Awareness / CombatBypass` = 0 |
| **SAIN adjustment** | Delay × 0.6 when SAIN is installed. SAIN already calculates slower detection in darkness, so leaving it unchanged would stack both effects | `Compatibility / SainDelayMultiplier` = 1 |
| **Muzzle flash** | Unsuppressed shots cause BRIGHT (fully exposed) for 1.5 seconds. Suppressed shots impose at least DIM for the same duration | `Exposure / MuzzleFlashDuration` = 0, `SuppressedShotDim` = false |
| **Visible lasers** | An active red/green laser imposes at least DIM. IR lasers/IR lights have no effect | `Exposure / VisibleLaserExposure` = false |
| **Movement** | Running adds 10% exposure; standing completely still subtracts 5% | `Exposure / MovementWeight` = 0 |
| **Light scan interval** | The full map light scan interval is now configurable (default 10 seconds, same as the original). Diagnostic logging reports scan time as `LIGHT_REFRESH lights=… ms=…` | The default matches the original |

SAIN detection is logged once in BepInEx at the first detection check of a raid:
`[VeilSight] SAIN detected=True delayMultiplier=0.60`

---

## Changes in the 4.1 port — no game code changes

To be clear, **the game-facing code needed no changes.**

This mod hooks **exactly one** EFT method:

```csharp
AccessTools.Method(typeof(EnemyInfo), nameof(EnemyInfo.SetVisible), new[] { typeof(bool) });
```

Checked against the actual SPT 4.1.5 `Assembly-CSharp.dll`:

| Used API | 4.1.5 status |
|---|---|
| `EnemyInfo.SetVisible(bool value)` | Still present — **even the parameter name remains `value`** |
| `EnemyInfo.Distance` / `.Person` / `.Owner` | Unchanged |
| `TacticalComboVisualController.LightMod` | Unchanged (`EFT.InventoryLogic.LightComponent`) |
| `PlayerBones.WeaponRoot` | Unchanged (`EFT.BifacialTransform`) |
| `LaserBeam`, `Player.FirearmController` | Unchanged |
| `EFT.EnvironmentEffect`, `EFT.Weather` | Unchanged |

The parameter name was checked because **Harmony binds patch arguments by name**.
If `SetVisible`'s parameter had changed from `value` to another name in 4.1, it would compile
but the patch would silently fail in-game. The actual 4.1.5 assembly was decompiled to confirm
that it is still `value`.

This mod has **no** reflection calls that find types/members using strings. The `AccessTools.Method`
line above is the only one, and it uses `nameof` + explicit types, so it is checked at compile time.

The actual diff therefore concerns version declarations:

| File | 4.0 | 4.1 |
|---|---|---|
| `Plugin.cs` | `[BepInDependency("com.SPT.core", "4.0.13")]` | `"4.1.0"` |
| `VeilSight.csproj` | `VersionPrefix 1.0.1` | `1.1.0` |
| `tools/Package-Release.ps1` | `$SptVersion = "4.0.13"` | `"4.1.5"` |

A build safeguard (`EnsureRealSptReflection`, below) and a default `SptRoot` were also added.

---

## Installation — you must build it yourself

The SPT launcher reads the **`spt-reflection` assembly version** referenced by a plugin to determine
which SPT version it was built for, and blocks the game from launching if it does not match:

> These mods were built for a different version of SPT than the one you are running (4.1.5):
> VeilSight.dll (built for SPT 1.0.0)

This means **you cannot simply use a DLL built by someone else**; you must build it against your own SPT installation.
Choose whichever of these three methods you prefer:

1. **Double-click `build.bat`** — the simplest option.
2. **Open `VeilSight.sln` in Visual Studio** → set the top configuration to `Release` → `빌드 > 솔루션 빌드` (Build > Build Solution).
   (Opening and building the csproj directly may skip NuGet restore and cause `NETSDK1004` — open the sln.
   If it still occurs, right-click the solution in `솔루션 탐색기` (Solution Explorer) → `NuGet 패키지 복원` (Restore NuGet Packages), then rebuild.)
3. Command line: `dotnet build VeilSight.sln -c Release`

**Release builds automatically copy to `BepInEx\plugins\VeilSight\VeilSight.dll`**
and also create `release\VeilSight-버전.zip`. To skip copying, use `-p:DeployVeilSight=false`.

The default `SptRoot` is `E:\SPT 4.1`, so you can build as is. For a different path:

```
dotnet build VeilSight.sln -c Release -p:SptRoot="D:\내SPT경로"
```

The build output is `bin\Release\VeilSight.dll`. It belongs at:

```
BepInEx\plugins\VeilSight\VeilSight.dll
```

As described above, Release builds copy it there automatically.

**There is no server mod.** The client plugin is all you need.

Building against a stub `spt-reflection.dll` (version 1.0.0.0) produces a DLL that **compiles but is blocked
only by the launcher**. The `EnsureRealSptReflection` target prevents this — if it finds a stub,
the build fails immediately with an explanation.

---

## How it works

1. Measures brightness at the player's position **once every 0.35 seconds** (sunlight/time/weather,
   ambient indoor lighting, nearby light fixtures, stance, flashlight).
2. Classifies the result as **DARK / DIM / BRIGHT**.
3. When a bot tries to confirm that it sees you (`SetVisible(true)`), that decision is
   **briefly held** based on brightness and distance.

| Brightness | Forgiving (default) | Standard |
|---|---|---|
| DARK | Base 2.20 seconds + distance × 0.022, up to 4.00 seconds | 1.76 seconds + distance × 0.0176, up to 3.20 seconds |
| DIM | Base 1.50 seconds + distance × 0.018, up to 3.00 seconds | 1.20 seconds + distance × 0.0144, up to 2.40 seconds |
| BRIGHT | No delay | No delay |

**Both presets always skip the delay within 6m.** Being unseen right in front of a bot would not make sense.

A hold is only a hold: **once time runs out, the bot will see you**. It does not grant permanent stealth.

**Standard** uses the same curve with 20% shorter delays. The original author recommended it for SAIN users.
Since 1.2.0, `SainDelayMultiplier` (default 0.6) is **automatically applied on top** when SAIN is installed,
so SAIN users are also advised to keep `Forgiving` and adjust only the multiplier (stacking Standard gives 0.8 × 0.6 = 0.48×).
VeilSight only checks whether SAIN is installed; it does not read or change SAIN settings.

### Flashlights / lasers

**Turning on a flashlight immediately counts as fully exposed** (disable with `FlashlightOverride`).
Since 1.2.0, **a visible laser alone also imposes at least DIM** (`VisibleLaserExposure`). IR equipment is still ignored.
Since 1.2.2, flashlights/lasers/IR are distinguished using attachment mode part names (`light_0`/`vis_0`/`il_0`/`ir_0`);
only modded attachments without these conventions fall back to checking light objects as before.

### Exposure meter

A small orange bar appears on-screen. DARK is on the left, DIM in the middle, and BRIGHT on the right.
The bar gets brighter with greater exposure. **Its smooth movement is only for readability;
actual detection always uses the latest sample** — detection has already changed even if the meter is still catching up.

Position, size, and smoothing are adjustable, and the meter can be hidden entirely.

---

## F12 settings (BepInEx Configuration Manager)

During a raid, open **F12 → `VeilSight`**. You can also edit the config file directly:

`BepInEx\config\com.sherrifpowpow.veilsight.cfg`

| Setting | Default | Description |
|---|---|---|
| `General / Enabled` | `true` | Toggle the entire mod |
| `General / SampleInterval` | `0.35` | Brightness sampling interval (seconds). 0.25~0.50 |
| `General / CloseRangeBypass` | `6` | No delay at or below this distance (m) |
| `Balance / DifficultyPreset` | `Forgiving` | `Forgiving` / `Standard` |
| `Exposure / FlashlightOverride` | `true` | An active flashlight counts as full exposure |
| `Exposure / PoseWeight` | `0.10` | Maximum exposure reduction from a low stance |
| `Meter / ShowMeter` | `true` | Show the meter |
| `Meter / SmoothTime` | `0.09` | Meter smoothing (seconds). Visual only |
| `Meter / Scale` | `1.0` | Meter size multiplier. 0.5~2.0 |
| `Meter / PositionX` | `0.50` | Horizontal position. 0=left, 1=right |
| `Meter / PositionY` | `0.88` | Vertical position. 0=top, 1=bottom |
| `Awareness / ReacquireMemory` | `10` | Time during which a bot that has seen you can reacquire you without delay (seconds). 0~30, 0=original behavior |
| `Awareness / CombatBypass` | `8` | No-delay period after either side hits the other (seconds). 0~30, 0=off |
| `Awareness / NightVisionBypass` | `true` | No delay for bots with active NVGs |
| `Compatibility / SainDelayMultiplier` | `0.6` | Delay multiplier applied only when SAIN is installed. 0~1, 1=original behavior |
| `Exposure / MuzzleFlashDuration` | `1.5` | Full exposure after an unsuppressed shot (seconds). 0~5, 0=off |
| `Exposure / SuppressedShotDim` | `true` | At least DIM for the same duration after a suppressed shot |
| `Exposure / VisibleLaserExposure` | `true` | An active visible laser imposes at least DIM |
| `Exposure / MovementWeight` | `0.10` | Running adds this value; standing still subtracts half. 0~0.5, 0=original behavior |
| `Performance / LightRefreshInterval` | `10` | Full map light scan interval (seconds). 5~60 |
| `Diagnostics / DiagnosticsEnabled` | `false` | Logs the entire decision process. Leave off for normal play |

---

## Safeguards (fail-open)

If lighting data cannot be read, a sample is too old, an exception occurs, or the mod is disabled,
**everything falls back to no delay**. A bug will not accidentally make you invincible.

Specifically, a sample older than `SampleInterval × 3` (minimum 1 second) is discarded,
and normal visibility is allowed.

---

## Compatibility · known limitations

- May conflict with other mods that modify `EnemyInfo.SetVisible`.
  In particular, **That's Lit** and related mods — do not use them together.
- The original author tested it alongside SAIN, Amands's Graphics, Better Night Skies, Dynamic Maps, Game Panel HUD,
  and Fontaine's FOV Fix. If HUD elements overlap, adjust the meter's position and size.
- Some angled ceiling light shafts in **Factory** are purely visual effects without actual light objects,
  so walking through them may not produce an immediate response on the meter. Ambient brightness itself
  is detected correctly. Purely decorative emissive surfaces have the same limitation. (This comes from map design.)
- **Labs, Factory day/night, and Labyrinth** have special handling due to their unusual lighting structures.
- Multiplayer (Fika) synchronization is outside the scope.
- **Known limitation (present in the original):** while detection is delayed, the game's visibility checks
  (`EnemyInfo.CheckLookEnemy`, the function bots use to inspect enemies) continue running, so a bot's
  "last seen position" and internal squad reports may still update. This follows from delaying only confirmed detection.

---

## Scope of changes

Changes only **the step where an enemy confirms it has seen you** (the 1.2.0 shot patch only records shot times). Hearing, searching, aiming, shooting, memory,
and all other bot behavior are untouched. BRIGHT receives no intervention; DIM/DARK each use
a curve with its own cap.

---

## License

The original **VeilSight** was created by **Sherrifpowpow** and released under the **MIT License**.
This fork is also MIT. The full text is in `LICENSE`.

Original author's note: inspired by That's Lit, but implemented independently **without taking a single line of its code**.
