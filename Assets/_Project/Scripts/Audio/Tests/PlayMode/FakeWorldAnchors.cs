using System.Collections.Generic;
using UnityEngine;
using MoonProject.Core;

namespace MoonProject.Audio.PlayModeTests
{
    /// <summary>
    /// <see cref="IWorldAnchors"/> with a straight test Whispering Canyon running north from a mouth 200 m from the
    /// base: the chasm between the lip and the landing, two alcoves, the terminus and the exit off to the east.
    /// </summary>
    public sealed class FakeWorldAnchors : IWorldAnchors
    {
        /// <summary>Well inside the canyon, between the alcoves.</summary>
        public static readonly Vector3 DeepInside = new Vector3(0f, 5f, 315f);

        /// <summary>On the floor of the chasm's trough, 12 m below the lip.</summary>
        public static readonly Vector3 TroughFloor = new Vector3(0f, -10f, 221f);

        private readonly List<WorldAnchor> _anchors = new List<WorldAnchor>();

        public FakeWorldAnchors()
        {
            Add(WorldAnchorIds.CanyonMouth, new Vector3(0f, 0f, 200f));
            Add(WorldAnchorIds.CanyonLip, new Vector3(0f, 2f, 210f));
            Add(WorldAnchorIds.CanyonLanding, new Vector3(0f, 2f, 232f));
            Add(WorldAnchorIds.CanyonLedge, new Vector3(-30f, 20f, 260f));
            Add(WorldAnchorIds.CanyonAlcovePrefix + 0, new Vector3(0f, 4f, 290f));
            Add(WorldAnchorIds.CanyonAlcovePrefix + 1, new Vector3(0f, 6f, 340f));
            Add(WorldAnchorIds.CanyonTerminus, new Vector3(0f, 8f, 400f));
            Add(WorldAnchorIds.CanyonExit, new Vector3(60f, 0f, 232f));
        }

        public int Count => _anchors.Count;

        public WorldAnchor Get(int index)
        {
            return _anchors[index];
        }

        public bool TryGet(string id, out WorldAnchor anchor)
        {
            for (int i = 0; i < _anchors.Count; i++)
            {
                if (_anchors[i].Id == id)
                {
                    anchor = _anchors[i];
                    return true;
                }
            }

            anchor = default;
            return false;
        }

        private void Add(string id, Vector3 position)
        {
            _anchors.Add(new WorldAnchor(id, position, Vector3.forward, 6f));
        }
    }
}
