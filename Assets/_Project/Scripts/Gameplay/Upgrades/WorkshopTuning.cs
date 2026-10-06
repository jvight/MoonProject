using UnityEngine;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// Kenji's workbench: where it stands by the lander and how its pad (the diegetic shop for rover abilities)
    /// glows. Created by the Gameplay/Tuning builder; runtime code only reads it.
    /// </summary>
    public sealed class WorkshopTuning : ScriptableObject
    {
        [Header("Placement")]
        [Tooltip("Where the workbench stands relative to the lander, in lander space (m): right of the museum shelf, " +
                 "slightly behind it, mirroring the radio tower on the left.")]
        [SerializeField] private Vector3 _benchOffset = new Vector3(10.5f, 0f, -2.5f);

        [Tooltip("Pad centre relative to the workbench, in lander space (m): in front of the bench.")]
        [SerializeField] private Vector3 _padOffset = new Vector3(0f, 0f, 3.2f);

        [Header("Pad")]
        [Tooltip("07 counts as parked on the pad within this many metres of its centre.")]
        [Range(0.5f, 6f)] [SerializeField] private float _padRadius = 2.4f;

        [Tooltip("Width (m) of the pad's ring of light.")]
        [Range(0.05f, 2f)] [SerializeField] private float _padRingWidth = 0.35f;

        [Tooltip("Pad brightness while the next ability is not affordable yet (it breathes softly).")]
        [Range(0f, 3f)] [SerializeField] private float _padIdle = 0.22f;

        [Tooltip("Pad brightness when the next ability is affordable: an invitation.")]
        [Range(0f, 3f)] [SerializeField] private float _padInviting = 0.65f;

        [Tooltip("Pad brightness while 07 is parked on it.")]
        [Range(0f, 3f)] [SerializeField] private float _padOccupied = 1f;

        [Tooltip("Pad brightness once every ability on the bench is bought.")]
        [Range(0f, 3f)] [SerializeField] private float _padDone = 0.12f;

        [Tooltip("Seconds per breath of the waiting pad.")]
        [Range(0.5f, 10f)] [SerializeField] private float _breathPeriod = 3.2f;

        [Tooltip("How deeply the waiting pad breathes (0 = steady).")]
        [Range(0f, 1f)] [SerializeField] private float _padBreathDepth = 0.35f;

        [Tooltip("Seconds (time constant) for the pad to brighten or dim; a purchase flare fades the same way.")]
        [Range(0f, 3f)] [SerializeField] private float _padEase = 0.4f;

        [Tooltip("Segments of the pad ring.")]
        [Range(8, 128)] [SerializeField] private int _padSegments = 48;

        [Header("Upgrade moment")]
        [Tooltip("Pad brightness the moment an ability is bought; it eases back down by itself.")]
        [Range(0f, 6f)] [SerializeField] private float _purchaseFlare = 2.4f;

        public Vector3 BenchOffset => _benchOffset;
        public Vector3 PadOffset => _padOffset;
        public float PurchaseFlare => _purchaseFlare;

        public PadLook PadLook => new PadLook(_padRadius, _padRingWidth, _padSegments, _padIdle, _padInviting,
            _padOccupied, _padDone, _breathPeriod, _padBreathDepth, _padEase);
    }
}
