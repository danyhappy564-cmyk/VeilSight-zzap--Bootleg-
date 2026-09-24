using System;
using System.Collections.Generic;
using Comfort.Common;
using SPT.Reflection.Patching;
using EFT;
using HarmonyLib;
using UnityEngine;

namespace VeilSight
{
    internal sealed class VisibilityGate : ModulePatch
    {
        private sealed class Pending
        {
            internal float Started;
            internal float LastTouched;
            internal float LastLog;
            internal ExposureBand Band;
            internal bool Released;
        }

        private static readonly Dictionary<EnemyInfo, Pending> PendingByPair = new Dictionary<EnemyInfo, Pending>();
        // Last time each bot actually saw the local player (visibility went through), for re-acquire memory.
        private static readonly Dictionary<EnemyInfo, float> LastConfirmedByPair = new Dictionary<EnemyInfo, float>();
        private static readonly List<EnemyInfo> CleanupBuffer = new List<EnemyInfo>();
        private static float _nextCleanup;
        private static float _nextBrightLog;
        private static float _nextBypassLog;
        private static GameWorld _world;

        protected override System.Reflection.MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(EnemyInfo), nameof(EnemyInfo.SetVisible), new[] { typeof(bool) });
        }

        [PatchPrefix]
        private static bool Prefix(EnemyInfo __instance, bool value)
        {
            try
            {
                EnsureWorld();
                if (!value)
                {
                    if (__instance != null)
                        PendingByPair.Remove(__instance);
                    return true;
                }

                if (!EftAccess.TryGetLocalPlayer(out var local) ||
                    __instance?.Owner == null || __instance.Person == null || !ReferenceEquals(__instance.Person, local))
                    return true;
                var key = __instance;

                if (!ModConfig.Enabled.Value)
                {
                    PendingByPair.Remove(key);
                    return true;
                }

                if (Time.realtimeSinceStartup >= _nextCleanup)
                    Cleanup();

                string bypass = FindAwarenessBypass(__instance);
                if (bypass != null)
                {
                    PendingByPair.Remove(key);
                    LogBypass(bypass, __instance);
                    return Allow(key);
                }

                var snapshot = ExposureSampler.Current;
                float snapshotAge = Time.realtimeSinceStartup - snapshot.SampleTime;
                float maximumSnapshotAge = Mathf.Max(1f, Mathf.Clamp(ModConfig.SampleInterval.Value, 0.25f, 0.5f) * 3f);
                if (!snapshot.Valid || snapshot.Band == ExposureBand.Unknown || snapshotAge < 0f || snapshotAge > maximumSnapshotAge)
                {
                    PendingByPair.Remove(key);
                    return Allow(key);
                }
                if (__instance.Distance <= Mathf.Max(0f, ModConfig.CloseRangeBypass.Value))
                {
                    PendingByPair.Remove(key);
                    return Allow(key);
                }
                // A recent shot is applied here as well as in the sampler so the flash counts immediately.
                snapshot.Band = ExposureSampler.Brighter(snapshot.Band, ShotTracker.MinimumBand());
                if (snapshot.Band == ExposureBand.Bright)
                {
                    if (ModConfig.DiagnosticsEnabled.Value && Time.realtimeSinceStartup >= _nextBrightLog)
                    {
                        _nextBrightLog = Time.realtimeSinceStartup + 1f;
                        Plugin.Log.LogInfo($"[VeilSight] VISIBILITY bright-bypass distance={__instance.Distance:0.0} flashlight={snapshot.Flashlight}");
                    }
                    PendingByPair.Remove(key);
                    return Allow(key);
                }

                bool dark = snapshot.Band == ExposureBand.Dark;
                ModConfig.GetDelayParameters(dark, out float baseDelay, out float distanceScaling,
                    out float maximumDelay);
                float required = baseDelay;
                required += Mathf.Max(0f, __instance.Distance) *
                    distanceScaling;
                required = Mathf.Min(required, maximumDelay);
                if (SainCompat.Loaded)
                    required *= Mathf.Clamp01(ModConfig.SainDelayMultiplier.Value);
                if (!PendingByPair.TryGetValue(key, out var pending))
                {
                    pending = new Pending
                    {
                        Started = Time.realtimeSinceStartup,
                        LastTouched = Time.realtimeSinceStartup,
                        Band = snapshot.Band
                    };
                    PendingByPair[key] = pending;
                }
                else if (pending.Band != snapshot.Band)
                {
                    pending.Band = snapshot.Band;
                }
                pending.LastTouched = Time.realtimeSinceStartup;

                if (pending.Released || Time.realtimeSinceStartup - pending.Started >= required)
                {
                    if (!pending.Released && ModConfig.DiagnosticsEnabled.Value)
                        Plugin.Log.LogInfo($"[VeilSight] VISIBILITY released bot={BotName(key)} band={snapshot.Band} distance={__instance.Distance:0.0} required={required:0.00}");
                    pending.Released = true;
                    LogDecision(key, snapshot, __instance.Distance, required, Time.realtimeSinceStartup - pending.Started, true);
                    return Allow(key);
                }

                LogDecision(key, snapshot, __instance.Distance, required, Time.realtimeSinceStartup - pending.Started, false);
                return false;
            }
            catch (Exception ex)
            {
                Plugin.Log.LogWarning("[VeilSight] visibility failed open: " + ex.GetType().Name);
                return true;
            }
        }

        private static bool Allow(EnemyInfo key)
        {
            LastConfirmedByPair[key] = Time.realtimeSinceStartup;
            return true;
        }

        /// <summary>
        /// Reasons a bot should see the local player without any darkness delay, or null if none apply.
        /// </summary>
        private static string FindAwarenessBypass(EnemyInfo info)
        {
            float memory = ModConfig.ReacquireMemory.Value;
            if (memory > 0f && LastConfirmedByPair.TryGetValue(info, out float confirmed) &&
                Time.realtimeSinceStartup - confirmed <= memory)
                return "memory";

            if (ModConfig.NightVisionBypass.Value && info.Owner.NightVision != null && info.Owner.NightVision.UsingNow)
                return "night-vision";

            float combat = ModConfig.CombatBypass.Value;
            if (combat > 0f &&
                (Time.time - info.LastGetHitTime <= combat || Time.time - info.LastDoHitTime <= combat))
                return "combat";

            return null;
        }

        private static void Cleanup()
        {
            _nextCleanup = Time.realtimeSinceStartup + 5f;
            float expiry = Mathf.Max(30f, ModConfig.LargestMaximumDelay + 10f);
            CleanupBuffer.Clear();
            foreach (var pair in PendingByPair)
                if (Time.realtimeSinceStartup - pair.Value.LastTouched > expiry)
                    CleanupBuffer.Add(pair.Key);
            foreach (var key in CleanupBuffer)
                PendingByPair.Remove(key);
            CleanupBuffer.Clear();

            float memoryExpiry = Mathf.Max(ModConfig.ReacquireMemory.Value, 0f) + 10f;
            foreach (var pair in LastConfirmedByPair)
                if (Time.realtimeSinceStartup - pair.Value > memoryExpiry)
                    CleanupBuffer.Add(pair.Key);
            foreach (var key in CleanupBuffer)
                LastConfirmedByPair.Remove(key);
            CleanupBuffer.Clear();
        }

        private static void LogBypass(string reason, EnemyInfo info)
        {
            if (!ModConfig.DiagnosticsEnabled.Value || Time.realtimeSinceStartup < _nextBypassLog)
                return;
            _nextBypassLog = Time.realtimeSinceStartup + 1f;
            Plugin.Log.LogInfo($"[VeilSight] VISIBILITY bypass={reason} bot={BotName(info)} distance={info.Distance:0.0}");
        }

        private static string BotName(EnemyInfo info)
        {
            return info.Owner != null ? info.Owner.name : "?";
        }

        internal static void ResetState()
        {
            PendingByPair.Clear();
            LastConfirmedByPair.Clear();
            ShotTracker.Reset();
            _nextCleanup = 0f;
            _nextBrightLog = 0f;
            _nextBypassLog = 0f;
            _world = null;
        }

        private static void EnsureWorld()
        {
            var world = Singleton<GameWorld>.Instance;
            if (ReferenceEquals(_world, world))
                return;
            PendingByPair.Clear();
            LastConfirmedByPair.Clear();
            _nextCleanup = 0f;
            _world = world;
        }

        private static void LogDecision(EnemyInfo key, ExposureSnapshot snapshot, float distance, float required, float elapsed, bool allowed)
        {
            if (!ModConfig.DiagnosticsEnabled.Value || Time.realtimeSinceStartup < PendingByPair[key].LastLog + 1f)
                return;
            PendingByPair[key].LastLog = Time.realtimeSinceStartup;
            Plugin.Log.LogInfo($"[VeilSight] VISIBILITY bot={BotName(key)} band={snapshot.Band} distance={distance:0.0} required={required:0.00} elapsed={elapsed:0.00} allowed={allowed}");
        }
    }
}
