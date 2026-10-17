using System;
using UnityEngine;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// One sealed crate of the opening's supply (docs/design/opening-v2.md §4): where it stands, in a world anchor's
    /// frame, which of Art's containers it is, and what cutting its seal gives 07's hold.
    /// </summary>
    [Serializable]
    public struct SupplyCache
    {
        [Tooltip("Save id of the crate (never rename: it keeps an opened crate open across loads).")]
        [SerializeField] private string _id;

        [Tooltip("The world anchor the crate stands by (e.g. place.lander).")]
        [SerializeField] private string _anchorId;

        [Tooltip("Metres from the anchor in its frame: x to its right, y up (unused: it rests on the ground), z ahead.")]
        [SerializeField] private Vector3 _offset;

        [Tooltip("Degrees the crate's front turns from the anchor's forward.")]
        [Range(-180f, 180f)] [SerializeField] private float _yaw;

        [Tooltip("Art's container prefab name in GameplayVisuals (e.g. CoilCrate).")]
        [SerializeField] private string _container;

        [Tooltip("Metal, Wiring and Optics the opened crate gives 07's hold.")]
        [SerializeField] private Recipe _yield;

        public SupplyCache(string id, string anchorId, Vector3 offset, float yaw, string container, Recipe yield)
        {
            _id = id;
            _anchorId = anchorId;
            _offset = offset;
            _yaw = yaw;
            _container = container;
            _yield = yield;
        }

        public string Id => _id ?? string.Empty;

        public string AnchorId => _anchorId ?? string.Empty;

        public Vector3 Offset => _offset;

        public float Yaw => _yaw;

        public string Container => _container ?? string.Empty;

        public Recipe Yield => _yield;
    }
}
