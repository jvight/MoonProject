using System;
using UnityEngine;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// One of Art's props set on a node of a salvage site or derelict with identity (relay.2's service hut on 06's
    /// HutSocket, 0.5.5 §1.7): solid to 07, nothing to cut.
    /// </summary>
    [Serializable]
    public struct SocketProp
    {
        [Tooltip("The node of the site or derelict it is set on (a direct child of its root).")]
        [SerializeField] private string _socket;

        [Tooltip("Art's prop prefab, its root set on the socket with identity.")]
        [SerializeField] private GameObject _prefab;

        public SocketProp(string socket, GameObject prefab)
        {
            _socket = socket;
            _prefab = prefab;
        }

        public string Socket => _socket;

        public GameObject Prefab => _prefab;
    }
}
