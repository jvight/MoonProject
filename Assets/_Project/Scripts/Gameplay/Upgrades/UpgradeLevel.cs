using System;
using UnityEngine;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// One purchasable level of an upgrade: its cost and its effects. Its title and description are player-facing
    /// prose and live in the localization tables under "upgrade.&lt;id&gt;.&lt;level&gt;.*".
    /// </summary>
    [Serializable]
    public sealed class UpgradeLevel
    {
        [Tooltip("Scrap it costs.")]
        [Range(1, 1000)] [SerializeField] private int _cost = 10;

        [Tooltip("Clear radio signal radius (m) once bought; 0 = this upgrade does not touch the radio.")]
        [Range(0f, 1000f)] [SerializeField] private float _signalRadius;

        [Tooltip("How much brighter the base glows once bought (1 = unchanged).")]
        [Range(0.5f, 4f)] [SerializeField] private float _lightBoost = 1f;

        public UpgradeLevel(int cost, float signalRadius, float lightBoost)
        {
            _cost = cost;
            _signalRadius = signalRadius;
            _lightBoost = lightBoost;
        }

        public int Cost => _cost;

        public float SignalRadius => _signalRadius;

        public float LightBoost => _lightBoost;
    }
}
