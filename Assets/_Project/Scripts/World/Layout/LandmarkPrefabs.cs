using System;
using UnityEngine;

namespace MoonProject.World
{
    /// <summary>
    /// Art's prefabs World plans for: Kestrel-3's hull fragment variants, which Gameplay stands on the scrap field's
    /// cuttable slots (<see cref="Core.IScrapField"/>); their count is the slots' variant range.
    /// </summary>
    [Serializable]
    public sealed class LandmarkPrefabs
    {
        [Tooltip("HullFragment_0..n: Kestrel-3's hull fragments, the cuttable slots' variants (0.5.5).")]
        [SerializeField] private GameObject[] _hullFragments = new GameObject[0];

        public GameObject[] HullFragments => _hullFragments;

        /// <summary>Null when every prefab is assigned, otherwise what is missing.</summary>
        public string Problem()
        {
            if (_hullFragments == null || _hullFragments.Length == 0)
            {
                return "Kestrel-3's hull fragments must be assigned.";
            }

            foreach (GameObject fragment in _hullFragments)
            {
                if (fragment == null)
                {
                    return "A Kestrel-3 hull fragment is missing.";
                }
            }

            return null;
        }
    }
}
