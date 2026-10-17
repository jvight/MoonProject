using System;
using UnityEngine;

namespace MoonProject.World
{
    /// <summary>
    /// Where World stands the opening's places (opening v2 §4): the Lumen station ruins cluster, the landed cargo
    /// lander and 07's old charging cable from its dead post to the lander's port.
    /// </summary>
    [Serializable]
    public sealed class LandmarkSettings
    {
        [Header("Lumen station ruins cluster (RUINS-WORLD)")]
        [Tooltip("Pieces of the Lumen station ruins cluster arranged relative to SiteSettings.RuinsCenter: " +
            "habitat left, radio tower behind, workshop right, fallen solar array in front, plus scatter set, " +
            "with drivable lanes between them.")]
        [SerializeField] private RuinPlacement[] _ruins =
        {
            new RuinPlacement("HabitatModules", new Vector2(-5.5f, 2.0f), 10f, 5.8f),
            new RuinPlacement("RadioTower", new Vector2(1.5f, 8.2f), 8f, 3.8f),
            new RuinPlacement("KenjiWorkshop", new Vector2(6.8f, 1.2f), 16f, 3.6f),
            new RuinPlacement("FallenSolarArray", new Vector2(2.2f, -5.8f), 20f, 3.8f),
            new RuinPlacement("ScatterSet", new Vector2(-5.2f, -5.2f), -18f, 2.6f),
        };

        [Header("The landed cargo lander (opening v2 §4)")]
        [Tooltip("How far the landed lander leans off level, degrees, about the axis from its port side to the far " +
            "side: a hard landing on uneven ground, slightly tilted.")]
        [Range(0f, 8f)]
        [SerializeField] private float _landerTilt = 2.5f;

        [Tooltip("07's old charging cable from its dead post at the wake point to the lander's port.")]
        [SerializeField] private GroundCableSettings _wakeCable = new GroundCableSettings();

        public RuinPlacement[] Ruins => _ruins;
        public float LanderTilt => _landerTilt;
        public GroundCableSettings WakeCable => _wakeCable;
    }
}
