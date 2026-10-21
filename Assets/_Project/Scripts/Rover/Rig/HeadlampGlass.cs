using UnityEngine;

namespace MoonProject.Rover
{
    /// <summary>
    /// The glow of 07's headlamp glasses: the R1_LampAmber slot of its Body mesh (and the brow pod lenses), following
    /// the road light's level so the lamps visibly switch off and on with it (the wake's ready performance, the dock,
    /// the crawl). The slot keeps its material's own emission strength as "on"; the level scales it through one reused
    /// property block, so the rest of the body is untouched and a frame allocates nothing.
    /// </summary>
    public sealed class HeadlampGlass
    {
        private static readonly int EmissionStrengthId = Shader.PropertyToID("_EmissionStrength");

        private readonly Renderer _body;
        private readonly int _slot;
        private readonly float _onStrength;
        private readonly bool _singleMaterial;
        private readonly MaterialPropertyBlock _block = new MaterialPropertyBlock();

        private HeadlampGlass(Renderer body, int slot, float onStrength, bool singleMaterial)
        {
            _body = body;
            _slot = slot;
            _onStrength = onStrength;
            _singleMaterial = singleMaterial;
        }

        /// <summary>
        /// The glow of material <paramref name="slot"/> on <paramref name="body"/>, or false with the
        /// <paramref name="problem"/> when the renderer or the slot is missing or has no emission strength.
        /// </summary>
        public static bool TryCreate(Renderer body, int slot, out HeadlampGlass glow, out string problem)
        {
            glow = null;
            Material[] materials = body != null ? body.sharedMaterials : null;
            if (materials == null || slot < 0 || slot >= materials.Length || materials[slot] == null)
            {
                problem = $"the headlamp glass slot {slot} is not a material of the body renderer.";
                return false;
            }

            if (!materials[slot].HasProperty(EmissionStrengthId))
            {
                problem = $"the headlamp glass material '{materials[slot].name}' has no _EmissionStrength.";
                return false;
            }

            glow = new HeadlampGlass(body, slot, materials[slot].GetFloat(EmissionStrengthId), materials.Length == 1);
            problem = null;
            return true;
        }

        /// <summary>Sets the glass to <paramref name="level"/> (0 dark .. 1 its material's own glow).</summary>
        public void Apply(float level)
        {
            float strength = _onStrength * Mathf.Clamp01(level);
            _body.GetPropertyBlock(_block, _slot);
            _block.SetFloat(EmissionStrengthId, strength);
            _body.SetPropertyBlock(_block, _slot);
            if (_slot == 0 && _singleMaterial)
            {
                _body.GetPropertyBlock(_block);
                _block.SetFloat(EmissionStrengthId, strength);
                _body.SetPropertyBlock(_block);
            }
        }
    }
}
