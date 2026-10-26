using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Object = UnityEngine.Object;

namespace MoonProject.Art.PlayModeTests
{
    /// <summary>
    /// One render profile of the look spike, applied at runtime only: "shipped" leaves the game's pipeline as it is;
    /// "max" swaps in a clone of the URP asset (8K main-light shadows packed into a shorter, near-weighted cascade
    /// range, tighter biases), gives the capture camera high-quality sharpened TAA, screen-space contact shadows
    /// (<see cref="LookMaxContactShadows"/>), a regolith bounce fill (<see cref="LookMaxBounceLight"/>) and denser
    /// near terrain detail (<see cref="LookMaxTerrainDetail"/>); "aces" is "max" tone-mapped by ACES instead of the
    /// game's Neutral, a little brighter; "cc0" is "max" with the CC0 photo-scanned regolith and rock layers; "stp" is
    /// "max" rendered at a lower scale and upscaled by STP. Nothing is saved: <see cref="Dispose"/> puts the original
    /// asset back.
    /// </summary>
    public sealed class LookMaxProfile : System.IDisposable
    {
        public const string Shipped = "shipped";
        public const string Max = "max";
        public const string Stp = "stp";
        public const string Aces = "aces";
        public const string Cc0 = "cc0";

        /// <summary>Every profile, in the order the session shoots them.</summary>
        public static readonly string[] All = { Shipped, Max, Cc0, Aces, Stp };

        private const int MaxShadowResolution = 8192;
        private const float MaxShadowDistance = 220f;
        private static readonly Vector3 MaxCascadeSplits = new Vector3(0.03f, 0.1f, 0.3f);
        private const float MaxDepthBias = 0.06f;
        private const float MaxNormalBias = 0.3f;
        private const float StpRenderScale = 0.67f;
        private const float TaaSharpening = 0.5f;
        private const int OverrideVolumePriority = 1000;
        private const float AcesExposure = 0.35f;

        private readonly RenderPipelineAsset _original;
        private readonly UniversalRenderPipelineAsset _clone;
        private readonly LookMaxContactShadows _contactShadows;
        private readonly LookMaxBounceLight _bounce;
        private readonly LookMaxTerrainDetail _terrain;
        private readonly GameObject _volumeHost;
        private readonly VolumeProfile _volumeProfile;

        public LookMaxProfile(string name, Camera camera, LookMaxVista vista)
        {
            Name = name;
            _original = QualitySettings.renderPipeline;
            UniversalAdditionalCameraData data = camera.GetUniversalAdditionalCameraData();
            if (name == Shipped)
            {
                return;
            }

            if (name != Max && name != Stp && name != Aces && name != Cc0)
            {
                throw new System.ArgumentException($"Unknown look-max profile '{name}'.", nameof(name));
            }

            RenderPipelineAsset current = _original != null ? _original : GraphicsSettings.defaultRenderPipeline;
            var source = (UniversalRenderPipelineAsset)current;
            _clone = Object.Instantiate(source);
            _clone.name = source.name + " (look-max " + name + ")";
            _clone.mainLightShadowmapResolution = MaxShadowResolution;
            _clone.shadowDistance = MaxShadowDistance;
            _clone.shadowCascadeCount = 4;
            _clone.cascade4Split = MaxCascadeSplits;
            _clone.shadowDepthBias = MaxDepthBias;
            _clone.shadowNormalBias = MaxNormalBias;
            _clone.msaaSampleCount = 1;
            if (name == Stp)
            {
                _clone.renderScale = StpRenderScale;
                _clone.upscalingFilter = UpscalingFilterSelection.STP;
            }

            QualitySettings.renderPipeline = _clone;
            data.antialiasing = AntialiasingMode.TemporalAntiAliasing;
            data.taaSettings.quality = TemporalAAQuality.VeryHigh;
            data.taaSettings.contrastAdaptiveSharpening = TaaSharpening;
            _contactShadows = new LookMaxContactShadows(camera);
            _bounce = LookMaxBounceLight.Create();
            _terrain = new LookMaxTerrainDetail(name == Cc0 ? vista.cc0 : null);
            if (name == Aces)
            {
                _volumeHost = new GameObject("LookMaxAcesVolume");
                var volume = _volumeHost.AddComponent<Volume>();
                volume.isGlobal = true;
                volume.priority = OverrideVolumePriority;
                _volumeProfile = ScriptableObject.CreateInstance<VolumeProfile>();
                _volumeProfile.Add<Tonemapping>(true).mode.Override(TonemappingMode.ACES);
                _volumeProfile.Add<ColorAdjustments>(true).postExposure.Override(AcesExposure);
                volume.sharedProfile = _volumeProfile;
            }
        }

        public string Name { get; }

        /// <summary>Matches the profile's sun-driven extras to the sun; call after moving the clock.</summary>
        public void FollowSun()
        {
            if (_bounce != null)
            {
                _bounce.Follow();
            }
        }

        public void Dispose()
        {
            if (_clone == null)
            {
                return;
            }

            QualitySettings.renderPipeline = _original;
            Object.Destroy(_clone);
            _contactShadows.Dispose();
            Object.Destroy(_bounce.gameObject);
            _terrain.Dispose();
            if (_volumeHost != null)
            {
                Object.Destroy(_volumeHost);
                Object.Destroy(_volumeProfile);
            }
        }
    }
}
