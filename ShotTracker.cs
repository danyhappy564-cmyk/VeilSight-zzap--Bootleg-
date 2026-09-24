using System;
using System.Reflection;
using BepInEx.Bootstrap;
using EFT;
using HarmonyLib;
using SPT.Reflection.Patching;
using UnityEngine;

namespace VeilSight
{
    /// <summary>
    /// Remembers when the local player last fired, so a muzzle flash in the dark can expose them.
    /// </summary>
    internal static class ShotTracker
    {
        private static float _lastLoudShot = float.NegativeInfinity;
        private static float _lastSuppressedShot = float.NegativeInfinity;

        internal static void Record(bool silenced)
        {
            if (silenced)
                _lastSuppressedShot = Time.realtimeSinceStartup;
            else
                _lastLoudShot = Time.realtimeSinceStartup;
        }

        /// <summary>
        /// The brightest band a recent shot forces, or Unknown when no shot applies.
        /// </summary>
        internal static ExposureBand MinimumBand()
        {
            float duration = ModConfig.MuzzleFlashDuration.Value;
            if (duration <= 0f)
                return ExposureBand.Unknown;
            float now = Time.realtimeSinceStartup;
            if (now - _lastLoudShot <= duration)
                return ExposureBand.Bright;
            if (ModConfig.SuppressedShotDim.Value && now - _lastSuppressedShot <= duration)
                return ExposureBand.Dim;
            return ExposureBand.Unknown;
        }

        internal static void Reset()
        {
            _lastLoudShot = float.NegativeInfinity;
            _lastSuppressedShot = float.NegativeInfinity;
        }
    }

    internal sealed class ShotPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(Player), nameof(Player.OnMakingShot));
        }

        [PatchPostfix]
        private static void Postfix(Player __instance)
        {
            try
            {
                if (!EftAccess.TryGetLocalPlayer(out var local) || !ReferenceEquals(__instance, local))
                    return;
                bool silenced = __instance.HandsController is Player.FirearmController firearm && firearm.IsSilenced;
                ShotTracker.Record(silenced);
                if (ModConfig.DiagnosticsEnabled.Value)
                    Plugin.Log.LogInfo($"[VeilSight] SHOT silenced={silenced}");
            }
            catch (Exception ex)
            {
                Plugin.Log.LogWarning("[VeilSight] shot tracking failed: " + ex.GetType().Name);
            }
        }
    }

    internal static class SainCompat
    {
        internal const string SainGuid = "me.sol.sain";
        private static bool? _loaded;

        // Checked lazily from raid code so SAIN has finished loading by the time we ask.
        internal static bool Loaded
        {
            get
            {
                if (_loaded == null)
                {
                    _loaded = Chainloader.PluginInfos.ContainsKey(SainGuid);
                    Plugin.Log.LogInfo($"[VeilSight] SAIN detected={_loaded.Value} delayMultiplier={(_loaded.Value ? ModConfig.SainDelayMultiplier.Value : 1f):0.00}");
                }
                return _loaded.Value;
            }
        }
    }
}
