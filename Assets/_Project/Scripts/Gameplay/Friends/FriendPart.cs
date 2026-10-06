using System;
using UnityEngine;

namespace MoonProject.Gameplay
{
    /// <summary>One of a friend's missing parts: a stable id (saves) and its Art pickup prefab.</summary>
    [Serializable]
    public sealed class FriendPart
    {
        [Tooltip("Stable id within the friend (e.g. rotor).")]
        [SerializeField] private string _id = string.Empty;

        [Tooltip("Art pickup prefab (Generated/Art/Friends/Part_*.prefab): meshes only, pivot at the centre of mass.")]
        [SerializeField] private GameObject _prefab;

        public FriendPart(string id, GameObject prefab)
        {
            _id = id;
            _prefab = prefab;
        }

        public string Id => _id;

        public GameObject Prefab => _prefab;
    }
}
