using System;
using UnityEngine;
using MoonProject.Core;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// One level of an upgrade: the recipe it is crafted from (salvaged materials, docs/features/M3-13) and its effects
    /// (signal reach, base warmth, a rover ability). Its
    /// title and description are player-facing prose and live in the localization tables under
    /// "upgrade.&lt;id&gt;.&lt;level&gt;.*".
    /// </summary>
    [Serializable]
    public sealed class UpgradeLevel
    {
        [Tooltip("Salvaged materials it is crafted from.")]
        [SerializeField] private Recipe _recipe;

        [Tooltip("Clear radio signal radius (m) once bought; 0 = this upgrade does not touch the radio.")]
        [Range(0f, 1000f)] [SerializeField] private float _signalRadius;

        [Tooltip("How much brighter the base glows once bought (1 = unchanged).")]
        [Range(0.5f, 4f)] [SerializeField] private float _lightBoost = 1f;

        [Tooltip("This level unlocks a rover ability (granted through IRoverAbilities on purchase and on load).")]
        [SerializeField] private bool _grantsAbility;

        [Tooltip("The ability it unlocks.")]
        [SerializeField] private RoverAbility _ability;

        public UpgradeLevel(Recipe recipe, float signalRadius, float lightBoost)
        {
            _recipe = recipe;
            _signalRadius = signalRadius;
            _lightBoost = lightBoost;
        }

        /// <summary>A level that unlocks <paramref name="ability"/>, crafted from <paramref name="recipe"/>.</summary>
        public UpgradeLevel(Recipe recipe, RoverAbility ability)
        {
            _recipe = recipe;
            _lightBoost = 1f;
            _grantsAbility = true;
            _ability = ability;
        }

        public Recipe Recipe => _recipe;

        public float SignalRadius => _signalRadius;

        public float LightBoost => _lightBoost;

        public bool GrantsAbility => _grantsAbility;

        public RoverAbility Ability => _ability;
    }
}
