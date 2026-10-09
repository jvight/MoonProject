using System.Collections.Generic;
using UnityEngine;
using MoonProject.Core;

namespace MoonProject.Gameplay.PlayModeTests
{
    /// <summary>
    /// The World's scene system as the editor preview sees it (<see cref="IWorldPreview"/>), standing in for it so the
    /// preview is tested without the World assembly: not generated until <see cref="Generate"/> hands it its anchors.
    /// It lives in this runtime test assembly because Unity attaches no component from an editor-only one, and the
    /// EditMode preview tests add it to a scene.
    /// </summary>
    public sealed class StandInWorldPreview : MonoBehaviour, IWorldPreview, IWorldAnchors
    {
        private readonly List<WorldAnchor> _anchors = new List<WorldAnchor>();

        public bool HasGeneratedWorld { get; private set; }

        public IWorldAnchors Anchors => this;

        public int Count => _anchors.Count;

        /// <summary>Generates the stand-in world with exactly <paramref name="anchors"/>.</summary>
        public void Generate(IEnumerable<WorldAnchor> anchors)
        {
            _anchors.Clear();
            _anchors.AddRange(anchors);
            HasGeneratedWorld = true;
        }

        public WorldAnchor Get(int index)
        {
            return _anchors[index];
        }

        public bool TryGet(string id, out WorldAnchor anchor)
        {
            foreach (WorldAnchor candidate in _anchors)
            {
                if (candidate.Id == id)
                {
                    anchor = candidate;
                    return true;
                }
            }

            anchor = default;
            return false;
        }
    }
}
