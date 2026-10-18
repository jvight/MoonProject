using System;
using UnityEngine;

namespace MoonProject.World
{
    /// <summary>
    /// How the opening's old power cables lie on the dust (opening v2 §4): Art's R1 cable kit laid joint to joint from
    /// 07's dead post along the crew tracks to the lander's port (one 45-degree bend, the plug in the port), torn
    /// about midway with a splice box beside the tear, and from the port to the build apron; settled in a few slow
    /// sways, a line the eye follows.
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

        [Tooltip("Where along its run from 07's post the old cable is torn (0 = at the post, 1 = at the corner by " +
            "the lander): about midway, where the drive's eye is on it.")]
        [Range(0.2f, 0.8f)]
        [SerializeField] private float _tearShare = 0.5f;

        [Tooltip("Metres between the two torn ends' copper.")]
        [Range(0.1f, 1.5f)]
        [SerializeField] private float _tearGap = 0.45f;

        [Tooltip("Degrees the far torn end lies kinked off the line, as it snapped back when it parted.")]
        [Range(0f, 40f)]
        [SerializeField] private float _tearKink = 14f;

        [Tooltip("Metres beside the tear that the splice box lies, never fitted: the repair it was meant for.")]
        [Range(0.5f, 4f)]
        [SerializeField] private float _spliceSide = 1.3f;

        [Tooltip("Metres the cable runs straight into the lander's port after its one 45-degree bend.")]
        [Range(3f, 30f)]
        [SerializeField] private float _approachLength = 8f;

        public float Wander => _wander;
        public float Bends => _bends;
        public float PortInset => _portInset;
        public float TearShare => _tearShare;
        public float TearGap => _tearGap;
        public float TearKink => _tearKink;
        public float SpliceSide => _spliceSide;
        public float ApproachLength => _approachLength;
    }
}
