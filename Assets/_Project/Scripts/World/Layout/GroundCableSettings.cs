using System;
using UnityEngine;

namespace MoonProject.World
{
    /// <summary>
    /// How the opening's old power cables lie on the dust (opening v2 §4): Art's tileable R1 cable segments laid joint
    /// to joint from 07's dead post to the lander's port, and from the port to the build apron, settled in a few slow
    /// bends, a line the eye follows.
    /// </summary>
    [Serializable]
    public sealed class GroundCableSettings
    {
        [Tooltip("How far a cable wanders side to side at most, metres: a dragged cable settling in slow bends.")]
        [Range(0f, 6f)]
        [SerializeField] private float _wander = 1.6f;

        [Tooltip("Number of slow bends along the whole cable.")]
        [Range(1f, 12f)]
        [SerializeField] private float _bends = 3f;

        [Tooltip("How far inside the lander's footprint edge, along its port side's facing, the service port stands, " +
            "metres: the port's back sits against the hull between the landing legs.")]
        [Range(0f, 3f)]
        [SerializeField] private float _portInset = 0.9f;

        public float Wander => _wander;
        public float Bends => _bends;
        public float PortInset => _portInset;
    }
}
