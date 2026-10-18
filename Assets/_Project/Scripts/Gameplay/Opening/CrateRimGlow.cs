using System;
using UnityEngine;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// The soft warm rim glow of an open crate (Art's RimGlow on Crate_Cargo_Open): it warms as 07 comes within reach while
    /// the crate still holds something, brighter the nearer 07 is over the outer band, and cools off once it is
    /// emptied or 07 drives away, easing both ways. Writes only <c>_EmissionStrength</c> and <c>_BloomSafeMax</c>
    /// into the renderers' property blocks (Art's values read from their material, times the tuning's strength) and
    /// keeps the rim's object off while it is unlit; a small warm light over the rim follows the same glow, since the
    /// rim line itself sits just below the crate's walls from 07's camera.
    /// </summary>
    public sealed class CrateRimGlow
    {
        private static readonly int StrengthId = Shader.PropertyToID("_EmissionStrength");
        private static readonly int CapId = Shader.PropertyToID("_BloomSafeMax");
        private static readonly int ColorId = Shader.PropertyToID("_EmissionColor");

        private readonly GameObject _rim;
        private readonly Light _light;
        private readonly float _lightIntensity;
        private readonly Renderer[] _renderers;
        private readonly float[] _full;
        private readonly float[] _cap;
        private readonly float _reach;
        private readonly float _band;
        private readonly float _fadeSeconds;
        private readonly MaterialPropertyBlock _block = new MaterialPropertyBlock();
        private float _level;
        private float _shown = -1f;

        /// <param name="rim">Art's rim glow object on the crate.</param>
        /// <param name="light">The small warm light over the rim, tinted here with the rim's own colour.</param>
        /// <param name="tuning">The glow's reach, band, fade, strength and light (OpeningTuning).</param>
        public CrateRimGlow(GameObject rim, Light light, OpeningTuning tuning)
        {
            _rim = rim != null ? rim : throw new ArgumentNullException(nameof(rim));
            _light = light != null ? light : throw new ArgumentNullException(nameof(light));
            if (tuning == null)
            {
                throw new ArgumentNullException(nameof(tuning));
            }

            float strength = tuning.CrateRimStrength;
            _lightIntensity = tuning.CrateRimLightIntensity;
            _renderers = rim.GetComponentsInChildren<Renderer>(true);
            if (_renderers.Length == 0)
            {
                throw new ArgumentException($"The crate rim '{rim.name}' shows no mesh to glow.", nameof(rim));
            }

            _full = new float[_renderers.Length];
            _cap = new float[_renderers.Length];
            for (int i = 0; i < _renderers.Length; i++)
            {
                Material material = _renderers[i].sharedMaterial;
                if (material == null || !material.HasProperty(StrengthId) || !material.HasProperty(CapId) ||
                    !material.HasProperty(ColorId))
                {
                    throw new ArgumentException($"The crate rim '{rim.name}' has no material with an emission " +
                                                "strength and bloom cap (Art's R1_CrateRimGlowAmber).", nameof(rim));
                }

                // Art's bloom-safe cap rises with the strength, or the cap would clip the stronger glow back.
                _full[i] = material.GetFloat(StrengthId) * strength;
                _cap[i] = material.GetFloat(CapId) * strength;
            }

            _light.color = _renderers[0].sharedMaterial.GetColor(ColorId);
            _reach = Mathf.Max(tuning.CrateRimReach, 0.01f);
            _band = Mathf.Clamp(tuning.CrateRimBand, 0.01f, _reach);
            _fadeSeconds = Mathf.Max(tuning.CrateRimFadeSeconds, 0.01f);
            Apply();
        }

        /// <summary>The glow as written, 0 (unlit) .. 1 (full).</summary>
        public float Glow => Ease.InOutSine(_level);

        /// <summary>
        /// The share of the glow a crate <paramref name="distance"/> metres from 07 asks for: none beyond the reach,
        /// full inside its band.
        /// </summary>
        public static float Share(float distance, float reach, float band)
        {
            return Mathf.Clamp01((reach - distance) / Mathf.Max(band, 0.01f));
        }

        /// <summary>Eases toward the glow 07's distance asks for while the crate still holds something.</summary>
        public void Step(bool holds, float distance, float deltaTime)
        {
            float target = holds ? Share(distance, _reach, _band) : 0f;
            _level = Mathf.MoveTowards(_level, target, deltaTime / _fadeSeconds);
            Apply();
        }

        private void Apply()
        {
            float glow = Glow;
            if (Mathf.Approximately(glow, _shown))
            {
                return;
            }

            _shown = glow;
            bool lit = glow > 0f;
            if (_rim.activeSelf != lit)
            {
                _rim.SetActive(lit);
                _light.enabled = lit;
            }

            _light.intensity = _lightIntensity * glow;

            for (int i = 0; i < _renderers.Length; i++)
            {
                _renderers[i].GetPropertyBlock(_block);
                _block.SetFloat(StrengthId, _full[i] * glow);
                _block.SetFloat(CapId, _cap[i]);
                _renderers[i].SetPropertyBlock(_block);
            }
        }
    }
}
