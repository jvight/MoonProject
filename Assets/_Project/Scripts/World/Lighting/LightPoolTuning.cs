using System;
using UnityEngine;
using MoonProject.Core;

namespace MoonProject.World
{
    /// <summary>
    /// One warm light pool on the ground of home (M3-18 V3): a shadowless point light hung low over a warm point, so
    /// the base sits on a warm floor instead of cold dust. It wakes with the warm point it belongs to.
    /// </summary>
    [Serializable]
    public sealed class LightPoolTuning
    {
        [Tooltip("The warm point this pool belongs to: it eases up with that point's wake.")]
        [SerializeField] private HomeLight _wakesWith;

        [Tooltip("Name of the node in the built base the pool hangs from (Art's base kit, e.g. DockAnchor).")]
        [SerializeField] private string _anchor;

        [Tooltip("Where the light hangs, in the anchor's frame (metres). Low over the ground, so the pool is a soft " +
            "disc rather than a wash.")]
        [SerializeField] private Vector3 _offset;

        [Tooltip("Light range (metres): roughly the pool's radius on the ground.")]
        [Range(1f, 20f)]
        [SerializeField] private float _range;

        [Tooltip("Light intensity when the warm point is fully lit. Enough to warm lit dust, never to bloom it.")]
        [Range(0f, 12f)]
        [SerializeField] private float _intensity;

        public LightPoolTuning(HomeLight wakesWith, string anchor, Vector3 offset, float range, float intensity)
        {
            _wakesWith = wakesWith;
            _anchor = anchor;
            _offset = offset;
            _range = range;
            _intensity = intensity;
        }

        public HomeLight WakesWith => _wakesWith;
        public string Anchor => _anchor;
        public Vector3 Offset => _offset;
        public float Range => _range;
        public float Intensity => _intensity;
    }
}
