using System;
using UnityEngine;

namespace MoonProject.World
{
    /// <summary>
    /// The two places left of the old relay network (M3-06): relay.1, the dead west mast the opening shows from the
    /// apron, and micro.06, 06's spot by the old canyon-mouth mast (Rover's elder glance). Both stand where the old
    /// network's search put them.
    /// </summary>
    [Serializable]
    public sealed class RelaySettings
    {
        [Tooltip("Radius of the west mast's flat, drivable, rock-free pad, metres.")]
        [Range(2f, 6f)]
        [SerializeField] private float _padRadius = 3f;

        [Tooltip("Where relay.1, the dead west mast, stands on the basin floor (x, z), metres from home.")]
        [SerializeField] private Vector2 _westMast = new Vector2(-204.853f, -74.56037f);

        [Tooltip("Where micro.06, 06's spot by the old canyon-mouth mast, lies (x, z), metres from home.")]
        [SerializeField] private Vector2 _elderSite = new Vector2(265.42465f, 84.128975f);

        [Tooltip("Radius of 06's spot, metres.")]
        [Range(1f, 8f)]
        [SerializeField] private float _elderRadius = 4f;

        public float PadRadius => _padRadius;
        public Vector2 WestMast => _westMast;
        public Vector2 ElderSite => _elderSite;
        public float ElderRadius => _elderRadius;
    }
}
