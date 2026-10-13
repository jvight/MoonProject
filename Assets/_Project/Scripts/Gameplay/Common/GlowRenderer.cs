using System;
using UnityEngine;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// Drives the SoftGlow <c>_Intensity</c> of one renderer through a cached MaterialPropertyBlock (no material
    /// copies, no allocation). At zero the renderer is switched off so dark effects cost no draw call.
    /// </summary>
    public sealed class GlowRenderer
    {
        /// <summary>Below this intensity an effect is invisible and is not drawn.</summary>
        public const float VisibleThreshold = 0.002f;

        private static readonly int IntensityId = Shader.PropertyToID("_Intensity");
        private static readonly int ColorId = Shader.PropertyToID("_Color");

        private readonly Renderer _renderer;
        private readonly MaterialPropertyBlock _block = new MaterialPropertyBlock();

        public GlowRenderer(Renderer renderer)
        {
            _renderer = renderer != null ? renderer : throw new ArgumentNullException(nameof(renderer));
            Apply(0f);
        }

        public float Intensity { get; private set; }

        public Renderer Renderer => _renderer;

        /// <summary>
        /// Tints the glow <paramref name="color"/> instead of its material's colour (a held piece's amber halo),
        /// from the next <see cref="Apply"/> on.
        /// </summary>
        public void Tint(Color color)
        {
            _block.SetColor(ColorId, color);
        }

        public void Apply(float intensity)
        {
            Intensity = Mathf.Max(0f, intensity);
            bool visible = Intensity > VisibleThreshold;
            if (_renderer.enabled != visible)
            {
                _renderer.enabled = visible;
            }

            if (visible)
            {
                _block.SetFloat(IntensityId, Intensity);
                _renderer.SetPropertyBlock(_block);
            }
        }
    }
}
