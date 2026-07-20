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
        private static readonly List<EnemyInfo> CleanupBuffer = new List<EnemyInfo>();
        private static float _nextCleanup;
        private static float _nextBrightLog;
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

                var snapshot = ExposureSampler.Current;
                float snapshotAge = Time.realtimeSinceStartup - snapshot.SampleTime;
                float maximumSnapshotAge = Mathf.Max(1f, Mathf.Clamp(ModConfig.SampleInterval.Value, 0.25f, 0.5f) * 3f);
                if (!snapshot.Valid || snapshot.Band == ExposureBand.Unknown || snapshotAge < 0f || snapshotAge > maximumSnapshotAge)
                {
                    PendingByPair.Remove(key);
                    return true;
                }
                if (Time.realtimeSinceStartup >= _nextCleanup)
                    Cleanup();
                if (__instance.Distance <= Mathf.Max(0f, ModConfig.CloseRangeBypass.Value))
                {
                    PendingByPair.Remove(key);
                    return true;
                }
                if (snapshot.Band == ExposureBand.Bright)
                {
                    if (ModConfig.DiagnosticsEnabled.Value && Time.realtimeSinceStartup >= _nextBrightLog)
                    {
                        _nextBrightLog = Time.realtimeSinceStartup + 1f;
                        Plugin.Log.LogInfo($"[VeilSight] VISIBILITY bright-bypass distance={__instance.Distance:0.0} flashlight={snapshot.Flashlight}");
                    }
                    PendingByPair.Remove(key);
                    return true;
                }

                bool dark = snapshot.Band == ExposureBand.Dark;
                ModConfig.GetDelayParameters(dark, out float baseDelay, out float distanceScaling,
                    out float maximumDelay);
                float required = baseDelay;
                required += Mathf.Max(0f, __instance.Distance) *
                    distanceScaling;
                required = Mathf.Min(required, maximumDelay);
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
                    pending.Released = true;
                    LogDecision(key, snapshot, __instance.Distance, required, Time.realtimeSinceStartup - pending.Started, true);
                    return true;
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
        }

        internal static void ResetState()
        {
            PendingByPair.Clear();
            _nextCleanup = 0f;
            _nextBrightLog = 0f;
            _world = null;
        }

        private static void EnsureWorld()
        {
            var world = Singleton<GameWorld>.Instance;
            if (ReferenceEquals(_world, world))
                return;
            PendingByPair.Clear();
            _nextCleanup = 0f;
            _world = world;
        }

        private static void LogDecision(EnemyInfo key, ExposureSnapshot snapshot, float distance, float required, float elapsed, bool allowed)
        {
            if (!ModConfig.DiagnosticsEnabled.Value || Time.realtimeSinceStartup < PendingByPair[key].LastLog + 1f)
                return;
            PendingByPair[key].LastLog = Time.realtimeSinceStartup;
            Plugin.Log.LogInfo($"[VeilSight] VISIBILITY band={snapshot.Band} distance={distance:0.0} required={required:0.00} elapsed={elapsed:0.00} allowed={allowed}");
        }
    }
}
