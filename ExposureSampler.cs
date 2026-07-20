using System;
using System.Collections.Generic;
using Comfort.Common;
using EFT;
using EFT.EnvironmentEffect;
using EFT.Weather;
using UnityEngine;
using UnityEngine.Rendering;

namespace VeilSight
{
    internal enum ExposureBand { Unknown, Bright, Dim, Dark }

    internal struct ExposureSnapshot
    {
        internal bool Valid;
        internal ExposureBand Band;
        internal float Score;
        internal bool Flashlight;
        internal float SampleTime;
    }

    internal sealed class ExposureSampler : MonoBehaviour
    {
        private const float ShNightTop = 0.002f;
        private const float ShDayTop = 0.10f;
        private const float AmbientMaximum = 0.30f;
        private const float DimThreshold = 0.325f;
        private const float BrightThreshold = 0.45f;
        private const float ArtificialActivationThreshold = 0.012f;
        private const float ArtificialReleaseThreshold = 0.008f;
        private const float ArtificialExposureFloor = DimThreshold;
        private const float ArtificialExposureSlope = 1.5f;
        private const float WeatherlessInteriorLightBaseline = 0.006f;
        private const float WeatherlessInteriorLightHold = 6f;
        private const float FactoryDayOutdoorExposure = 0.80f;
        private const float OpenDaylightMinimumSunHeight = 0.20f;

        internal static ExposureSnapshot Current;
        private readonly List<Light> _lights = new List<Light>();
        private float _nextSample;
        private float _nextLightRefresh;
        private float _nextDiagnostic;
        private bool _initialized;
        private bool _artificialLightActive;
        private bool _disabled;
        private GameWorld _world;
        private float _lastWeatherlessMapLight = float.NegativeInfinity;
        private readonly Vector3[] _shDirections = { Vector3.up };
        private readonly Color[] _shColors = new Color[1];
        private readonly RaycastHit[] _occlusionHits = new RaycastHit[32];

        private void Update()
        {
            if (!ModConfig.Enabled.Value)
            {
                if (!_disabled)
                {
                    ResetSamplerState();
                    VisibilityGate.ResetState();
                    _disabled = true;
                }
                return;
            }
            _disabled = false;
            if (Time.realtimeSinceStartup < _nextSample)
                return;
            _nextSample = Time.realtimeSinceStartup + Mathf.Clamp(ModConfig.SampleInterval.Value, 0.25f, 0.5f);
            Sample();
        }

        private void Sample()
        {
            try
            {
                if (!EftAccess.TryGetLocalPlayer(out var player) || !EftAccess.TryGetHead(player, out var head))
                {
                    if (_world != null)
                        VisibilityGate.ResetState();
                    ResetSamplerState();
                    return;
                }

                var world = Singleton<GameWorld>.Instance;
                if (!ReferenceEquals(_world, world))
                {
                    ResetSamplerState();
                    VisibilityGate.ResetState();
                    _world = world;
                }

                if (!_initialized || Time.realtimeSinceStartup >= _nextLightRefresh)
                {
                    RefreshLights();
                    _initialized = true;
                    _nextLightRefresh = Time.realtimeSinceStartup + 10f;
                }

                float sunHeight = 0f;
                float cloudiness = 0f;
                float shDc = 0f;
                float shTop = 0f;
                bool shValid = false;
                bool timeOfDayAvailable = false;
                string locationId = world?.LocationId;
                bool labsLighting = string.Equals(locationId, "laboratory", StringComparison.OrdinalIgnoreCase);
                bool factoryDay = string.Equals(locationId, "factory4_day", StringComparison.OrdinalIgnoreCase);
                bool factoryNight = string.Equals(locationId, "factory4_night", StringComparison.OrdinalIgnoreCase);
                bool labyrinthLighting = string.Equals(locationId, "labyrinth", StringComparison.OrdinalIgnoreCase);
                bool simpleSkyAmbient = factoryDay || labyrinthLighting;
                bool weatherlessMap = labsLighting || factoryDay || factoryNight || labyrinthLighting;
                var weather = WeatherController.Instance;
                if (simpleSkyAmbient)
                {
                    var simpleSky = TODSkySimple.Instance;
                    if (simpleSky != null)
                    {
                        timeOfDayAvailable = true;
                        SphericalHarmonicsL2 sh = simpleSky.SH;
                        shDc = RawLuminance(sh[0, 0], sh[1, 0], sh[2, 0]);
                        sh.Evaluate(_shDirections, _shColors);
                        shTop = RawLuminance(_shColors[0].r, _shColors[0].g, _shColors[0].b);
                        shValid = !float.IsNaN(shTop) && !float.IsInfinity(shTop);
                    }
                }
                else if (weather != null)
                {
                    sunHeight = weather.SunHeight;
                    if (weather.WeatherCurve != null)
                        cloudiness = weather.WeatherCurve.Cloudiness;

                    var timeOfDay = weather.TimeOfDayController;
                    if (timeOfDay != null)
                    {
                        timeOfDayAvailable = true;
                        SphericalHarmonicsL2 sh = timeOfDay.SH;
                        shDc = RawLuminance(sh[0, 0], sh[1, 0], sh[2, 0]);
                        sh.Evaluate(_shDirections, _shColors);
                        shTop = RawLuminance(_shColors[0].r, _shColors[0].g, _shColors[0].b);
                        shValid = !float.IsNaN(shTop) && !float.IsInfinity(shTop);
                    }
                }
                else if (labsLighting || factoryNight)
                {
                    shValid = true;
                }

                if (!shValid)
                {
                    _artificialLightActive = false;
                    Current = default;
                    if (ModConfig.DiagnosticsEnabled.Value && Time.realtimeSinceStartup >= _nextDiagnostic)
                    {
                        _nextDiagnostic = Time.realtimeSinceStartup + 2f;
                        Plugin.Log.LogInfo($"[VeilSight] EXPOSURE_INVALID reason=ambient-source weather={weather != null} timeOfDay={timeOfDayAvailable} shTop={shTop:0.000000}");
                    }
                    return;
                }

                var sceneSky = TOD_Sky.Instance;
                var sky = weatherlessMap ? null : sceneSky;
                var skyDirection = sky != null ? sky.LocalLightDirection : Vector3.up;
                if (skyDirection.sqrMagnitude < 0.01f)
                    skyDirection = Vector3.up;
                skyDirection.Normalize();

                bool blocked = IsOccluded(head + Vector3.up * 0.1f, skyDirection, 2000f, player.Transform.Original);
                float ambient = Mathf.InverseLerp(ShNightTop, ShDayTop, shTop) * AmbientMaximum;
                float direct = !blocked && sky != null
                    ? Luminance(sky.LightColor) * Mathf.Clamp01(sky.LightIntensity)
                    : 0f;
                if (sky != null && !sky.IsDay)
                    direct *= 0.25f;
                float daylight = Mathf.Clamp01(ambient + direct);
                var environmentManager = EnvironmentManager.Instance;
                if (environmentManager != null && environmentManager.InBunker)
                    daylight *= 0.25f;
                else if (sky != null && sky.IsDay && sunHeight >= OpenDaylightMinimumSunHeight && !blocked &&
                         environmentManager != null && environmentManager.Environment == EnvironmentType.Outdoor)
                    daylight = Mathf.Max(daylight, BrightThreshold);
                if (factoryDay)
                {
                    if (environmentManager != null && environmentManager.Environment == EnvironmentType.Outdoor)
                        daylight = Mathf.Max(daylight, FactoryDayOutdoorExposure);
                }

                float localLight = FindStrongestLocalLight(head, player.Transform.Original);
                if (labsLighting)
                    localLight = Mathf.Clamp01(localLight + WeatherlessInteriorLightBaseline);
                bool flashlight = ModConfig.FlashlightOverride.Value && EftAccess.IsWeaponLightActive(player);
                float poseWeight = Mathf.Clamp01(ModConfig.PoseWeight.Value);
                float pose = player.IsInPronePose ? 1f - poseWeight :
                    Mathf.Lerp(1f - poseWeight * 0.5f, 1f, Mathf.Clamp01(player.PoseLevel));
                float baseExposure = Mathf.Clamp01(daylight + localLight);
                if (_artificialLightActive)
                    _artificialLightActive = localLight >= ArtificialReleaseThreshold;
                else
                    _artificialLightActive = localLight >= ArtificialActivationThreshold;
                float artificialExposure = _artificialLightActive
                    ? Mathf.Clamp01(ArtificialExposureFloor + localLight * ArtificialExposureSlope * pose)
                    : 0f;
                float score = Mathf.Clamp01(Mathf.Max(baseExposure * pose, artificialExposure));
                bool weatherlessHold = false;
                if (labsLighting)
                {
                    if (_artificialLightActive)
                        _lastWeatherlessMapLight = Time.realtimeSinceStartup;
                    else if (score < DimThreshold && Time.realtimeSinceStartup - _lastWeatherlessMapLight <= WeatherlessInteriorLightHold)
                    {
                        score = DimThreshold;
                        weatherlessHold = true;
                    }
                }
                if (flashlight)
                    score = 1f;

                var band = score >= BrightThreshold ? ExposureBand.Bright :
                    score >= DimThreshold ? ExposureBand.Dim : ExposureBand.Dark;
                Current = new ExposureSnapshot
                {
                    Valid = true,
                    Band = band,
                    Score = score,
                    Flashlight = flashlight,
                    SampleTime = Time.realtimeSinceStartup
                };

                if (ModConfig.DiagnosticsEnabled.Value && Time.realtimeSinceStartup >= _nextDiagnostic)
                {
                    _nextDiagnostic = Time.realtimeSinceStartup + 2f;
                    float raidHour = -1f;
                    var gameWorld = Singleton<GameWorld>.Instance;
                    if (gameWorld?.GameDateTime != null)
                    {
                        var raidTime = gameWorld.GameDateTime.Calculate();
                        raidHour = raidTime.Hour + raidTime.Minute / 60f + raidTime.Second / 3600f;
                    }

                    string environment = environmentManager != null ? environmentManager.Environment.ToString() : "none";

                    Plugin.Log.LogInfo($"[VeilSight] EXPOSURE_RAW location={locationId} score={score:0.000} band={band} daylight={daylight:0.000} ambient={ambient:0.000} direct={direct:0.000} blocked={blocked} local={localLight:0.000} artificial={artificialExposure:0.000} artificialActive={_artificialLightActive} weatherlessHold={weatherlessHold} flashlight={flashlight} pose={pose:0.000} hour={raidHour:0.000} sunY={sunHeight:0.000} cloud={cloudiness:0.000} shValid={shValid} shDC={shDc:0.000000} shTop={shTop:0.000000} environment={environment} sceneSky={sceneSky != null}");
                }
            }
            catch (Exception ex)
            {
                _artificialLightActive = false;
                Current = default;
                Plugin.Log.LogWarning("[VeilSight] exposure failed open: " + ex.GetType().Name);
            }
        }

        private void OnDisable()
        {
            ResetSamplerState();
            VisibilityGate.ResetState();
        }

        private void OnDestroy()
        {
            ResetSamplerState();
            VisibilityGate.ResetState();
        }

        private void ResetSamplerState()
        {
            _world = null;
            _initialized = false;
            _nextLightRefresh = 0f;
            _artificialLightActive = false;
            _lastWeatherlessMapLight = float.NegativeInfinity;
            _lights.Clear();
            Current = default;
            EftAccess.ResetEmitterCache();
        }

        private void RefreshLights()
        {
            _lights.Clear();
            foreach (var light in UnityEngine.Object.FindObjectsByType<Light>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (light == null)
                    continue;
                if ((light.type == LightType.Point || light.type == LightType.Spot) && light.range > 0f)
                    _lights.Add(light);
            }
        }

        private float FindStrongestLocalLight(Vector3 head, Transform playerRoot)
        {
            float strongest = 0f;
            Light strongestLight = null;
            float runnerUp = 0f;
            Light runnerUpLight = null;
            foreach (var light in _lights)
            {
                if (light == null || !light.enabled || !light.gameObject.activeInHierarchy)
                    continue;
                Vector3 delta = light.transform.position - head;
                float distance = delta.magnitude;
                if (distance <= 0.01f || distance >= light.range)
                    continue;
                Vector3 direction = delta / distance;
                if (light.type == LightType.Spot &&
                    Vector3.Dot(light.transform.forward, -direction) < Mathf.Cos(light.spotAngle * 0.5f * Mathf.Deg2Rad))
                    continue;
                float contribution = light.intensity * Luminance(light.color) * (1f - distance / light.range);
                if (contribution > strongest)
                {
                    runnerUp = strongest;
                    runnerUpLight = strongestLight;
                    strongest = contribution;
                    strongestLight = light;
                }
                else if (contribution > runnerUp)
                {
                    runnerUp = contribution;
                    runnerUpLight = light;
                }
            }

            if (strongestLight == null)
                return 0f;

            bool strongestBlocked = IsLightBlocked(head, strongestLight, playerRoot);
            float resolved = strongestBlocked ? strongest * 0.15f : strongest;
            if (strongestBlocked && runnerUpLight != null)
            {
                float resolvedRunnerUp = IsLightBlocked(head, runnerUpLight, playerRoot) ? runnerUp * 0.15f : runnerUp;
                resolved = Mathf.Max(resolved, resolvedRunnerUp);
            }
            return Mathf.Clamp01(resolved * 0.35f);
        }

        private bool IsLightBlocked(Vector3 head, Light light, Transform playerRoot)
        {
            Vector3 toLight = light.transform.position - head;
            float distance = toLight.magnitude;
            return distance > 0.1f && IsOccluded(head, toLight / distance, distance - 0.1f, playerRoot);
        }

        private bool IsOccluded(Vector3 origin, Vector3 direction, float distance, Transform playerRoot)
        {
            int hitCount = Physics.RaycastNonAlloc(origin, direction, _occlusionHits, distance,
                Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < hitCount; i++)
            {
                var collider = _occlusionHits[i].collider;
                if (collider == null)
                    continue;
                if (playerRoot != null && collider.transform.IsChildOf(playerRoot))
                    continue;
                return true;
            }
            return false;
        }

        private static float Luminance(Color color)
        {
            return Mathf.Clamp01(color.r * 0.2126f + color.g * 0.7152f + color.b * 0.0722f);
        }

        private static float RawLuminance(float r, float g, float b)
        {
            return r * 0.2126f + g * 0.7152f + b * 0.0722f;
        }
    }
}
