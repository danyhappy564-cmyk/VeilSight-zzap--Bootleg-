using BepInEx;
using BepInEx.Logging;
using UnityEngine;

namespace VeilSight
{
    [BepInPlugin("com.sherrifpowpow.veilsight", "VeilSight", BuildVersion.Value)]
    [BepInDependency("com.SPT.core", "4.1.0")]
    [BepInDependency(SainCompat.SainGuid, BepInDependency.DependencyFlags.SoftDependency)]
    [BepInProcess("EscapeFromTarkov.exe")]
    public sealed class Plugin : BaseUnityPlugin
    {
        internal static ManualLogSource Log { get; private set; }

        private void Awake()
        {
            Log = Logger;
            ModConfig.Bind(Config);
            gameObject.AddComponent<ExposureSampler>();
            gameObject.AddComponent<ExposureMeter>();
            new VisibilityGate().Enable();
            new ShotPatch().Enable();
            Log.LogInfo($"[VeilSight] v{BuildVersion.Value} loaded.");
        }
    }
}
