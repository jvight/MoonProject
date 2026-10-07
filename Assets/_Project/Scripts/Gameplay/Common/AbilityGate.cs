using System;
using UnityEngine;
using MoonProject.Core;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// The rover ability a place needs (design ruling 10: gates are real), e.g. Hover-Jump for everything past the
    /// canyon's chasm. Ungated content is reachable from the start.
    /// </summary>
    [Serializable]
    public sealed class AbilityGate
    {
        [Tooltip("Reaching it needs a rover ability.")]
        [SerializeField] private bool _gated;

        [Tooltip("The ability it needs (when gated).")]
        [SerializeField] private RoverAbility _ability;

        public AbilityGate(bool gated, RoverAbility ability)
        {
            _gated = gated;
            _ability = ability;
        }

        /// <summary>Reachable from the start.</summary>
        public static AbilityGate Open => new AbilityGate(false, default);

        public bool Gated => _gated;

        public RoverAbility Ability => _ability;

        public bool IsOpen(IRoverAbilities abilities)
        {
            if (abilities == null)
            {
                throw new ArgumentNullException(nameof(abilities));
            }

            return !_gated || abilities.Has(_ability);
        }
    }
}
