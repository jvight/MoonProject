using System;
using UnityEngine;

namespace MoonProject.World
{
    /// <summary>
    /// The warm pool a lit relay mast's lamp pours onto its 4 m charge ring (docs/features/M3-20 "Chargers", M3-18 V3):
    /// dark while the mast is dormant, a warm pool readable from far off at night once it is lit, warmer still while it
    /// fills 07's cells.
    /// </summary>
    [Serializable]
    public sealed class ChargerPoolSettings
    {
        [Tooltip("Radius (m) of the pool's light disc on the pad (the ring is 2 m; the glow spills a little past it).")]
        [Range(1f, 6f)] [SerializeField] private float _radius = 2.8f;

        [Tooltip("Height (m) of the disc above the pad's centre, clear of the ground's facets.")]
        [Range(0f, 0.5f)] [SerializeField] private float _lift = 0.06f;

        [Tooltip("Glow of a lit mast's pool (linear multiplier of the disc's authored warmth).")]
        [Range(0f, 4f)] [SerializeField] private float _litGlow = 0.9f;

        [Tooltip("Glow while the pad fills 07's cells.")]
        [Range(0f, 4f)] [SerializeField] private float _chargingGlow = 1.5f;

        [Tooltip("Seconds (time constant) a pool takes to warm or fade.")]
        [Range(0.05f, 5f)] [SerializeField] private float _ease = 0.9f;

        public float Radius => _radius;
        public float Lift => _lift;
        public float LitGlow => _litGlow;
        public float ChargingGlow => _chargingGlow;
        public float Ease => _ease;
    }
}
