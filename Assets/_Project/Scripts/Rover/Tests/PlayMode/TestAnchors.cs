using System;
using UnityEngine;
using MoonProject.Core;

namespace MoonProject.Rover.PlayModeTests
{
    /// <summary>
    /// <see cref="IWorldAnchors"/> stand-in with the one anchor 07 reads: the micro.06 site where its elder 06 sits,
    /// placed by the test (far away by default, so 07 never meets 06 unless a test brings it near).
    /// </summary>
    public sealed class TestAnchors : IWorldAnchors
    {
        private const float FarAway = 5000f;

        private WorldAnchor _elder = new WorldAnchor(WorldAnchorIds.Micro06, new Vector3(FarAway, 0f, FarAway),
            Vector3.forward, 4f);

        public int Count => 1;

        /// <summary>Puts the micro.06 site (06) at <paramref name="position"/>.</summary>
        public void PlaceElder(Vector3 position)
        {
            _elder = new WorldAnchor(WorldAnchorIds.Micro06, position, Vector3.forward, _elder.Radius);
        }

        public WorldAnchor Get(int index)
        {
            if (index != 0)
            {
                throw new ArgumentOutOfRangeException(nameof(index), index, "The stand-in holds one anchor.");
            }

            return _elder;
        }

        public bool TryGet(string id, out WorldAnchor anchor)
        {
            anchor = _elder;
            return id == WorldAnchorIds.Micro06;
        }
    }
}
