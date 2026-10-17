using System;
using UnityEngine;

namespace MoonProject.World
{
    /// <summary>
    /// One R1 prop stood near a world anchor at scene-build time (opening v2 §4: Kestrel-3's debris, the big boulders
    /// near the routes): which anchor, which prefab, its XZ offset from the anchor, its yaw and how deep it sits in the
    /// dust.
    /// </summary>
    [Serializable]
    public struct PropPlacement
    {
        [Tooltip("The world anchor the prop stands near (e.g. place.midway, site.kestrel).")]
        [SerializeField] private string _anchor;

        [Tooltip("R1 prefab name in its kit folder (e.g. Kestrel_HullPanel, Rock_12).")]
        [SerializeField] private string _id;

        [Tooltip("XZ offset from the anchor's position, metres (+X east, +Z north).")]
        [SerializeField] private Vector2 _offset;

        [Tooltip("Yaw of the prop, degrees clockwise from +Z.")]
        [SerializeField] private float _yaw;

        [Tooltip("How deep the prop sits below the dust line, metres: debris and boulders settle in.")]
        [SerializeField] private float _sink;

        public PropPlacement(string anchor, string id, Vector2 offset, float yaw, float sink)
        {
            _anchor = anchor;
            _id = id;
            _offset = offset;
            _yaw = yaw;
            _sink = sink;
        }

        public string Anchor => _anchor;
        public string Id => _id;
        public Vector2 Offset => _offset;
        public float Yaw => _yaw;
        public float Sink => _sink;
    }
}
