using System;
using UnityEngine;

namespace MoonProject.World
{
    /// <summary>
    /// relay.1's lamp once the call is answered (opening v3 R8, <see cref="RelayAnswerLamp"/>): dark as the dead mast
    /// it is until relay.1 answers 07, then a warm amber halo at the mast's top that keys three short and one long with
    /// the call's own sign. Seen across the night basin, so it never shrinks below a small angle.
    /// </summary>
    [Serializable]
    public sealed class RelayLampSettings
    {
        [Tooltip("Brightness of the lamp's halo at the top of a key. Its amber peak stays under the bloom threshold " +
            "(checked by WorldSettings), so it is a lamp, never a sun.")]
        [Range(0f, 8f)]
        [SerializeField] private float _haloIntensity = 1.2f;

        [Tooltip("Radius of the lamp's halo up close, metres.")]
        [Range(0.05f, 3f)]
        [SerializeField] private float _haloRadius = 0.6f;

        [Tooltip("Smallest angular size of the halo, degrees: the first lure after the answer, so a warm point that " +
            "still stands out in the whole night frame from the base, 300 m off.")]
        [Range(0f, 2f)]
        [SerializeField] private float _haloMinAngle = 0.9f;

        [Tooltip("Distance (m) from which the halo's minimum size starts to grow.")]
        [Range(1f, 200f)]
        [SerializeField] private float _haloMinFrom = 40f;

        [Tooltip("Distance (m) at which the halo's minimum size is full.")]
        [Range(10f, 1500f)]
        [SerializeField] private float _haloMinFull = 500f;

        [Tooltip("Metres the halo floats above the mast's lamp node.")]
        [Range(0f, 2f)]
        [SerializeField] private float _haloLift = 0.2f;

        public float HaloIntensity => _haloIntensity;
        public float HaloRadius => _haloRadius;
        public float HaloMinAngle => _haloMinAngle;
        public float HaloMinFrom => _haloMinFrom;
        public float HaloMinFull => Mathf.Max(_haloMinFull, _haloMinFrom + 1f);
        public float HaloLift => _haloLift;
    }
}
