using System;
using UnityEngine;

namespace MoonProject.World
{
    /// <summary>
    /// The places left of the old relay network (M3-06): relay.1, the dead west mast the opening shows from the apron;
    /// micro.06, 06's spot by the old canyon-mouth mast (Rover's elder glance); and relay.2, that mast's hut beside 06
    /// (opening v3 5.2b). They stand where the old network's search put them.
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

        [Tooltip("Where relay.2's hut stands from micro.06, metres in 06's frame (x to its right, z toward the canyon " +
            "mouth): on 06's left, the side it slumps onto, so it leans against the hut.")]
        [SerializeField] private Vector2 _hutOffset = new Vector2(-1.8f, 0.2f);

        [Tooltip("Radius of the ground kept clear round relay.2's hut, metres.")]
        [Range(1f, 8f)]
        [SerializeField] private float _hutRadius = 3f;

        public float PadRadius => _padRadius;
        public Vector2 WestMast => _westMast;
        public Vector2 ElderSite => _elderSite;
        public float ElderRadius => _elderRadius;
        public Vector2 HutOffset => _hutOffset;
        public float HutRadius => _hutRadius;
    }
}
