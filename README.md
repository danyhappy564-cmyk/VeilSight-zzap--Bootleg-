# VeilSight

Darkness should matter.

VeilSight makes Tarkov's lighting affect how quickly enemies confirm you as a target. Stay in shadow and you gain a brief, distance-based detection advantage. Step into strong light or switch on a flashlight and that advantage disappears.

It is lightweight, predictable, and designed to improve stealth without replacing EFT's combat AI or making the player invisible.

## Installation

Extract the ZIP directly into your main SPT folder. The installed file should be:

`BepInEx/plugins/VeilSight/VeilSight.dll`

Built for SPT 4.0.13. No server-side mod is required.

## Features

- Live exposure based on daylight, darkness, indoor lighting, nearby fixtures, stance, and weapon lights
- A compact amber meter showing your current exposure
- Separate behavior for DIM and DARK conditions
- Distance-scaled delays with guaranteed eventual detection
- Flashlights expose you; lasers do not
- Two straightforward balance presets
- Configurable meter position, size, smoothing, and visibility
- Fail-open safeguards: missing lighting data never grants accidental protection

## Balance presets

**Forgiving** is the default and provides the full validated stealth effect. It is a good match for easier or slower AI presets.

**Standard** shortens the effect by 20% for default or faster AI behavior.

Both presets stop helping inside 6 metres. VeilSight does not inspect or modify SAIN settings; simply choose the preset that best matches your AI setup.

## Exposure meter

The meter reads from DARK on the left through DIM to BRIGHT on the right. Its motion is smoothed for readability, but gameplay always uses the latest exposure sample. The meter brightens as your exposure rises and can be moved, resized, or disabled in the configuration.

## Configuration

Press **F12** in game, open the mod configuration menu, and select **VeilSight**. Settings can also be edited directly in:

`BepInEx/config/com.sherrifpowpow.veilsight.cfg`

Detailed diagnostics are disabled by default and should only be enabled for troubleshooting or bug reports.

## Compatibility and known limitations

VeilSight was developed and tested in a modded SPT environment including SAIN, Amands's Graphics, Better Night Skies, Dynamic Maps, Game Panel HUD, and Fontaine's FOV Fix. HUD overlap can be resolved with the meter position and scale settings.

Mods that replace the same EFT visibility-confirmation method may conflict. Multiplayer synchronization is outside the scope of version 1.0.0.

Factory is broadly supported, but some angled ceiling-light shafts are visual effects without a matching physical light signal. VeilSight recognizes their surrounding illumination, but the meter may not react at the exact moment you cross the visible beam. Purely decorative emissive surfaces can have the same limitation when the map provides no corresponding light component.

Labs, Factory day/night, and Labyrinth receive dedicated handling for their unusual lighting setups.

## Technical scope

VeilSight changes only the enemy-to-local-player visibility-confirmation step. It does not replace hearing, searching, aiming, shooting, memory, or general bot behavior. BRIGHT conditions bypass the effect, DIM and DARK use separate capped curves, and stale or invalid exposure data always fails open.

## Inspiration

That’s Lit was an important inspiration for this project. I loved the original mod and have a lot of respect for what it brought to SPT.

VeilSight is a new, independent implementation built specifically for SPT 4.0.13. It is intended to stand on its own as a performance-conscious replacement for players who want the complete lighting-to-detection experience, rather than an add-on that must run beside the original. Its focused design uses low-frequency sampling, cached scene and weapon lights, one narrow visibility hook, no server component, and no hard dependency on an AI overhaul. That keeps the runtime work small and predictable while still handling the full exposure and delayed-detection loop.

No code from That’s Lit is included in VeilSight. Apparently, you folks really enjoy lurking in dark corners; Saaaame lol.

## License

VeilSight was created by **Sherrifpowpow** and is released under the MIT License. See the included license file for the full terms.
