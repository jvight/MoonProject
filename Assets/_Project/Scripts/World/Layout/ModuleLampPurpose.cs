using UnityEngine;

namespace MoonProject.World
{
    /// <summary>
    /// What the power module's status lamp is for (opening v3 R4): while the module sits unhooked at the ruin it is a
    /// lure, blinking "come here" from dusk; once 07 has it on the cable, has brought it home or has built the bank on
    /// it, it has done its job and glows calm and steady, powered. The change eases over the tuned time; the first
    /// state (a loaded game) is taken at once. Plain logic.
    /// </summary>
    public sealed class ModuleLampPurpose
    {
        private bool _known;
        private float _poweredTarget;

        /// <summary>How powered the lamp looks, 0 (the lure's blink) to 1 (the steady glow).</summary>
        public float Powered { get; private set; }

        /// <summary>The module waits at the ruin: the lamp is the thing to go to.</summary>
        public bool IsLure => _poweredTarget <= 0f;

        public void Step(bool atRuin, float deltaTime, float fadeSeconds)
        {
            _poweredTarget = atRuin ? 0f : 1f;
            if (!_known)
            {
                _known = true;
                Powered = _poweredTarget;
                return;
            }

            Powered = Mathf.MoveTowards(Powered, _poweredTarget, deltaTime / Mathf.Max(fadeSeconds, 1e-3f));
        }

        /// <summary>The lamp's level: the lure's <paramref name="blink"/> eased toward the steady glow.</summary>
        public float Level(float blink, float steady)
        {
            return Mathf.Lerp(blink, steady, Powered);
        }

        /// <summary>
        /// The module waits at the ruin: off the cable and within <paramref name="radius"/> of the ruin's spot (both
        /// on the ground plane).
        /// </summary>
        public static bool AtRuin(bool towing, Vector3 module, Vector3 ruin, float radius)
        {
            float dx = module.x - ruin.x;
            float dz = module.z - ruin.z;
            return !towing && dx * dx + dz * dz <= radius * radius;
        }
    }
}
