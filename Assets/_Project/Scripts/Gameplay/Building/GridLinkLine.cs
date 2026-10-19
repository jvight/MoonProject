using UnityEngine;
using UnityEngine.Rendering;
using MoonProject.Core;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// The build ghost's cable preview: a warm amber line along the very path the built cable will lie on
    /// (<see cref="BaseCablePath"/>), from the ghost's port to the grid hub's it would join, or, with nothing in reach,
    /// a short cable end dangling from the ghost with a dull loose plug ("no link"). Placement stays allowed either
    /// way; this only shows what the structure would be wired to. Allocation-free per frame.
    /// </summary>
    public sealed class GridLinkLine
    {
        // Points along the loose cable end: enough for a smooth droop over its metre or so.
        private const int LoosePoints = 12;

        private readonly BuildingTuning _tuning;
        private readonly LineRenderer _line;
        private readonly Vector3[] _points = new Vector3[BaseCablePath.MaxPoints];

        public GridLinkLine(Transform parent, Material material, BuildingTuning tuning)
        {
            _tuning = tuning;
            var host = new GameObject("GridLinkPreview");
            host.transform.SetParent(parent, false);
            _line = host.AddComponent<LineRenderer>();
            _line.sharedMaterial = material;
            _line.useWorldSpace = true;
            _line.numCapVertices = 2;
            _line.shadowCastingMode = ShadowCastingMode.Off;
            _line.receiveShadows = false;
            _line.lightProbeUsage = LightProbeUsage.Off;
            _line.reflectionProbeUsage = ReflectionProbeUsage.Off;
            _line.enabled = false;
        }

        /// <summary>
        /// The cable runs from the ghost's port at <paramref name="from"/> to the hub's at <paramref name="to"/>,
        /// along the ground as the built cable will.
        /// </summary>
        public void ShowLink(Vector3 from, Vector3 to, ITerrainQuery terrain)
        {
            int count = BaseCablePath.Write(from, to, terrain, _tuning, _points);
            Draw(count, _tuning.LinkColor, _tuning.LinkWidth, _tuning.LinkWidth);
        }

        /// <summary>No hub in reach: a short cable end hangs from the ghost at <paramref name="from"/>.</summary>
        public void ShowLoose(Vector3 from, float yaw)
        {
            Vector3 start = from + Vector3.up * _tuning.LinkHeight;
            Vector3 outward = Quaternion.Euler(0f, yaw, 0f) * Vector3.back * _tuning.LooseLength;
            for (int i = 0; i < LoosePoints; i++)
            {
                float t = i / (float)(LoosePoints - 1);
                _points[i] = start + outward * t + Vector3.down * (_tuning.LinkHeight * t * t);
            }

            Draw(LoosePoints, _tuning.LooseColor, _tuning.LinkWidth, _tuning.LoosePlugWidth);
        }

        public void Hide()
        {
            _line.enabled = false;
        }

        private void Draw(int count, Color color, float startWidth, float endWidth)
        {
            _line.positionCount = count;
            for (int i = 0; i < count; i++)
            {
                _line.SetPosition(i, _points[i]);
            }

            _line.startColor = color;
            _line.endColor = color;
            _line.startWidth = startWidth;
            _line.endWidth = endWidth;
            _line.enabled = true;
        }
    }
}
