using UnityEngine;
using UnityEngine.Rendering;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// The build ghost's cable preview: a warm amber cable sagging from the ghost to the grid hub it would join, or,
    /// with nothing in reach, a short cable end dangling from the ghost with a dull loose plug ("no link"). Placement
    /// stays allowed either way; this only shows what the structure would be wired to. Allocation-free per frame.
    /// </summary>
    public sealed class GridLinkLine
    {
        // Points along the cable: enough for a smooth sag at the preview's few metres.
        private const int CablePoints = 12;

        private readonly BuildingTuning _tuning;
        private readonly LineRenderer _line;
        private readonly Vector3[] _points = new Vector3[CablePoints];

        public GridLinkLine(Transform parent, Material material, BuildingTuning tuning)
        {
            _tuning = tuning;
            var host = new GameObject("GridLinkPreview");
            host.transform.SetParent(parent, false);
            _line = host.AddComponent<LineRenderer>();
            _line.sharedMaterial = material;
            _line.useWorldSpace = true;
            _line.positionCount = CablePoints;
            _line.numCapVertices = 2;
            _line.shadowCastingMode = ShadowCastingMode.Off;
            _line.receiveShadows = false;
            _line.lightProbeUsage = LightProbeUsage.Off;
            _line.reflectionProbeUsage = ReflectionProbeUsage.Off;
            _line.enabled = false;
        }

        /// <summary>
        /// The cable runs from the ghost at <paramref name="from"/> to the hub at <paramref name="to"/>.
        /// </summary>
        public void ShowLink(Vector3 from, Vector3 to)
        {
            Vector3 lift = Vector3.up * _tuning.LinkHeight;
            Vector3 start = from + lift;
            Vector3 end = to + lift;
            for (int i = 0; i < CablePoints; i++)
            {
                float t = i / (float)(CablePoints - 1);
                // 4t(1 - t) is 1 at the cable's middle and 0 at its ends: the sag peaks halfway.
                _points[i] = Vector3.Lerp(start, end, t) + Vector3.down * (_tuning.LinkSag * 4f * t * (1f - t));
            }

            Draw(_tuning.LinkColor, _tuning.LinkWidth, _tuning.LinkWidth);
        }

        /// <summary>No hub in reach: a short cable end hangs from the ghost at <paramref name="from"/>.</summary>
        public void ShowLoose(Vector3 from, float yaw)
        {
            Vector3 start = from + Vector3.up * _tuning.LinkHeight;
            Vector3 outward = Quaternion.Euler(0f, yaw, 0f) * Vector3.back * _tuning.LooseLength;
            for (int i = 0; i < CablePoints; i++)
            {
                float t = i / (float)(CablePoints - 1);
                _points[i] = start + outward * t + Vector3.down * (_tuning.LinkHeight * t * t);
            }

            Draw(_tuning.LooseColor, _tuning.LinkWidth, _tuning.LoosePlugWidth);
        }

        public void Hide()
        {
            _line.enabled = false;
        }

        private void Draw(Color color, float startWidth, float endWidth)
        {
            _line.SetPositions(_points);
            _line.startColor = color;
            _line.endColor = color;
            _line.startWidth = startWidth;
            _line.endWidth = endWidth;
            _line.enabled = true;
        }
    }
}
