using System;
using UnityEngine;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// One towable load of the opening and where it first stands (docs/design/opening-v3-purpose.md R6: the habitat's
    /// power module, the battery bank's frame): Art's prefab, with a TowHitch node and a collider to slide on, set
    /// down on the ground in a world anchor's or a Lumen ruin piece's frame. Once towed, its own saved pose wins.
    /// </summary>
    [Serializable]
    public struct TowLoadSpot
    {
        [Tooltip("The load's tow id (never rename: it is how its saved pose and World's lamp find it).")]
        [SerializeField] private string _id;

        [Tooltip("The frame it stands in: a world anchor or a Lumen ruin piece ([World]/Ruins/<id>).")]
        [SerializeField] private string _anchorId;

        [Tooltip("Metres from the frame, level: x to its right, z ahead (y unused: it rests on the ground).")]
        [SerializeField] private Vector3 _offset;

        [Tooltip("Degrees the load's front (its hitch side) turns from the frame's forward.")]
        [Range(-180f, 180f)] [SerializeField] private float _yaw;

        [Tooltip("Art's prefab: a TowHitch node on its front face and a collider underneath to slide on.")]
        [SerializeField] private GameObject _prefab;

        [Tooltip("The load's mass (kg): what the cable feels; the speed cap carries it for 07.")]
        [Range(10f, 2000f)] [SerializeField] private float _mass;

        public string Id => _id ?? string.Empty;

        public string AnchorId => _anchorId ?? string.Empty;

        public Vector3 Offset => _offset;

        public float Yaw => _yaw;

        public GameObject Prefab => _prefab;

        public float Mass => _mass;
    }
}
