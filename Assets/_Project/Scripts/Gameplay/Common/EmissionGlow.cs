using System;
using UnityEngine;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// Brightens an Art glow renderer (lander windows, shelf and tower light strips) through the palette material's
    /// emission, per the rig contract: a MaterialPropertyBlock sets <c>_EmissionColor</c> to white × intensity
    /// (1 = as authored, 0 = dark, above 1 = brighter, HDR). Cached block, no allocation per update.
    /// </summary>
    public sealed class EmissionGlow
    {
        /// <summary>Changes smaller than this are not pushed to the renderer.</summary>
        private const float Epsilon = 0.001f;

        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

        private readonly Renderer _renderer;
        private readonly MaterialPropertyBlock _block = new MaterialPropertyBlock();
        private float _applied = -1f;

        public EmissionGlow(Renderer renderer)
        {
            _renderer = renderer != null ? renderer : throw new ArgumentNullException(nameof(renderer));
        }

        public float Intensity => Mathf.Max(0f, _applied);

        public void Apply(float intensity)
        {
            intensity = Mathf.Max(0f, intensity);
            if (Mathf.Abs(intensity - _applied) < Epsilon)
            {
                return;
            }

            _applied = intensity;
            _renderer.GetPropertyBlock(_block);
            _block.SetColor(EmissionColorId, Color.white * intensity);
            _renderer.SetPropertyBlock(_block);
        }
    }
}
