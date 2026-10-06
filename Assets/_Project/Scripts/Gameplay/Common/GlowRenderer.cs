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

        private readonly Renderer _renderer;
        private readonly MaterialPropertyBlock _block = new MaterialPropertyBlock();

        public GlowRenderer(Renderer renderer)
        {
            _renderer = renderer != null ? renderer : throw new ArgumentNullException(nameof(renderer));
            Apply(0f);
        }

        public float Intensity { get; private set; }

        public Renderer Renderer => _renderer;

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
