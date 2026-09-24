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

        [Flags]
        internal enum DeviceMode
        {
            None = 0,
            WhiteLight = 1,
            VisibleLaser = 2,
            IRLight = 4,
            IRLaser = 8
        }

        private struct NamedMode
        {
            internal Transform Mode;
            internal DeviceMode Flags;
        }

        private static readonly List<NamedMode> NamedModes = new List<NamedMode>();

        /// <summary>
        /// Modes that are switched on right now. Devices whose prefabs follow the game's naming
        /// convention (light_0 white light, vis_0 visible laser, il_0 IR light, ir_0 IR laser) are
        /// classified by name, so IR and laser-only modes are not mistaken for a flashlight. Devices
        /// without those names fall back to the original Light-component scan, counted as white light.
        /// </summary>
        internal static DeviceMode GetActiveDevices(Player player)
        {
            if (!(player?.HandsController is Player.FirearmController firearm))
            {
                ResetEmitterCache();
                return DeviceMode.None;
            }

            if (!ReferenceEquals(_cachedFirearm, firearm) || Time.realtimeSinceStartup >= _nextEmitterRefresh)
                RefreshEmitterCache(player, firearm);

            var active = DeviceMode.None;
            foreach (var named in NamedModes)
                if (named.Mode != null && named.Mode.gameObject.activeInHierarchy)
                    active |= named.Flags;

            foreach (var light in WeaponLights)
                if (light != null && light.enabled && light.gameObject.activeInHierarchy &&
                    light.intensity > 0f && light.range > 0f)
                {
                    active |= DeviceMode.WhiteLight;
                    break;
                }
            return active;
        }

        internal static void ResetEmitterCache()
        {
            _cachedFirearm = null;
            _nextEmitterRefresh = 0f;
            WeaponLights.Clear();
            NamedModes.Clear();
        }

        private static void RefreshEmitterCache(Player player, Player.FirearmController firearm)
        {
            _cachedFirearm = firearm;
            _nextEmitterRefresh = Time.realtimeSinceStartup + 1f;
            WeaponLights.Clear();
            NamedModes.Clear();

            var root = player?.PlayerBones?.WeaponRoot.Original;
            if (root == null)
                return;

            foreach (var controller in root.GetComponentsInChildren<TacticalComboVisualController>(true))
            {
                if (controller == null)
                    continue;
                if (CollectNamedModes(controller))
                    continue;
                if (controller.LightMod == null)
                    continue;
                foreach (var light in controller.GetComponentsInChildren<Light>(true))
                    if (light != null && light.gameObject.name != "laserBeamLight" &&
                        light.GetComponentInParent<LaserBeam>(true) == null &&
                        !WeaponLights.Contains(light))
                        WeaponLights.Add(light);
            }
        }

        /// <summary>
        /// Records this device's modes by child name. Returns false when none of its modes use the
        /// naming convention, so the caller can fall back to scanning Light components.
        /// </summary>
        private static bool CollectNamedModes(TacticalComboVisualController controller)
        {
            List<Transform> modes;
            try
            {
                modes = ModesField(controller);
            }
            catch (Exception)
            {
                return false;
            }
            if (modes == null)
                return false;

            bool anyNamed = false;
            foreach (var mode in modes)
            {
                if (mode == null)
                    continue;
                var flags = DeviceMode.None;
                for (int i = 0; i < mode.childCount; i++)
                    flags |= Classify(mode.GetChild(i).name);
                if (flags == DeviceMode.None)
                    continue;
                anyNamed = true;
                NamedModes.Add(new NamedMode { Mode = mode, Flags = flags });
            }
            return anyNamed;
        }

        private static DeviceMode Classify(string name)
        {
            if (name.StartsWith("light_0", StringComparison.OrdinalIgnoreCase))
                return DeviceMode.WhiteLight;
            if (name.StartsWith("vis_0", StringComparison.OrdinalIgnoreCase))
                return DeviceMode.VisibleLaser;
            if (name.StartsWith("il_0", StringComparison.OrdinalIgnoreCase))
                return DeviceMode.IRLight;
            if (name.StartsWith("ir_0", StringComparison.OrdinalIgnoreCase))
                return DeviceMode.IRLaser;
            return DeviceMode.None;
        }
    }
}
