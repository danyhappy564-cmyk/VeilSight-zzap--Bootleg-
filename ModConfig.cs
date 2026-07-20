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
