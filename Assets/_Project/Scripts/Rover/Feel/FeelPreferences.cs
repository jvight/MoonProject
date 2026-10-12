using UnityEngine;
using MoonProject.Core;

namespace MoonProject.Rover
{
    /// <summary>
    /// The player's feel preferences, registered as <see cref="IFeelSettings"/>: rumble on by default and at full
    /// strength (owner, 2026-10-12). UI applies saved values.
    /// </summary>
    public sealed class FeelPreferences : IFeelSettings
    {
        private float _strength = 1f;

        public bool RumbleEnabled { get; set; } = true;

        public float RumbleStrength
        {
            get => _strength;
            set => _strength = Mathf.Clamp01(value);
        }
    }
}
