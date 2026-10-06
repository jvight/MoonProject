using System;
using UnityEngine;

namespace MoonProject.Gameplay
{
    /// <summary>One purchasable level of an upgrade: its cost and what it does.</summary>
    [Serializable]
    public sealed class UpgradeLevel
    {
        [Tooltip("Short name shown in the shop (e.g. \"Wake the old mast\").")]
        [SerializeField] private string _title = string.Empty;

        [Tooltip("One warm line describing what this level does.")]
        [SerializeField] private string _description = string.Empty;

        [Tooltip("Scrap it costs.")]
        [Range(1, 1000)] [SerializeField] private int _cost = 10;

        [Tooltip("Clear radio signal radius (m) once bought; 0 = this upgrade does not touch the radio.")]
        [Range(0f, 1000f)] [SerializeField] private float _signalRadius;

        [Tooltip("How much brighter the base glows once bought (1 = unchanged).")]
        [Range(0.5f, 4f)] [SerializeField] private float _lightBoost = 1f;

        public UpgradeLevel(string title, string description, int cost, float signalRadius, float lightBoost)
        {
            _title = title;
            _description = description;
            _cost = cost;
            _signalRadius = signalRadius;
            _lightBoost = lightBoost;
        }

        public string Title => _title;

        public string Description => _description;

        public int Cost => _cost;

        public float SignalRadius => _signalRadius;

        public float LightBoost => _lightBoost;
    }
}
