using System;
using UnityEngine;

namespace MoonProject.World
{
    /// <summary>Art's landmark kit (Generated/Art/Landmarks) as <see cref="LandmarkPlacer"/> places it.</summary>
    [Serializable]
    public sealed class LandmarkPrefabs
    {
        [Tooltip("RoutePost: a rover-height survey stake with a reflector (L2).")]
        [SerializeField] private GameObject _routePost;

        [Tooltip("ChasmBridge_Near: the near lip's two anchor posts (L4).")]
        [SerializeField] private GameObject _bridgeNear;

        [Tooltip("ChasmBridge_Far: the far anchor with its lamp (L4).")]
        [SerializeField] private GameObject _bridgeFar;

        [Tooltip("ChasmBridge_Cable: the snapped cable lying in the trough (L4).")]
        [SerializeField] private GameObject _bridgeCable;

        [Tooltip("DishFrame_0..n: the antenna farm's toppled dish frames (L7).")]
        [SerializeField] private GameObject[] _dishFrames = new GameObject[0];

        public GameObject RoutePost => _routePost;
        public GameObject BridgeNear => _bridgeNear;
        public GameObject BridgeFar => _bridgeFar;
        public GameObject BridgeCable => _bridgeCable;
        public GameObject[] DishFrames => _dishFrames;

        /// <summary>Null when every prefab is assigned, otherwise what is missing.</summary>
        public string Problem()
        {
            if (_routePost == null || _bridgeNear == null || _bridgeFar == null || _bridgeCable == null)
            {
                return "Landmark prefabs (route post, bridge near, far and cable) must be assigned.";
            }

            if (_dishFrames == null || _dishFrames.Length == 0)
            {
                return "Landmark dish frames must be assigned.";
            }

            foreach (GameObject frame in _dishFrames)
            {
                if (frame == null)
                {
                    return "A landmark dish frame is missing.";
                }
            }

            return null;
        }
    }
}
