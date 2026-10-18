using UnityEngine;

namespace MoonProject.Rover
{
    /// <summary>
    /// The glow of 07's two headlamp glasses (RoverModel 'Glass_L' and 'Glass_R', on R1_LampAmber), following the road
    /// light's level so the lamps visibly switch off and on with it (the wake's ready performance, the dock, the
    /// crawl). Each glass keeps its material's own emission strength as "on"; the level scales it through one reused
    /// property block, so a frame allocates nothing.
    /// </summary>
    public sealed class HeadlampGlass
    {
        private static readonly int EmissionStrengthId = Shader.PropertyToID("_EmissionStrength");

        private readonly Renderer[] _glasses;
        private readonly float[] _onStrength;
        private readonly MaterialPropertyBlock _block = new MaterialPropertyBlock();

        private HeadlampGlass(Renderer[] glasses, float[] onStrength)
        {
            _glasses = glasses;
            _onStrength = onStrength;
        }

        /// <summary>
        /// The glow for <paramref name="glasses"/>, or false with the <paramref name="problem"/> when one is missing or
        /// its material has no emission strength (R1_LampAmber expected).
        /// </summary>
        public static bool TryCreate(Renderer[] glasses, out HeadlampGlass glow, out string problem)
        {
            glow = null;
            if (glasses == null)
            {
                problem = "no headlamp glasses are assigned.";
                return false;
            }

            var onStrength = new float[glasses.Length];
            for (int i = 0; i < glasses.Length; i++)
            {
                Material material = glasses[i] != null ? glasses[i].sharedMaterial : null;
                if (material == null || !material.HasProperty(EmissionStrengthId))
                {
                    problem = $"headlamp glass {i} has no material with _EmissionStrength (R1_LampAmber expected).";
                    return false;
                }

                onStrength[i] = material.GetFloat(EmissionStrengthId);
            }

            glow = new HeadlampGlass(glasses, onStrength);
            problem = null;
            return true;
        }

        /// <summary>Sets every glass to <paramref name="level"/> (0 dark .. 1 its material's own glow).</summary>
        public void Apply(float level)
        {
            float share = Mathf.Clamp01(level);
            for (int i = 0; i < _glasses.Length; i++)
            {
                _glasses[i].GetPropertyBlock(_block);
                _block.SetFloat(EmissionStrengthId, _onStrength[i] * share);
                _glasses[i].SetPropertyBlock(_block);
            }
        }
    }
}
