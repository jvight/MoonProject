using System;
using UnityEngine;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// Brightens an Art glow renderer (lander windows, shelf and tower light strips, friends' lamps) through the
    /// palette material's emission, per the glow contract (docs/ARCHITECTURE.md, "Glow modulation"): a
    /// MaterialPropertyBlock sets <c>_EmissionColor</c> to the vector (i, i, i, 1), a linear multiplier of the
    /// palette's HDR emission (1 = as authored, 0 = dark, 2 = twice the light). Never a colour: Unity would read it as
    /// gamma and raise it to about the power 2.2. Cached block, no allocation per update.
    /// </summary>
    public sealed class EmissionGlow
    {
        /// <summary>Changes smaller than this are not pushed to the renderer.</summary>
        private const float Epsilon = 0.001f;

        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");
        private static readonly int EmissionStrengthId = Shader.PropertyToID("_EmissionStrength");

        private readonly Renderer _renderer;
        private readonly MaterialPropertyBlock _block = new MaterialPropertyBlock();
        private float _applied = -1f;
        private Color _tint = Color.white;

        public EmissionGlow(Renderer renderer)
        {
            _renderer = renderer != null ? renderer : throw new ArgumentNullException(nameof(renderer));
        }

        public float Intensity => Mathf.Max(0f, _applied);

        public void Apply(float intensity)
        {
            Apply(Color.white, intensity);
        }

        /// <summary>
        /// The same with a per-channel <paramref name="tint"/>: a linear multiplier of each emission channel (a lamp
        /// strip turning from cyan to gold), never read as a gamma colour.
        /// </summary>
        public void Apply(Color tint, float intensity)
        {
            intensity = Mathf.Max(0f, intensity);
            if (Mathf.Abs(intensity - _applied) < Epsilon && Mathf.Abs(tint.r - _tint.r) < Epsilon &&
                Mathf.Abs(tint.g - _tint.g) < Epsilon && Mathf.Abs(tint.b - _tint.b) < Epsilon)
            {
                return;
            }

            _applied = intensity;
            _tint = tint;
            float r = tint.r * intensity;
            float g = tint.g * intensity;
            float b = tint.b * intensity;
            float peak = Mathf.Max(r, Mathf.Max(g, b));
            _renderer.GetPropertyBlock(_block);
            _block.SetVector(EmissionColorId, new Vector4(r, g, b, 1f));
            _block.SetFloat(EmissionStrengthId, Mathf.Clamp01(peak));
            _renderer.SetPropertyBlock(_block);
        }
    }
}
