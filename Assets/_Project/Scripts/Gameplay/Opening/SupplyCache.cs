using System;
using System.Collections.Generic;
using UnityEngine;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// One fixed source of the opening's supply (docs/design/opening-v3-purpose.md §4): where it stands, in a world
    /// anchor's frame, which of Art's containers it is, what it gives 07's hold and the one structure that supply is
    /// for (its link: every item is spent within it). A sealed source gives when its seal is cut, an open one (the
    /// lander's charger kit) when 07 comes within reach; it may also hold story parts (the spare igniter).
    /// </summary>
    [Serializable]
    public struct SupplyCache
    {
        [Tooltip("Save id of the crate (never rename: it keeps an opened crate open across loads).")]
        [SerializeField] private string _id;

        [Tooltip("The world anchor the crate stands by (e.g. place.lander).")]
        [SerializeField] private string _anchorId;

        [Tooltip("Metres from the anchor in its frame: x to its right, y up (unused: it rests on the ground), " +
                 "z ahead.")]
        [SerializeField] private Vector3 _offset;

        [Tooltip("Degrees the crate's front turns from the anchor's forward.")]
        [Range(-180f, 180f)] [SerializeField] private float _yaw;

        [Tooltip("Art's R1 crate prefab name in GameplayVisuals (Crate_M, Crate_L or Crate_Cargo_Open), " +
                 "stood at Art's size.")]
        [SerializeField] private string _container;

        [Tooltip("Metal, Wiring and Optics the opened crate gives 07's hold.")]
        [SerializeField] private Recipe _yield;

        [Tooltip("The structure this supply is for (StructureIds, e.g. charging_pad): the link its items are spent in.")]
        [SerializeField] private string _forStructure;

        [Tooltip("The crate stands open from the start: no cutting, 07 collects it within reach (the lander's, B4).")]
        [SerializeField] private bool _startsOpen;

        [Tooltip("Story parts the crate also holds (their ids in 07's hold, e.g. the spare igniter), taken together.")]
        [SerializeField] private string[] _parts;

        public SupplyCache(string id, string anchorId, Vector3 offset, float yaw, string container, Recipe yield,
            string forStructure, bool startsOpen = false, string[] parts = null)
        {
            _forStructure = forStructure;
            _startsOpen = startsOpen;
            _parts = parts;
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

        /// <summary>The structure this supply is for: the link its items are spent in.</summary>
        public string ForStructure => _forStructure ?? string.Empty;

        public bool StartsOpen => _startsOpen;

        public IReadOnlyList<string> Parts => _parts ?? Array.Empty<string>();

    }
}
