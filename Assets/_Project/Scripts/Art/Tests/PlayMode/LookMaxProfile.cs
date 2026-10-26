using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Object = UnityEngine.Object;

namespace MoonProject.Art.PlayModeTests
{
    /// <summary>
    /// One render profile of the look spike, applied at runtime only: "shipped" leaves the game's pipeline as it is;
    /// "max" swaps in a clone of the URP asset (8K main-light shadows packed into a shorter, near-weighted cascade
    /// range, tighter biases, a 64-bit colour buffer) and gives the capture camera high-quality TAA; "stp" is "max"
    /// rendered at a lower scale and upscaled by STP. Nothing is saved: <see cref="Dispose"/> puts the original asset
    /// back.
    /// </summary>
    public sealed class LookMaxProfile : System.IDisposable
    {
        public const string Shipped = "shipped";
        public const string Max = "max";
        public const string Stp = "stp";

        private const int MaxShadowResolution = 8192;
        private const float MaxShadowDistance = 220f;
        private static readonly Vector3 MaxCascadeSplits = new Vector3(0.03f, 0.1f, 0.3f);
        private const float MaxDepthBias = 0.06f;
        private const float MaxNormalBias = 0.3f;
        private const float StpRenderScale = 0.67f;

        private readonly RenderPipelineAsset _original;
        private readonly UniversalRenderPipelineAsset _clone;

        public LookMaxProfile(string name, Camera camera)
        {
            Name = name;
            _original = QualitySettings.renderPipeline;
            UniversalAdditionalCameraData data = camera.GetUniversalAdditionalCameraData();
            if (name == Shipped)
            {
                return;
            }

            if (name != Max && name != Stp)
            {
                throw new System.ArgumentException($"Unknown look-max profile '{name}'.", nameof(name));
            }

            var source = (UniversalRenderPipelineAsset)(_original != null ? _original : GraphicsSettings.defaultRenderPipeline);
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
        }

        public string Name { get; }

        /// <summary>Whether the profile needs consecutive frames to converge (TAA or STP history).</summary>
        public bool IsTemporal => Name != Shipped;

        public void Dispose()
        {
            if (_clone == null)
            {
                return;
            }

            QualitySettings.renderPipeline = _original;
            Object.Destroy(_clone);
        }
    }
}
