using System;
using UnityEngine;

namespace MoonProject.World
{
    /// <summary>
    /// How 07's old charging cable lies on the dust between its dead post and the lander's port (opening v2 §4): thin,
    /// dark rubber, settled in a few slow bends, a line the eye follows from the wake to the lander.
    /// </summary>
    [Serializable]
    public sealed class GroundCableSettings
    {
        [Tooltip("Radius of the cable, metres: a heavy power lead.")]
        [Range(0.01f, 0.1f)]
        [SerializeField] private float _radius = 0.035f;

        [Tooltip("Spacing of the points along the cable, metres: about the terrain's fine facet, so it hugs the dust.")]
        [Range(0.25f, 4f)]
        [SerializeField] private float _step = 1f;

        [Tooltip("How far the cable wanders side to side at most, metres: a dragged cable settling in slow bends.")]
        [Range(0f, 6f)]
        [SerializeField] private float _wander = 1.6f;

        [Tooltip("Number of slow bends along the whole cable.")]
        [Range(1f, 12f)]
        [SerializeField] private float _bends = 3f;

        [Tooltip("How far from the lander's centre the cable ends, along its port side's facing, metres: at the port " +
            "on the hull's side (the R1 CargoLander is 5.2 m deep).")]
        [Range(1f, 6f)]
        [SerializeField] private float _portReach = 2.9f;

        public float Radius => _radius;
        public float Step => _step;
        public float Wander => _wander;
        public float Bends => _bends;
        public float PortReach => _portReach;
    }
}
