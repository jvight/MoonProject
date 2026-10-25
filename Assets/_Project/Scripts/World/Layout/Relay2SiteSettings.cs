using System;
using UnityEngine;

namespace MoonProject.World
{
    /// <summary>
    /// relay.2's hut by the canyon mouth (opening v3 5.2b): a small 1980s relay hut (its anchor and clearing in
    /// <see cref="RelaySettings"/>) that frames 06, slumped against it with its battery bay toward the approach, and
    /// the spot on the approach where 07 stops to scan the bay; and relay.2's power line, laid from the hut across the
    /// ground and into the canyon's dark slot, toward the supply lander that fed it (opening-v3-why-chain R11), so
    /// 07 follows it in.
    /// </summary>
    [Serializable]
    public sealed class Relay2SiteSettings
    {
        [Tooltip("How far out from 06, toward the canyon mouth, 07 stops to scan its battery bay, metres.")]
        [Range(2f, 10f)]
        [SerializeField] private float _scanApproachDistance = 4f;

        [Tooltip("Degrees the hut's door turns from facing the canyon mouth: angled toward the approach.")]
        [Range(-90f, 90f)]
        [SerializeField] private float _hutYaw = 15f;

        [Header("The power line into the slot")]
        [Tooltip("How far right of the slot's centre line the power line lies along its floor, metres (negative to " +
            "the left, the hut's side, so it comes in along one wall without crossing the mouth): off to one side, so " +
            "07 drives in beside it, never over it.")]
        [Range(-3f, 3f)]
        [SerializeField] private float _lineSide = -1.6f;

        [Tooltip("How far past the slot's turn the line runs on into the dark, metres: out of sight round the corner.")]
        [Range(0f, 40f)]
        [SerializeField] private float _lineDepth = 14f;

        [Tooltip("Spacing of the line's waypoints along the slot, metres: short enough to follow its turn.")]
        [Range(1f, 10f)]
        [SerializeField] private float _lineStep = 3f;

        [Tooltip("How far the line wanders off its straight runs, metres: an old cable settled in the dust.")]
        [Range(0f, 3f)]
        [SerializeField] private float _lineWander = 0.5f;

        [Tooltip("Slow side-to-side bends along each of the line's runs.")]
        [Range(0f, 4f)]
        [SerializeField] private float _lineBends = 1.5f;

        public float ScanApproachDistance => _scanApproachDistance;
        public float LineSide => _lineSide;
        public float LineDepth => _lineDepth;
        public float LineStep => _lineStep;
        public float LineWander => _lineWander;
        public float LineBends => _lineBends;
        public float HutYaw => _hutYaw;
    }
}
