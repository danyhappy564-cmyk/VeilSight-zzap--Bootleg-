using BepInEx.Configuration;

namespace VeilSight
{
    internal enum DifficultyPreset
    {
        Forgiving,
        Standard
    }

    internal static class ModConfig
    {
        internal static ConfigEntry<bool> Enabled;
        internal static ConfigEntry<float> SampleInterval;
        internal static ConfigEntry<float> CloseRangeBypass;
        internal static ConfigEntry<DifficultyPreset> Difficulty;
        internal static ConfigEntry<bool> FlashlightOverride;
        internal static ConfigEntry<float> PoseWeight;
        internal static ConfigEntry<bool> ShowMeter;
        internal static ConfigEntry<float> MeterSmoothTime;
        internal static ConfigEntry<float> MeterScale;
        internal static ConfigEntry<float> MeterPositionX;
        internal static ConfigEntry<float> MeterPositionY;
        internal static ConfigEntry<bool> DiagnosticsEnabled;
        internal static ConfigEntry<float> ReacquireMemory;
        internal static ConfigEntry<float> CombatBypass;
        internal static ConfigEntry<bool> NightVisionBypass;
        internal static ConfigEntry<float> SainDelayMultiplier;
        internal static ConfigEntry<float> MuzzleFlashDuration;
        internal static ConfigEntry<bool> SuppressedShotDim;
        internal static ConfigEntry<bool> VisibleLaserExposure;
        internal static ConfigEntry<float> MovementWeight;
        internal static ConfigEntry<float> LightRefreshInterval;

        internal static void Bind(ConfigFile config)
        {
            Enabled = config.Bind("General", "Enabled", true,
                "Enables VeilSight exposure sampling and visibility-delay behavior.");
            SampleInterval = config.Bind("General", "SampleInterval", 0.35f,
                new ConfigDescription("Seconds between exposure samples.", new AcceptableValueRange<float>(0.25f, 0.50f)));
            CloseRangeBypass = config.Bind("General", "CloseRangeBypass", 6f,
                new ConfigDescription("Distance in metres at or below which visibility is never delayed.", new AcceptableValueRange<float>(0f, 100f)));
            Difficulty = config.Bind("Balance", "DifficultyPreset", DifficultyPreset.Forgiving,
                "Forgiving uses the validated full delay curve. Standard uses the same curve at 80% duration for faster enemies or harder AI presets.");
            FlashlightOverride = config.Bind("Exposure", "FlashlightOverride", true,
                "Treats an active visible weapon light as fully exposed. Laser-only modes are ignored.");
            PoseWeight = config.Bind("Exposure", "PoseWeight", 0.10f,
                new ConfigDescription("Maximum exposure reduction from a low stance.", new AcceptableValueRange<float>(0f, 1f)));
            ShowMeter = config.Bind("Meter", "ShowMeter", true,
                "Shows the exposure meter during a raid.");
            MeterSmoothTime = config.Bind("Meter", "SmoothTime", 0.09f,
                new ConfigDescription("Seconds used to smooth meter movement. This affects display only.", new AcceptableValueRange<float>(0.05f, 1.50f)));
            MeterScale = config.Bind("Meter", "Scale", 1f,
                new ConfigDescription("Exposure meter size multiplier.", new AcceptableValueRange<float>(0.50f, 2f)));
            MeterPositionX = config.Bind("Meter", "PositionX", 0.50f,
                new ConfigDescription("Horizontal meter position, from left (0) to right (1).", new AcceptableValueRange<float>(0f, 1f)));
            MeterPositionY = config.Bind("Meter", "PositionY", 0.88f,
                new ConfigDescription("Vertical meter position, from top (0) to bottom (1).", new AcceptableValueRange<float>(0f, 1f)));
            ReacquireMemory = config.Bind("Awareness", "ReacquireMemory", 10f,
                new ConfigDescription("Seconds a bot remembers you after it actually spotted you. Re-seeing you within this window skips the delay, so peeking in and out of darkness does not reset it. 0 restores the original behavior.", new AcceptableValueRange<float>(0f, 30f)));
            CombatBypass = config.Bind("Awareness", "CombatBypass", 8f,
                new ConfigDescription("Seconds after you hit a bot, or it hit you, during which that bot gets no delay. 0 disables.", new AcceptableValueRange<float>(0f, 30f)));
            NightVisionBypass = config.Bind("Awareness", "NightVisionBypass", true,
                "Bots with night vision switched on are not delayed by darkness.");
            SainDelayMultiplier = config.Bind("Compatibility", "SainDelayMultiplier", 0.6f,
                new ConfigDescription("Delay multiplier used only when SAIN is installed, because SAIN already slows spotting in the dark. 1 keeps the full VeilSight delay.", new AcceptableValueRange<float>(0f, 1f)));
            MuzzleFlashDuration = config.Bind("Exposure", "MuzzleFlashDuration", 1.5f,
                new ConfigDescription("Seconds you count as fully exposed after an unsuppressed shot. 0 disables.", new AcceptableValueRange<float>(0f, 5f)));
            SuppressedShotDim = config.Bind("Exposure", "SuppressedShotDim", true,
                "A suppressed shot in the dark counts as DIM for the muzzle flash duration instead of DARK.");
            VisibleLaserExposure = config.Bind("Exposure", "VisibleLaserExposure", true,
                "An active visible laser raises you to at least DIM. IR lasers and IR lights are ignored.");
            MovementWeight = config.Bind("Exposure", "MovementWeight", 0.10f,
                new ConfigDescription("Exposure change from movement: sprinting adds this much, standing still removes half of it. 0 restores the original behavior.", new AcceptableValueRange<float>(0f, 0.5f)));
            LightRefreshInterval = config.Bind("Performance", "LightRefreshInterval", 10f,
                new ConfigDescription("Seconds between full scans for scene lights. Raise it if you notice a small hitch at a fixed interval on light-heavy maps such as Streets; diagnostics log how long each scan takes.", new AcceptableValueRange<float>(5f, 60f)));
            DiagnosticsEnabled = config.Bind("Diagnostics", "DiagnosticsEnabled", false,
                "Writes detailed exposure and visibility decisions to the BepInEx log. Leave disabled for normal play.");
        }

        internal static void GetDelayParameters(bool dark, out float baseDelay, out float distanceScaling,
            out float maximumDelay)
        {
            bool forgiving = Difficulty.Value == DifficultyPreset.Forgiving;
            if (dark)
            {
                baseDelay = forgiving ? 2.20f : 1.76f;
                distanceScaling = forgiving ? 0.022f : 0.0176f;
                maximumDelay = forgiving ? 4.00f : 3.20f;
                return;
            }

            baseDelay = forgiving ? 1.50f : 1.20f;
            distanceScaling = forgiving ? 0.018f : 0.0144f;
            maximumDelay = forgiving ? 3.00f : 2.40f;
        }

        internal static float LargestMaximumDelay =>
            Difficulty.Value == DifficultyPreset.Forgiving ? 4.00f : 3.20f;
    }
}
