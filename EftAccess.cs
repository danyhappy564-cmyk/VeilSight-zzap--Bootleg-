using System.Collections.Generic;
using Comfort.Common;
using EFT;
using EFT.InventoryLogic;
using UnityEngine;

namespace VeilSight
{
    internal static class EftAccess
    {
        private static readonly List<Light> WeaponLights = new List<Light>();
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

        internal static void ResetEmitterCache()
        {
            _cachedFirearm = null;
            _nextEmitterRefresh = 0f;
            WeaponLights.Clear();
        }

        private static void RefreshEmitterCache(Player player, Player.FirearmController firearm)
        {
            _cachedFirearm = firearm;
            _nextEmitterRefresh = Time.realtimeSinceStartup + 1f;
            WeaponLights.Clear();

            var root = player?.PlayerBones?.WeaponRoot.Original;
            if (root == null)
                return;

            foreach (var controller in root.GetComponentsInChildren<TacticalComboVisualController>(true))
            {
                if (controller == null || controller.LightMod == null)
                    continue;
                foreach (var light in controller.GetComponentsInChildren<Light>(true))
                    if (light != null && light.gameObject.name != "laserBeamLight" &&
                        light.GetComponentInParent<LaserBeam>(true) == null &&
                        !WeaponLights.Contains(light))
                        WeaponLights.Add(light);
            }
        }
    }
}
