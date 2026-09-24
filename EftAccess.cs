using System;
using System.Collections.Generic;
using Comfort.Common;
using EFT;
using EFT.InventoryLogic;
using HarmonyLib;
using UnityEngine;

namespace VeilSight
{
    internal static class EftAccess
    {
        private static readonly List<Light> WeaponLights = new List<Light>();
        private static readonly List<Transform> VisibleLaserEmitters = new List<Transform>();
        private static readonly AccessTools.FieldRef<TacticalComboVisualController, List<Transform>> ModesField =
            AccessTools.FieldRefAccess<TacticalComboVisualController, List<Transform>>("_ligthbeamsTransforms");
        private static Player.FirearmController _cachedFirearm;
        private static float _nextEmitterRefresh;

        internal static bool TryGetLocalPlayer(out Player player)
        {
            player = Singleton<GameWorld>.Instance?.MainPlayer;
            return player != null;
        }

        internal static bool TryGetHead(Player player, out Vector3 head)
        {
            head = default;
            if (player?.MainParts == null || !player.MainParts.TryGetValue(BodyPartType.head, out var part) || part == null)
                return false;
            head = part.Position;
            return true;
        }

        internal static bool IsWeaponLightActive(Player player)
        {
            if (!(player?.HandsController is Player.FirearmController firearm))
            {
                ResetEmitterCache();
                return false;
            }

            if (!ReferenceEquals(_cachedFirearm, firearm) || Time.realtimeSinceStartup >= _nextEmitterRefresh)
                RefreshEmitterCache(player, firearm);

            foreach (var light in WeaponLights)
                if (light != null && light.enabled && light.gameObject.activeInHierarchy &&
                    light.intensity > 0f && light.range > 0f)
                    return true;
            return false;
        }

        /// <summary>
        /// True when a visible (non-IR) laser mode is switched on. Child names follow the game's
        /// device prefab convention: vis_0* is a visible laser, ir_0* an IR laser.
        /// </summary>
        internal static bool IsVisibleLaserActive(Player player)
        {
            if (!(player?.HandsController is Player.FirearmController firearm))
            {
                ResetEmitterCache();
                return false;
            }

            if (!ReferenceEquals(_cachedFirearm, firearm) || Time.realtimeSinceStartup >= _nextEmitterRefresh)
                RefreshEmitterCache(player, firearm);

            foreach (var emitter in VisibleLaserEmitters)
                if (emitter != null && emitter.gameObject.activeInHierarchy)
                    return true;
            return false;
        }

        internal static void ResetEmitterCache()
        {
            _cachedFirearm = null;
            _nextEmitterRefresh = 0f;
            WeaponLights.Clear();
            VisibleLaserEmitters.Clear();
        }

        private static void RefreshEmitterCache(Player player, Player.FirearmController firearm)
        {
            _cachedFirearm = firearm;
            _nextEmitterRefresh = Time.realtimeSinceStartup + 1f;
            WeaponLights.Clear();
            VisibleLaserEmitters.Clear();

            var root = player?.PlayerBones?.WeaponRoot.Original;
            if (root == null)
                return;

            foreach (var controller in root.GetComponentsInChildren<TacticalComboVisualController>(true))
            {
                if (controller == null)
                    continue;
                CollectVisibleLasers(controller);
                if (controller.LightMod == null)
                    continue;
                foreach (var light in controller.GetComponentsInChildren<Light>(true))
                    if (light != null && light.gameObject.name != "laserBeamLight" &&
                        light.GetComponentInParent<LaserBeam>(true) == null &&
                        !WeaponLights.Contains(light))
                        WeaponLights.Add(light);
            }
        }

        private static void CollectVisibleLasers(TacticalComboVisualController controller)
        {
            List<Transform> modes;
            try
            {
                modes = ModesField(controller);
            }
            catch (Exception)
            {
                return;
            }
            if (modes == null)
                return;
            foreach (var mode in modes)
            {
                if (mode == null)
                    continue;
                for (int i = 0; i < mode.childCount; i++)
                {
                    var child = mode.GetChild(i);
                    if (child.name.StartsWith("vis_0", StringComparison.OrdinalIgnoreCase) &&
                        !VisibleLaserEmitters.Contains(child))
                        VisibleLaserEmitters.Add(child);
                }
            }
        }
    }
}
